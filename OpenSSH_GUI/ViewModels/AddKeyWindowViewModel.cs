using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.ViewModels;

public sealed class AddKeyWindowViewModel(ILogger<AddKeyWindowViewModel>? logger, SshKeyManager? sshKeyManager, IMessageBoxProvider? messageBoxProvider)
    : ViewModelBase<AddKeyWindowViewModel>(logger), IValidatableViewModel
{
    private readonly ILogger<AddKeyWindowViewModel> _logger = logger ?? NullLogger<AddKeyWindowViewModel>.Instance;

    public AddKeyWindowViewModel() : this(null, null, null) { }

    public static SshKeyType[] SshKeyTypes { get; } = Enum.GetValues<SshKeyType>();
    public static SshKeyFormat[] SshKeyFormats { get; } = Enum.GetValues<SshKeyFormat>();

    public ValidationHelper KeyNameValidationHelper
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = new(new ValidationContext());

    public SshKeyType SelectedKeyType
    {
        get;
        set
        {
            try
            {
                this.RaiseAndSetIfChanged(ref field, value);
                KeyName = $"id_{Enum.GetName(value)!.ToLower()}";
                AvaliableKeySizes = value.SupportedKeySizes;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error setting key type");
            }
        }
    }

    public bool CanChangeKeySize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public IEnumerable<int> AvaliableKeySizes
    {
        get;
        set
        {
            try
            {
                this.RaiseAndSetIfChanged(ref field, value.OrderDescending());
                CanChangeKeySize = value.Count() > 1;
                SelectedKeySize = value.OrderDescending().First();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error setting avaliable key sizes");
            }
        }
    } = [];

    public int SelectedKeySize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }


    public SshKeyFormat KeyFormat
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = SshKeyFormat.OpenSSH;


    public string KeyName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "id_rsa";

    public string Comment { get; set; } = $"{Environment.UserName}@{Environment.MachineName}";
    public string Password { get; set; } = "";

    public IValidationContext ValidationContext { get; } = new ValidationContext();

    protected override async Task OnBooleanSubmitAsync(bool inputParameter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sshKeyManager);
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
                genParm.Encryption = new SshKeyEncryptionAes256(Password, Aes256Mode.CBC,
                    genParm.KeyFormat is SshKeyFormat.PuTTYv3 ? new PuttyV3Encryption() : null);
            if (!string.IsNullOrWhiteSpace(Comment))
                genParm.Comment = Comment;

            await sshKeyManager.GenerateNewKey(fullNewFilePath, genParm);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating key");
            await messageBoxProvider!.ShowMessageBoxAsync(StringsAndTexts.Error, e.Message, MessageBoxButtons.Ok, MessageBoxIcon.Error);
        }
    }

    public override ValueTask InitializeAsync(IInitializerParameters<AddKeyWindowViewModel>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        KeyNameValidationHelper = this.ValidationRule(
            e => e.KeyName,
            name => name is not null && !File.Exists(Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), name)),
            StringsAndTexts.AddKeyWindowFilenameError
        );
        AvaliableKeySizes = SelectedKeyType.SupportedKeySizes;
        SelectedKeyType = SshKeyTypes.First();
        return base.InitializeAsync(parameters, cancellationToken);
    }
}