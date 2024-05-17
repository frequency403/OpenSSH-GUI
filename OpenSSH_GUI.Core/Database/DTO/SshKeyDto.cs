// File Created by: Oliver Schantz
// Created: 17.05.2024 - 08:05:19
// Last edit: 17.05.2024 - 08:05:19

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Keys;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Database.DTO;

public class SshKeyDto
{
    [Key]
    public string AbsolutePath { get; set; }
    
    public SshKeyFormat Format { get; set; }
    
    [Encrypted]
    public string? Password { get; set; }
    

    public ISshKey ToKey()
    {
        return AbsolutePath switch
        {
            var x when x.EndsWith("pub") => new SshPublicKey(AbsolutePath, Password),
            var x when x.EndsWith("ppk") => new PpkKey(AbsolutePath, Password),
            _ => new SshPrivateKey(AbsolutePath, Password)
        };
    }
}