// File Created by: Oliver Schantz
// Created: 21.05.2024 - 15:05:17
// Last edit: 21.05.2024 - 15:05:18

using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Interfaces.Credentials;

namespace OpenSSH_GUI.Resources.Wrapper;

public class ManagedCredential
{
    public bool InUse { get; set; }
    public IConnectionCredentials Credentials { get; set; }
}