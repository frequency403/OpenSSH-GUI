using System.Collections.ObjectModel;
using OpenSSHALib.Models;
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
