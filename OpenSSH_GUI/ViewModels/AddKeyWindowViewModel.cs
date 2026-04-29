using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSSH_GUI.Core.Configuration;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public sealed partial class AddKeyWindowViewModel : ViewModelBase, IValidatableViewModel
{
    private const string KeyPrefix = "id";
    private readonly ILogger<AddKeyWindowViewModel> _logger;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly SshKeyManager _sshKeyManager;
    private readonly IOptionsMonitor<ApplicationConfiguration> _applicationConfigurationMonitor;

    [ObservableAsProperty(ReadOnly = true)]
    private int[] _availableKeySizes = [];

    [ObservableAsProperty(ReadOnly = true)]
    private bool _canChangeKeySize;

    [Reactive] private string _comment = SshKeyGenerateInfo.DefaultSshKeyComment;

    [Reactive] private SshKeyFormat _keyFormat = SshKeyGenerateInfo.DefaultSshKeyFormat;

    [Reactive] private string _keyName = string.Empty;

    [Reactive] private string _password = string.Empty;

    [Reactive] private int _selectedKeySize;

    [Reactive] private SshKeyType _selectedKeyType = SshKeyGenerateInfo.DefaultSshKeyType;

    public AddKeyWindowViewModel(ILogger<AddKeyWindowViewModel> logger,
        SshKeyManager sshKeyManager,
        IOptionsMonitor<ApplicationConfiguration> applicationConfigurationMonitor,
        IMessageBoxProvider messageBoxProvider)
    {
        _logger = logger;
        _sshKeyManager = sshKeyManager;
        _applicationConfigurationMonitor = applicationConfigurationMonitor;
        _messageBoxProvider = messageBoxProvider;
        
        var selectedKeyTypeChanged = this.WhenAnyValue(vm => vm.SelectedKeyType)
            .ObserveOn(AvaloniaScheduler.Instance);

        _availableKeySizesHelper = selectedKeyTypeChanged
            .Select(e => e.SupportedKeySizes.OrderDescending().ToArray())
            .ToProperty(
                this, vm => vm.AvailableKeySizes,
                SshKeyGenerateInfo.DefaultSshKeyType.SupportedKeySizes.OrderDescending().ToArray())
            .DisposeWith(Disposables);

        selectedKeyTypeChanged
            .Subscribe(e =>
            {
                if (DefaultKeyNames.Values.Any(keyName =>
                        string.IsNullOrWhiteSpace(KeyName) ||
                        string.Equals(keyName, KeyName, StringComparison.OrdinalIgnoreCase)))
                    if (DefaultKeyNames.TryGetValue(e, out var defaultKeyName))
                        KeyName = defaultKeyName;

                SelectedKeySize = e switch
                {
                    SshKeyType.ECDSA => SshKeyGenerateInfo.DefaultEcdsaSshKeyLength,
                    SshKeyType.ED25519 => SshKeyGenerateInfo.DefaultEd25519SshKeyLength,
                    SshKeyType.RSA => SshKeyGenerateInfo.DefaultRsaSshKeyLength,
                    _ => SshKeyGenerateInfo.DefaultSshKeyType.SupportedKeySizes.Max()
                };
            })
            .DisposeWith(Disposables);

        _canChangeKeySizeHelper = this.WhenAnyValue(vm => vm.AvailableKeySizes)
            .Select(e => e.Length > 1)
            .ToProperty(
                this, vm => vm.CanChangeKeySize,
                initialValue: SshKeyGenerateInfo.DefaultSshKeyType.SupportedKeySizes.Any())
            .DisposeWith(Disposables);

        this.WhenAnyValue(vm => vm.KeyName)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(name =>
            {
                if (string.IsNullOrWhiteSpace(name) && DefaultKeyNames.TryGetValue(SelectedKeyType, out var value))
                    KeyName = value;
            }).DisposeWith(Disposables);

        KeyNameValidationHelper =
            this.ValidationRule(e => e.KeyName, IsPropertyValid, StringsAndTexts.AddKeyWindowFilenameError)
                .DisposeWith(Disposables);
    }

    public static IDictionary<SshKeyType, string> DefaultKeyNames { get; } = Enum.GetValues<SshKeyType>()
        .Select(type =>
            new KeyValuePair<SshKeyType, string>(type, string.Join("_", KeyPrefix, Enum.GetName(type)!.ToLower())))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    public static SshKeyType[] SshKeyTypes { get; } = Enum.GetValues<SshKeyType>();
    public static SshKeyFormat[] SshKeyFormats { get; } = Enum.GetValues<SshKeyFormat>();

    public ValidationHelper KeyNameValidationHelper { get; }

    public IValidationContext ValidationContext { get; } = new ValidationContext();

    private bool IsPropertyValid(string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) return false;
        return !File.Exists(Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), arg));
    }

    /// <inheritdoc />
    protected override async Task BooleanSubmitAsync(
        bool inputParameter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(_sshKeyManager);
        if (!inputParameter) return;

        var fullNewFilePath = Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), KeyName);
        if (File.Exists(fullNewFilePath)) return;

        try
        {
            var genParm = new SshKeyGenerateInfo(SelectedKeyType)
            {
                KeyFormat = KeyFormat
            };

            if (!string.IsNullOrWhiteSpace(Password))
                genParm.Encryption = new SshKeyEncryptionAes256(
                    Password,
                    Aes256Mode.CBC,
                    genParm.KeyFormat is SshKeyFormat.PuTTYv3 ? new PuttyV3Encryption() : null);

            if (!string.IsNullOrWhiteSpace(Comment))
                genParm.Comment = Comment;

            var genResult = await _sshKeyManager.GenerateNewKey(fullNewFilePath, genParm, true);
            genResult.ThrowIfFailure();
            CloseOnBooleanSubmit = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating key");
            await _messageBoxProvider.ShowErrorMessageBoxAsync(e, StringsAndTexts.Error);
            CloseOnBooleanSubmit = false;
        }
    }
}