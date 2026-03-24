using System.Diagnostics.CodeAnalysis;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
///     Represents metadata and operations related to a specific SSH key file.
///     Provides access to the associated private and potential public key file information,
///     as well as details about key format and available conversion options.
/// </summary>
public class SshKeyFileInformation(SshKeyFileSource keyFileSource)
{
    /// <summary>
    ///     Represents the internal <see cref="FileInfo" /> object associated with the SSH key file.
    ///     This variable is used to perform various file operations and retrieve metadata of the specified SSH key file path.
    /// </summary>
    private readonly FileInfo _fileInfo = new(keyFileSource.AbsolutePath);

    public SshKeyFileSource KeyFileSource => keyFileSource;
    public bool CanChangeFileName => !keyFileSource.ProvidedByConfig;

    /// <summary>
    ///     Indicates whether the SSH key file associated with the instance conforms to the OpenSSH format.
    /// </summary>
    /// <remarks>
    ///     This property evaluates the format of the SSH key file based on its current extension or metadata.
    ///     If the key format matches <see cref="SshKeyFormat.OpenSSH" />, the property returns <c>true</c>;
    ///     otherwise, it returns <c>false</c>.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(PublicKeyFileName))]
    private bool IsOpenSshKey => CurrentFormat is SshKeyFormat.OpenSSH;

    /// <summary>
    ///     Gets the name of the file represented by the current instance of
    ///     <see cref="SshKeyFileInformation" />.
    /// </summary>
    /// <remarks>
    ///     This property provides the file name, including its extension, as a string.
    ///     It is derived from the <c>FileInfo</c> instance initialized with the file path.
    /// </remarks>
    public string Name => _fileInfo.Name;

    /// <summary>
    ///     Gets the file name of the public key associated with the current SSH key file,
    ///     if the key format is OpenSSH. If the current key format is not OpenSSH, this property returns <c>null</c>.
    /// </summary>
    /// <remarks>
    ///     The public key file name is constructed by changing the extension of the current file name
    ///     to ".pub" if the key format is OpenSSH. For other formats, there is no associated public key file.
    /// </remarks>
    /// <value>
    ///     A string representing the file name of the public key for OpenSSH keys, or <c>null</c>
    ///     if the key is in a format other than OpenSSH.
    /// </value>
    public string? PublicKeyFileName => IsOpenSshKey ? Path.ChangeExtension(_fileInfo.FullName, "pub") : null;

    /// <summary>
    ///     Gets the full path of the SSH key file, including the file name and extension.
    /// </summary>
    /// <remarks>
    ///     This property provides the complete path to the file as a string, based on the
    ///     <see cref="FileInfo.FullName" /> property. It represents the file location on
    ///     the filesystem.
    /// </remarks>
    public string FullName => _fileInfo.FullName;

    /// <summary>
    ///     Indicates whether the associated SSH key file exists in the file system.
    /// </summary>
    /// <remarks>
    ///     This property checks the existence of the file represented by this instance
    ///     by verifying its status in the file system. It returns <c>true</c> if the file
    ///     is found, and <c>false</c> otherwise. The property is useful for validation
    ///     and ensures that operations on the file are only performed when it is available.
    /// </remarks>
    public bool Exists => _fileInfo.Exists;

    /// <summary>
    ///     Gets the name of the directory where the SSH key file is located.
    /// </summary>
    /// <remarks>
    ///     This property retrieves the full path of the directory containing the SSH key
    ///     file associated with this instance. If the file is not associated with a valid directory,
    ///     the property may return <c>null</c>.
    /// </remarks>
    public string? DirectoryName => _fileInfo.DirectoryName;

    /// <summary>
    ///     Represents a collection of key-related files associated with an SSH key.
    /// </summary>
    public FileInfo[] Files => new[] { FullName, PublicKeyFileName }.Where(e => !string.IsNullOrEmpty(e))
        .Select(e => new FileInfo(e!)).ToArray();

    /// <summary>
    ///     Gets the current format of the SSH key file.
    /// </summary>
    /// <remarks>
    ///     The <c>CurrentFormat</c> property determines the format of the SSH key file
    ///     based on its file extension. It returns <c>SshKeyFormat.PuTTYv3</c> if the
    ///     file extension is ".ppk", otherwise it defaults to <c>SshKeyFormat.OpenSSH</c>.
    ///     This property is used to identify the key format for further operations.
    /// </remarks>
    public SshKeyFormat CurrentFormat => _fileInfo.Extension switch
    {
        ".ppk" => SshKeyFormat.PuTTYv3,
        _ => SshKeyFormat.OpenSSH
    };

    /// <summary>
    ///     Gets the default format to which the current SSH key can be converted.
    /// </summary>
    /// <remarks>
    ///     This property evaluates the list of available formats for conversion, and selects the default based on the
    ///     following criteria:
    ///     If the OpenSSH format is available for conversion, it will be chosen as the default.
    ///     Otherwise, the highest-ranking format in the list of available formats (in descending order) is selected.
    /// </remarks>
    /// <value>
    ///     The <see cref="SshKeyFormat" /> representing the default conversion format for the SSH key.
    /// </value>
    /// <seealso cref="AvailableFormatsForConversion" />
    public SshKeyFormat DefaultConversionFormat => AvailableFormatsForConversion.Contains(SshKeyFormat.OpenSSH)
        ? SshKeyFormat.OpenSSH
        : AvailableFormatsForConversion.OrderDescending().First();

    /// <summary>
    ///     Gets the collection of SSH key formats that the current key can be converted to,
    ///     excluding its current format.
    /// </summary>
    /// <remarks>
    ///     This property provides a dynamic list of possible target formats for conversion
    ///     based on the current format of the key. It ensures that the current format is
    ///     excluded from the list of available options. Examples of SSH key formats include
    ///     OpenSSH and PuTTYv3.
    /// </remarks>
    public IEnumerable<SshKeyFormat> AvailableFormatsForConversion =>
        Enum.GetValues<SshKeyFormat>().Where(e => e != CurrentFormat);
}