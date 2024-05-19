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
    public static async Task<ConnectionCredentialsDto?> SaveDtoInDatabase(this IConnectionCredentials cc)
    {
            await using var dbContext = new OpenSshGuiDbContext();
            ConnectionCredentialsDto baseDto = new()
            {
                Id = cc.Id,
                Hostname = cc.Hostname,
                Username = cc.Username,
                AuthType = cc.AuthType,
                KeyDtos = []
            };
            var entity = dbContext.ConnectionCredentialsDtos.Add(baseDto);
            await dbContext.SaveChangesAsync();
            baseDto = entity.Entity;

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

            await dbContext.SaveChangesAsync();
            return await dbContext.ConnectionCredentialsDtos.FindAsync(baseDto.Id);
        
    }
}