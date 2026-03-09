using System.Diagnostics;
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

namespace OpenSSH_GUI.ViewModels;

public sealed class AddKeyWindowViewModel(KeyLocatorService keyLocatorService)
    : ViewModelBase<AddKeyWindowViewModel>, IValidatableViewModel
{
    private static readonly SshKeyType[] _sshKeyTypes = Enum.GetValues<SshKeyType>();
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

    public SshKeyType[] SshKeyTypes => _sshKeyTypes;

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

    protected override async ValueTask<AddKeyWindowViewModel?> OnBooleanSubmitAsync(bool inputParameter)
    {
        _createKey = inputParameter;
        if (!_createKey)
        {
            RequestClose();
            return null;
        }

        var fullNewFilePath = Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), KeyName);
        if (!File.Exists(fullNewFilePath)) return null;
        try
        {
            await keyLocatorService.GenerateNewKeyInFile(fullNewFilePath, new SshKeyGenerateParams(
                SelectedKeyType,
                KeyFormat,
                string.IsNullOrWhiteSpace(KeyName) ? null : KeyName,
                null,
                string.IsNullOrWhiteSpace(Password) ? null : Password,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment
            ));
            RequestClose();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating key");
            var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, e.Message,
                ButtonEnum.Ok, Icon.Error);
            await msgBox.ShowAsync();
        }

        return null;
    }

    public override ValueTask InitializeAsync(IInitializerParameters<AddKeyWindowViewModel>? parameters = null, CancellationToken cancellationToken = default)
    {
        KeyNameValidationHelper = this.ValidationRule(
            e => e.KeyName,
            name => name is not null && !File.Exists(Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), name)),
            StringsAndTexts.AddKeyWindowFilenameError
        );
        _selectedKeyType = _sshKeyTypes.First();
        return base.InitializeAsync(parameters, cancellationToken);
    }
}