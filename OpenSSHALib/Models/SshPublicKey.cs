#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:03

#endregion

using OpenSSHALib.Interfaces;
using Renci.SshNet;

namespace OpenSSHALib.Models;

public class SshPublicKey(string absoluteFilePath) : SshKey(absoluteFilePath), ISshPublicKey
{
    public ISshKey PrivateKey { get; } = new SshPrivateKey(Path.ChangeExtension(absoluteFilePath, null));

    public override IPrivateKeySource GetRenciKeyType()
    {
        return PrivateKey.GetRenciKeyType();
    }
}