#region CopyrightNotice
// File Created by: Oliver Schantz
// Created: 17.05.2024 - 00:05:10
// Last edit: 17.05.2024 - 00:05:10
#endregion

using System.Security.Cryptography;
using System.Text;

namespace OpenSSH_GUI.Core.Extensions;

public static class StringEnAndDecryption
{
    private static RandomNumberGenerator RNG = RandomNumberGenerator.Create();
    
    public static string Encrypt(this string input)
    {
        var contentBytes = Encoding.Unicode.GetBytes(input);
        var randomBytes = new byte[4];
        RNG.GetNonZeroBytes(randomBytes);
        var byteList = randomBytes.Reverse().ToList();
        byteList.AddRange(contentBytes.Reverse());
        RNG.GetNonZeroBytes(randomBytes);
        byteList.AddRange(randomBytes);
        return Convert.ToBase64String(byteList.ToArray());
    }

    public static string Decrypt(this string input)
    {
        var fromB64 = Convert.FromBase64String(input).ToList();
        fromB64.RemoveRange(0, 4);
        fromB64.RemoveRange(fromB64.Count -4, 4);
        var fromB64Array = fromB64.ToArray().Reverse().ToArray();
        return Encoding.Unicode.GetString(fromB64Array);
    }
}