using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ExportWindowViewModel(ILogger<ExportWindowViewModel> logger) : ViewModelBase<ExportWindowViewModel>(logger)
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

        BooleanSubmit = ReactiveCommand.Create<bool, ExportWindowViewModel?>(boolean =>
        {
            if (!boolean) return null;
            if (Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;
            var mainWindow = desktop.MainWindow;
            var clipboard = mainWindow.Clipboard;

            clipboard.SetTextAsync(Export).ConfigureAwait(false);
            return this;
        });
        base.Initialize(parameters);
    }
}

public record ExportWindowViewModelInitializerParameters : IInitializerParameters<ExportWindowViewModel>
{
    public string WindowTitle { get; init; } = string.Empty;
    public string Export { get; init; } = string.Empty;
}