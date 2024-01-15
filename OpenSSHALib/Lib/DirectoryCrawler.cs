using OpenSSHALib.Extensions;
using OpenSSHALib.Models;

namespace OpenSSHALib.Lib;

public static class DirectoryCrawler
{
    public static IEnumerable<SshPublicKey> GetAllKeys()
    {
        return Directory.EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*.pub", SearchOption.AllDirectories)
            .Select(
                e => new SshPublicKey(e));
    }
}