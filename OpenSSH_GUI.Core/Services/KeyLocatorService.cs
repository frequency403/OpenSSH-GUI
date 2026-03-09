using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshKey = SshNet.Keygen.SshKey;

namespace OpenSSH_GUI.Core.Services;

public class KeyLocatorService
{
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
        SshKeys = new ObservableCollection<SshKeyFile>();
        _searchingTask = SearchForKeysAndUpdateCollection();
    }

    public ObservableCollection<SshKeyFile> SshKeys { get; }
    public NotifyCollectionChangedEventHandler SshKeysCollectionChanged { get; set; }

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

    public async ValueTask GenerateNewKeyInFile(string fullFilePath, SshKeyGenerateParams generateParams)
    {
        if (File.Exists(fullFilePath))
            throw new InvalidOperationException("File already exists");
        if (GenerateKeyFile() is not { } keyFile)
            throw new InvalidOperationException("Key file not generated");

        await using var privateStream = new MemoryStream();
        var createdKey = SshKey.Generate(privateStream, generateParams.ToInfo());
        var privateKeyFilePath = fullFilePath;
        switch (generateParams.KeyFormat)
        {
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
                privateKeyFilePath = generateParams.KeyFormat.ChangeExtension(generateParams.FullFilePath);
                await using (var privateFileStream = new FileStream(privateKeyFilePath, FileMode.OpenOrCreate))
                await using (var privateStreamWriter = new StreamWriter(privateFileStream))
                {
                    await privateStreamWriter.WriteAsync(createdKey.ToPuttyFormat());
                }

                break;
            case SshKeyFormat.OpenSSH:
            default:
                var pubPath = generateParams.KeyFormat.ChangeExtension(generateParams.FullFilePath);
                privateKeyFilePath = generateParams.KeyFormat.ChangeExtension(generateParams.FullFilePath, false);
                await using (var privateFileStream = new FileStream(privateKeyFilePath, FileMode.OpenOrCreate))
                await using (var privateStreamWriter = new StreamWriter(privateFileStream))
                {
                    await privateStreamWriter.WriteAsync(createdKey.ToOpenSshFormat());
                }
                await using (var publicFileStream = new FileStream(pubPath, FileMode.OpenOrCreate))
                await using (var publicStreamWriter = new StreamWriter(publicFileStream))
                {
                    await publicStreamWriter.WriteAsync(createdKey.ToOpenSshPublicFormat());
                }

                break;
        }

        await keyFile.Load(privateKeyFilePath, Encoding.UTF8.GetBytes(generateParams.Password));
        SshKeys.Add(keyFile);
    }

    public void RerunSearch()
    {
        if (_searching)
            throw new Exception("Can't rerun search while searching");
        _searchingTask = SearchForKeysAndUpdateCollection();
    }

    private async Task SearchForKeysAndUpdateCollection()
    {
        _searching = true;
        SshKeys.CollectionChanged -= SshKeysCollectionChanged;
        await foreach (var key in _directoryCrawler.GetNewFromDiskAsyncEnumerable())
            SshKeys.Add(key);
        SshKeys.CollectionChanged += SshKeysCollectionChanged;
        _searching = false;
    }
}