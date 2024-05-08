#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:56

#endregion

using Renci.SshNet;

namespace OpenSSHALib.Models;

public class SshPrivateKey(string absoluteFilePath) : SshKey(absoluteFilePath)
{
    public override IPrivateKeySource GetRenciKeyType()
    {
        return new PrivateKeyFile(AbsoluteFilePath);
    }
}