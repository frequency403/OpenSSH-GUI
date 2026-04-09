using System.Reactive.Disposables.Fluent;
using Avalonia.Input.Platform;
using DryIoc;
using JetBrains.Annotations;
using Material.Icons;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Dialogs.Models;
using OpenSSH_GUI.Resources;
using ReactiveUI;
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

    [Reactive] private SshKeyFile _keyFile; // REFACTOR: Setter can be private
    [Reactive] private string _password = string.Empty; // REFACTOR: TO OAPH
    [Reactive] private string _windowTitle = "Key info"; // REFACTOR: TO OAPH

    public FileInfoWindowViewModel(ILogger<FileInfoWindowViewModel> logger, IMessageBoxProvider messageBoxProvider,
        IResolver resolver, IClipboard clipboard, SshKeyManager keyManager) : base(logger)
    {
        _logger = logger;
        _messageBoxProvider = messageBoxProvider;
        _resolver = resolver;
        _clipboard = clipboard;
        _keyManager = keyManager;
        _keyFile = resolver.Resolve<SshKeyFile>();
        this.WhenAnyValue(vm => vm.KeyFile)
            .Subscribe(e =>
            {
                WindowTitle = string.Join(" ", e.FileName, e.Format, e.Comment);
                Password = e.Password.IsValid
                    ? e.Password.GetPasswordString()
                    : string.Empty;
            }).DisposeWith(Disposables);
    }


    public override ValueTask InitializeAsync(FileInfoViewModelInitializer parameters,
        CancellationToken cancellationToken = default)
    {
        KeyFile = _keyManager.SshKeys.SingleOrDefault(x => x.FingerprintString == parameters.KeyFingerprint) ??
                  _resolver.Resolve<SshKeyFile>();
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
            switch (si)
            {
                case null:
                    return;
                case { Value: { Length: > 0 } password }
                    when !KeyFile.Password.WrittenSpan.SequenceEqual(password.Span):
                    await _keyManager.ChangePasswordOfKeyAsync(KeyFile, password, token: cancellationToken);
                    break;
                default:
                    await _messageBoxProvider.ShowMessageBoxAsync(
                        "Password cannot be empty or equal to current password", "Password cannot be empty or equal",
                        MessageBoxButtons.Ok, MaterialIconKind.InformationOutline);
                    break;
            }
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
            await _keyManager.ChangeFormatOfKeyAsync(KeyFile, format, cancellationToken);
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
            InitialValue = keyFile.FileName ?? string.Empty,
            Message = "ChangeMe",
            Prompt = "EnterNewFilename",
            Watermark = "Enter new filename",
            Validator = argument =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(argument);
                return _keyManager.SshKeys.Any(k => string.Equals(k.FileName, argument, StringComparison.Ordinal))
                    ? "Filename already exists"
                    : null;
            }
        });
        if (validatedInputResult is { IsConfirmed: true, Value: { Length: > 0 } filename })
            try
            {
                await _keyManager.RenameKeyAsync(keyFile, filename, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error renaming key file");
                await _messageBoxProvider.ShowErrorMessageBoxAsync(e);
            }
    }

    [ReactiveCommand]
    private async Task DeleteKeyAsync(SshKeyFile keyFile, CancellationToken cancellationToken = default)
    {
        if (await _messageBoxProvider.ShowMessageBoxAsync(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, MessageBoxButtons.YesNo,
                MaterialIconKind.QuestionMarkCircleOutline) is MessageBoxResult.Yes)
            if (await _keyManager.TryDeleteKeyAsync(keyFile, cancellationToken) is
                { success: false, exception: { } error })
                await _messageBoxProvider.ShowMessageBoxAsync(
                    string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName)
                    , error.Message);
        RequestClose();
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
        }
    }
}

// REFACTOR: Finder a better way to do this
public class FileInfoViewModelInitializer : IInitializerParameters<FileInfoWindowViewModel>
{
    public required string KeyFingerprint { get; set; }
}