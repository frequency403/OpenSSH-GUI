#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:39

#endregion

using System.Collections.ObjectModel;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.Misc;

public interface IKnownHostsFile : IReactiveObject
{
    static string LineEnding { get; set; }
    ObservableCollection<IKnownHost> KnownHosts { get; }
    Task ReadContentAsync(FileStream? stream = null);
    void SyncKnownHosts(IEnumerable<IKnownHost> newKnownHosts);
    Task UpdateFile();
    string GetUpdatedContents(PlatformID platformId);
}