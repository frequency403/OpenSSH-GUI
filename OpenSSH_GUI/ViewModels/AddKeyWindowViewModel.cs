using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.ViewModels;

public sealed class AddKeyWindowViewModel(ISshKeyManager sshKeyManager, ILogger<AddKeyWindowViewModel> logger)
    : ViewModelBase<AddKeyWindowViewModel>(logger), IValidatableViewModel
{
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
                logger.LogError(e, "Error setting key type");
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
                logger.LogError(e, "Error setting avaliable key sizes");
            }
        }
    }

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
            var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, e.Message,
                ButtonEnum.Ok, Icon.Error);
            await msgBox.ShowAsync();
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