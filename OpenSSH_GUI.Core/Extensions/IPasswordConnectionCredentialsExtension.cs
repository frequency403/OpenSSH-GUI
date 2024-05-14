// File Created by: Oliver Schantz
// Created: 14.05.2024 - 10:05:08
// Last edit: 14.05.2024 - 10:05:08

using System.Security.Cryptography;
using System.Text;
using OpenSSH_GUI.Core.Interfaces.Credentials;

namespace OpenSSH_GUI.Core.Extensions;

public static class IPasswordConnectionCredentialsExtension
{
    private static readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();
    
    public static void EncryptPassword(this IPasswordConnectionCredentials credentials)
    {
        if(credentials.EncryptedPassword) return;
        var prependBytes = new byte[3];
        var appendBytes = new byte[3];
        _generator.GetNonZeroBytes(prependBytes);
        _generator.GetNonZeroBytes(appendBytes);
        var bytes = prependBytes.ToList();
        bytes.AddRange(Encoding.UTF8.GetBytes(credentials.Password));
        bytes.AddRange(appendBytes);
        bytes.Reverse();
        credentials.Password = Convert.ToBase64String(bytes.ToArray());
        credentials.EncryptedPassword = true;
    }

    public static void DecryptPassword(this IPasswordConnectionCredentials credentials)
    {
        if (!credentials.EncryptedPassword) return;
        var rearrangedBytes = Convert.FromBase64String(credentials.Password).Reverse().ToList();
        rearrangedBytes.RemoveRange(0, 3);
        rearrangedBytes.RemoveRange(rearrangedBytes.Count -3, 3);
        credentials.Password = Encoding.UTF8.GetString(rearrangedBytes.ToArray());
        credentials.EncryptedPassword = false;
    }
}