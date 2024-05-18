// File Created by: Oliver Schantz
// Created: 18.05.2024 - 16:05:59
// Last edit: 18.05.2024 - 16:05:59

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Settings;
using OpenSSH_GUI.Core.Lib.Static;

namespace OpenSSH_GUI.Core.Database.DTO;

public class ConnectionCredentialsDto
{
    [Key]
    public int Id { get; set; }
    
    [Encrypted]
    public string Hostname { get; set; }
    [Encrypted]
    public string Username { get; set; }
    public int Port { get; set; }
    public AuthType AuthType { get; set; }
    
    public virtual List<SshKeyDto> KeyDtos { get; set; }
    
    [Encrypted]
    public string? Password { get; set; }
    public bool PasswordEncrypted { get; set; }

    public IConnectionCredentials ToCredentials()
    {
        return AuthType switch
        {
            AuthType.Key => new KeyConnectionCredentials(Hostname, Username, KeyDtos.First().ToKey()) {Id = this.Id},
            AuthType.Password => new PasswordConnectionCredentials(Hostname, Username, Password,
                PasswordEncrypted) {Id = this.Id},
            AuthType.MultiKey => new MultiKeyConnectionCredentials(Hostname, Username, KeyDtos.Select(e => e.ToKey()))
        };
    }
}