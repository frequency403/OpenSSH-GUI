using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshKey = SshNet.Keygen.SshKey;

namespace OpenSSH_GUI.Core.Services;

public class KeyLocatorService : ReactiveObject
{
    private const string PuttyKeyFileExtension = ".ppk";
    private const string OpenSshKeyFileExtension = ".pub";
    
    private readonly DirectoryCrawler _directoryCrawler;
    private readonly ILogger<KeyLocatorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly FileSystemWatcher _watcher = new();
    private bool _searching;
    private Task _searchingTask;
    private bool _addingAKey;

    public KeyLocatorService(ILogger<KeyLocatorService> logger, DirectoryCrawler directoryCrawler,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _directoryCrawler = directoryCrawler;
        _serviceProvider = serviceProvider;
        _watcher = new FileSystemWatcher
        {
            Path = SshConfigFilesExtension.GetBaseSshPath(),
            EnableRaisingEvents = true,
        };
        _watcher.Filters.Add("*.pub");
        _watcher.Filters.Add("*.ppk");
        _watcher.Created += async (_, eventArgs) => await WatcherOnCreated(eventArgs);
        _watcher.Deleted += WatcherOnDeleted;
        SshKeysInternal = [];
        SshKeysInternal.CollectionChanged += SshKeysOnCollectionChanged;
        _searchingTask = SearchForKeysAndUpdateCollection();
    }

    private void WatcherOnDeleted(object? sender, FileSystemEventArgs eventArgs)
    {
        if (SshKeys.Where(key => string.Equals(key.AbsoluteFilePath,
                Path.ChangeExtension(Path.GetFullPath(eventArgs.FullPath), null),
                StringComparison.OrdinalIgnoreCase)) is not { } enumerable) return;
        {
            try
            {
                var key = enumerable.Single();
                _logger.LogDebug("Key {key} deleted", key.AbsoluteFilePath);
                SshKeyGotDeleted(key, EventArgs.Empty);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting key");
            }
        }
    }

    private async Task WatcherOnCreated(FileSystemEventArgs e)
    {
        if(_addingAKey) return;
        var keyFilePath = string.Equals(Path.GetExtension(e.FullPath), PuttyKeyFileExtension, StringComparison.OrdinalIgnoreCase) ? e.FullPath : Path.ChangeExtension(e.FullPath, null);
        if(SshKeys.Any(key => string.Equals(key.AbsoluteFilePath, keyFilePath, StringComparison.OrdinalIgnoreCase)))
            return;
        await AddKeyAsync(keyFilePath);
    }

    private void SshKeysOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if(e.NewItems is { } collection)
                    foreach (var key in collection.OfType<SshKeyFile>())
                    {
                        try
                        {
                            key.GotDeleted += SshKeyGotDeleted;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Error adding GotDeleted event handler");
                        }
                    }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is { } oldItems)
                    foreach (var key in oldItems.OfType<SshKeyFile>())
                    {
                        try
                        {
                            key.GotDeleted -= SshKeyGotDeleted;
                            key.Dispose();
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Error removing GotDeleted event handler");
                        }
                    }
                break;
        }
    }
    
    public IReadOnlyCollection<SshKeyFile> SshKeys => SshKeysInternal;
    private ObservableCollection<SshKeyFile> SshKeysInternal { get; }

    public void ChangeOrder(Func<IEnumerable<SshKeyFile>, IEnumerable<SshKeyFile>> orderFunc)
    {
        var reordered = orderFunc(SshKeys).ToList();

        for (var i = 0; i < reordered.Count; i++)
        {
            var oldIndex = SshKeysInternal.IndexOf(reordered[i]);
            if (oldIndex != i)
                SshKeysInternal.Move(oldIndex, i);
        }
    }

    private SshKeyFile? GenerateKeyFile()
    {
        SshKeyFile? file = null;
        try
        {
            file = _serviceProvider.GetService<SshKeyFile>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error resolving generic SshKeyFile");
        }

        return file;
    }

    public async ValueTask GenerateNewKey(string fullFilePath, SshKeyGenerateInfo generateParamsInfo)
    {
        if (File.Exists(fullFilePath))
            throw new InvalidOperationException("File already exists");
        if (GenerateKeyFile() is not { } keyFile)
            throw new InvalidOperationException("Key file not generated");
        _addingAKey = true;
        try
        {
            await using var privateStream = new MemoryStream();
            var createdKey = SshKey.Generate(privateStream, generateParamsInfo);
            var privateKeyFilePath = fullFilePath;
            switch (generateParamsInfo.KeyFormat)
            {
                case SshKeyFormat.PuTTYv2:
                case SshKeyFormat.PuTTYv3:
                    privateKeyFilePath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath);
                    await using (var privateFileStream = new FileStream(privateKeyFilePath, FileMode.OpenOrCreate))
                    await using (var privateStreamWriter = new StreamWriter(privateFileStream))
                    {
                        await privateStreamWriter.WriteAsync(createdKey.ToPuttyFormat(generateParamsInfo.Encryption,
                            generateParamsInfo.KeyFormat));
                    }

                    break;
                case SshKeyFormat.OpenSSH:
                default:
                    var pubPath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath);
                    privateKeyFilePath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath, false);
                    await using (var privateFileStream = new FileStream(privateKeyFilePath, FileMode.OpenOrCreate))
                    await using (var privateStreamWriter = new StreamWriter(privateFileStream))
                    {
                        await privateStreamWriter.WriteAsync(createdKey.ToOpenSshFormat(generateParamsInfo.Encryption));
                    }

                    await using (var publicFileStream = new FileStream(pubPath, FileMode.OpenOrCreate))
                    await using (var publicStreamWriter = new StreamWriter(publicFileStream))
                    {
                        await publicStreamWriter.WriteAsync(createdKey.ToOpenSshPublicFormat());
                    }

                    break;
            }

            await keyFile.Load(privateKeyFilePath, Encoding.UTF8.GetBytes(generateParamsInfo.Encryption.Passphrase));
            SshKeysInternal.Add(keyFile);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error generating key");
        }
        finally
        {
            _addingAKey = false;
        }
    }

    public Task RerunSearchAsync() => _searching ? throw new InvalidOperationException("Can't rerun search while searching") : SearchForKeysAndUpdateCollection();

    private async Task AddKeyAsync(string fullFilePath)
    {
        try
        {
            if (GenerateKeyFile() is not { } keyFileGenerated)
                throw new InvalidOperationException("Key file not generated");

            await keyFileGenerated.Load(fullFilePath);
            keyFileGenerated.GotDeleted += SshKeyGotDeleted;
            SshKeysInternal.Add(keyFileGenerated);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error loading keyfile {filePath}", fullFilePath);
        }
    }
    
    private async Task SearchForKeysAndUpdateCollection()
    {
        _searching = true;
        foreach (var key in await _directoryCrawler.GetPossibleKeyFilesOnDisk())
        {
            await AddKeyAsync(key);
        }
        _searching = false;
    }

    private void SshKeyGotDeleted(object? sender, EventArgs e)
    {
        if (sender is not SshKeyFile key) return;
        try
        {
            key.GotDeleted -= SshKeyGotDeleted;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error removing GotDeleted event handler");
        }
        SshKeysInternal.Remove(key);
    }
}