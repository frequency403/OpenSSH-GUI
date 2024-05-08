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
    bool AddAuthorizedKey(ISshPublicKey key);
    bool ApplyChanges(IEnumerable<IAuthorizedKey> keys);
    IAuthorizedKeysFile PersistChangesInFile();
    Task<bool> AddAuthorizedKeyAsync(ISshPublicKey key);
    bool RemoveAuthorizedKey(ISshPublicKey key);
    string ExportFileContent(bool local = true, PlatformID? platform = null);
}