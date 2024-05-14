#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:18

#endregion

using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Lib.Keys;

public class SshPublicKey(string absoluteFilePath) : SshKey(absoluteFilePath), ISshPublicKey
{
    public ISshKey PrivateKey { get; } = new SshPrivateKey(Path.ChangeExtension(absoluteFilePath, null));
}