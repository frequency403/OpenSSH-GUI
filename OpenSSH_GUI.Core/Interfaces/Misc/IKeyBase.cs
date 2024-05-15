#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 01:05:51
// Last edit: 15.05.2024 - 01:05:48

#endregion

using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Keys;
using Renci.SshNet;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Interfaces.Misc;

public interface IKeyBase
{
    string AbsoluteFilePath { get; }
    string Fingerprint { get; }
    SshKeyFormat Format { get; }
    string ExportOpenSshPublicKey();
    string ExportOpenSshPrivateKey();
    string ExportPuttyPublicKey();
    string ExportPuttyPpkKey();
    string ExportTextOfKey();
    Task ExportToDiskAsync(SshKeyFormat format);
    string ExportAuthorizedKeyEntry();
    IAuthorizedKey ExportAuthorizedKey();
    void ExportToDisk(SshKeyFormat format);
    void ExportToDisk(SshKeyFormat format, out ISshKey? key);
    IPrivateKeySource GetRenciKeyType();
    void DeleteKey();
    ISshKey? Convert(SshKeyFormat format);
    ISshKey? Convert(SshKeyFormat format, ILogger logger);
    ISshKey? Convert(SshKeyFormat format, bool move, ILogger logger);
}