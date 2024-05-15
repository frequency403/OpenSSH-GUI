#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:32

#endregion

using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Interfaces.Keys;

public interface IPpkKey : ISshKey
{
    EncryptionType EncryptionType { get; }
    string PublicKeyString { get; }
    string PrivateKeyString { get; }
    string PrivateMAC { get; }
}