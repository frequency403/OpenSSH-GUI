#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:22

#endregion

using Microsoft.Extensions.Logging;
using Renci.SshNet;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Interfaces.Keys;

public interface ISshKey
{
    public SshKeyFormat Format { get; }
    public string AbsoluteFilePath { get; }
    public string KeyTypeString { get; }
    public string Filename { get; }
    public string Comment { get; }
    public bool IsPublicKey { get; }
    public bool IsPuttyKey { get; }
    public ISshKeyType KeyType { get; }
    public string Fingerprint { get; }

    public string ExportOpenSshPublicKey();
    public string ExportOpenSshPrivateKey();
    public string ExportPuttyPublicKey();
    public string ExportPuttyPpkKey();
    public string? ExportTextOfKey();
    public Task ExportToDiskAsync(SshKeyFormat format = SshKeyFormat.OpenSSH);
    public string ExportAuthorizedKeyEntry();
    public void ExportToDisk(SshKeyFormat format = SshKeyFormat.OpenSSH);
    public IPrivateKeySource GetRenciKeyType();
    public void DeleteKey();
    public ISshKey? Convert(SshKeyFormat format);
    public ISshKey? Convert(SshKeyFormat format, ILogger logger);
}