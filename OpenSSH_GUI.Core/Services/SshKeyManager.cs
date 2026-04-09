using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using DryIoc;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
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

    private readonly DirectoryCrawler _directoryCrawler;
    private readonly ILogger<SshKeyManager> _logger;
    private readonly IResolver _resolver;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private readonly ObservableCollection<SshKeyFile> _sshKeysInternal = [];
    private readonly FileSystemWatcher _watcher;
    private ILogger<SshKeyManager>? _backupLogger;

    private SerilogLoggerFactory? _loggerFactory;

    public SshKeyManager(
        ILogger<SshKeyManager> logger,
        DirectoryCrawler directoryCrawler,
        IResolver resolver)
    {
        _logger = logger;
        _directoryCrawler = directoryCrawler;
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

    // REFACTOR: Use BackedUpFile throughout the file
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

    private static IEnumerable<FileInfo> MoveToBackupDirectory(params FileInfo[] files)
    {
        foreach (var file in files)
        {
            var destination = Path.Combine(BackupDirectory, string.Join(".", file.Name, BackupFileExtension));
            file.MoveTo(destination);
            yield return new FileInfo(destination);
        }
    }

    /// <summary>
    ///     Performs the initial SSH key search on disk.
    ///     Must be called after the DI container is fully built.
    /// </summary>
    public void InitialSearchAsync()
    {
        SearchForKeysAndUpdateCollection();
    }

    public async Task ChangePasswordOfKeyAsync(SshKeyFile key, ReadOnlyMemory<byte> newPassword,
        Encoding? encoding = null, CancellationToken token = default)
    {
        EnableTempLogger();
        encoding ??= Encoding.UTF8;
        var semaphoreAquired = false;
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

            semaphoreAquired = true;
            backupFiles = BackupFiles(key.KeyFiles).ToArray();

            if (key.Format is { } and not SshKeyFormat.OpenSSH)
            {
                Log(LogLevel.Debug, "Detected PuTTY key {key} - need to change format first", keyFilePath);
                additionalDeleteFiles = (await WriteToFileInSpecificFormat(SshKeyFormat.OpenSSH,
                    key.Password.ToSshKeyEncryption(), privateKeyFile, keyFilePath)).ToArray();
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
                keyFilePath = (await WriteToFileInSpecificFormat(format, keyFile.Password.ToSshKeyEncryption(),
                    keyFile.PrivateKeyFile ?? throw new Exception("Private key file not found"), keyFilePath)).First();
                Log(LogLevel.Debug, "New file path: {newFilePath}", keyFilePath);
                foreach (var deleteFile in additionalDeleteFiles) File.Delete(deleteFile);
            }

            key.Load(SshKeyFileSource.FromDisk(keyFilePath), newPassword.Span);
            Log(LogLevel.Debug, "Successfully changed password of key {key}", keyFilePath);
            DeleteBackupFiles(backupFiles);
        }
        catch (Exception e)
        {
            errorsOccured = true;
            Log(LogLevel.Error, e, "Error changing password of key {key}", keyFilePath);
            RestoreBackupFiles(backupFiles);
        }
        finally
        {
            if (semaphoreAquired)
                _semaphoreSlim.Release();
            DisableTempLogger(errorsOccured);
        }
    }


    public async Task<(bool success, Exception? exception)> TryDeleteKeyAsync(SshKeyFile key,
        CancellationToken token = default)
    {
        EnableTempLogger();
        var errrorsOccured = false;
        Exception? exception = null;
        var semaphoreAquired = false;
        try
        {
            semaphoreAquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token);
            foreach (var keyFile in key.KeyFiles)
                try
                {
                    keyFile.Delete();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Debug, "Error while deleting key {key}: {ex}", key.AbsoluteFilePath, ex);
                    exception = exception is null ? ex : new AggregateException(exception, ex);
                    errrorsOccured = true;
                }
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error deleting key");
            errrorsOccured = true;
            exception = exception is null ? e : new AggregateException(exception, e);
        }
        finally
        {
            if (semaphoreAquired)
                _semaphoreSlim.Release();
            DisableTempLogger(errrorsOccured);
        }

        return (errrorsOccured, exception);
    }

    public async Task RenameKeyAsync(SshKeyFile key, string newFileName, CancellationToken token = default)
    {
        var semaphoreAquired = false;
        EnableTempLogger();
        var errorsOccured = false;
        try
        {
            semaphoreAquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token);
            if (!semaphoreAquired)
                return;

            var files = new List<string>();

            foreach (var file in key.KeyFileInfo?.Files ?? [])
            {
                var newFileNameWithMatchingExtension = Path.ChangeExtension(newFileName,
                    string.IsNullOrEmpty(file.Extension) ? null : file.Extension);
                var destination = Path.Combine(
                    file.DirectoryName ?? SshConfigFilesExtension.GetBaseSshPath(),
                    newFileNameWithMatchingExtension);
                if (File.Exists(destination))
                    throw new InvalidOperationException($"File {destination} already exists");
                file.MoveTo(destination);
                files.Add(file.Extension.EndsWith("ppk") ? destination : Path.ChangeExtension(destination, null));
            }

            key.Load(SshKeyFileSource.FromDisk(files.Distinct().Single()));
        }
        catch (Exception e)
        {
            errorsOccured = true;
            Log(LogLevel.Error, e, "Failed to change filename of {className}", nameof(SshKeyFile));
            throw;
        }
        finally
        {
            if (semaphoreAquired)
                _semaphoreSlim.Release();
            DisableTempLogger(errorsOccured);
        }
    }

    private async ValueTask WriteToFile(string filePath, string content, Encoding? encoding = null)
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

        await using var fileStream = new FileStream(filePath, FileStreamOptions);
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
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error while writing file {filePath}", filePath);
            throw;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented, true);
                Log(LogLevel.Debug, "Freeing memory");
            }
        }

        Log(LogLevel.Debug, "Successfully wrote file {filePath}", filePath);
    }

    /// <summary>
    ///     Changes the format of an existing SSH key.
    /// </summary>
    /// <param name="key">The SSH key file to change.</param>
    /// <param name="newFormat">The target SSH key format.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ChangeFormatOfKeyAsync(
        SshKeyFile key,
        SshKeyFormat newFormat,
        CancellationToken token = default)
    {
        if (!key.IsInitialized)
            throw new InvalidOperationException("Key file not initialized");

        PrivateKeyFile? privateKeyFile = key;
        ArgumentNullException.ThrowIfNull(privateKeyFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(key.AbsoluteFilePath);

        EnableTempLogger();
        var filePath = newFormat.ChangeExtension(Path.GetFullPath(key.AbsoluteFilePath), false);
        var writtenFiles = new List<string>();
        IEnumerable<FileInfo> backupFiles = [];
        var semaphoreAquired = false;
        var errorsOccured = false;
        try
        {
            backupFiles = MoveToBackupDirectory(key.KeyFiles).ToArray();

            semaphoreAquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(2), token);
            if (!semaphoreAquired)
                throw new InvalidOperationException("Another key operation is in progress");

            writtenFiles.AddRange(await WriteToFileInSpecificFormat(newFormat,
                key.Password.ToSshKeyEncryption(),
                privateKeyFile,
                filePath));

            key.Load(SshKeyFileSource.FromDisk(filePath));
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
                    Log(LogLevel.Warning, ex, "Could not delete backup file '{path}'", writtenFile);
                }

            foreach (var backupFileInfo in backupFiles)
            {
                if (backupFileInfo.Directory?.Parent?.FullName is { } parentDirectory)
                    try
                    {
                        var destination = Path.Combine(parentDirectory, backupFileInfo.Name.Replace(".bak", ""));
                        backupFileInfo.MoveTo(destination);
                        Log(LogLevel.Warning, "Restored backup file {backupFile} to its original location {original}",
                            backupFileInfo.FullName, destination);
                        continue;
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
                    }

                Log(LogLevel.Warning, "Could not move backup file {backupFile} to its original location",
                    backupFileInfo.FullName);
                break;
            }

            throw exc;
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

    private async ValueTask<IEnumerable<string>> WriteToFileInSpecificFormat(SshKeyFormat format,
        ISshKeyEncryption encryption,
        IPrivateKeySource privateKeySource, string filePath)
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
                var pubKeyFormat = format.ChangeExtension(filePath);
                await WriteToFile(pubKeyFormat,
                    privateKeySource.ToOpenSshPublicFormat());
                writtenFiles.Add(pubKeyFormat);
                break;
        }

        var privateFilePath = format.ChangeExtension(filePath, false);
        await WriteToFile(privateFilePath, privateKeyFileContent);
        writtenFiles.Add(privateFilePath);
        return writtenFiles;
    }

    private ValueTask<IEnumerable<string>> WriteToFileInSpecificFormat(SshKeyGenerateInfo generateInfo,
        GeneratedPrivateKey createdKey, string filePath)
    {
        return WriteToFileInSpecificFormat(generateInfo.KeyFormat, generateInfo.Encryption, createdKey, filePath);
    }

    /// <summary>
    ///     Generates a new SSH key.
    /// </summary>
    /// <param name="fullFilePath">The full path where the new key should be stored.</param>
    /// <param name="generateParamsInfo">Parameters for key generation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public async ValueTask GenerateNewKey(string fullFilePath, SshKeyGenerateInfo generateParamsInfo)
    {
        if (File.Exists(fullFilePath))
            throw new InvalidOperationException("File already exists");
        if (GenerateKeyFile() is not { } keyFile)
            throw new InvalidOperationException("Key file not generated");
        if (!await _semaphoreSlim.WaitAsync(100))
            throw new InvalidOperationException("Another key operation is in progress");
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

            await WriteToFileInSpecificFormat(generateParamsInfo, createdKey, filePath);

            var keyFileSource = SshKeyFileSource.FromDisk(filePath);
            if (string.IsNullOrWhiteSpace(generateParamsInfo.Encryption.Passphrase))
                keyFile.Load(keyFileSource);
            else
                keyFile.Load(keyFileSource, Encoding.UTF8.GetBytes(generateParamsInfo.Encryption.Passphrase));

            _sshKeysInternal.Add(keyFile);
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error generating key");
            throw;
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
    public async Task RerunSearchAsync()
    {
        if (!await _semaphoreSlim.WaitAsync(100))
            throw new InvalidOperationException("Another key operation is in progress");
        try
        {
            _sshKeysInternal.Clear();
            SearchForKeysAndUpdateCollection();
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Unhandled error during key re-search");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
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
            if (_resolver.GetService<SshKeyFile>() is { } keyFile) return keyFile;
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error resolving generic SshKeyFile");
        }

        return null;
    }

    private void AddKeyRange(params IEnumerable<SshKeyFileSource> keyFiles)
    {
        foreach (var keyFile in keyFiles)
            AddKey(keyFile);
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

    private void SearchForKeysAndUpdateCollection()
    {
        if (_directoryCrawler.IsSearching) return;
        var semaphoreAquired = false;
        var errorsOccured = false;
        EnableTempLogger();
        try
        {
            semaphoreAquired = _semaphoreSlim.Wait(TimeSpan.FromSeconds(5));
            AddKeyRange(_directoryCrawler.GetPossibleKeyFilesOnDisk());
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error searching for keys");
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