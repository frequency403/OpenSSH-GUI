#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:24

#endregion

using System.Security.Cryptography;
using System.Text;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Credentials;

namespace OpenSSH_GUI.Core.Extensions;

public static class ConnectionCredentialsExtensions
{
    
    
    
    public static void EncryptPassword(this IConnectionCredentials credentials)
    {
        switch (credentials)
        {
            case IPasswordConnectionCredentials pass:
                if (pass.EncryptedPassword) return;
                pass.Password = StringExtensions.Encrypt(pass.Password);
                pass.EncryptedPassword = true;
                break;
            case IKeyConnectionCredentials key:
                if (key.PasswordEncrypted) return;
                key.KeyPassword = StringExtensions.Encrypt(key.KeyPassword);
                key.PasswordEncrypted = true;
                break;
            case IMultiKeyConnectionCredentials keys:
                if (keys.PasswordsEncrypted) return;
                keys.Passwords = keys.Passwords.Select(e =>
                {
                    e = new KeyValuePair<string, string?>(e.Key, StringExtensions.Encrypt(e.Value));
                    return e;
                }).ToDictionary();
                keys.PasswordsEncrypted = true;
                break;
        }
    }

    public static void DecryptPassword(this IConnectionCredentials credentials)
    {
        switch (credentials)
        {
            case IPasswordConnectionCredentials pass:
                if (!pass.EncryptedPassword) return;
                pass.Password = StringExtensions.Decrypt(pass.Password);
                pass.EncryptedPassword = true;
                break;
            case IKeyConnectionCredentials key:
                if (!key.PasswordEncrypted) return;
                key.KeyPassword = StringExtensions.Decrypt(key.KeyPassword);
                key.PasswordEncrypted = true;
                break;
            case IMultiKeyConnectionCredentials keys:
                if (!keys.PasswordsEncrypted) return;
                keys.Passwords = keys.Passwords.Select(e =>
                {
                    e = new KeyValuePair<string, string?>(e.Key, StringExtensions.Decrypt(e.Value));
                    return e;
                }).ToDictionary();
                keys.PasswordsEncrypted = true;
                keys.Keys = keys.Keys.Select(e =>
                {
                    if (e.NeedPassword)
                        e = e.SetPassword(keys.Passwords.FirstOrDefault(f => string.Equals(f.Key, e.AbsoluteFilePath))
                            .Value);

                    return e;
                });
                keys.PasswordsEncrypted = false;
                break;
        }
    }

    public static IEnumerable<KeyConnectionCredentials> ToKeyConnectionCredentials(
        this IMultiKeyConnectionCredentials multiKeyConnectionCredentials)
    {
        return multiKeyConnectionCredentials.Keys.Select(key => new KeyConnectionCredentials(multiKeyConnectionCredentials.Hostname, multiKeyConnectionCredentials.Username,
            key));
    }

    public static MultiKeyConnectionCredentials ToMultiKeyConnectionCredentials(
        this IEnumerable<IKeyConnectionCredentials> keyConnectionCredentials)
    {
        keyConnectionCredentials = keyConnectionCredentials.ToArray();
        var firstElement = keyConnectionCredentials.First();
        foreach (var kcc in keyConnectionCredentials)
        {
            if (kcc.Key.NeedPassword)
            {
                kcc.RenewKey(kcc.PasswordEncrypted ? StringExtensions.Decrypt(kcc.KeyPassword) : kcc.KeyPassword); 
            }
        }
        return new MultiKeyConnectionCredentials(firstElement.Hostname, firstElement.Username,
            keyConnectionCredentials.Select(e => e.Key));
    }
}