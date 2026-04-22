using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reflection;
using OpenSSH_GUI.Core.Extensions;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
///     Represents metadata and operations related to a specific SSH key file.
///     Provides access to the associated private and potential public key file information,
///     as well as details about key format and available conversion options.
/// </summary>
public sealed partial record SshKeyFileInformation : ReactiveRecord, IDisposable
{
    private static readonly SshKeyFormat[] AvailableFormats = Enum.GetValues<SshKeyFormat>();
    private readonly CompositeDisposable _disposables = new();

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
    [ObservableAsProperty(ReadOnly = true)]
    private SshKeyFormat[] _availableFormatsForConversion = [];

    [ObservableAsProperty(ReadOnly = true)]
    private bool _canChangeFileName;

    /// <summary>
    ///     Gets the current format of the SSH key file.
    /// </summary>
    /// <remarks>
    ///     The <c>CurrentFormat</c> property determines the format of the SSH key file
    ///     based on its file extension. It returns <c>SshKeyFormat.PuTTYv3</c> if the
    ///     file extension is ".ppk", otherwise it defaults to <c>SshKeyFormat.OpenSSH</c>.
    ///     This property is used to identify the key format for further operations.
    /// </remarks>
    [ObservableAsProperty(ReadOnly = true)]
    private SshKeyFormat _currentFormat = SshKeyFormat.OpenSSH;

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
    [ObservableAsProperty(ReadOnly = true)]
    private SshKeyFormat _defaultConversionFormat = SshKeyFormat.OpenSSH;

    /// <summary>
    ///     Gets the name of the directory where the SSH key file is located.
    /// </summary>
    /// <remarks>
    ///     This property retrieves the full path of the directory containing the SSH key
    ///     file associated with this instance. If the file is not associated with a valid directory,
    ///     the property may return <c>null</c>.
    /// </remarks>
    [ObservableAsProperty(ReadOnly = true)]
    private string? _directoryName;

    /// <summary>
    ///     Indicates whether the associated SSH key file exists in the file system.
    /// </summary>
    /// <remarks>
    ///     This property checks the existence of the file represented by this instance
    ///     by verifying its status in the file system. It returns <c>true</c> if the file
    ///     is found, and <c>false</c> otherwise. The property is useful for validation
    ///     and ensures that operations on the file are only performed when it is available.
    /// </remarks>
    [ObservableAsProperty(ReadOnly = true)]
    private bool _exists;

    /// <summary>
    ///     Represents the internal <see cref="FileInfo" /> object associated with the SSH key file.
    ///     This variable is used to perform various file operations and retrieve metadata of the specified SSH key file path.
    /// </summary>
    [ObservableAsProperty(ReadOnly = true)]
    private FileInfo _fileInfo = new(Assembly.GetExecutingAssembly().Location);

    /// <summary>
    ///     Gets the name of the file represented by the current instance of
    ///     <see cref="SshKeyFileInformation" />.
    /// </summary>
    /// <remarks>
    ///     This property provides the file name, including its extension, as a string.
    ///     It is derived from the <c>FileInfo</c> instance initialized with the file path.
    /// </remarks>
    [ObservableAsProperty(ReadOnly = true)]
    private string _fileName = string.Empty;

    /// <summary>
    ///     Represents a collection of key-related files associated with an SSH key.
    /// </summary>
    [ObservableAsProperty(ReadOnly = true)]
    private FileInfo[] _files = [];

    /// <summary>
    ///     Gets the full path of the SSH key file, including the file name and extension.
    /// </summary>
    /// <remarks>
    ///     This property provides the complete path to the file as a string, based on the
    ///     <see cref="FileInfo.FullName" /> property. It represents the file location on
    ///     the filesystem.
    /// </remarks>
    [ObservableAsProperty(ReadOnly = true)]
    private string _fullFileName = string.Empty;

    /// <summary>
    ///     Indicates whether the SSH key file associated with the instance conforms to the OpenSSH format.
    /// </summary>
    /// <remarks>
    ///     This property evaluates the format of the SSH key file based on its current extension or metadata.
    ///     If the key format matches <see cref="SshKeyFormat.OpenSSH" />, the property returns <c>true</c>;
    ///     otherwise, it returns <c>false</c>.
    /// </remarks>
    [ObservableAsProperty(ReadOnly = true)]
    private bool _isOpenSshKey;

    [Reactive(SetModifier = AccessModifier.Private)]
    private SshKeyFileSource _keyFileSource;

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
    [ObservableAsProperty(ReadOnly = true)]
    private string? _publicKeyFileName;

