// File Created by: Oliver Schantz
// Created: 18.05.2024 - 13:05:38
// Last edit: 18.05.2024 - 13:05:38

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Misc;

public readonly record struct SshKeyGenerateParams
{
    public SshKeyGenerateParams(KeyType type, 
        SshKeyFormat format, 
        string? fileName = null, 
        string? filePath = null, 
        string? password = null,
        string? comment = null,
        int? keyLength = null)
    {
        FileName = fileName ?? Path.ChangeExtension(Path.GetFileName("id_" + Path.GetTempFileName()), null);
        FilePath = filePath ?? SshConfigFilesExtension.GetBaseSshPath();
        KeyType = type;
        Comment = comment ?? "by OpenSSH-GUI";
        KeyFormat = format;
        KeyLength = keyLength ?? (int)type;
        Password = password;
    }
    /// <summary>
    /// Gets or sets the file name for the SSH key.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Represents the file path for an SSH key.
    /// </summary>
    /// <remarks>Defaults to ~/.shh directory when null</remarks>
    public string FilePath { get; }

    /// <summary>
    /// Represents the type of SSH key.
    /// </summary>
    public KeyType KeyType { get; }

    /// <summary>
    /// Represents a comment associated with an SSH key.
    /// </summary>
    /// <remarks>Defaults to "by OpenSSH-GUI" when null</remarks>
    public string Comment { get; }

    /// <summary>
    /// Specifies the format for SSH key generation.
    /// </summary>
    public SshKeyFormat KeyFormat { get; }

    /// <summary>
    /// Represents the length of an SSH key.
    /// </summary>
    public int KeyLength { get; }

    /// <summary>
    /// Represents the password associated with an SSH key.
    /// </summary>
    public string? Password { get; }

    public string FullFilePath => Path.Combine(FilePath, FileName);
}