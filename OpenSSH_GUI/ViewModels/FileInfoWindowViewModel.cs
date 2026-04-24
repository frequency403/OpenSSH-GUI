using System.Collections.ObjectModel;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Input.Platform;
using DynamicData;
using JetBrains.Annotations;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
public partial class FileInfoWindowViewModel : ViewModelBase<SshKeyFileSource>
{
    private readonly IClipboard _clipboard;
    private readonly SshKeyManager _keyManager;
    private readonly ILogger<FileInfoWindowViewModel> _logger;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly IServiceProvider _serviceProvider;

    [ObservableAsProperty(ReadOnly = true)]
    private string _associatedFilesHeader = string.Empty;

    [Reactive(SetModifier = AccessModifier.Private)]
    private SshKeyFile _keyFile;

    [ObservableAsProperty(ReadOnly = true)]
    private string _password = string.Empty;

    [ObservableAsProperty(ReadOnly = true)]
    private string _windowTitle = "Key info";

    [ReactiveCollection]
    private ObservableCollection<SshKeyFormat> _keyFormats = [];
    
    [ObservableAsProperty(ReadOnly = true)]
    private SshKeyFormat _defaultKeyFormat;

    public FileInfoWindowViewModel(ILogger<FileInfoWindowViewModel> logger, IMessageBoxProvider messageBoxProvider,
        IServiceProvider serviceProvider, IClipboard clipboard, SshKeyManager keyManager)
    {
        _logger = logger;
        _messageBoxProvider = messageBoxProvider;
        _serviceProvider = serviceProvider;
        _clipboard = clipboard;
        _keyManager = keyManager;
        _keyFile = _serviceProvider.GetRequiredService<SshKeyFile>();

        _passwordHelper = this.WhenAnyValue(vm => vm.KeyFile.Password.IsValid)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Select(_ => KeyFile.Password.IsValid
                ? KeyFile.Password.GetPasswordString()
                : string.Empty
            ).ToProperty(this, vm => vm.Password)
            .DisposeWith(Disposables);

        _windowTitleHelper =
            this.WhenAnyValue(vm => vm.KeyFile.FileName, vm => vm.KeyFile.Format, vm => vm.KeyFile.Comment)
                .ObserveOn(AvaloniaScheduler.Instance)
                .Select(e => string.Join(" ", e.Item1, e.Item2, e.Item3))
                .ToProperty(this, vm => vm.WindowTitle)
                .DisposeWith(Disposables);

        _associatedFilesHeaderHelper = this.WhenAnyValue(vm => vm.KeyFile.KeyFiles)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Select(e => string.Format(StringsAndTexts.FileInfoWindowFoundAssociatedFiles, e?.Length ?? 0))
            .ToProperty(this, vm => vm.AssociatedFilesHeader)
            .DisposeWith(Disposables);
        
        _defaultKeyFormatHelper = this.WhenAnyValue(vm => vm.KeyFile.KeyFileInfo)
            .ObserveOn(AvaloniaScheduler.Instance)
            .WhereNotNull()
            .Select(e => e.DefaultConversionFormat)
            .ToProperty(this, vm => vm.DefaultKeyFormat)
            .DisposeWith(Disposables);
        
        this.WhenAnyValue(vm => vm.KeyFile.KeyFileInfo)
            .ObserveOn(AvaloniaScheduler.Instance)
            .WhereNotNull()
            .Subscribe(OnNext)
            .DisposeWith(Disposables);
    }

    private void OnNext(SshKeyFileInformation obj)
    {
        KeyFormats.Clear();
        KeyFormats.AddRange(obj.AvailableFormatsForConversion.Order().ToArray());
    }

    private void SetKeyOrDefault(SshKeyFileSource? source = null)
    {
        KeyFile = (source is not null
                      ? _keyManager.SshKeys.SingleOrDefault(x => x.KeyFileInfo?.KeyFileSource == source)
                      : null)
                  ?? _serviceProvider.GetRequiredService<SshKeyFile>();
    }

    public override ValueTask InitializeAsync(SshKeyFileSource? parameters,
        CancellationToken cancellationToken = default)
    {
        SetKeyOrDefault(parameters);
        return base.InitializeAsync(parameters, cancellationToken);
    }

