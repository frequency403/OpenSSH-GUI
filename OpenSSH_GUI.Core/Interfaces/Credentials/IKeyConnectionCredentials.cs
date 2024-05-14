#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:38

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

public interface IKeyConnectionCredentials : IConnectionCredentials
{
    [JsonIgnore]
    ISshKey? Key { get; set; }

    public string KeyFilePath { get; }

    void RenewKey();
}