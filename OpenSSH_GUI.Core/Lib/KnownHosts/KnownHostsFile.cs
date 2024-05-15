#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:30

#endregion

using System.Collections.ObjectModel;
using System.Text;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

public class KnownHostsFile : ReactiveObject, IKnownHostsFile
{
    private readonly string _fileKnownHostsPath = "";
    private readonly bool _isFromServer;

    private ObservableCollection<IKnownHost> _knownHosts = [];

    public KnownHostsFile(string knownHostsPathOrContent, bool fromServer = false)
    {
        _isFromServer = fromServer;
        if (_isFromServer)
        {
            SetKnownHosts(knownHostsPathOrContent);
        }
        else
        {
            _fileKnownHostsPath = knownHostsPathOrContent;
            ReadContent();
        }
    }

    public static string LineEnding { get; set; } = "\r\n";

    public ObservableCollection<IKnownHost> KnownHosts
    {
        get => _knownHosts;
        private set => this.RaiseAndSetIfChanged(ref _knownHosts, value);
    }

    public async Task ReadContentAsync(FileStream? stream = null)
    {
        if (_isFromServer) return;
        if (stream is null)
        {
            await using var fileStream = File.OpenRead(_fileKnownHostsPath);
            using var streamReader = new StreamReader(fileStream);
            SetKnownHosts(await streamReader.ReadToEndAsync());
        }
        else
        {
            using var streamReader = new StreamReader(stream);
            SetKnownHosts(await streamReader.ReadToEndAsync());
        }
    }

    public void SyncKnownHosts(IEnumerable<IKnownHost> newKnownHosts)
    {
        KnownHosts = new ObservableCollection<IKnownHost>(newKnownHosts);
    }

    public async Task UpdateFile()
    {
        if (_isFromServer) return;
        await using var fileStream = File.OpenWrite(_fileKnownHostsPath);
        fileStream.SetLength(0);
        await fileStream.FlushAsync();
        var newContent = KnownHosts
            .Where(e => !e.DeleteWholeHost)
            .Aggregate("", (current, host) => current + host.GetAllEntries());
        var newContentBytes = Encoding.Default.GetBytes(newContent);
        fileStream.SetLength(newContentBytes.Length);
        await fileStream.WriteAsync(newContentBytes);
        await fileStream.FlushAsync();
        SetKnownHosts(newContent);
    }

    public string GetUpdatedContents(PlatformID platformId)
    {
        if (!_isFromServer) return "";
        LineEnding = platformId == PlatformID.Unix ? LineEnding : "`r`n";
        var newContent = KnownHosts
            .Where(e => !e.DeleteWholeHost)
            .Aggregate("", (current, host) => current + host.GetAllEntries());
        SetKnownHosts(newContent);
        return newContent;
    }

    private void SetKnownHosts(string fileContent)
    {
        KnownHosts = new ObservableCollection<IKnownHost>(fileContent
            .Split(LineEnding)
            .Where(e => !string.IsNullOrEmpty(e))
            .GroupBy(e => e.Split(' ')[0])
            .Select(e => new KnownHost(e)));
    }

    private void ReadContent(FileStream? stream = null)
    {
        if (_isFromServer) return;
        if (stream is null)
        {
            using var fileStream = File.OpenRead(_fileKnownHostsPath);
            using var streamReader = new StreamReader(fileStream);
            SetKnownHosts(streamReader.ReadToEnd());
        }
        else
        {
            using var streamReader = new StreamReader(stream);
            SetKnownHosts(streamReader.ReadToEnd());
        }
    }
}