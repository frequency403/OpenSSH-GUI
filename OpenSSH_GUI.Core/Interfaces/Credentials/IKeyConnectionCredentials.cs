#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

public interface IKeyConnectionCredentials : IConnectionCredentials
{
    [JsonIgnore] ISshKey? Key { get; set; }

    string KeyFilePath { get; }

    void RenewKey();
}