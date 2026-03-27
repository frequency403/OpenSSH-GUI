using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Text;
using DryIoc;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using Org.BouncyCastle.Tls;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Renci.SshNet;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshKey = SshNet.Keygen.SshKey;

namespace OpenSSH_GUI.Core.Services;

/// <summary>
///     Manager for SSH keys on the local machine.
///     Provides functionality for searching, generating, and changing formats of SSH keys.
/// </summary>
public partial class SshKeyManager : ReactiveObject, IDisposable
{
    private const string BackupFileExtension = ".bak";

    private static readonly FileStreamOptions FileStreamOptions = new()
    {
        BufferSize = 0,
        Access = FileAccess.ReadWrite,
        Mode = FileMode.OpenOrCreate,
        Share = FileShare.ReadWrite
    };

    private readonly DirectoryCrawler _directoryCrawler;
    private readonly ILogger<SshKeyManager> _logger;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly IResolver _resolver;
    private readonly FileSystemWatcher _watcher;

    private volatile bool _searching;

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
        
        _sshKeysHelper = Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => _sshKeysInternal.CollectionChanged += h,
                h => _sshKeysInternal.CollectionChanged -= h)
            .Select(IReadOnlyCollection<SshKeyFile> (_) => _sshKeysInternal).ToProperty(this, vm => vm.SshKeys);
        
        _sshKeysCountHelper = _sshKeysInternal.WhenPropertyChanged(col => col.Count)
            .Select(e => e.Value)
            .ToProperty(this, vm => vm.SshKeysCount);
    }
    
    /// <summary>
    ///     Performs the initial SSH key search on disk.
    ///     Must be called after the DI container is fully built.
    /// </summary>
    public Task InitialSearchAsync(CancellationToken token = default)
        => SearchForKeysAndUpdateCollection();

    private readonly ObservableCollection<SshKeyFile> _sshKeysInternal = [];
    /// <summary>
    ///     Gets the collection of detected SSH keys.
    /// </summary>
    [ObservableAsProperty(ReadOnly = true)] private IReadOnlyCollection<SshKeyFile> _sshKeys = [];
    
    /// <summary>
    /// Gets the number of detected SSH keys.
    /// </summary>
    [ObservableAsProperty(ReadOnly = true)] private int _sshKeysCount;

    public async Task<(bool success, Exception? exception)> TryDeleteKeyAsync(SshKeyFile key, CancellationToken token = default)
    {
        var success = true;
        Exception? exception = null;
        var semaphoreAquired = false;
        try
        {
            semaphoreAquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), token);
            foreach (var keyFile in key.KeyFiles)
            {
                try
                {
                   keyFile.Delete();
                }
                catch(Exception ex)
                {
                    _logger.LogDebug("Error while deleting key {key}: {ex}", key.AbsoluteFilePath, ex);
                    exception = exception is null ? ex : new AggregateException(exception, ex);
                    success = false;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting key");
            success = false;
            exception = exception is null ? e : new AggregateException(exception, e);
        }
        finally
        {
            if (semaphoreAquired)
                _semaphoreSlim.Release();
        }
        return (success, exception);
    }

    public async Task RenameKeyAsync(SshKeyFile key, string newFileName, CancellationToken token = default)
    {
        var semaphoreAquired = false;
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

            var mainFile = files.Distinct().Single();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to change filename of {className}", nameof(SshKeyFile));
            throw;
        }
        finally
        {
            if (semaphoreAquired)
                _semaphoreSlim.Release();
        }
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

        var filePath = newFormat.ChangeExtension(Path.GetFullPath(key.AbsoluteFilePath), false);

        var password = key.Password.IsValid
            ? key.Password.GetPasswordString()
            : null;

        var backedUpFiles = new List<(string backup, string original)>();
        var semaphoreAquired = false;
        try
        {
            foreach (var existingFile in key.KeyFiles.Select(e => e.FullName))
            {
                var backup = existingFile + BackupFileExtension;
                File.Move(existingFile, backup, true);
                backedUpFiles.Add((backup, existingFile));
            }
            
            semaphoreAquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(2), token);
            if (!semaphoreAquired)
                throw new InvalidOperationException("Another key operation is in progress");

            switch (newFormat)
            {
                case SshKeyFormat.OpenSSH:
                    await using (var privateFileStream = new FileStream(filePath, FileStreamOptions))
                    await using (var streamWriter = new StreamWriter(privateFileStream, Encoding.UTF8))
                    {
                        await streamWriter.WriteAsync(key.Password.IsValid
                            ? privateKeyFile.ToOpenSshFormat(key.Password.GetPasswordString())
                            : privateKeyFile.ToOpenSshFormat());
                    }

                    await using (var publicFileStream =
                                 new FileStream(newFormat.ChangeExtension(filePath), FileStreamOptions))
                    await using (var streamWriter = new StreamWriter(publicFileStream, Encoding.UTF8))
                    {
                        await streamWriter.WriteAsync(privateKeyFile.ToOpenSshPublicFormat());
                    }

                    break;

                case SshKeyFormat.PuTTYv2:
                case SshKeyFormat.PuTTYv3:
                default:
                    await using (var privateFileStream = new FileStream(filePath, FileStreamOptions))
                    await using (var streamWriter = new StreamWriter(privateFileStream, Encoding.UTF8))
                    {
                        await streamWriter.WriteAsync(password is not null
                            ? privateKeyFile.ToPuttyFormat(password, newFormat)
                            : privateKeyFile.ToPuttyFormat(newFormat));
                    }

                    break;
            }

            foreach (var (backup, _) in backedUpFiles)
                TryDeleteFile(backup);

            await AddKeyAsync(SshKeyFileSource.FromDisk(filePath));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error changing format of key – attempting rollback");
            foreach (var (backup, original) in backedUpFiles)
                try
                {
                    File.Copy(backup, original, true);
                    TryDeleteFile(backup);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx,
                        "Rollback failed for '{original}' – manual recovery may be required",
                        original);
                }

            throw;
        }
        finally
        {
            if (semaphoreAquired)
                _semaphoreSlim.Release();
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
            await using var privateStream = new MemoryStream();
            var createdKey = SshKey.Generate(privateStream, generateParamsInfo);

            switch (generateParamsInfo.KeyFormat)
            {
                case SshKeyFormat.PuTTYv2:
                case SshKeyFormat.PuTTYv3:
                    var puttyPath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath);
                    await using (var fs = new FileStream(puttyPath, FileStreamOptions))
                    await using (var sw = new StreamWriter(fs))
                    {
                        await sw.WriteAsync(createdKey.ToPuttyFormat(
                            generateParamsInfo.Encryption, generateParamsInfo.KeyFormat));
                    }

                    keyFile.Load(SshKeyFileSource.FromDisk(puttyPath),
                        Encoding.UTF8.GetBytes(generateParamsInfo.Encryption.Passphrase));
                    break;

                case SshKeyFormat.OpenSSH:
                default:
                    var pubPath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath);
                    var privatePath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath, false);
                    await using (var fs = new FileStream(privatePath, FileStreamOptions))
                    await using (var sw = new StreamWriter(fs))
                    {
                        await sw.WriteAsync(
                            createdKey.ToOpenSshFormat(generateParamsInfo.Encryption));
                    }

                    await using (var fs = new FileStream(pubPath, FileStreamOptions))
                    await using (var sw = new StreamWriter(fs))
                    {
                        await sw.WriteAsync(createdKey.ToOpenSshPublicFormat());
                    }

                    keyFile.Load(SshKeyFileSource.FromDisk(privatePath),
                        Encoding.UTF8.GetBytes(generateParamsInfo.Encryption.Passphrase));
                    break;
            }

            _sshKeysInternal.Add(keyFile);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error generating key");
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
        if (_searching)
            throw new InvalidOperationException("Can't rerun search while searching");
        try
        {
            _sshKeysInternal.Clear();
            await SearchForKeysAndUpdateCollection();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled error during key re-search");
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
        try
        {
            if (_sshKeysInternal.SingleOrDefault(k => k.AbsoluteFilePath == Path.ChangeExtension(e.OldFullPath, null)) is
                { } oldKey)
                SshKeyGotDeleted(oldKey, EventArgs.Empty);
            await AddKeyAsync(SshKeyFileSource.FromDisk(Path.ChangeExtension(e.FullPath, null)));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error handling renamed key");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private void WatcherOnDeleted(object? sender, FileSystemEventArgs eventArgs)
    {
        if (!_semaphoreSlim.Wait(100))
            return;
        try
        {
            var normalizedPath = Path.ChangeExtension(
                Path.GetFullPath(eventArgs.FullPath), null);

            var key = SshKeys.SingleOrDefault(k =>
                string.Equals(k.AbsoluteFilePath, normalizedPath,
                    StringComparison.OrdinalIgnoreCase));

            if (key is null)
                return;

            _logger.LogDebug("Key {key} deleted", key.AbsoluteFilePath);
            SshKeyGotDeleted(key, EventArgs.Empty);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling deleted key");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private async Task WatcherOnCreated(FileSystemEventArgs e)
    {
        if (!await _semaphoreSlim.WaitAsync(100))
            return;
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

            await AddKeyAsync(SshKeyFileSource.FromDisk(keyFilePath));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error adding key");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private SshKeyFile? GenerateKeyFile()
    {
        try
        {
            if (_resolver.GetService<SshKeyFile>() is { } keyFile)
            {
                return keyFile;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error resolving generic SshKeyFile");
        }
        return null;
    }
    
    private async Task AddKeyAsync(SshKeyFileSource keyFileSource)
    {
        if (_sshKeysInternal.Any(k =>
                string.Equals(k.AbsoluteFilePath, keyFileSource.AbsolutePath,
                    StringComparison.OrdinalIgnoreCase)))
            return;
        try
        {
            if (GenerateKeyFile() is not { } keyFileGenerated)
                throw new InvalidOperationException("Key file not generated");

            keyFileGenerated.Load(keyFileSource, ReadOnlySpan<byte>.Empty);
            _sshKeysInternal.Add(keyFileGenerated);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error loading keyfile {filePath}", keyFileSource.AbsolutePath);
        }
    }

    private async Task SearchForKeysAndUpdateCollection()
    {
        Interlocked.Exchange(ref _searching, true);
        try
        {
            foreach (var key in await _directoryCrawler.GetPossibleKeyFilesOnDisk())
                await AddKeyAsync(key);
        }
        finally
        {
            Interlocked.Exchange(ref _searching, false);
        }
    }

    private void SshKeyGotDeleted(object? sender, EventArgs e)
    {
        if (sender is not SshKeyFile key) return;
        _sshKeysInternal.Remove(key);
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not delete temporary file '{path}'", path);
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _semaphoreSlim.Dispose();
        foreach (var sshKeyFile in SshKeys)
        {
            sshKeyFile.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}