#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:30

#endregion

using System.Collections.ObjectModel;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using OpenSSH_GUI.Core.Lib.Static;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

/// Represents a known hosts file.
/// /
public class KnownHostsFile : ReactiveObject, IKnownHostsFile
{
    /// Represents the path to the known hosts file.
    /// /
    private readonly string _fileKnownHostsPath = "";

    /// <summary>
    ///     Gets or sets a boolean value indicating whether the `KnownHostsFile` object is created from a server or not.
    /// </summary>
    private readonly bool _isFromServer;

    /// <summary>
    ///     Represents a collection of known host entries in a file.
    /// </summary>
    private ObservableCollection<IKnownHost> _knownHosts = [];

    /// <summary>
    ///     Represents a known hosts file that stores information about trusted hosts.
    /// </summary>
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

    /// <summary>
    ///     Represents a file that contains known SSH hosts and their keys.
    /// </summary>
    public static string LineEnding { get; set; } = "\r\n";

    /// <summary>
    ///     Represents a known hosts file.
    /// </summary>
    public ObservableCollection<IKnownHost> KnownHosts
    {
        get => _knownHosts;
        private set => this.RaiseAndSetIfChanged(ref _knownHosts, value);
    }

    /// <summary>
    ///     Asynchronously reads the contents of the known hosts file.
    /// </summary>
    /// <param name="stream">
    ///     The file stream to read from. If null, the method reads from the file specified in the
    ///     constructor.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReadContentAsync(FileStream? stream = null)
    {
        if (_isFromServer) return;
        if (stream is null)
        {
            using var streamReader = new StreamReader(FileOperations.OpenOrCreate(_fileKnownHostsPath));
            SetKnownHosts(await streamReader.ReadToEndAsync());
        }
        else
        {
            using var streamReader = new StreamReader(stream);
            SetKnownHosts(await streamReader.ReadToEndAsync());
        }
    }

    /// <summary>
    ///     Synchronizes the known hosts with the given list of new known hosts.
    /// </summary>
    /// <param name="newKnownHosts">The new known hosts to synchronize.</param>
    public void SyncKnownHosts(IEnumerable<IKnownHost> newKnownHosts)
    {
        KnownHosts = new ObservableCollection<IKnownHost>(newKnownHosts);
    }

    /// <summary>
    ///     Updates the content of the known hosts file.
    /// </summary>
    /// <returns>A task representing the update operation.</returns>
    public async Task UpdateFile()
    {
        if (_isFromServer) return;
        await using var streamWriter = new StreamWriter(FileOperations.OpenTruncated(_fileKnownHostsPath));
        var newContent = KnownHosts
            .Where(e => !e.DeleteWholeHost)
            .Aggregate("", (current, host) => current + host.GetAllEntries());
        await streamWriter.WriteAsync(newContent);
        SetKnownHosts(newContent);
    }

    /// <summary>
    ///     Retrieves the updated contents of the known hosts file.
    /// </summary>
    /// <param name="platformId">The platform ID of the server.</param>
    /// <returns>The updated contents of the known hosts file as a string.</returns>
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

    /// <summary>
    ///     Sets the known hosts for the file.
    /// </summary>
    /// <param name="fileContent">The contents of the known hosts file.</param>
    private void SetKnownHosts(string fileContent)
    {
        KnownHosts = new ObservableCollection<IKnownHost>(fileContent
            .Split(LineEnding)
            .Where(e => !string.IsNullOrEmpty(e))
            .GroupBy(e => e.Split(' ')[0])
            .Select(e => new KnownHost(e)));
    }

    /// <summary>
    ///     Reads the content of the known hosts file.
    /// </summary>
    /// <param name="stream">Optional file stream to read from. If null, the file specified during instantiation will be used.</param>
    private void ReadContent(FileStream? stream = null)
    {
        ReadContentAsync(stream).GetAwaiter().GetResult();
    }
}