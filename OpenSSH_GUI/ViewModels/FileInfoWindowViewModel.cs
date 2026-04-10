using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Input.Platform;
using DryIoc;
using JetBrains.Annotations;
using Material.Icons;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Dialogs.Models;
using OpenSSH_GUI.Resources;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using SshNet.Keygen;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class FileInfoWindowViewModel : ViewModelBase<FileInfoWindowViewModel, FileInfoViewModelInitializer>
{
    private readonly IClipboard _clipboard;
    private readonly SshKeyManager _keyManager;
    private readonly ILogger<FileInfoWindowViewModel> _logger;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly IResolver _resolver;

    [Reactive(SetModifier = AccessModifier.Private)] private SshKeyFile _keyFile;
    [ObservableAsProperty(ReadOnly = true)] private string _password = string.Empty;
    [ObservableAsProperty(ReadOnly = true)] private string _windowTitle = "Key info";

    public FileInfoWindowViewModel(ILogger<FileInfoWindowViewModel> logger, IMessageBoxProvider messageBoxProvider,
        IResolver resolver, IClipboard clipboard, SshKeyManager keyManager) : base(logger)
    {
        _logger = logger;
        _messageBoxProvider = messageBoxProvider;
        _resolver = resolver;
        _clipboard = clipboard;
        _keyManager = keyManager;
        _keyFile = _resolver.Resolve<SshKeyFile>();
        var keyFileChanged = this.WhenAnyValue(vm => vm.KeyFile)
            .ObserveOn(AvaloniaScheduler.Instance);
        
        _passwordHelper = keyFileChanged
            .Select(e => e.Password.IsValid
                ? e.Password.GetPasswordString()
                : string.Empty
            ).ToProperty(this, vm => vm.Password)
            .DisposeWith(Disposables);
        
        _windowTitleHelper = keyFileChanged
            .Select(e => string.Join(" ", e.FileName, e.Format, e.Comment))
            .ToProperty(this, vm => vm.WindowTitle)
            .DisposeWith(Disposables);
    }

    private void SetKeyOrDefault(string? fingerprint = null)
    {
        KeyFile = _keyManager.SshKeys.SingleOrDefault(x => x.Fingerprint == (fingerprint ?? string.Empty)) ??
                  _resolver.Resolve<SshKeyFile>();
    }

    public override ValueTask InitializeAsync(FileInfoViewModelInitializer parameters,
        CancellationToken cancellationToken = default)
    {
        SetKeyOrDefault(parameters.KeyFingerprint);
        return base.InitializeAsync(parameters, cancellationToken);
    }

    [ReactiveCommand]
    private async Task ChangePasswordOfKeyFileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var si = await _messageBoxProvider.ShowSecureInputAsync(new SecureInputParams
            {
                Buttons = MessageBoxButtons.OkCancel,
                Icon = MaterialIconKind.KeyOutline,
                MinLength = 0,
                Prompt = $"Enter a new password for key {KeyFile.FileName}",
                Title = "Change password"
            });
            if (si is null)
            {
                _logger.LogInformation("User canceled password change");
                return;
            }

            (await _keyManager.ChangePasswordOfKeyAsync(KeyFile, si.Value, token: cancellationToken)).ThrowIfFailure();
            _logger.LogInformation("Key file password changed");
            SetKeyOrDefault(KeyFile.Fingerprint);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error changing password of key file");
            await _messageBoxProvider.ShowErrorMessageBoxAsync(e);
        }
    }

    [ReactiveCommand]
    private async Task ChangeFormatOfKeyFileAsync(SshKeyFormat format, CancellationToken cancellationToken = default)
    {
        try
        {
            (await _keyManager.ChangeFormatOfKeyAsync(KeyFile, format, cancellationToken)).ThrowIfFailure();
            _logger.LogInformation("Key file format changed");
            SetKeyOrDefault(KeyFile.Fingerprint);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error changing format of key file");
            await _messageBoxProvider.ShowErrorMessageBoxAsync(e);
        }
    }

    [ReactiveCommand]
    private async Task ChangeFileNameAsync(SshKeyFile keyFile, CancellationToken cancellationToken = default)
    {
        var validatedInputResult = await _messageBoxProvider.ShowValidatedInputAsync(new ValidatedInputParams
        {
            Buttons = MessageBoxButtons.OkCancel,
            Icon = MaterialIconKind.FileEditOutline,
            InitialValue = Path.GetFileNameWithoutExtension(keyFile.FileName) ?? string.Empty,
            Message = "ChangeMe",
            Prompt = "EnterNewFilename",
            Watermark = "Enter new filename",
            Validator = argument => string.IsNullOrWhiteSpace(argument) ? "Filename cannot be empty" : null
        });
        if (validatedInputResult is { IsConfirmed: true, Value: { Length: > 0 } filename })
        {
            try
            {
                var result = await _keyManager.RenameKeyAsync(keyFile, filename, token: cancellationToken);
                while (result is { IsSuccess: false })
                {
                    result.ThrowIfFailure();
                    
                    if (await _messageBoxProvider.ShowMessageBoxAsync(new MessageBoxParams // REFACTOR: Localize
                        {
                            Title = "Confirm File Overwrite",
                            Message =
                                $"The keyfile {filename} already exist. Do you want to overwrite it?",
                            Buttons = MessageBoxButtons.YesNo,
                            Icon = MaterialIconKind.QuestionMarkCircleOutline
                        }) is not MessageBoxResult.Yes)
                    {
                        throw new OperationCanceledException("User canceled operation");
                    }
                    _logger.LogInformation("User confirmed overwrite of key file");
                    result = await _keyManager.RenameKeyAsync(keyFile, filename, true, cancellationToken);
                }
                _logger.LogInformation("Key file renamed");
                SetKeyOrDefault(keyFile.Fingerprint);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error renaming key file");
                await _messageBoxProvider.ShowErrorMessageBoxAsync(e);
            }
        }
        else
        {
            _logger.LogInformation("User canceled key file rename");
        }
    }

    [ReactiveCommand]
    private async Task DeleteKeyAsync(SshKeyFile keyFile, CancellationToken cancellationToken = default)
    {
        if (await _messageBoxProvider.ShowMessageBoxAsync(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, MessageBoxButtons.YesNo,
                MaterialIconKind.QuestionMarkCircleOutline) is MessageBoxResult.Yes)
        {
            try
            {
                (await _keyManager.TryDeleteKeyAsync(keyFile, cancellationToken)).ThrowIfFailure();
                _logger.LogInformation("Key file deleted");
                RequestClose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting key file");
                await _messageBoxProvider.ShowErrorMessageBoxAsync(e,
                    string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName));
            }
        }
        else
        {
            _logger.LogInformation("User canceled key file deletion");
        }
    }

    [ReactiveCommand]
    private async Task CopyPasswordIntoClipboardAsync(SshKeyFilePassword password, CancellationToken token = default)
    {
        try
        {
            await _clipboard.SetTextAsync(password.GetPasswordString());
            await _clipboard.FlushAsync();
            await _messageBoxProvider.ShowMessageBoxAsync("Password copied to clipboard",
                "Password copied to clipboard", MessageBoxButtons.Ok, MaterialIconKind.InformationOutline);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error copying password to clipboard");
            await _messageBoxProvider.ShowErrorMessageBoxAsync(e);
        }
    }
}

// REFACTOR: Finder a better way to do this
public class FileInfoViewModelInitializer : IInitializerParameters<FileInfoWindowViewModel>
{
    public required string KeyFingerprint { get; set; }
}