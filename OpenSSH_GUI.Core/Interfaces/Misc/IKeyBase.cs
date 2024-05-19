#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 01:05:51
// Last edit: 15.05.2024 - 01:05:48

#endregion

using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Keys;
using Renci.SshNet;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Interfaces.Misc;

public interface IKeyBase
{
    bool HasPassword { get; }
    bool NeedPassword { get; }
    bool PasswordSuccess { get; set; }
    string? Password { get; set; }
    string AbsoluteFilePath { get; }
    string Fingerprint { get; }
    string Filename { get; }
    SshKeyFormat Format { get; }
    ISshKeyType KeyType { get; }
    string? ExportOpenSshPublicKey();
    string? ExportOpenSshPrivateKey();
    string? ExportPuttyPublicKey();
    string? ExportPuttyPpkKey();
    string? ExportTextOfKey();
    string? ExportAuthorizedKeyEntry();
    IAuthorizedKey ExportAuthorizedKey();
    IPrivateKeySource? GetSshNetKeyType();
    void DeleteKey();
    SshKeyDto ToDto();
}