#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

using System.Collections.ObjectModel;
using OpenSSH_GUI.Core.Interfaces.Keys;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;

public interface IAuthorizedKeysFile : IReactiveObject
{
    ObservableCollection<IAuthorizedKey> AuthorizedKeys { get; set; }
    bool AddAuthorizedKey(ISshKey key);
    bool ApplyChanges(IEnumerable<IAuthorizedKey> keys);
    IAuthorizedKeysFile PersistChangesInFile();
    Task<bool> AddAuthorizedKeyAsync(ISshKey key);
    bool RemoveAuthorizedKey(ISshKey key);
    string ExportFileContent(bool local = true, PlatformID? platform = null);
}