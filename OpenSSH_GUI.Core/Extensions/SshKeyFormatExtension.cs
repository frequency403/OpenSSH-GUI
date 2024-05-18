// File Created by: Oliver Schantz
// Created: 18.05.2024 - 14:05:52
// Last edit: 18.05.2024 - 14:05:52

using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Extensions;

public static class SshKeyFormatExtension
{
    private const string OpenSshPublicKeyExtension = ".pub";
    private const string PuttyKeyExtension = ".ppk";
    
    public static string? GetExtension(this SshKeyFormat format, bool @public = true) => format switch
    {
        SshKeyFormat.OpenSSH => OpenSshPublicKeyExtension,
        SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => PuttyKeyExtension,
        { } when !@public => null,
        _ => null
    };

    public static string ChangeExtension(this SshKeyFormat format, string path, bool @public = true) => format switch
    {
        SshKeyFormat.OpenSSH => Path.ChangeExtension(path, format.GetExtension(@public)),
        SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => Path.ChangeExtension(path, format.GetExtension(@public)),
        _ => path
    };
}