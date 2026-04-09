using System.Reactive.Disposables.Fluent;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public sealed partial class AddKeyWindowViewModel : ViewModelBase<AddKeyWindowViewModel>, IValidatableViewModel
{
    private const string KeyPrefix = "id";
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly SshKeyManager _sshKeyManager;

    [Reactive(SetModifier = AccessModifier.Private)]
    private int[] _avaliableKeySizes = [];

    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _canChangeKeySize;

    // REFACTOR: Set to Renci's default value
    [Reactive] private string _comment = $"{Environment.UserName}@{Environment.MachineName}";

    [Reactive] private SshKeyFormat _keyFormat = SshKeyFormat.OpenSSH;

    // REFACTOR: Use a better default value
    [Reactive] private string _keyName = "id_rsa";

    [Reactive] private string _password = "";

    [Reactive] private int _selectedKeySize;

    [Reactive] private SshKeyType _selectedKeyType;

    public AddKeyWindowViewModel(ILogger<AddKeyWindowViewModel> logger,
        SshKeyManager sshKeyManager,
        IMessageBoxProvider messageBoxProvider) : base(logger)
    {
        _sshKeyManager = sshKeyManager;
        _messageBoxProvider = messageBoxProvider;

        this.WhenAnyValue(vm => vm.SelectedKeyType)
            .Subscribe(e =>
            {
                if (SshKeyTypes.Select(kt => string.Join("_", KeyPrefix, Enum.GetName(kt)!.ToLower()))
                    .Any(ktp => KeyName.EndsWith(ktp, StringComparison.Ordinal)))
                    KeyName = string.Join("_", KeyPrefix, Enum.GetName(e)!.ToLower());

                AvaliableKeySizes = e.SupportedKeySizes.OrderDescending().ToArray();
                SelectedKeySize = AvaliableKeySizes.FirstOrDefault();
                CanChangeKeySize = AvaliableKeySizes.Length > 1;
            })
            .DisposeWith(Disposables);

        KeyNameValidationHelper =
            this.ValidationRule(e => e.KeyName, IsPropertyValid, StringsAndTexts.AddKeyWindowFilenameError)
                .DisposeWith(Disposables);
        SelectedKeyType = SshKeyTypes.First();
    }

    public static SshKeyType[] SshKeyTypes { get; } = Enum.GetValues<SshKeyType>();
    public static SshKeyFormat[] SshKeyFormats { get; } = Enum.GetValues<SshKeyFormat>();

    public ValidationHelper KeyNameValidationHelper { get; }

    public IValidationContext ValidationContext { get; } = new ValidationContext();

    private static bool IsPropertyValid(string? arg)
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

            await _sshKeyManager.GenerateNewKey(fullNewFilePath, genParm);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating key");
            await _messageBoxProvider.ShowErrorMessageBoxAsync(e, StringsAndTexts.Error);
        }
    }
}