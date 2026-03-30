using System.Buffers;
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
using SshNet.Keygen.SshKeyEncryption;
using SshKey = SshNet.Keygen.SshKey;

namespace OpenSSH_GUI.Core.Services;

/// <summary>
///     Manager for SSH keys on the local machine.
///     Provides functionality for searching, generating, and changing formats of SSH keys.
/// </summary>
public class SshKeyManager : ReactiveObject, IDisposable
{
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
        SshKeys = new ReadOnlyObservableCollection<SshKeyFile>(_sshKeysInternal);
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
    public ReadOnlyObservableCollection<SshKeyFile> SshKeys { get; }

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

            key.Load(SshKeyFileSource.FromDisk(files.Distinct().Single()));
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

    private async ValueTask WriteToFile(string filePath, string content, Encoding? encoding = null)
    {
        if (encoding is null)
        {
            encoding ??= Encoding.UTF8;
            _logger.LogDebug("Using default encoding:  {encoding}", encoding);
        }
        else
        {
            _logger.LogDebug("Using encoding: {encoding}", encoding);
        }

        await using var fileStream = new FileStream(filePath, FileStreamOptions);
        _logger.LogDebug("Opened file {filePath}", filePath);
        
        byte[]? rented = null;
        var buffer = content.Length <= 256
            ? stackalloc byte[256]
            : (rented = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(content)));
        _logger.LogDebug("Allocated {byteCount} bytes", buffer.Length);
        try
        {
            var writtenBytes = encoding.GetBytes(content, buffer);
            _logger.LogDebug("Writing {byteCount} bytes into file {filePath}", writtenBytes, filePath);
            fileStream.Write(buffer[..writtenBytes]);
            
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while writing file {filePath}", filePath);
            throw;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
                _logger.LogDebug("Freeing memory");
            }
        }
        _logger.LogDebug("Successfully wrote file {filePath}", filePath);
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

        var writtenFiles = new List<string>();
        var backupDir = Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), AppDomain.CurrentDomain.FriendlyName);
        var semaphoreAquired = false;
        try
        {
            foreach (var existingFile in key.KeyFiles)
            {
                if(!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);
                var backup =  existingFile.Name + ".bak";
                existingFile.MoveTo(Path.Combine(backupDir, backup));
            }
            
            semaphoreAquired = await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(2), token);
            if (!semaphoreAquired)
                throw new InvalidOperationException("Another key operation is in progress");
            
            var sshKeyEncryption = key.Password is { IsValid: true } keyPassword
                ? new SshKeyEncryptionAes256(
                    keyPassword.GetPasswordString(),
                    (newFormat is SshKeyFormat.PuTTYv3 ? new PuttyV3Encryption() : null))
                : SshKeyGenerateInfo.DefaultSshKeyEncryption;

            var files = await WriteToFileInSpecificFormat(newFormat,
                sshKeyEncryption,
                privateKeyFile,
                filePath);
            writtenFiles.AddRange(files);
            
            key.Load(SshKeyFileSource.FromDisk(filePath));
            
            if(Directory.Exists(backupDir))
                Directory.Delete(backupDir, true);
        }
        catch (Exception e)
        {
            var exc = e;
            _logger.LogError(e, "Error changing format of key – attempting rollback");
            foreach (var writtenFile in writtenFiles)
            {
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
                    _logger.LogWarning(ex, "Could not delete backup file '{path}'", writtenFile);
                }
            }

            foreach (var backupFileInfo in Directory.EnumerateFiles(backupDir, "*.bak", SearchOption.AllDirectories).Select(file => new FileInfo(file)))
            {
                if (backupFileInfo.Directory?.Parent?.FullName is { } parentDirectory)
                {
                    try
                    {
                        var destination = Path.Combine(parentDirectory, backupFileInfo.Name.Replace(".bak", ""));
                        backupFileInfo.MoveTo(destination);
                        _logger.LogWarning("Restored backup file {backupFile} to its original location {original}",
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
                }
                _logger.LogWarning("Could not move backup file {backupFile} to its original location", backupFileInfo.FullName);
                break;
            }
            throw exc;
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

    private async ValueTask<IEnumerable<string>> WriteToFileInSpecificFormat(SshKeyFormat format, ISshKeyEncryption encryption,
        IPrivateKeySource privateKeySource, string filePath)
    {
        var privateKeyFileContent = encryption is SshKeyEncryptionAes256
            ? format is SshKeyFormat.OpenSSH ? privateKeySource.ToOpenSshFormat(encryption) : privateKeySource.ToPuttyFormat(encryption, format)
            : format is SshKeyFormat.OpenSSH ? privateKeySource.ToOpenSshFormat() : privateKeySource.ToPuttyFormat(format);
        var writtenFiles  = new List<string>();
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

        await WriteToFile(filePath, privateKeyFileContent);
        writtenFiles.Add(filePath);
        return writtenFiles;
    }

    private ValueTask<IEnumerable<string>> WriteToFileInSpecificFormat(SshKeyGenerateInfo generateInfo, GeneratedPrivateKey createdKey, string filePath) => 
        WriteToFileInSpecificFormat(generateInfo.KeyFormat, generateInfo.Encryption, createdKey, filePath);

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
            GeneratedPrivateKey? createdKey = null;

            try
            {
                await using var privateStream = new MemoryStream();
                createdKey = SshKey.Generate(privateStream, generateParamsInfo);
                if(createdKey is null)
                    throw new InvalidOperationException("Could not generate new key");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while generating key file {filePath}", fullFilePath);
                throw;
            }

            var filePath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath, false);

            await WriteToFileInSpecificFormat(generateParamsInfo, createdKey, filePath);
            
            var keyFileSource = SshKeyFileSource.FromDisk(filePath);
            if(string.IsNullOrWhiteSpace(generateParamsInfo.Encryption.Passphrase))
                keyFile.Load(keyFileSource);
            else
                keyFile.Load(keyFileSource, Encoding.UTF8.GetBytes(generateParamsInfo.Encryption.Passphrase));
            
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
            AddKey(SshKeyFileSource.FromDisk(Path.ChangeExtension(e.FullPath, null)));
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

            AddKey(SshKeyFileSource.FromDisk(keyFilePath));
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
            _logger.LogError(e, "Error loading keyfile {filePath}", keyFileSource.AbsolutePath);
        }
    }

    private async Task SearchForKeysAndUpdateCollection()
    {
        Interlocked.Exchange(ref _searching, true);
        try
        {
            foreach (var key in await _directoryCrawler.GetPossibleKeyFilesOnDisk())
                AddKey(key);
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