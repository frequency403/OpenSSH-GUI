#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:53

#endregion

namespace OpenSSHALib.Interfaces;

public interface IPasswordConnectionCredentials : IConnectionCredentials
{
    string Password { get; init; }
}