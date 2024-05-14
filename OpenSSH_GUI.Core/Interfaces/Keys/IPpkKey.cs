#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:38

#endregion

using System.Diagnostics.CodeAnalysis;
using OpenSSH_GUI.Core.Enums;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Interfaces.Keys;

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