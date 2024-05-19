#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

/// <summary>
/// Represents the interface for multi-key connection credentials.
/// </summary>
public interface IMultiKeyConnectionCredentials : IConnectionCredentials
{
    /// <summary>
    /// Represents the credentials for a multi-key connection.
    /// </summary>
    [JsonIgnore] IEnumerable<ISshKey>? Keys { get; set; }
}