    [ReactiveCommand]
    private async Task ChangePasswordOfKeyFileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var si = await _messageBoxProvider.ShowSecureInputAsync(
                new SecureInputParams
                {
                    Buttons = MessageBoxButtons.OkCancel,
                    Icon = MaterialIconKind.KeyOutline,
                    MinLength = 0,
                    Prompt = string.Format(StringsAndTexts.FileInfoWindowEnterNewPassword, KeyFile.FileName),
                    Title = StringsAndTexts.FileInfoWindowChangePassword
                });
            if (si is null)
            {
                _logger.LogInformation("User canceled password change");
                return;
            }

            (await _keyManager.ChangePasswordOfKeyAsync(KeyFile, si.Value, token: cancellationToken)).ThrowIfFailure();
            _logger.LogInformation("Key file password changed");
            SetKeyOrDefault(KeyFile.KeyFileInfo?.KeyFileSource);
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
            SetKeyOrDefault(KeyFile.KeyFileInfo?.KeyFileSource);
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
        var validatedInputResult = await _messageBoxProvider.ShowValidatedInputAsync(
            new ValidatedInputParams
            {
                Buttons = MessageBoxButtons.OkCancel,
                Icon = MaterialIconKind.FileEditOutline,
                InitialValue = Path.GetFileNameWithoutExtension(keyFile.FileName) ?? string.Empty,
                Message = StringsAndTexts.FileInfoWindowChangeMessage,
                Prompt = StringsAndTexts.FileInfoWindowEnterNewFilename,
                Watermark = StringsAndTexts.FileInfoWindowEnterNewFilename,
                Validator = argument => string.IsNullOrWhiteSpace(argument)
                    ? StringsAndTexts.FileInfoWindowFilenameCannotBeEmpty
                    : null
            });
        if (validatedInputResult is { IsConfirmed: true, Value: { Length: > 0 } filename })
            try
            {
                var result = await _keyManager.RenameKeyAsync(keyFile, filename, token: cancellationToken);
                while (result is { IsSuccess: false })
                {
                    result.ThrowIfFailure();

                    if (await _messageBoxProvider.ShowMessageBoxAsync(
                            new MessageBoxParams
                            {
                                Title = StringsAndTexts.FileInfoWindowConfirmFileOverwrite,
                                Message =
                                    string.Format(StringsAndTexts.FileInfoWindowFileAlreadyExists, filename),
                                Buttons = MessageBoxButtons.YesNo,
                                Icon = MaterialIconKind.QuestionMarkCircleOutline
                            }) is not MessageBoxResult.Yes)
                        throw new OperationCanceledException("User canceled operation");
                    _logger.LogInformation("User confirmed overwrite of key file");
                    result = await _keyManager.RenameKeyAsync(keyFile, filename, true, cancellationToken);
                }

                _logger.LogInformation("Key file renamed");
                SetKeyOrDefault(KeyFile.KeyFileInfo?.KeyFileSource);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error renaming key file");
                await _messageBoxProvider.ShowErrorMessageBoxAsync(e);
            }
        else
            _logger.LogInformation("User canceled key file rename");
    }

    [ReactiveCommand]
    private async Task DeleteKeyAsync(SshKeyFile keyFile, CancellationToken cancellationToken = default)
    {
        if (await _messageBoxProvider.ShowMessageBoxAsync(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, MessageBoxButtons.YesNo,
                MaterialIconKind.QuestionMarkCircleOutline) is MessageBoxResult.Yes)
            try
            {
                (await _keyManager.TryDeleteKeyAsync(keyFile, cancellationToken)).ThrowIfFailure();
                _logger.LogInformation("Key file deleted");
                RequestClose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting key file");
                await _messageBoxProvider.ShowErrorMessageBoxAsync(
                    e,
                    string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName));
            }
        else
            _logger.LogInformation("User canceled key file deletion");
    }

    [ReactiveCommand]
    private async Task CopyPasswordIntoClipboardAsync(SshKeyFilePassword password, CancellationToken token = default)
    {
        try
        {
            await _clipboard.SetTextAsync(password.GetPasswordString());
            await _clipboard.FlushAsync();
            await _messageBoxProvider.ShowMessageBoxAsync(
                StringsAndTexts.FileInfoWindowPasswordCopied,
                StringsAndTexts.FileInfoWindowPasswordCopied, MessageBoxButtons.Ok,
                MaterialIconKind.InformationOutline);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error copying password to clipboard");
            await _messageBoxProvider.ShowErrorMessageBoxAsync(e);
        }
    }
}