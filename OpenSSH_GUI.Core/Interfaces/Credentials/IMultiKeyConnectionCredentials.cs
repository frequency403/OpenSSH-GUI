#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

public interface IMultiKeyConnectionCredentials : IConnectionCredentials
{
    [JsonIgnore] IEnumerable<ISshKey>? Keys { get; set; }
}