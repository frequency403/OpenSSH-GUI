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
using SshNet.Keygen.SshKeyEncryption;
using SshKey = SshNet.Keygen.SshKey;

namespace OpenSSH_GUI.Core.Services;

public class KeyLocatorService : ReactiveObject
{
    private readonly IDisposable _keyCountWatcherDisposable;
    private readonly DirectoryCrawler _directoryCrawler;
    private readonly ILogger<KeyLocatorService> _logger;
    private readonly IServiceProvider serviceProvider;
    private bool _searching;
    private Task _searchingTask;

    public KeyLocatorService(ILogger<KeyLocatorService> logger, DirectoryCrawler directoryCrawler,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _directoryCrawler = directoryCrawler;
        this.serviceProvider = serviceProvider;
        SshKeys = [];
        SshKeys.CollectionChanged += SshKeysOnCollectionChanged;
        _searchingTask = SearchForKeysAndUpdateCollection();
        _keyCountWatcherDisposable = SshKeys.ObservableForProperty<ICollection<SshKeyFile>, int>(nameof(SshKeys.Count)).Subscribe(change =>
        {
            SshKeysCount = change.Value;
        });
    }

    private void SshKeysOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var key in e.NewItems.OfType<SshKeyFile>())
                {
                    key.GotDeleted += SshKeyGotDeleted;
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (var key in e.OldItems.OfType<SshKeyFile>())
                {
                    key.GotDeleted -= SshKeyGotDeleted;
                }
                break;
        }
    }

    public ObservableCollection<SshKeyFile> SshKeys
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int SshKeysCount
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void ChangeOrder(IOrderedEnumerable<SshKeyFile> collection)
    {
        SshKeys = new ObservableCollection<SshKeyFile>(collection);
    }

    private SshKeyFile? GenerateKeyFile()
    {
        SshKeyFile? file = null;
        try
        {
            file = serviceProvider.GetService<SshKeyFile>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error resolving generic SshKeyFile");
        }

        return file;
    }

    public async ValueTask GenerateNewKeyInFile(string fullFilePath, SshKeyGenerateInfo generateParamsInfo)
    {
        if (File.Exists(fullFilePath))
            throw new InvalidOperationException("File already exists");
        if (GenerateKeyFile() is not { } keyFile)
            throw new InvalidOperationException("Key file not generated");

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
                    await privateStreamWriter.WriteAsync(createdKey.ToPuttyFormat(generateParamsInfo.Encryption, generateParamsInfo.KeyFormat));
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
        SshKeys.Add(keyFile);
    }

    public Task RerunSearchAsync() => _searching ? throw new InvalidOperationException("Can't rerun search while searching") : SearchForKeysAndUpdateCollection();

    private async Task SearchForKeysAndUpdateCollection()
    {
        _searching = true;
        await foreach (var key in _directoryCrawler.GetNewFromDiskAsyncEnumerable())
        {
            key.GotDeleted += SshKeyGotDeleted;
            SshKeys.Add(key);
        }
        _searching = false;
    }

    private void SshKeyGotDeleted(object? sender, EventArgs e)
    {
        if(sender is SshKeyFile key)
            SshKeys.Remove(key);
    }
}