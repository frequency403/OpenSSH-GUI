#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

public interface IPasswordConnectionCredentials : IConnectionCredentials
{
    string Password { get; set; }
    bool EncryptedPassword { get; set; }
}