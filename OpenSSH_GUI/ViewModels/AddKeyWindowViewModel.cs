using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.ViewModels;

public sealed class AddKeyWindowViewModel(KeyLocatorService keyLocatorService, ILogger<AddKeyWindowViewModel> logger)
    : ViewModelBase<AddKeyWindowViewModel>(logger), IValidatableViewModel
{
    private bool _createKey;
    private SshKeyType _selectedKeyType;

    public ValidationHelper KeyNameValidationHelper
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = new(new ValidationContext());

    public SshKeyType SelectedKeyType
    {
        get => _selectedKeyType;
        set
        {
            try
            {
                KeyName = $"id_{Enum.GetName(value)!.ToLower()}";
                this.RaiseAndSetIfChanged(ref _selectedKeyType, value);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }

    public static SshKeyType[] SshKeyTypes { get; } = Enum.GetValues<SshKeyType>();

    public SshKeyFormat KeyFormat
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = SshKeyFormat.OpenSSH;

    public SshKeyFormat[] SshKeyFormats { get; } = Enum.GetValues<SshKeyFormat>();


    public string KeyName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "id_rsa";

    public string Comment { get; set; } = $"{Environment.UserName}@{Environment.MachineName}";
    public string Password { get; set; } = "";

    public IValidationContext ValidationContext { get; } = new ValidationContext();

    protected override async Task OnBooleanSubmitAsync(bool inputParameter, CancellationToken cancellationToken = default)
    {
        _createKey = inputParameter;
        if (!_createKey)
        {
            return;
        }

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
            
            await keyLocatorService.GenerateNewKeyInFile(fullNewFilePath, genParm);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating key");
            var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, e.Message,
                ButtonEnum.Ok, Icon.Error);
            await msgBox.ShowAsync();
        }
    }

    public override ValueTask InitializeAsync(IInitializerParameters<AddKeyWindowViewModel>? parameters = null, CancellationToken cancellationToken = default)
    {
        KeyNameValidationHelper = this.ValidationRule(
            e => e.KeyName,
            name => name is not null && !File.Exists(Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), name)),
            StringsAndTexts.AddKeyWindowFilenameError
        );
        _selectedKeyType = SshKeyTypes.First();
        return base.InitializeAsync(parameters, cancellationToken);
    }
}