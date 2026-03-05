using System.Collections.ObjectModel;
using System.Collections.Specialized;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Lib.Misc;

namespace OpenSSH_GUI.Core.Services;

public class KeyLocatorService
{
    private readonly ILogger<KeyLocatorService> _logger;
    private readonly DirectoryCrawler _directoryCrawler;
    private Task _searchingTask;
    private bool _searching;
    
    public KeyLocatorService(ILogger<KeyLocatorService> logger, DirectoryCrawler directoryCrawler)
    {
        _logger = logger;
        _directoryCrawler = directoryCrawler;
        SshKeys = new ObservableCollectionExtended<SshKeyFile>();
        _searchingTask = SearchForKeysAndUpdateCollection();
    }

    public ObservableCollection<SshKeyFile> SshKeys { get; } 
    public NotifyCollectionChangedEventHandler SshKeysCollectionChanged { get; set; }
    

    public void RerunSearch()
    {
        if(_searching)
            throw new Exception("Can't rerun search while searching");
        _searchingTask = SearchForKeysAndUpdateCollection();
    }

    private async Task SearchForKeysAndUpdateCollection()
    {
        _searching = true;
        SshKeys.CollectionChanged -= SshKeysCollectionChanged;
        await foreach(var key in _directoryCrawler.GetNewFromDiskAsyncEnumerable())
            SshKeys.Add(key);
        SshKeys.CollectionChanged += SshKeysCollectionChanged;
        _searching = false;
    }
}