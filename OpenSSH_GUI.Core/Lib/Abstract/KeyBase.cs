#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:46
// Last edit: 15.05.2024 - 01:05:48

#endregion

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.Keys;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.PuttyKeyFile;

namespace OpenSSH_GUI.Core.Lib.Abstract;

public abstract class KeyBase : IKeyBase
{
    private IPrivateKeySource _keySource;

    protected KeyBase(string absoluteFilePath, string? password = null)
    {
        AbsoluteFilePath = absoluteFilePath;
        Filename = Path.GetFileNameWithoutExtension(AbsoluteFilePath);
        Password = password;
        try
        {
            _keySource = Path.GetExtension(AbsoluteFilePath) switch
            {
                var x when x.Contains("ppk") => new PuttyKeyFile(AbsoluteFilePath, Password),
                var x when x.Contains("pub") => new PrivateKeyFile(Path.ChangeExtension(AbsoluteFilePath, null),
                    Password),
                _ => new PrivateKeyFile(AbsoluteFilePath, Password)
            };
            if (password is not null) PasswordSuccess = true;
        }
        catch (SshPassPhraseNullOrEmptyException e)
        {
            Password = "";
            return;
        }
        catch (Exception e)
        {
            return;
        }
        var name = _keySource.HostKeyAlgorithms.FirstOrDefault()?.Name;
        Fingerprint = _keySource.FingerprintHash();
    }

    public bool NeedPassword => HasPassword && !PasswordSuccess;
    private bool PasswordSuccess { get; }
    public bool HasPassword => Password is not null;
    public string? Password { get; set; }
    public string Filename { get; }
    public string AbsoluteFilePath { get; private set; }
    public SshKeyFormat Format { get; protected init; }
    public string Fingerprint { get; protected set; }

    public ISshKey SetPassword(string password)
    {
        Password = password;
        return Path.GetExtension(AbsoluteFilePath) switch
        {
            var x when x.Contains("ppk") => new PpkKey(AbsoluteFilePath, password),
            var x when x.Contains("pub") => new SshPublicKey(AbsoluteFilePath, password),
            _ => new SshPrivateKey(Path.ChangeExtension(AbsoluteFilePath, null), password)
        };
    }
    
    public string ExportAuthorizedKeyEntry()
    {
        return ExportOpenSshPublicKey();
    }

    public IAuthorizedKey ExportAuthorizedKey()
    {
        return new AuthorizedKey(ExportAuthorizedKeyEntry());
    }

    public IPrivateKeySource GetRenciKeyType()
    {
        return _keySource;
    }

    public void DeleteKey()
    {
        if (Format is SshKeyFormat.OpenSSH && Path.GetExtension(AbsoluteFilePath).Contains("pub"))
        {
            File.Delete(Path.ChangeExtension(AbsoluteFilePath, null));
        }
        File.Delete(AbsoluteFilePath);
    }

    public string ExportOpenSshPublicKey()
    {
        return _keySource.ToOpenSshPublicFormat();
    }

    public string ExportOpenSshPrivateKey()
    {
        return _keySource.ToOpenSshFormat();
    }

    public string ExportPuttyPublicKey()
    {
        return _keySource.ToPuttyPublicFormat();
    }

    public string ExportPuttyPpkKey()
    {
        return _keySource.ToPuttyFormat();
    }

    public abstract string ExportTextOfKey();

    public Task ExportToDiskAsync(SshKeyFormat format)
    {
        return new Task(() => ExportToDisk(format));
    }

    public void ExportToDisk(SshKeyFormat format)
    {
        ExportToDisk(format, out _);
    }

    public void ExportToDisk(SshKeyFormat format, out ISshKey? key)
    {
        key = null;
        var privateFilePath = "";
        switch (format)
        {
            case SshKeyFormat.OpenSSH:
                privateFilePath = GetUniqueFilePath(Path.ChangeExtension(AbsoluteFilePath, null));
                var publicFilePath = Path.ChangeExtension(privateFilePath, ".pub");
                using (var privateWriter = new StreamWriter(privateFilePath, false))
                {
                    privateWriter.WriteAsync(ExportOpenSshPrivateKey());
                }

                using (var publicWriter = new StreamWriter(publicFilePath, false))
                {
                    publicWriter.WriteAsync(ExportOpenSshPublicKey());
                }

                key = new SshPublicKey(publicFilePath);
                break;
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
            default:
                privateFilePath = GetUniqueFilePath(Path.ChangeExtension(AbsoluteFilePath, ".ppk"));
                using (var privateWriter = new StreamWriter(privateFilePath, false))
                {
                    privateWriter.WriteAsync(ExportPuttyPpkKey());
                }

                key = new PpkKey(privateFilePath);
                break;
        }
    }

    public ISshKey? Convert(SshKeyFormat format)
    {
        return Convert(format, false, NullLogger.Instance);
    }

    public ISshKey? Convert(SshKeyFormat format, ILogger logger)
    {
        return Convert(format, false, logger);
    }

    public ISshKey? Convert(SshKeyFormat format, bool move, ILogger logger)
    {
        if (format.Equals(Format)) return null;
        ExportToDisk(format, out var key);
        if (this is IPpkKey && move)
            try
            {
                var directory = Directory.GetParent(AbsoluteFilePath)!.CreateSubdirectory("PPK");
                var newFileDestination = Path.Combine(directory.FullName, Path.GetFileName(AbsoluteFilePath));
                File.Move(AbsoluteFilePath, newFileDestination);
                AbsoluteFilePath = newFileDestination;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error moving the file");
            }

        if (key is null) logger.LogError("Error converting the key");
        return key;
    }

    private string GetUniqueFilePath(string originalFilePath)
    {
        if (File.Exists(originalFilePath))
            originalFilePath = Path.Combine(
                Path.GetDirectoryName(originalFilePath),
                $"{Path.GetFileNameWithoutExtension(originalFilePath)}_{DateTime.Now:yy_MM_dd_HH_mm}");

        return originalFilePath;
    }
}