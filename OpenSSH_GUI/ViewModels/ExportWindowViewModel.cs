using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ExportWindowViewModel(IClipboardService clipboardService) : ViewModelBase<ExportWindowViewModel>
{
    public string WindowTitle { get; private set; } = "";
    public string Export { get; private set; } = "";

    public override void Initialize(IInitializerParameters<ExportWindowViewModel>? parameters = null)
    {
        if (parameters is ExportWindowViewModelInitializerParameters initializerParameters)
        {
            WindowTitle = initializerParameters.WindowTitle;
            Export = initializerParameters.Export;
        }
        BooleanSubmit = ReactiveCommand.CreateFromTask<bool, ExportWindowViewModel?>(Execute);
        base.Initialize(parameters);
    }

    private async Task<ExportWindowViewModel?> Execute(bool arg)
    {
        if(arg)
            await clipboardService.SetTextAsync(Export);
        RequestClose();
        return this;
    }
}

public record ExportWindowViewModelInitializerParameters : IInitializerParameters<ExportWindowViewModel>
{
    public string WindowTitle { get; init; } = string.Empty;
    public string Export { get; init; } = string.Empty;
}