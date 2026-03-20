using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.MVVM;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public class ExportWindowViewModel(ILogger<ExportWindowViewModel> logger, IClipboardService clipboardService) : ViewModelBase<ExportWindowViewModel>(logger)
{
    public string WindowTitle { get; private set; } = "";
    public string Export { get; private set; } = "";

    public override async ValueTask InitializeAsync(IInitializerParameters<ExportWindowViewModel>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters is ExportWindowViewModelInitializerParameters initializerParameters)
        {
            WindowTitle = initializerParameters.WindowTitle;
            Export = initializerParameters.Export;
        }

        await base.InitializeAsync(parameters, cancellationToken);
    }

    protected override async Task OnBooleanSubmitAsync(bool inputParameter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clipboardService);
        if (inputParameter)
            await clipboardService.SetTextAsync(Export);
    }
}

public record ExportWindowViewModelInitializerParameters : IInitializerParameters<ExportWindowViewModel>
{
    public string WindowTitle { get; init; } = string.Empty;
    public string Export { get; init; } = string.Empty;
}