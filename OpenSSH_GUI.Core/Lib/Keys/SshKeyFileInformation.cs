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
        var keyFileSourceChanged = this.WhenAnyValue(vm => vm.KeyFileSource)
            .ObserveOn(AvaloniaScheduler.Instance);
        var fileInfoChanged = this.WhenAnyValue(vm => vm.FileInfo)
            .ObserveOn(AvaloniaScheduler.Instance);
        var currentFormatChanged = this.WhenAnyValue(vm => vm.CurrentFormat)
            .ObserveOn(AvaloniaScheduler.Instance);
        
        _canChangeFileNameHelper = keyFileSourceChanged
            .Select(fileSource => !fileSource.ProvidedByConfig)
            .ToProperty(this, vm => vm.CanChangeFileName, initialValue: false)
            .DisposeWith(_disposables);
        
        _fileInfoHelper = keyFileSourceChanged
            .Select(fileSource => new FileInfo(fileSource.AbsolutePath))
            .ToProperty(this, vm => vm.FileInfo, initialValue: new FileInfo(Assembly.GetExecutingAssembly().Location))
            .DisposeWith(_disposables);
        
       _currentFormatHelper = fileInfoChanged
           .Select(info => info.Extension switch
           {
               SshKeyFormatExtension.PuttyKeyFileExtension => SshKeyFormat.PuTTYv3,
               _ => SshKeyFormat.OpenSSH
           }).ToProperty(this, vm => vm.CurrentFormat)
           .DisposeWith(_disposables);

       _fileNameHelper = fileInfoChanged
           .Select(e => e.Name)
           .ToProperty(this, vm => vm.FileName, initialValue: string.Empty)
           .DisposeWith(_disposables);

       _fullFileNameHelper = fileInfoChanged
           .Select(e => e.FullName)
           .ToProperty(this, vm => vm.FullFileName, initialValue: string.Empty)
           .DisposeWith(_disposables);
       
       _existsHelper = fileInfoChanged
           .Select(e => e.Exists)
           .ToProperty(this, vm => vm.Exists, initialValue: false)
           .DisposeWith(_disposables);
       
       _directoryNameHelper = fileInfoChanged
           .Select(e => e.DirectoryName)
           .ToProperty(this, vm => vm.DirectoryName, initialValue: null)
           .DisposeWith(_disposables);

       _isOpenSshKeyHelper = currentFormatChanged
           .Select(e => e is SshKeyFormat.OpenSSH)
           .ToProperty(this, vm => vm.IsOpenSshKey, initialValue: false)
           .DisposeWith(_disposables);
       
       _publicKeyFileNameHelper = this.WhenAnyValue(vm => vm.IsOpenSshKey)
           .Select(e => e  ? Path.ChangeExtension(FileInfo.FullName, SshKeyFormatExtension.OpenSshPublicKeyFileExtension) : null)
           .ToProperty(this, vm => vm.PublicKeyFileName, initialValue: null)
           .DisposeWith(_disposables);
       
       _availableFormatsForConversionHelper = currentFormatChanged
           .Select(e => AvailableFormats.Where(f => f != e).ToArray())
           .ToProperty(this, vm => vm.AvailableFormatsForConversion, initialValue: [])
           .DisposeWith(_disposables);
       
       _defaultConversionFormatHelper = this.WhenAnyValue(vm => vm.AvailableFormatsForConversion)
           .Select(e => e.Contains(SshKeyFormat.OpenSSH) ? SshKeyFormat.OpenSSH : e.OrderByDescending(f => f == SshKeyFormat.OpenSSH).FirstOrDefault(SshKeyFormat.OpenSSH))
           .ToProperty(this, vm => vm.DefaultConversionFormat, initialValue: SshKeyFormat.OpenSSH)
           .DisposeWith(_disposables);

       
        _filesHelper = this.WhenAnyValue(vm => vm.FullFileName, vm => vm.PublicKeyFileName)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Select(tuple => new[] { tuple.Item1, tuple.Item2 }.Where(e => !string.IsNullOrEmpty(e))
                .Select(e => new FileInfo(e!)).ToArray())
            .ToProperty(this, vm => vm.Files, initialValue: [])
            .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}