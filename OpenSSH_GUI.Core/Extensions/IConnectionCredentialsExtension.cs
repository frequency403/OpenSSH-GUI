#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:24

#endregion

using Microsoft.EntityFrameworkCore;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Interfaces.Credentials;

namespace OpenSSH_GUI.Core.Extensions;

/// <summary>
/// Extension class for connection credentials.
/// </summary>
public static class ConnectionCredentialsExtensions
{
    /// <summary>
    /// Saves the given connection credentials DTO in the database.
    /// </summary>
    /// <param name="cc">The connection credentials.</param>
    /// <returns>A task representing the asynchronous operation. The task result is the saved connection credentials DTO.</returns>
    public static async Task<ConnectionCredentialsDto?> SaveDtoInDatabase(this IConnectionCredentials cc)
    {
            await using var dbContext = new OpenSshGuiDbContext();
            
            var foundDuplicate = (cc is IMultiKeyConnectionCredentials or IPasswordConnectionCredentials
                ? await dbContext.ConnectionCredentialsDtos.FirstOrDefaultAsync(c => 
                    c.Hostname == cc.Hostname && 
                    c.Username == cc.Username && 
                    c.AuthType == cc.AuthType)
                : null) ?? (cc is IKeyConnectionCredentials dkcc
                ? await dbContext.ConnectionCredentialsDtos.FirstOrDefaultAsync(c => c.Hostname == cc.Hostname && 
                                                                            c.Username == cc.Username && 
                                                                            c.AuthType == cc.AuthType &&
                                                                            c.KeyDtos.FirstOrDefault(e => e.AbsolutePath != dkcc.Key.AbsoluteFilePath) == null)
                : null);
            if (foundDuplicate is not null) return null;
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
                        mcc.Keys.Select(b => b.AbsoluteFilePath).Contains(a.AbsolutePath));
                    baseDto.KeyDtos.AddRange(dbKeys);
                    break;
            }

            await dbContext.SaveChangesAsync();
            return await dbContext.ConnectionCredentialsDtos.FindAsync(baseDto.Id);
        
    }

    public static async Task RemoveDtoFromDatabase(this IConnectionCredentials cc)
    {
        await using var dbContext = new OpenSshGuiDbContext();
        await dbContext.ConnectionCredentialsDtos.Where(e => e.Id == cc.Id).ExecuteDeleteAsync();
        await dbContext.SaveChangesAsync();
    }
}