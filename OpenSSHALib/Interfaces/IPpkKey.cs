#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:53

#endregion

using System.Diagnostics.CodeAnalysis;
using OpenSSHALib.Enums;
using SshNet.Keygen;

namespace OpenSSHALib.Interfaces;

public interface IPpkKey : ISshKey
{
    EncryptionType EncryptionType { get; }
    string PublicKeyString { get; }
    string PrivateKeyString { get; }
    string PrivateMAC { get; }
    ISshPublicKey? ConvertToOpenSshKey(out string errorMessage, bool temp = false, bool move = true);
    public Task<string> ExportKeyAsync(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH);
    public string ExportKey(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH);
    public bool MoveFileToSubFolder([NotNullWhen(false)] out Exception? error);
}