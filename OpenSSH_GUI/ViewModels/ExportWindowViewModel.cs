using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.MVVM;

namespace OpenSSH_GUI.ViewModels;

public class ExportWindowViewModel(IClipboardService clipboardService) : ViewModelBase<ExportWindowViewModel>
{
    public string WindowTitle { get; private set; } = "";
    public string Export { get; private set; } = "";

    public override async ValueTask InitializeAsync(IInitializerParameters<ExportWindowViewModel>? parameters = null, CancellationToken cancellationToken = default)
    {
        if (parameters is ExportWindowViewModelInitializerParameters initializerParameters)
        {
            WindowTitle = initializerParameters.WindowTitle;
            Export = initializerParameters.Export;
        }

        await base.InitializeAsync(parameters, cancellationToken);
    }

    protected override async ValueTask<ExportWindowViewModel?> OnBooleanSubmitAsync(bool inputParameter)
    {
        if (inputParameter)
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