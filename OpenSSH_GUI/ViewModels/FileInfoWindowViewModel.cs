using System.Reactive;
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

namespace OpenSSH_GUI.ViewModels;
[UsedImplicitly]
public partial class FileInfoWindowViewModel : ViewModelBase<FileInfoWindowViewModel, FileInfoViewModelInitializer>
{
    private readonly ILogger<FileInfoWindowViewModel> _logger;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly SshKeyManager _keyManager;

    public FileInfoWindowViewModel(ILogger<FileInfoWindowViewModel> logger, IMessageBoxProvider messageBoxProvider, SshKeyManager keyManager) : base(logger)
    {
        _logger = logger;
        _messageBoxProvider = messageBoxProvider;
        _keyManager = keyManager;
        ChangeFileNames = ReactiveCommand.CreateFromTask<SshKeyFile>(ChangeFileNameAsync);
        DeleteKey = ReactiveCommand.CreateFromTask<SshKeyFile>(DeleteKeyAsync);
    }
    
    [Reactive]
    private SshKeyFile _keyFile;
    
    public ReactiveCommand<SshKeyFile, Unit> ChangeFileNames { get; } 
    public ReactiveCommand<SshKeyFile, Unit> DeleteKey { get; }
    
    public override ValueTask InitializeAsync(FileInfoViewModelInitializer parameters, CancellationToken cancellationToken = default)
    {
        KeyFile = parameters.Key;
        return base.InitializeAsync(parameters, cancellationToken);
    }

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
            keyFile.ChangeFilenameOnDisk(filename);
    }

    private async Task DeleteKeyAsync(SshKeyFile keyFile, CancellationToken cancellationToken = default)
    {
        if (await _messageBoxProvider.ShowMessageBoxAsync(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) is MessageBoxResult.Yes)
            if(!keyFile.Delete(out var error))
                await _messageBoxProvider.ShowMessageBoxAsync(
                    string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, keyFile.FileName)
                    , error.Message, MessageBoxButtons.Ok, MessageBoxIcon.Error);
    }
}

public class FileInfoViewModelInitializer : IInitializerParameters<FileInfoWindowViewModel>
{
    public required SshKeyFile Key { get; set; }
}