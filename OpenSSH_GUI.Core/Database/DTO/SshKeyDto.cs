// File Created by: Oliver Schantz
// Created: 17.05.2024 - 08:05:19
// Last edit: 17.05.2024 - 08:05:19

using System.ComponentModel.DataAnnotations;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Static;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Database.DTO;

public class SshKeyDto
{
    [Key]
    public int Id { get; set; }
    
    public string AbsolutePath { get; set; }
    
    public SshKeyFormat Format { get; set; }
    
    [Encrypted]
    public string? Password { get; set; }
    
    public virtual IEnumerable<ConnectionCredentialsDto> ConnectionCredentialsDto { get; set; }

    public ISshKey? ToKey()
    {
        return KeyFactory.FromPath(AbsolutePath, Password, Id);
    }
}