// File Created by: Oliver Schantz
// Created: 21.05.2024 - 14:05:58
// Last edit: 21.05.2024 - 14:05:58

using System.Collections.Generic;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Resources.Wrapper;

public class CredentialGroup
{
    public string Hostname { get; set; }
    public string Username { get; set; }
    public Dictionary<AuthType, List<ManagedCredential>> Credentials { get; set; }
    public string Display => $"{Username}@{Hostname}";
}