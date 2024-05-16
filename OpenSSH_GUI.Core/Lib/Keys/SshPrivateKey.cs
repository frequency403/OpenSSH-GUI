#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:26

#endregion

using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Lib.Keys;

public class SshPrivateKey(string absoluteFilePath, string? password = null) : SshKey(absoluteFilePath, password), ISshPrivateKey
{
}