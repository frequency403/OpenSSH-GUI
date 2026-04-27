using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Renci.SshNet;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Services;

/// <summary>
///     Manager for SSH keys on the local machine.
///     Provides functionality for searching, generating, and changing formats of SSH keys.
/// </summary>
public sealed partial class SshKeyManager : ReactiveObject, IDisposable
{
    private readonly ILogger<SshKeyManager> _logger;
    private readonly IDirectoryCrawler _directoryCrawler;
    private readonly ISshKeyFactory _keyFactory;
    private readonly ISshKeyGenerator _keyGenerator;
    private readonly IKeyFileWriterService _keyFileWriterService;
    private readonly IKeyFileBackupService _backupService;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly ObservableCollection<SshKeyFile> _sshKeysInternal = [];

    [Reactive] private bool _processing;

    public SshKeyManager(
        ILogger<SshKeyManager> logger,
        IDirectoryCrawler directoryCrawler,
        ISshKeyFactory keyFactory,
        ISshKeyGenerator keyGenerator,
        IKeyFileWriterService keyFileWriterService,
        IKeyFileBackupService backupService)
    {
        _logger = logger;
        _directoryCrawler = directoryCrawler;
        _keyFactory = keyFactory;
        _keyGenerator = keyGenerator;
        _keyFileWriterService = keyFileWriterService;
        _backupService = backupService;
        SshKeys = new ReadOnlyObservableCollection<SshKeyFile>(_sshKeysInternal);
    }

    /// <summary>
    ///     Gets the collection of detected SSH keys.
    /// </summary>
    public ReadOnlyObservableCollection<SshKeyFile> SshKeys { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphoreSlim.Dispose();
        foreach (var sshKeyFile in SshKeys) sshKeyFile.Dispose();
    }

    /// <summary>
    ///     Performs the initial SSH key search on disk.
    ///     Must be called after the DI container is fully built.
    /// </summary>
    public async ValueTask InitialSearchAsync(CancellationToken token = default)
    {
        Processing = true;
        await SearchForKeysAndUpdateCollectionAsync(token);
        Processing = false;
    }

