using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text;
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
    private readonly ILogger<FileInfoWindowViewModel> _logger;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly IResolver _resolver;
    private readonly IClipboard _clipboard;
    private readonly SshKeyManager _keyManager;

    public FileInfoWindowViewModel(ILogger<FileInfoWindowViewModel> logger, IMessageBoxProvider messageBoxProvider, IResolver resolver, IClipboard clipboard, SshKeyManager keyManager) : base(logger)
    {
        _logger = logger;
        _messageBoxProvider = messageBoxProvider;
        _resolver = resolver;
        _clipboard = clipboard;
        _keyManager = keyManager;
        _keyFile = resolver.Resolve<SshKeyFile>();
        _windowTitleHelper = this.WhenAnyValue(vm => vm.KeyFile)
            .Select(e => string.Join(" ", e.FingerprintString, e.FileName))
            .ToProperty(this, vm => vm.WindowTitle).DisposeWith(Disposables);
    }
    
    [Reactive]
    private SshKeyFile _keyFile;
    
    [ObservableAsProperty] private string _windowTitle = "Key info";
    
    
    public override ValueTask InitializeAsync(FileInfoViewModelInitializer parameters, CancellationToken cancellationToken = default)
    {
        KeyFile = _keyManager.SshKeys.SingleOrDefault(x => x.FingerprintString == parameters.KeyFingerprint) ?? _resolver.Resolve<SshKeyFile>();
        return base.InitializeAsync(parameters, cancellationToken);
    }

    [ReactiveCommand]
    private Task ChangeFormatOfKeyFileAsync(SshKeyFormat format, CancellationToken cancellationToken = default) => 
        _keyManager.ChangeFormatOfKeyAsync(KeyFile, format, cancellationToken);

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
                return _keyManager.SshKeys.Any(k => k.FileName == argument) ? "Filename already exists" : null;
            }
        });
        if (validatedInputResult is { IsConfirmed: true, Value: { Length: > 0 } filename })
            await _keyManager.RenameKeyAsync(keyFile, filename, cancellationToken);
    }

    [ReactiveCommand]
    private async Task DeleteKeyAsync(SshKeyFile keyFile, CancellationToken cancellationToken = default)
    {
        if (await _messageBoxProvider.ShowMessageBoxAsync(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) is MessageBoxResult.Yes)
            if((await _keyManager.TryDeleteKeyAsync(keyFile, cancellationToken)) is { success: false, exception: { } error})
                await _messageBoxProvider.ShowMessageBoxAsync(
                    string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName)
                    , error.Message, MessageBoxButtons.Ok, MessageBoxIcon.Error);
    }

    [ReactiveCommand]
    private async Task CopyPasswordIntoClipboardAsync(SshKeyFilePassword password, CancellationToken token = default)
    {
        try
        {
            Span<char> passwordSpan = stackalloc char[password.Length];
            Encoding.UTF8.GetChars(password.WrittenSpan, passwordSpan);
            await _clipboard.SetTextAsync(passwordSpan.ToString());
            await _clipboard.FlushAsync();
            await _messageBoxProvider.ShowMessageBoxAsync("Password copied to clipboard", "Password copied to clipboard", MessageBoxButtons.Ok, MessageBoxIcon.Information);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error copying password to clipboard");
        }
    }
}

public class FileInfoViewModelInitializer : IInitializerParameters<FileInfoWindowViewModel>
{
    public required string KeyFingerprint { get; set; }
}