﻿using System.Collections.ObjectModel;
using System.Text;
using ReactiveUI;

namespace OpenSSHALib.Models;

public class KnownHostsFile : ReactiveObject
{
    internal static string LineEnding = "\r\n";
    private readonly string _fileKnownHostsPath = "";
    private readonly bool _isFromServer;

    private ObservableCollection<KnownHost> _knownHosts = [];

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

    public ObservableCollection<KnownHost> KnownHosts
    {
        get => _knownHosts;
        private set => this.RaiseAndSetIfChanged(ref _knownHosts, value);
    }

    private void SetKnownHosts(string fileContent)
    {
        KnownHosts = new ObservableCollection<KnownHost>(fileContent
            .Split(LineEnding)
            .Where(e => !string.IsNullOrEmpty(e))
            .GroupBy(e => e.Split(' ')[0])
            .Select(e => new KnownHost(e)));
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

    public void SyncKnownHosts(IEnumerable<KnownHost> newKnownHosts)
    {
        KnownHosts = new ObservableCollection<KnownHost>(newKnownHosts);
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
}