    public SshKeyFileInformation(SshKeyFileSource keyFileSource)
    {
        KeyFileSource = keyFileSource;
        var initialFileInfo = !string.IsNullOrWhiteSpace(keyFileSource.AbsolutePath)
            ? new FileInfo(keyFileSource.AbsolutePath)
            : new FileInfo(Assembly.GetExecutingAssembly().Location);

        var initialFormat = initialFileInfo.Extension == SshKeyFormatExtension.PuttyKeyFileExtension
            ? SshKeyFormat.PuTTYv3
            : SshKeyFormat.OpenSSH;

        var initialIsOpenSsh = initialFormat == SshKeyFormat.OpenSSH;

        var initialPublicKeyFileName = initialIsOpenSsh
            ? Path.ChangeExtension(
                initialFileInfo.FullName,
                SshKeyFormatExtension.OpenSshPublicKeyFileExtension)
            : null;

        var initialAvailableFormats = AvailableFormats
            .Where(f => f != initialFormat)
            .ToArray();

        var initialDefaultFormat = initialAvailableFormats.Contains(SshKeyFormat.OpenSSH)
            ? SshKeyFormat.OpenSSH
            : initialAvailableFormats.FirstOrDefault();

        var initialFiles = new[] { initialFileInfo.FullName, initialPublicKeyFileName }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => new FileInfo(p!))
            .ToArray();

        var keyFileSourceChanged = this.WhenAnyValue(x => x.KeyFileSource);
        var fileInfoChanged = this.WhenAnyValue(x => x.FileInfo);
        var currentFormatChanged = this.WhenAnyValue(x => x.CurrentFormat);

        _canChangeFileNameHelper = keyFileSourceChanged
            .Select(src => src is { ProvidedByConfig: false })
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.CanChangeFileName,
                initialValue: keyFileSource is { ProvidedByConfig: false })
            .DisposeWith(_disposables);

        _fileInfoHelper = keyFileSourceChanged
            .Select(src => !string.IsNullOrWhiteSpace(src.AbsolutePath)
                ? new FileInfo(src.AbsolutePath)
                : new FileInfo(Assembly.GetExecutingAssembly().Location))
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.FileInfo, initialValue: initialFileInfo)
            .DisposeWith(_disposables);

        _currentFormatHelper = fileInfoChanged
            .Select(info => info.Extension switch
            {
                SshKeyFormatExtension.PuttyKeyFileExtension => SshKeyFormat.PuTTYv3,
                _ => SshKeyFormat.OpenSSH
            })
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.CurrentFormat, initialValue: initialFormat)
            .DisposeWith(_disposables);

        _fileNameHelper = fileInfoChanged
            .Select(info => info.Name)
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.FileName, initialValue: initialFileInfo.Name)
            .DisposeWith(_disposables);

        _fullFileNameHelper = fileInfoChanged
            .Select(info => info.FullName)
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.FullFileName, initialValue: initialFileInfo.FullName)
            .DisposeWith(_disposables);

        _existsHelper = fileInfoChanged
            .Select(info => info.Exists)
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.Exists, initialValue: initialFileInfo.Exists)
            .DisposeWith(_disposables);

        _directoryNameHelper = fileInfoChanged
            .Select(info => info.DirectoryName)
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.DirectoryName, initialValue: initialFileInfo.DirectoryName)
            .DisposeWith(_disposables);

        _isOpenSshKeyHelper = currentFormatChanged
            .Select(fmt => fmt == SshKeyFormat.OpenSSH)
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.IsOpenSshKey, initialValue: initialIsOpenSsh)
            .DisposeWith(_disposables);

        _publicKeyFileNameHelper = this
            .WhenAnyValue(x => x.IsOpenSshKey, x => x.FileInfo)
            .Select(tuple =>
            {
                var (isOpenSsh, info) = tuple;
                if (!isOpenSsh) return null;
                var path = info?.FullName;
                return !string.IsNullOrWhiteSpace(path)
                    ? Path.ChangeExtension(path, SshKeyFormatExtension.OpenSshPublicKeyFileExtension)
                    : null;
            })
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.PublicKeyFileName, initialValue: initialPublicKeyFileName)
            .DisposeWith(_disposables);

        _availableFormatsForConversionHelper = currentFormatChanged
            .Select(e => AvailableFormats.Where(f => f != e).ToArray())
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.AvailableFormatsForConversion,
                initialValue: initialAvailableFormats)
            .DisposeWith(_disposables);

        _defaultConversionFormatHelper = this
            .WhenAnyValue(x => x.AvailableFormatsForConversion)
            .Select(list =>
            {
                if (list.Length == 0 || list.Contains(SshKeyFormat.OpenSSH))
                    return SshKeyFormat.OpenSSH;
                return list[0];
            })
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.DefaultConversionFormat,
                initialValue: initialDefaultFormat)
            .DisposeWith(_disposables);

        _filesHelper = this
            .WhenAnyValue(x => x.FullFileName, x => x.PublicKeyFileName)
            .Select(tuple => new[] { tuple.Item1, tuple.Item2 }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => new FileInfo(p!))
                .ToArray())
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.Files, initialValue: initialFiles)
            .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}