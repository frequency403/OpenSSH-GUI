#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Misc;

namespace OpenSSH_GUI.Core.Lib.Settings;

/// <summary>
/// Represents the application settings.
/// </summary>
public class ApplicationSettings(
    ILogger<IApplicationSettings> logger,
    OpenSshGuiDbContext dbContext) : IApplicationSettings
{
    private Settings settings;
    /// <summary>
    /// Initializes the application settings.
    /// </summary>
    public void Init()
    {
        try
        {
            logger.LogInformation("Searching for duplicates in the database.....");
            settings = dbContext.Settings.First();
            var deleteCount = dbContext.ConnectionCredentialsDtos
                .Where(e => e.AuthType == AuthType.Key)
                .GroupBy(f => new
                {
                    ((KeyConnectionCredentials)f.ToCredentials()).Hostname,
                    ((KeyConnectionCredentials)f.ToCredentials()).Username
                })
                .Where(group => group.Count() > 1)
                .Select(group => group.Key).ExecuteDelete();
            if (deleteCount > 0)
            {
                logger.LogInformation("Deleted {delCount} duplicates from the database", deleteCount);
            }
            else
            {
                logger.LogInformation("No duplicates were found in the database");
            }
            dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while initializing application settings! Continuing with default settings");
        }
    }
    
    /// <summary>
    /// Adds a known server
    /// </summary>
    /// <param name="credentials">The connection credentials of the server to add.</param>
    /// <returns>True if the server was added successfully, false otherwise.</returns>
    public bool AddKnownServer(IConnectionCredentials credentials)
    {
        return AddKnownServerAsync(credentials).Result;
    }

    private bool HasDuplicateEntry(IConnectionCredentials credentials)
    {
        return dbContext.ConnectionCredentialsDtos
            .Any(e => e.Hostname == credentials.Hostname &&
                      e.Username == credentials.Username &&
                      e.AuthType == credentials.AuthType);
    }
    
    /// <summary>
    /// Adds a known server to settings
    /// </summary>
    /// <param name="credentials">The connection credentials for the server.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a boolean indicating whether the server was added successfully or not.</returns>
    public async Task<bool> AddKnownServerAsync(IConnectionCredentials credentials)
    {
        try
        {
            ShrinkKnownServers();
            if (HasDuplicateEntry(credentials)) return false;
            dbContext.ConnectionCredentialsDtos.Add(credentials.ToDto());
            //@TODO !Headache with EF!
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error adding known server to database");
            return false;
        }

        return true;
    }
    
    
    /// <summary>
    /// Shrinks the list of known servers if it exceeds the maximum allowed number.
    /// </summary>
    /// <returns>True if the list was successfully shrunk, false otherwise.</returns>
    private bool ShrinkKnownServers()
    {
        try
        {
            if (dbContext.ConnectionCredentialsDtos.Count() <= settings.MaxSavedServers) return false;
            var baseValue = dbContext.ConnectionCredentialsDtos.Count() - settings.MaxSavedServers;
            dbContext.ConnectionCredentialsDtos.OrderByDescending(e => e.Id).Take(baseValue).ExecuteDelete();
            dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error shrinking known servers");
            return false;
        }

        return true;
    }
}