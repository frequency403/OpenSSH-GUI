#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:24

#endregion

using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Interfaces.Credentials;

namespace OpenSSH_GUI.Core.Extensions;

public static class ConnectionCredentialsExtensions
{
    public static ConnectionCredentialsDto ToDto(this IConnectionCredentials cc)
    {
        ConnectionCredentialsDto baseDto = new()
        {
            Id = cc.Id,
            Hostname = cc.Hostname,
            Username = cc.Username,
            AuthType = cc.AuthType,
            KeyDtos = []
        };

        using var dbContext = new OpenSshGuiDbContext();
        switch (cc)
        {
            case IPasswordConnectionCredentials pcc:
                baseDto.Password = pcc.Password;
                baseDto.PasswordEncrypted = pcc.EncryptedPassword;
                break;
            case IKeyConnectionCredentials kcc:
                baseDto.KeyDtos.Add(dbContext.KeyDtos.First(e => e.AbsolutePath == kcc.Key.AbsoluteFilePath));
                break;
            case IMultiKeyConnectionCredentials mcc:
                var dbKeys = dbContext.KeyDtos.Where(a =>
                    mcc.Keys.Any(b => b.AbsoluteFilePath == a.AbsolutePath));
                baseDto.KeyDtos.AddRange(dbKeys);
                break;
        }

        return baseDto;
    }
}