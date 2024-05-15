#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

#endregion

using System.Collections.ObjectModel;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.KnownHosts;

public interface IKnownHostsFile : IReactiveObject
{
    static string LineEnding { get; set; }
    ObservableCollection<IKnownHost> KnownHosts { get; }
    Task ReadContentAsync(FileStream? stream = null);
    void SyncKnownHosts(IEnumerable<IKnownHost> newKnownHosts);
    Task UpdateFile();
    string GetUpdatedContents(PlatformID platformId);
}