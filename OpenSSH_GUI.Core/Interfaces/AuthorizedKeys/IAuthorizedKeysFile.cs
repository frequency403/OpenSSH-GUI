#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:28

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