    /// <summary>
    ///     Changes the password of an SSH key file, handling both OpenSSH and PuTTY formats transparently.
    ///     If the key is in PuTTY format, it will be temporarily converted to OpenSSH, the password changed,
    ///     and then converted back to the original format.
    /// </summary>
    /// <param name="key">The SSH key file whose password should be changed.</param>
    /// <param name="newPassword">The new password to set, encoded using <paramref name="encoding" />.</param>
    /// <param name="encoding">
    ///     The encoding used to interpret <paramref name="newPassword" />. Defaults to <see cref="Encoding.UTF8" /> if
    ///     <c>null</c>.
    /// </param>
    /// <param name="token">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <exception cref="ArgumentNullException">Thrown if the private key file of <paramref name="key" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the resolved key file path is null or whitespace.</exception>
    /// <exception cref="TimeoutException">Thrown if the internal semaphore could not be acquired within 5 seconds.</exception>
    /// <exception cref="Exception">
    ///     Thrown if <c>ssh-keygen</c> exits with a non-zero code, or if intermediate key file operations fail.
    ///     On failure, all modified files are restored from backup.
    /// </exception>
    public async ValueTask<KeyManagerOperationResult> ChangePasswordOfKeyAsync(SshKeyFile key,
        ReadOnlyMemory<byte> newPassword,
        Encoding? encoding = null, CancellationToken token = default)
    {
        _backupService.BeginOperationLog();
        encoding ??= Encoding.UTF8;
        var semaphoreAcquired = false;
        var errorsOccured = false;
        BackedUpFile[] backupFiles = [];
        string[] additionalDeleteFiles = [];
        var keyFilePath = string.Empty;
        try
        {
            Processing = true;
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
            backupFiles = _backupService.BackupFiles(key.KeyFiles).ToArray();

            if (key.Format is { } and not SshKeyFormat.OpenSSH)
            {
                Log(LogLevel.Debug, "Detected PuTTY key {key} - need to change format first", keyFilePath);
                additionalDeleteFiles = (await _keyFileWriterService.WriteToFileInSpecificFormat(
                    SshKeyFormat.OpenSSH,
                    key.Password.ToSshKeyEncryption(), privateKeyFile, keyFilePath, true)).ToArray();

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
                    Log(
                        LogLevel.Error, "ssh-keygen exited with code {exitCode} and message: {message}",
                        process.ExitCode, message);
                    throw new Exception($"ssh-keygen exited with code {process.ExitCode}");
                }

                var output = await process.StandardOutput.ReadToEndAsync(token);
                Log(LogLevel.Debug, "ssh-keygen exited without errors and output: {message}", output);
            }

            if (key.Format is { } format and not SshKeyFormat.OpenSSH)
            {
                var keyFile = _keyFactory.Create();
                keyFile.Load(SshKeyFileSource.FromDisk(keyFilePath), newPassword.Span);
                Log(
                    LogLevel.Debug,
                    "Changes to the password were made in OpenSSH Format - need to change format to Putty again");
                keyFilePath = (await _keyFileWriterService.WriteToFileInSpecificFormat(
                    format, keyFile.Password.ToSshKeyEncryption(),
                    keyFile.PrivateKeyFile ?? throw new Exception("Private key file not found"), keyFilePath,
                    true)).First();

                Log(LogLevel.Debug, "New file path: {newFilePath}", keyFilePath);
                foreach (var deleteFile in additionalDeleteFiles) File.Delete(deleteFile);
            }

            key.Load(SshKeyFileSource.FromDisk(keyFilePath), newPassword.Span);
            Log(LogLevel.Debug, "Successfully changed password of key {key}", keyFilePath);
            _backupService.DeleteBackupFiles(backupFiles);
            return KeyManagerOperationResult.Success();
        }
        catch (Exception e)
        {
            errorsOccured = true;
            Log(LogLevel.Error, e, "Error changing password of key {key}", keyFilePath);
            _backupService.RestoreBackupFiles(backupFiles);
            return KeyManagerOperationResult.FromException(e);
        }
        finally
        {
            if (semaphoreAcquired)
                _semaphoreSlim.Release();
            _backupService.EndOperationLog(errorsOccured);
            Processing = false;
        }
    }

    /// <summary>
    ///     Attempts to delete all files associated with the given SSH key.
    ///     Unlike <see cref="ChangePasswordOfKeyAsync" />, this method does not throw on failure —
    ///     instead, all encountered exceptions are aggregated and returned alongside a success flag.
    /// </summary>
    /// <param name="key">The SSH key file to delete, including all associated key files.</param>
    /// <param name="token">A cancellation token to observe while waiting for the semaphore.</param>
    /// <returns>
    ///     A <see cref="KeyManagerOperationResult" /> indicating success, or containing an
    ///     <see cref="AggregateException" /> if one or more files could not be deleted.
    /// </returns>
    public async ValueTask<KeyManagerOperationResult> TryDeleteKeyAsync(SshKeyFile key,
        CancellationToken token = default)
    {
        _backupService.BeginOperationLog();
        var errorsOccured = false;
        Exception? exception = null;
        var semaphoreAcquired = false;
        try
        {
            Processing = true;
            semaphoreAcquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token);
            foreach (var keyFile in key.KeyFiles)
                try
                {
                    keyFile.Delete();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Debug, ex, "Error while deleting key {key}", key.AbsoluteFilePath);
                    exception = exception is null ? ex : new AggregateException(exception, ex);
                }

            if (exception is not null)
                throw exception;
            Log(LogLevel.Debug, "Successfully deleted key {key}", key.AbsoluteFilePath);
            _sshKeysInternal.Remove(key);
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
            _backupService.EndOperationLog(errorsOccured);
            Processing = false;
        }
    }

    /// <summary>
    ///     Renames all files associated with the given <see cref="SshKeyFile" /> to a new base file name,
    ///     preserving each file's original extension. If any target file already exists and
    ///     <paramref name="overwrite" /> is <see langword="false" />, a conflict result is returned.
    ///     On failure, all files are restored from backup.
    /// </summary>
    /// <param name="key">
    ///     The <see cref="SshKeyFile" /> whose associated files are to be renamed.
    ///     After a successful rename, the key is reloaded from the new primary file.
    /// </param>
    /// <param name="newFileName">
    ///     The new base file name (without extension) to assign to all files of the key.
    ///     Each file retains its original extension.
    /// </param>
    /// <param name="overwrite">A flag to indicate forceful overwrite of any existent files.</param>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the semaphore
    ///     and during file move operations.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when another key operation is already in progress and the semaphore
    ///     could not be acquired within the timeout.
    /// </exception>
    /// <remarks>
    ///     File moves are performed via <see cref="FileInfo.MoveTo(string, bool)" /> wrapped in
    ///     <see cref="Task.Run(Action, CancellationToken)" />,
    ///     since no native async move API exists in .NET. On same-volume moves, this is an atomic
    ///     metadata operation. Backups are created before any file is moved and deleted only on full success;
    ///     on any failure the backup is restored.
    /// </remarks>
    public async ValueTask<KeyManagerOperationResult> RenameKeyAsync(SshKeyFile key, string newFileName,
        bool overwrite = false, CancellationToken token = default)
    {
        var semaphoreAcquired = false;
        _backupService.BeginOperationLog();
        var errorsOccurred = false;
        BackedUpFile[] backupFiles = [];
        try
        {
            Processing = true;
            semaphoreAcquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token);
            if (!semaphoreAcquired)
                throw new InvalidOperationException("Another key operation is in progress");

            backupFiles = _backupService.BackupFiles(key.KeyFiles).ToArray();
            var filePairs = (key.KeyFileInfo?.Files ?? []).Select(file =>
            {
                ArgumentNullException.ThrowIfNull(file.Directory);
                var newFileNameForFile = Path.ChangeExtension(
                    newFileName,
                    string.IsNullOrEmpty(file.Extension) ? null : file.Extension);
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
                await Task.Run(() => source.MoveTo(target, true), token);
                Log(LogLevel.Debug, "Successfully renamed file {file} to {newFileName}", source.FullName, source.Name);
            }

            var expectedExtension = key.Format?.GetExtension(false);
            if (filePairs.Select(p => p.Source).FirstOrDefault(file =>
                    string.Equals(
                        string.IsNullOrEmpty(file.Extension) ? null : file.Extension,
                        expectedExtension,
                        StringComparison.OrdinalIgnoreCase) &&
                    file.Exists) is { } keyFileToLoad)
            {
                Log(LogLevel.Debug, "Loading key file {keyFile}", keyFileToLoad.FullName);
                key.Load(SshKeyFileSource.FromDisk(keyFileToLoad.FullName));
                Log(LogLevel.Debug, "Successfully loaded key file {keyFile}", keyFileToLoad.FullName);
                _backupService.DeleteBackupFiles(backupFiles);
                return KeyManagerOperationResult.Success();
            }

            Log(LogLevel.Warning, "No valid key file found for key format {format}", key.Format);
            throw new Exception("No valid key file found for key format");
        }
        catch (Exception e)
        {
            errorsOccurred = true;
            Log(LogLevel.Error, e, "Failed to change filename of {className}", nameof(SshKeyFile));
            _backupService.RestoreBackupFiles(backupFiles);
            return KeyManagerOperationResult.FromException(e);
        }
        finally
        {
            if (semaphoreAcquired)
                _semaphoreSlim.Release();
            _backupService.EndOperationLog(errorsOccurred);
            Processing = false;
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
            return KeyManagerOperationResult.Failure(new InvalidOperationException("Key file not initialized"));

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

        _backupService.BeginOperationLog();
        var filePath = newFormat.ChangeExtension(Path.GetFullPath(key.AbsoluteFilePath), false);
        var writtenFiles = new List<string>();
        BackedUpFile[] backupFiles = [];
        var semaphoreAcquired = false;
        var errorsOccured = false;
        try
        {
            Processing = true;
            backupFiles = _backupService.BackupFiles(key.KeyFiles).ToArray();

            semaphoreAcquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(2), token);
            if (!semaphoreAcquired)
                throw new InvalidOperationException("Another key operation is in progress");

            writtenFiles.AddRange(
                await _keyFileWriterService.WriteToFileInSpecificFormat(
                    newFormat,
                    key.Password.ToSshKeyEncryption(),
                    privateKeyFile,
                    filePath, true));

            key.Load(SshKeyFileSource.FromDisk(filePath));
            Log(
                LogLevel.Debug, "Successfully changed format of key {key} to {format}",
                key.AbsoluteFilePath, newFormat);

            foreach (var backupFile in backupFiles)
            {
                if (!writtenFiles.Contains(backupFile.InitialFile.FullName, StringComparer.OrdinalIgnoreCase))
                {
                    backupFile.InitialFile.Delete();
                    Log(LogLevel.Debug, "Deleted source key file {file}", backupFile.InitialFile.FullName);
                }
            }

            _backupService.DeleteBackupFiles(backupFiles);
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
                        AggregateException agg => new AggregateException(agg.InnerExceptions.Append(ex)),
                        not null => new AggregateException(exc, ex),
                        _ => ex
                    };
                    Log(LogLevel.Warning, ex, "Could not delete created file '{path}'", writtenFile);
                }

            try
            {
                _backupService.RestoreBackupFiles(backupFiles);
            }
            catch (Exception exception)
            {
                exc = exc switch
                {
                    AggregateException agg => new AggregateException(agg.InnerExceptions.Append(exception)),
                    not null => new AggregateException(exc, exception),
                    _ => exception
                };
                Log(LogLevel.Warning, exception, "Could not restore backup files");
            }

            return KeyManagerOperationResult.FromException(exc);
        }
        finally
        {
            if (semaphoreAcquired)
                _semaphoreSlim.Release();
            _backupService.EndOperationLog(errorsOccured);
            Processing = false;
        }
    }

    /// <summary>
    ///     Generates a new SSH key and adds it to the managed collection.
    /// </summary>
    /// <param name="fullFilePath">The full path where the new key should be stored.</param>
    /// <param name="generateParamsInfo">Parameters for key generation.</param>
    /// <param name="overwrite">Whether to overwrite an existing file at the target path.</param>
    /// <returns>A <see cref="KeyManagerOperationResult" /> indicating the outcome of the operation.</returns>
    public async ValueTask<KeyManagerOperationResult> GenerateNewKey(string fullFilePath,
        SshKeyGenerateInfo generateParamsInfo, bool overwrite = false)
    {
        if (File.Exists(fullFilePath) && !overwrite)
            return KeyManagerOperationResult.Failure(new InvalidOperationException("File already exists"));
        if (!await _semaphoreSlim.WaitAsync(100))
            return KeyManagerOperationResult.FromException(
                new InvalidOperationException("Another key operation is in progress"));
        try
        {
            Processing = true;
            var filePath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath, false);
            var keyFile = await _keyGenerator.Generate(filePath, generateParamsInfo, overwrite);
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
            Processing = false;
        }
    }

    /// <summary>
    ///     Triggers a re-search for SSH keys on disk and rebuilds the managed collection.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="KeyManagerOperationResult" /> indicating the outcome of the operation.</returns>
    public async ValueTask<KeyManagerOperationResult> RerunSearchAsync(CancellationToken token = default)
    {
        if (!await _semaphoreSlim.WaitAsync(100, token))
            return KeyManagerOperationResult.FromException(
                new InvalidOperationException("Another key operation is in progress"));
        try
        {
            Processing = true;
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
            Processing = false;
        }

        return KeyManagerOperationResult.Success();
    }

    private void AddKey(SshKeyFileSource keyFileSource)
    {
        if (_sshKeysInternal.Any(k =>
                string.Equals(
                    k.AbsoluteFilePath, keyFileSource.AbsolutePath,
                    StringComparison.OrdinalIgnoreCase)))
            return;
        try
        {
            var keyFileGenerated = _keyFactory.Create();
            keyFileGenerated.Load(keyFileSource);
            _sshKeysInternal.Add(keyFileGenerated);
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, e, "Error loading keyfile {filePath}", keyFileSource.AbsolutePath);
        }
    }

    private async ValueTask<KeyManagerOperationResult> SearchForKeysAndUpdateCollectionAsync(
        CancellationToken token = default)
    {
        if (_directoryCrawler.IsSearching)
            return KeyManagerOperationResult.Conflict(new InvalidOperationException("Key search already in progress"));
        var semaphoreAcquired = false;
        var errorsOccured = false;
        _backupService.BeginOperationLog();
        try
        {
            semaphoreAcquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token);
            await foreach (var sshKey in _directoryCrawler.GetPossibleKeyFilesOnDiskAsyncEnumerable(token))
                AddKey(sshKey);
            return KeyManagerOperationResult.Success();
        }
        catch (Exception e)
        {
            errorsOccured = true;
            Log(LogLevel.Error, e, "Error searching for keys");
            return KeyManagerOperationResult.Failure(e);
        }
        finally
        {
            if (semaphoreAcquired)
                _semaphoreSlim.Release();
            _backupService.EndOperationLog(errorsOccured);
        }
    }

#pragma warning disable CA2254
    private void Log(LogLevel level, [StructuredMessageTemplate] string? message, params object?[] args)
    {
        _logger.Log(level, message, args);
        _backupService.WriteToOperationLog(level, message, args);
    }

    private void Log(LogLevel level, Exception? exception, [StructuredMessageTemplate] string? message,
        params object?[] args)
    {
        _logger.Log(level, exception, message, args);
        _backupService.WriteToOperationLog(level, exception, message, args);
    }
#pragma warning restore CA2254
}