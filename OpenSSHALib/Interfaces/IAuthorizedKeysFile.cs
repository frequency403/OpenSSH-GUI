#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:53

#endregion

using System.Collections.ObjectModel;
using ReactiveUI;

namespace OpenSSHALib.Interfaces;

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