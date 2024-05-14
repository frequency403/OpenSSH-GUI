#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:38

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

public interface IMultiKeyConnectionCredentials : IConnectionCredentials
{
    [JsonIgnore]
    IEnumerable<ISshKey>? Keys { get; set; }
}