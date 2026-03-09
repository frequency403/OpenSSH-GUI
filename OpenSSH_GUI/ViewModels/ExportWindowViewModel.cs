using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.MVVM;

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

        base.Initialize(parameters);
    }

    protected override async Task<ExportWindowViewModel?> OnBooleanSubmit(bool inputParameter)
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