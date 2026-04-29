using System.Reflection;
using OpenSSH_GUI.Core.Extensions;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
///     Represents metadata and operations related to a specific SSH key file.
///     All properties are computed eagerly at construction time and are immutable thereafter.
/// </summary>
public sealed record SshKeyFileInformation
{
    private static readonly SshKeyFormat[] AvailableFormats = Enum.GetValues<SshKeyFormat>();

    /// <summary>
    ///     Initializes a new instance of <see cref="SshKeyFileInformation" />
    ///     and eagerly computes all metadata from the provided <paramref name="keyFileSource" />.
    /// </summary>
    /// <param name="keyFileSource">The source descriptor for the SSH key file.</param>
    public SshKeyFileInformation(SshKeyFileSource keyFileSource)
    {
        KeyFileSource = keyFileSource;
        CanChangeFileName = keyFileSource is { ProvidedByConfig: false };

        FileInfo = !string.IsNullOrWhiteSpace(keyFileSource.AbsolutePath)
            ? new FileInfo(keyFileSource.AbsolutePath)
            : new FileInfo(Assembly.GetExecutingAssembly().Location);

        FileName = FileInfo.Name;
        FullFileName = FileInfo.FullName;
        DirectoryName = FileInfo.DirectoryName;
        Exists = FileInfo.Exists;

        CurrentFormat = FileInfo.Extension == PathExtensions.PuttyKeyFileExtension
            ? SshKeyFormat.PuTTYv3
            : SshKeyFormat.OpenSSH;

        IsOpenSshKey = CurrentFormat == SshKeyFormat.OpenSSH;

        PublicKeyFileName = IsOpenSshKey
            ? Path.ChangeExtension(FullFileName, PathExtensions.OpenSshPublicKeyFileExtension)
            : null;

        AvailableFormatsForConversion = AvailableFormats
            .Where(f => f != CurrentFormat)
            .ToArray();

        DefaultConversionFormat = AvailableFormatsForConversion.Contains(SshKeyFormat.OpenSSH)
            ? SshKeyFormat.OpenSSH
            : AvailableFormatsForConversion.FirstOrDefault();

        Files = new[] { FullFileName, PublicKeyFileName }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => new FileInfo(p!))
            .ToArray();
    }

    /// <inheritdoc cref="SshKeyFileSource" />
    public SshKeyFileSource KeyFileSource { get; }

    /// <summary>Gets the <see cref="FileInfo" /> for the private key file.</summary>
    public FileInfo FileInfo { get; }

    /// <summary>Gets the file name including extension.</summary>
    public string FileName { get; }

    /// <summary>Gets the full absolute file path.</summary>
    public string FullFileName { get; }

    /// <summary>Gets the directory containing the key file, or <c>null</c> if unavailable.</summary>
    public string? DirectoryName { get; }

    /// <summary>Gets whether the key file exists on disk.</summary>
    public bool Exists { get; }

    /// <summary>Gets the detected format of the key file.</summary>
    public SshKeyFormat CurrentFormat { get; }

    /// <summary>Gets whether the key is in OpenSSH format.</summary>
    public bool IsOpenSshKey { get; }

    /// <summary>
    ///     Gets the absolute path of the associated public key file,
    ///     or <c>null</c> if the key is not in OpenSSH format.
    /// </summary>
    public string? PublicKeyFileName { get; }

    /// <summary>Gets all formats this key can be converted to, excluding its current format.</summary>
    public SshKeyFormat[] AvailableFormatsForConversion { get; }

    /// <summary>
    ///     Gets the recommended default conversion target.
    ///     Prefers OpenSSH; falls back to the first available format.
    /// </summary>
    public SshKeyFormat DefaultConversionFormat { get; }

    /// <summary>
    ///     Gets all files associated with this key (private + public if applicable).
    /// </summary>
    public FileInfo[] Files { get; }

    /// <summary>Gets whether the file name can be changed by the user.</summary>
    public bool CanChangeFileName { get; }

    /// <inheritdoc />
    public bool Equals(SshKeyFileInformation? other) => other is not null && KeyFileSource == other.KeyFileSource;

    /// <inheritdoc />
    public override int GetHashCode() => KeyFileSource.GetHashCode();
}