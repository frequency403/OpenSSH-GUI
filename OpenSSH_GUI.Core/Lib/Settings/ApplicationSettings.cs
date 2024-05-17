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
    DirectoryCrawler crawler,
    OpenSshGuiDbContext dbContext) : IApplicationSettings
{
    private readonly SettingsFile _settingsFile = dbContext.Settings.AsNoTracking().First();
    /// <summary>
    /// Initializes the application settings.
    /// </summary>
    public void Init()
    {
        try
        {
            DecryptAllPasswords();
            UpdateServerCredentials();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while initializing application settings! Continuing with default settings");
        }

        crawler.Refresh();
    }
    
    /// <summary>
    /// Updates the server credentials in the settings file.
    /// </summary>
    private void UpdateServerCredentials()
    {
        var settings = dbContext.Settings.Update(_settingsFile).Entity;
        var duplicates = settings.LastUsedServers.Where(e => e.AuthType == AuthType.Key)
            .Select(f => f as KeyConnectionCredentials)
            .Duplicates(g => g.Hostname)
            .Duplicates(h => h.Username)
            .ToArray();

        if (duplicates.Length > 0)
        {
            foreach (var item in duplicates)
                settings.LastUsedServers.Remove(item);

            var converted = duplicates.ToMultiKeyConnectionCredentials();
            settings.LastUsedServers.Add(converted);
            crawler.UpdateKeys(converted);
        }
        dbContext.SaveChanges();
    }
    
    /// <summary>
    /// Adds a known server to the settings file.
    /// </summary>
    /// <param name="credentials">The connection credentials of the server to add.</param>
    /// <returns>True if the server was added successfully, false otherwise.</returns>
    public bool AddKnownServer(IConnectionCredentials credentials)
    {
        return AddKnownServerAsync(credentials).Result;
    }

    /// <summary>
    /// Adds a known server to the settings file asynchronously.
    /// </summary>
    /// <param name="credentials">The connection credentials for the server.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a boolean indicating whether the server was added successfully or not.</returns>
    public async Task<bool> AddKnownServerAsync(IConnectionCredentials credentials)
    {
        try
        {
            ShrinkKnownServers();
            var settings = dbContext.Settings.Update(_settingsFile).Entity;
            var found = settings.LastUsedServers.Where(e =>
                string.Equals(e.Hostname, credentials.Hostname) && string.Equals(e.Username, credentials.Username));
            if (found.Any(e => e.AuthType.Equals(credentials.AuthType))) return false;
            if (credentials is MultiKeyConnectionCredentials multiKeyConnectionCredentials)
            {
                settings.LastUsedServers.AddRange(multiKeyConnectionCredentials.ToKeyConnectionCredentials());
                crawler.UpdateKeys(multiKeyConnectionCredentials);
            }
            else
            {
                settings.LastUsedServers.Add(credentials);
            }
            EncryptAllPasswords();
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
    /// Encrypts the passwords of all the servers in the settings file.
    /// </summary>
    private void EncryptAllPasswords()
    {
        var settings = dbContext.Settings.Update(_settingsFile).Entity;
        foreach (var server in settings.LastUsedServers)
        {
            server.EncryptPassword();
        }
        dbContext.SaveChanges();
    }

    /// <summary>
    /// Decrypts the passwords of all servers in the settings file.
    /// </summary>
    private void DecryptAllPasswords()
    {
        var settings = dbContext.Settings.Update(_settingsFile).Entity;
        foreach (var server in settings.LastUsedServers)
        {
           server.DecryptPassword();
        }

        dbContext.SaveChanges();
    }
    
    /// <summary>
    /// Shrinks the list of known servers if it exceeds the maximum allowed number.
    /// </summary>
    /// <returns>True if the list was successfully shrunk, false otherwise.</returns>
    private bool ShrinkKnownServers()
    {
        try
        {
            var settings = dbContext.Settings.Update(_settingsFile).Entity;
            if (settings.LastUsedServers.Count <= settings.MaxSavedServers) return false;
            var baseValue = settings.LastUsedServers.Count - settings.MaxSavedServers;
            for (var i = 0; i < baseValue; i++)
                settings.LastUsedServers.RemoveAt(i);
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