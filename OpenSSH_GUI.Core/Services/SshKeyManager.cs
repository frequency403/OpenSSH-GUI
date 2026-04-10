using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using DryIoc;
using JetBrains.Annotations;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Dialogs.Models;
using ReactiveUI;
using Renci.SshNet;
using Serilog;
using Serilog.Extensions.Logging;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;
using SshKey = SshNet.Keygen.SshKey;

namespace OpenSSH_GUI.Core.Services;

/// <summary>
///     Manager for SSH keys on the local machine.
///     Provides functionality for searching, generating, and changing formats of SSH keys.
/// </summary>
public sealed class SshKeyManager : ReactiveObject, IDisposable
{
    private const string BackupFileExtension = "bak";

    private static readonly FileStreamOptions FileStreamOptions = new()
    {
        BufferSize = 0,
        Access = FileAccess.ReadWrite,
        Mode = FileMode.OpenOrCreate,
        Share = FileShare.ReadWrite
    };

    private static readonly string BackupDirectory =
        Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), AppDomain.CurrentDomain.FriendlyName);

    private static string _backupLogFile =
        Path.Combine(BackupDirectory, Path.ChangeExtension(nameof(SshKeyManager), "log"));

    private readonly Lib.Misc.DirectoryCrawler _directoryCrawler;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly ILogger<SshKeyManager> _logger;
    private readonly IResolver _resolver;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private readonly ObservableCollection<SshKeyFile> _sshKeysInternal = [];
    private readonly FileSystemWatcher _watcher;
    private ILogger<SshKeyManager>? _backupLogger;

    private SerilogLoggerFactory? _loggerFactory;

    public SshKeyManager(
        ILogger<SshKeyManager> logger,
        Lib.Misc.DirectoryCrawler directoryCrawler,
        IMessageBoxProvider messageBoxProvider,
        IResolver resolver)
    {
        _logger = logger;
        _directoryCrawler = directoryCrawler;
        _messageBoxProvider = messageBoxProvider;
        _resolver = resolver;

        if (!OperatingSystem.IsWindows())
            FileStreamOptions.UnixCreateMode = (UnixFileMode)Convert.ToInt32("600", 8);

        _watcher = new FileSystemWatcher
        {
            Path = SshConfigFilesExtension.GetBaseSshPath(),
            EnableRaisingEvents = true
        };
        _watcher.Filters.Add("*.pub");
        _watcher.Filters.Add("*.ppk");
        _watcher.Created += async (_, eventArgs) => await WatcherOnCreated(eventArgs);
        _watcher.Deleted += WatcherOnDeleted;
        _watcher.Renamed += async (_, eventArgs) => await WatcherOnRenamed(eventArgs);
        SshKeys = new ReadOnlyObservableCollection<SshKeyFile>(_sshKeysInternal);
    }

    /// <summary>
    ///     Gets the collection of detected SSH keys.
    /// </summary>
    public ReadOnlyObservableCollection<SshKeyFile> SshKeys { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _watcher.Dispose();
        _semaphoreSlim.Dispose();
        foreach (var sshKeyFile in SshKeys) sshKeyFile.Dispose();
    }

    private void EnableTempLogger()
    {
        if (_backupLogger is not null) return;

        if (!Directory.Exists(BackupDirectory))
            Directory.CreateDirectory(BackupDirectory);

        _backupLogFile = Path.Combine(BackupDirectory,
            Path.ChangeExtension(
                "operation_log",
                "log"));
        _loggerFactory = new SerilogLoggerFactory(new LoggerConfiguration()
            .WriteTo.File(_backupLogFile)
            .MinimumLevel.Verbose()
            .CreateLogger(), true);
        _backupLogger = _loggerFactory.CreateLogger<SshKeyManager>();
    }

    private void DisableTempLogger(bool errorsOccured = false)
    {
        if (_backupLogger is null) return;
        _backupLogger = null;
        _loggerFactory?.Dispose();
        _loggerFactory = null;
        if (errorsOccured) return;
        try
        {
            Directory.Delete(BackupDirectory, true);
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error while cleaning up backup directory: {directory}", BackupDirectory);
        }
    }

    private IEnumerable<BackedUpFile> BackupFiles(params FileInfo[] files)
    {
        foreach (var file in files)
        {
            var destination = Path.Combine(BackupDirectory, string.Join(".", file.Name, BackupFileExtension));
            Log(LogLevel.Debug, "Backing up file {file} to {destination}", file.FullName, destination);
            var backup = new BackedUpFile { InitialFile = file, BackupFile = new FileInfo(destination) };
            backup.Backup();
            Log(LogLevel.Debug, "Successfully backed up file {file}", file.FullName);
            yield return backup;
        }
    }

    private void RestoreBackupFiles(params BackedUpFile[] files)
    {
        foreach (var file in files)
        {
            Log(LogLevel.Debug, "Restoring backup file {file} to {destination}", file.BackupFile.FullName,
                file.InitialFile.FullName);
            file.Restore();
            Log(LogLevel.Debug, "Successfully restored backup file {file}", file.BackupFile.FullName);
        }
    }

    private void DeleteBackupFiles(params BackedUpFile[] files)
    {
        foreach (var file in files)
        {
            Log(LogLevel.Debug, "Deleting backup file {file}", file.BackupFile.FullName);
            file.Delete();
            Log(LogLevel.Debug, "Successfully deleted backup file {file}", file.BackupFile.FullName);
        }
    }

    /// <summary>
    ///     Performs the initial SSH key search on disk.
    ///     Must be called after the DI container is fully built.
    /// </summary>
    public async ValueTask InitialSearchAsync(CancellationToken token = default)
    {
        await SearchForKeysAndUpdateCollectionAsync(token);
    }

    /// <summary>
    /// Changes the password of an SSH key file, handling both OpenSSH and PuTTY formats transparently.
    /// If the key is in PuTTY format, it will be temporarily converted to OpenSSH, the password changed,
    /// and then converted back to the original format.
    /// </summary>
    /// <param name="key">The SSH key file whose password should be changed.</param>
    /// <param name="newPassword">The new password to set, encoded using <paramref name="encoding"/>.</param>
    /// <param name="encoding">
    /// The encoding used to interpret <paramref name="newPassword"/>. Defaults to <see cref="Encoding.UTF8"/> if <c>null</c>.
    /// </param>
    /// <param name="token">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <exception cref="ArgumentNullException">Thrown if the private key file of <paramref name="key"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the resolved key file path is null or whitespace.</exception>
    /// <exception cref="TimeoutException">Thrown if the internal semaphore could not be acquired within 5 seconds.</exception>
    /// <exception cref="Exception">
    /// Thrown if <c>ssh-keygen</c> exits with a non-zero code, or if intermediate key file operations fail.
    /// On failure, all modified files are restored from backup.
    /// </exception>
    public async ValueTask<KeyManagerOperationResult> ChangePasswordOfKeyAsync(SshKeyFile key, ReadOnlyMemory<byte> newPassword,
        Encoding? encoding = null, CancellationToken token = default)
    {
        EnableTempLogger();
        encoding ??= Encoding.UTF8;
        var semaphoreAcquired = false;
        var errorsOccured = false;
        BackedUpFile[] backupFiles = [];
        string[] additionalDeleteFiles = [];
        var keyFilePath = string.Empty;
        try
        {
            keyFilePath = key.AbsoluteFilePath;
            var privateKeyFile = key.PrivateKeyFile;
            ArgumentNullException.ThrowIfNull(privateKeyFile);
            ArgumentException.ThrowIfNullOrWhiteSpace(keyFilePath);
            if (!await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token))
            {
                Log(LogLevel.Error, "Failed to acquire semaphore within 5 seconds");
                throw new TimeoutException("Failed to acquire semaphore within 5 seconds");
            }

            semaphoreAcquired = true;
            backupFiles = BackupFiles(key.KeyFiles).ToArray();

            if (key.Format is { } and not SshKeyFormat.OpenSSH)
            {
                Log(LogLevel.Debug, "Detected PuTTY key {key} - need to change format first", keyFilePath);
                if (await WriteToFileInSpecificFormat(SshKeyFormat.OpenSSH,
                        key.Password.ToSshKeyEncryption(), privateKeyFile, keyFilePath, true) is { } result)
                {
                    result.ThrowIfFailure();
                    if(result.IsSuccess)
                        additionalDeleteFiles = result.ResultValue.ToArray();
                }
                keyFilePath = additionalDeleteFiles.First(e => string.IsNullOrWhiteSpace(Path.GetExtension(e)));
                Log(LogLevel.Debug, "New file path: {newFilePath}", keyFilePath);
            }

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ssh-keygen",
                Arguments =
                    $"-p -f {keyFilePath} -P \"{key.Password.GetPasswordString()}\" -N \"{encoding.GetString(newPassword.Span)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (process.Start())
            {
                await process.WaitForExitAsync(token);
                if (process.ExitCode != 0)
                {
                    var message = await process.StandardError.ReadToEndAsync(token);
                    Log(LogLevel.Error, "ssh-keygen exited with code {exitCode} and message: {message}",
                        process.ExitCode,
                        message);
                    throw new Exception($"ssh-keygen exited with code {process.ExitCode}");
                }
                else
                {
                    var message = await process.StandardOutput.ReadToEndAsync(token);
                    Log(LogLevel.Debug, "ssh-keygen exited without errors and output: {message}", message);
                }
            }

            if (key.Format is { } format and not SshKeyFormat.OpenSSH)
            {
                if (GenerateKeyFile() is not { } keyFile)
                {
                    Log(LogLevel.Error, "Failed to generate key file for password change");
                    throw new Exception("Failed to generate key file for password change");
                }

                keyFile.Load(SshKeyFileSource.FromDisk(keyFilePath), newPassword.Span);
                Log(LogLevel.Debug,
                    "Changes to the password were made in OpenSSH Format - need to change format to Putty again");
                if (await WriteToFileInSpecificFormat(format, keyFile.Password.ToSshKeyEncryption(),
                        keyFile.PrivateKeyFile ?? throw new Exception("Private key file not found"), keyFilePath, true) is
                    { } result)
                {
                    result.ThrowIfFailure();
                    if(result.IsSuccess)
                        keyFilePath = result.ResultValue.First();
                }
                Log(LogLevel.Debug, "New file path: {newFilePath}", keyFilePath);
                foreach (var deleteFile in additionalDeleteFiles) File.Delete(deleteFile);
            }

            key.Load(SshKeyFileSource.FromDisk(keyFilePath), newPassword.Span);
            Log(LogLevel.Debug, "Successfully changed password of key {key}", keyFilePath);
            DeleteBackupFiles(backupFiles);
            return KeyManagerOperationResult.Success();
        }
        catch (Exception e)
        {
            errorsOccured = true;
            Log(LogLevel.Error, e, "Error changing password of key {key}", keyFilePath);
            RestoreBackupFiles(backupFiles);
            return KeyManagerOperationResult.FromException(e);
        }
        finally
        {
            if (semaphoreAcquired)
                _semaphoreSlim.Release();
            DisableTempLogger(errorsOccured);
        }
    }

    /// <summary>
    /// Attempts to delete all files associated with the given SSH key.
    /// Unlike <see cref="ChangePasswordOfKeyAsync"/>, this method does not throw on failure —
    /// instead, all encountered exceptions are aggregated and returned alongside a success flag.
    /// </summary>
    /// <param name="key">The SSH key file to delete, including all associated key files.</param>
    /// <param name="token">A cancellation token to observe while waiting for the semaphore.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description><c>success</c> — <c>true</c> if all files were deleted without error, <c>false</c> otherwise.</description></item>
    ///   <item><description><c>exception</c> — the exception that occurred, or an <see cref="AggregateException"/> if multiple errors were encountered. <c>null</c> on full success.</description></item>
    /// </list>
    /// </returns>
    public async ValueTask<KeyManagerOperationResult> TryDeleteKeyAsync(SshKeyFile key,
        CancellationToken token = default)
    {
        EnableTempLogger();
        var errorsOccured = false;
        Exception? exception = null;
        var semaphoreAcquired = false;
        try
        {
            semaphoreAcquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token);
            foreach (var keyFile in key.KeyFiles)
                try
                {
                    keyFile.Delete();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Debug, "Error while deleting key {key}: {ex}", key.AbsoluteFilePath, ex);
                    exception = exception is null ? ex : new AggregateException(exception, ex);
                }

            if (exception is not null)
                throw exception;
            Log(LogLevel.Debug, "Successfully deleted key {key}", key.AbsoluteFilePath);
            return KeyManagerOperationResult.Success();
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error deleting key");
            errorsOccured = true;
            exception = exception is null ? e : new AggregateException(exception, e);
            return KeyManagerOperationResult.FromException(exception);
        }
        finally
        {
            if (semaphoreAcquired)
                _semaphoreSlim.Release();
            DisableTempLogger(errorsOccured);
        }
    }

    /// <summary>
    /// Renames all files associated with the given <see cref="SshKeyFile"/> to a new base file name,
    /// preserving each file's original extension. If any target file already exists, the user is prompted
    /// to confirm the overwrite. On failure, all files are restored from backup.
    /// </summary>
    /// <param name="key">
    /// The <see cref="SshKeyFile"/> whose associated files are to be renamed.
    /// After a successful rename, the key is reloaded from the new primary file.
    /// </param>
    /// <param name="newFileName">
    /// The new base file name (without extension) to assign to all files of the key.
    /// Each file retains its original extension.
    /// </param>
    /// <param name="overwrite">A flag to indicate forceful overwrite of any existent files</param>
    /// <param name="token">
    /// A <see cref="CancellationToken"/> to observe while waiting for the semaphore
    /// and during file move operations.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when another key operation is already in progress and the semaphore
    /// could not be acquired within the timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via <paramref name="token"/>
    /// or when the user declines the overwrite confirmation dialog.
    /// </exception>
    /// <remarks>
    /// File moves are performed via <see cref="FileInfo.MoveTo(string, bool)"/> wrapped in <see cref="Task.Run(Action, CancellationToken)"/>,
    /// since no native async move API exists in .NET. On same-volume moves, this is an atomic
    /// metadata operation. Backups are created before any file is moved and deleted only on full success;
    /// on any failure the backup is restored.
    /// </remarks>
    public async ValueTask<KeyManagerOperationResult> RenameKeyAsync(SshKeyFile key, string newFileName, bool overwrite = false, CancellationToken token = default)
    {
        var semaphoreAcquired = false;
        EnableTempLogger();
        var errorsOccurred = false;
        BackedUpFile[] backupFiles = [];
        try
        {
            semaphoreAcquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token);
            if (!semaphoreAcquired)
                throw new InvalidOperationException("Another key operation is in progress");

            backupFiles = BackupFiles(key.KeyFiles).ToArray();
            var filePairs = (key.KeyFileInfo?.Files ?? []).Select(file =>
            {
                ArgumentNullException.ThrowIfNull(file.Directory);
                var newFileNameForFile = Path.ChangeExtension(newFileName, file.Extension);
                var destinationForFile = Path.Combine(file.Directory.FullName, newFileNameForFile);
                Log(LogLevel.Debug, "Renaming file {file} to {newFileName}", file.FullName, newFileNameForFile);
                Log(LogLevel.Debug, "Destination: {destination}", destinationForFile);
                return (Source: file, Target: destinationForFile);
            }).ToArray();

            if (!overwrite && filePairs.Any(p => File.Exists(p.Target)))
            {
                Log(LogLevel.Debug, "Destination files already exist");
                return KeyManagerOperationResult.Conflict(new Exception("Destination files already exist"));
            }
            
            foreach (var (source, target) in filePairs)
            {
                await Task.Run(() => source.MoveTo(target, overwrite: true), token);
                Log(LogLevel.Debug, "Successfully renamed file {file} to {newFileName}", source.FullName, source.Name);
            }

            if (filePairs.Select(p => p.Source).FirstOrDefault(file =>
                    string.Equals(file.Extension, key.Format?.GetExtension(false),
                        StringComparison.OrdinalIgnoreCase) &&
                    file.Exists) is { } keyFileToLoad)
            {
                Log(LogLevel.Debug, "Loading key file {keyFile}", keyFileToLoad.FullName);
                key.Load(SshKeyFileSource.FromDisk(keyFileToLoad.FullName));
                Log(LogLevel.Debug, "Successfully loaded key file {keyFile}", keyFileToLoad.FullName);
                DeleteBackupFiles(backupFiles);
                return KeyManagerOperationResult.Success();
            }
            else
            {
                Log(LogLevel.Warning, "No valid key file found for key format {format}", key.Format);
                throw new Exception("No valid key file found for key format");
            }
        }
        catch (Exception e)
        {
            errorsOccurred = true;
            Log(LogLevel.Error, e, "Failed to change filename of {className}", nameof(SshKeyFile));
            RestoreBackupFiles(backupFiles);
            return KeyManagerOperationResult.FromException(e);
        }
        finally
        {
            if (semaphoreAcquired)
                _semaphoreSlim.Release();
            DisableTempLogger(errorsOccurred);
        }
    }

    private async ValueTask<KeyManagerOperationResult> WriteToFile(string filePath, string content, bool overwrite = false, Encoding? encoding = null)
    {
        if (encoding is null)
        {
            encoding ??= Encoding.UTF8;
            Log(LogLevel.Debug, "Using default encoding: {encoding}", encoding.EncodingName);
        }
        else
        {
            Log(LogLevel.Debug, "Using encoding: {encoding}", encoding.EncodingName);
        }
        
        var fileInfo = new FileInfo(filePath);
        if(fileInfo.Exists && !overwrite)
        {
            Log(LogLevel.Warning, "File {filePath} already exists. Skipping write operation.", filePath);
            return KeyManagerOperationResult.Conflict(new Exception("File already exists"));
        }
        
        await using var fileStream = fileInfo.Open(FileStreamOptions);
        Log(LogLevel.Debug, "Opened file {filePath}", filePath);

        byte[]? rented = null;
        var buffer = content.Length <= 256
            ? stackalloc byte[256]
            : rented = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(content));
        Log(LogLevel.Debug, "Allocated {byteCount} bytes", buffer.Length);
        try
        {
            var writtenBytes = encoding.GetBytes(content, buffer);
            Log(LogLevel.Debug, "Writing {byteCount} bytes into file {filePath}", writtenBytes, filePath);
            fileStream.Write(buffer[..writtenBytes]);
            Log(LogLevel.Debug, "Successfully wrote file {filePath}", filePath);
            return KeyManagerOperationResult.Success();
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error while writing file {filePath}", filePath);
            return KeyManagerOperationResult.FromException(e);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented, true);
                Log(LogLevel.Debug, "Freeing memory");
            }
        }
    }

    /// <summary>
    ///     Changes the format of an existing SSH key.
    /// </summary>
    /// <param name="key">The SSH key file to change.</param>
    /// <param name="newFormat">The target SSH key format.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask<KeyManagerOperationResult> ChangeFormatOfKeyAsync(
        SshKeyFile key,
        SshKeyFormat newFormat,
        CancellationToken token = default)
    {
        if (!key.IsInitialized)
            KeyManagerOperationResult.Failure(new InvalidOperationException("Key file not initialized"));

        PrivateKeyFile? privateKeyFile = key;
        try
        {
            ArgumentNullException.ThrowIfNull(privateKeyFile);
            ArgumentException.ThrowIfNullOrWhiteSpace(key.AbsoluteFilePath);
        }
        catch (Exception e)
        {
            return KeyManagerOperationResult.Failure(e);
        }
        

        EnableTempLogger();
        var filePath = newFormat.ChangeExtension(Path.GetFullPath(key.AbsoluteFilePath), false);
        var writtenFiles = new List<string>();
        BackedUpFile[] backupFiles = [];
        var semaphoreAquired = false;
        var errorsOccured = false;
        try
        {
            backupFiles = BackupFiles(key.KeyFiles).ToArray();

            semaphoreAquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(2), token);
            if (!semaphoreAquired)
                throw new InvalidOperationException("Another key operation is in progress");

            if (await WriteToFileInSpecificFormat(newFormat,
                    key.Password.ToSshKeyEncryption(),
                    privateKeyFile,
                    filePath, true) is { } result)
            {
                result.ThrowIfFailure();
                if(result.IsSuccess)
                    writtenFiles.AddRange(result.ResultValue);
            }
            
            key.Load(SshKeyFileSource.FromDisk(filePath));
            Log(LogLevel.Debug, "Successfully changed format of key {key} to {format}", key.AbsoluteFilePath, newFormat);
            DeleteBackupFiles(backupFiles);
            return KeyManagerOperationResult.Success();
        }
        catch (Exception e)
        {
            var exc = e;
            errorsOccured = true;
            Log(LogLevel.Error, e, "Error changing format of key – attempting rollback");
            foreach (var writtenFile in writtenFiles)
                try
                {
                    File.Delete(writtenFile);
                }
                catch (Exception ex)
                {
                    exc = exc switch
                    {
                        AggregateException aggregateException => new AggregateException(
                            aggregateException.InnerExceptions.Append(ex)),
                        not null => new AggregateException(exc, ex),
                        _ => ex
                    };
                    Log(LogLevel.Warning, ex, "Could not delete created file '{path}'", writtenFile);
                }

            try
            {
                RestoreBackupFiles(backupFiles);
            }
            catch (Exception exception)
            {
                exc = exc switch
                {
                    AggregateException aggregateException => new AggregateException(
                        aggregateException.InnerExceptions.Append(exception)),
                    not null => new AggregateException(exc, exception),
                    _ => exception
                };
                Log(LogLevel.Warning, exception, "Could not restore backup files");
            }
            
            return KeyManagerOperationResult.FromException(exc);
        }
        finally
        {
            if (semaphoreAquired)
                _semaphoreSlim.Release();

            DisableTempLogger(errorsOccured);
        }
    }

    /// <summary>
    ///     Changes the order of the SSH keys in the collection.
    /// </summary>
    /// <param name="orderFunc">Function to reorder the keys.</param>
    public void ChangeOrder(Func<IEnumerable<SshKeyFile>, IEnumerable<SshKeyFile>> orderFunc)
    {
        var reordered = orderFunc(SshKeys).ToList();
        for (var i = 0; i < reordered.Count; i++)
        {
            var oldIndex = _sshKeysInternal.IndexOf(reordered[i]);
            if (oldIndex != i)
                _sshKeysInternal.Move(oldIndex, i);
        }
    }

    private async ValueTask<KeyManagerOperationResult<IEnumerable<string>>> WriteToFileInSpecificFormat(SshKeyFormat format,
        ISshKeyEncryption encryption,
        IPrivateKeySource privateKeySource, string filePath, bool overwrite = false)
    {
        var privateKeyFileContent = format is SshKeyFormat.OpenSSH
            ? privateKeySource.ToOpenSshFormat(encryption)
            : privateKeySource.ToPuttyFormat(encryption, format);
        var writtenFiles = new List<string>();
        switch (format)
        {
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
                break;
            case SshKeyFormat.OpenSSH:
            default:
            {
                var pubKeyFormat = format.ChangeExtension(filePath);
                if(await WriteToFile(pubKeyFormat, privateKeySource.ToOpenSshPublicFormat(), overwrite) is {IsSuccess: false} error)
                {
                    Log(LogLevel.Error, "Failed to write public key file {filePath}", pubKeyFormat);
                    return error.WithValue<IEnumerable<string>>([]);
                }
                writtenFiles.Add(pubKeyFormat);
                break;
            }
        }

        var privateFilePath = format.ChangeExtension(filePath, false);
        if(await WriteToFile(privateFilePath, privateKeyFileContent, overwrite) is {IsSuccess: false} error1)
        {
            Log(LogLevel.Error, "Failed to write private key file {filePath}", privateFilePath);
            return error1.WithValue<IEnumerable<string>>([]);
        }
        writtenFiles.Add(privateFilePath);
        return KeyManagerOperationResult<IEnumerable<string>>.Success(writtenFiles);
    }

    private ValueTask<KeyManagerOperationResult<IEnumerable<string>>> WriteToFileInSpecificFormat(SshKeyGenerateInfo generateInfo,
        GeneratedPrivateKey createdKey, string filePath, bool overwrite = false) =>
        WriteToFileInSpecificFormat(generateInfo.KeyFormat, generateInfo.Encryption, createdKey, filePath, overwrite);

    /// <summary>
    ///     Generates a new SSH key.
    /// </summary>
    /// <param name="fullFilePath">The full path where the new key should be stored.</param>
    /// <param name="generateParamsInfo">Parameters for key generation.</param>
    /// <param name="overwrite">Whether to overwrite existing file if it exists.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public async ValueTask<KeyManagerOperationResult> GenerateNewKey(string fullFilePath, SshKeyGenerateInfo generateParamsInfo, bool overwrite = false)
    {
        if(File.Exists(fullFilePath) && !overwrite)
            return KeyManagerOperationResult.Failure(new InvalidOperationException("File already exists"));
        if (GenerateKeyFile() is not { } keyFile)
            return KeyManagerOperationResult.FromException(new InvalidOperationException("Key file not generated"));
        if (!await _semaphoreSlim.WaitAsync(100))
            return KeyManagerOperationResult.FromException(new InvalidOperationException("Another key operation is in progress"));
        try
        {
            GeneratedPrivateKey? createdKey;
            try
            {
                await using var privateStream = new MemoryStream();
                createdKey = SshKey.Generate(privateStream, generateParamsInfo);
                if (createdKey is null)
                    throw new InvalidOperationException("Could not generate new key");
            }
            catch (Exception e)
            {
                Log(LogLevel.Error, e, "Error while generating key file {filePath}", fullFilePath);
                throw;
            }

            var filePath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath, false);

            if(await WriteToFileInSpecificFormat(generateParamsInfo, createdKey, filePath, overwrite) is {IsSuccess: false} error)
                return error;

            var keyFileSource = SshKeyFileSource.FromDisk(filePath);
            if (string.IsNullOrWhiteSpace(generateParamsInfo.Encryption.Passphrase))
                keyFile.Load(keyFileSource);
            else
                keyFile.Load(keyFileSource, Encoding.UTF8.GetBytes(generateParamsInfo.Encryption.Passphrase));

            _sshKeysInternal.Add(keyFile);
            return KeyManagerOperationResult.Success();
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error generating key");
            return KeyManagerOperationResult.FromException(e);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    /// <summary>
    ///     Triggers a re-search for SSH keys on the disk.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask<KeyManagerOperationResult> RerunSearchAsync(CancellationToken token = default)
    {
        if (!await _semaphoreSlim.WaitAsync(100, token))
            return KeyManagerOperationResult.FromException(new InvalidOperationException("Another key operation is in progress"));
        try
        {
            _sshKeysInternal.Clear();
            await SearchForKeysAndUpdateCollectionAsync(token);
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Unhandled error during key re-search");
            return KeyManagerOperationResult.FromException(e);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
        return KeyManagerOperationResult.Success();
    }

    private async Task WatcherOnRenamed(RenamedEventArgs e)
    {
        if (!await _semaphoreSlim.WaitAsync(100))
            return;
        EnableTempLogger();
        var errorsOccured = false;
        try
        {
            if (SshKeys.FirstOrDefault(key =>
                    key.KeyFileInfo is { KeyFileSource: { AbsolutePath: { } absolutePath } } &&
                    string.Equals(absolutePath, Path.ChangeExtension(e.OldFullPath, null))) is { } keyFile)
            {
                Log(LogLevel.Debug, "Reloading keyfile because it was renamed externally");
                keyFile.Load(keyFile.KeyFileInfo?.KeyFileSource.ProvidedByConfig ?? false
                    ? SshKeyFileSource.FromConfig(Path.ChangeExtension(e.FullPath, null))
                    : SshKeyFileSource.FromDisk(Path.ChangeExtension(e.OldFullPath, null)));
            }
        }
        catch (Exception exception)
        {
            errorsOccured = true;
            Log(LogLevel.Error, exception, "Error handling renamed key");
        }
        finally
        {
            _semaphoreSlim.Release();
            DisableTempLogger(errorsOccured);
        }
    }

    private void WatcherOnDeleted(object? sender, FileSystemEventArgs eventArgs)
    {
        if (!_semaphoreSlim.Wait(100))
            return;
        EnableTempLogger();
        var errorsOccured = false;
        try
        {
            var normalizedPath = Path.ChangeExtension(
                Path.GetFullPath(eventArgs.FullPath), null);

            var key = SshKeys.SingleOrDefault(k =>
                string.Equals(k.AbsoluteFilePath, normalizedPath,
                    StringComparison.OrdinalIgnoreCase));

            if (key is null)
                return;

            Log(LogLevel.Debug, "Key {key} got deleted externally", key.AbsoluteFilePath);
            _sshKeysInternal.Remove(key);
        }
        catch (Exception e)
        {
            errorsOccured = true;
            Log(LogLevel.Error, e, "Error handling deleted key");
        }
        finally
        {
            _semaphoreSlim.Release();
            DisableTempLogger(errorsOccured);
        }
    }

    private async Task WatcherOnCreated(FileSystemEventArgs e)
    {
        if (!await _semaphoreSlim.WaitAsync(100))
            return;
        EnableTempLogger();
        var errorsOccured = false;
        try
        {
            var keyFilePath = string.Equals(
                Path.GetExtension(e.FullPath),
                SshKeyFormatExtension.PuttyKeyFileExtension,
                StringComparison.OrdinalIgnoreCase)
                ? e.FullPath
                : Path.ChangeExtension(e.FullPath, null);

            if (SshKeys.Any(key =>
                    string.Equals(key.AbsoluteFilePath, keyFilePath,
                        StringComparison.OrdinalIgnoreCase)))
                return;

            AddKey(SshKeyFileSource.FromDisk(keyFilePath));
        }
        catch (Exception exception)
        {
            errorsOccured = true;
            Log(LogLevel.Error, exception, "Error adding key");
        }
        finally
        {
            _semaphoreSlim.Release();
            DisableTempLogger(errorsOccured);
        }
    }

    private SshKeyFile? GenerateKeyFile()
    {
        try
        {
            if (_resolver.Resolve<SshKeyFile>() is { } keyFile) 
                return keyFile;
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error resolving generic SshKeyFile");
        }

        return null;
    }

    private void AddKey(SshKeyFileSource keyFileSource)
    {
        if (_sshKeysInternal.Any(k =>
                string.Equals(k.AbsoluteFilePath, keyFileSource.AbsolutePath,
                    StringComparison.OrdinalIgnoreCase)))
            return;
        try
        {
            if (GenerateKeyFile() is not { } keyFileGenerated)
                throw new InvalidOperationException("Key file not generated");

            keyFileGenerated.Load(keyFileSource);
            _sshKeysInternal.Add(keyFileGenerated);
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error loading keyfile {filePath}", keyFileSource.AbsolutePath);
        }
    }

    private async ValueTask<KeyManagerOperationResult> SearchForKeysAndUpdateCollectionAsync(CancellationToken token = default)
    {
        if (_directoryCrawler.IsSearching) return KeyManagerOperationResult.Conflict(new InvalidOperationException("Key search already in progress"));
        var semaphoreAquired = false;
        var errorsOccured = false;
        EnableTempLogger();
        try
        {
            semaphoreAquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token);
            await foreach(var sshKey in _directoryCrawler.GetPossibleKeyFilesOnDiskAsyncEnumerable(token))
                AddKey(sshKey);
            return KeyManagerOperationResult.Success();
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error searching for keys");
            return KeyManagerOperationResult.Failure(e);
        }
        finally
        {
            if (semaphoreAquired)
                _semaphoreSlim.Release();
            DisableTempLogger(errorsOccured);
        }
    }
#pragma warning disable CA2254
    private void Log(LogLevel level, Exception? exception, [StructuredMessageTemplate] string? message,
        params object?[] args)
    {
        _logger.Log(level, exception, message, args);
        _backupLogger?.Log(level, exception, message, args);
    }

    private void Log(LogLevel level, [StructuredMessageTemplate] string? message, params object?[] args)
    {
        _logger.Log(level, message, args);
        _backupLogger?.Log(level, message, args);
    }
#pragma warning restore CA2254
}