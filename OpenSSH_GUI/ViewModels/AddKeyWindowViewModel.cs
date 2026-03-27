using System.Reactive.Disposables.Fluent;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Enums;
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
    private readonly SshKeyManager _sshKeyManager;
    private readonly IMessageBoxProvider _messageBoxProvider;

    public AddKeyWindowViewModel(ILogger<AddKeyWindowViewModel> logger,
        SshKeyManager sshKeyManager,
        IMessageBoxProvider messageBoxProvider) : base(logger)
    {
        _sshKeyManager = sshKeyManager;
        _messageBoxProvider = messageBoxProvider;
        
        this.WhenAnyValue(x => x.SelectedKeyType)
            .Subscribe(type =>
            {
                try
                {
                    KeyName = $"id_{Enum.GetName(type)!.ToLower()}";

                    var ordered = type.SupportedKeySizes.OrderDescending().ToList();
                    AvaliableKeySizes = ordered;
                    SelectedKeySize   = ordered.First();
                    CanChangeKeySize = ordered.Count > 1;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error reacting to key type change");
                }
            }).DisposeWith(Disposables);

        KeyNameValidationHelper = this.ValidationRule(e => e.KeyName, IsPropertyValid, StringsAndTexts.AddKeyWindowFilenameError).DisposeWith(Disposables);
        SelectedKeyType = SshKeyTypes.First();
    }

    private static bool IsPropertyValid(string? arg)
    {
        if(string.IsNullOrWhiteSpace(arg)) return false;
        return !File.Exists(Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), arg));
    }

    public static SshKeyType[] SshKeyTypes { get; } = Enum.GetValues<SshKeyType>();
    public static SshKeyFormat[] SshKeyFormats { get; } = Enum.GetValues<SshKeyFormat>();
    
    [Reactive]
    private SshKeyType _selectedKeyType;

    [Reactive]
    private IEnumerable<int> _avaliableKeySizes = [];

    [Reactive]
    private int _selectedKeySize;

    [Reactive]
    private SshKeyFormat _keyFormat = SshKeyFormat.OpenSSH;

    [Reactive]
    private string _keyName = "id_rsa";
    
    [Reactive]
    private bool _canChangeKeySize;
    
    public string Comment { get; set; } = $"{Environment.UserName}@{Environment.MachineName}";
    public string Password { get; set; } = "";
    public ValidationHelper KeyNameValidationHelper { get; }

    public IValidationContext ValidationContext { get; } = new ValidationContext();
    
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
            await _messageBoxProvider.ShowMessageBoxAsync(
                StringsAndTexts.Error,
                e.Message,
                MessageBoxButtons.Ok,
                MessageBoxIcon.Error);
        }
    }
}