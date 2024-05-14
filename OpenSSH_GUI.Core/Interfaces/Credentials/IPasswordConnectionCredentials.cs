#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:38

#endregion

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

public interface IPasswordConnectionCredentials : IConnectionCredentials
{
    string Password { get; set; }
    bool EncryptedPassword { get; set; }
}