using Avalonia.Input.Platform;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public class ExportWindowViewModel(ILogger<ExportWindowViewModel> logger, IClipboard clipboard) : ViewModelBase<ExportWindowViewModel, ExportWindowViewModelInitializerParameters>(logger)
{
    public string WindowTitle { get; private set; } = "";
    public string Export { get; private set; } = "";
    
    public override ValueTask InitializeAsync(ExportWindowViewModelInitializerParameters parameters,
        CancellationToken cancellationToken = default)
    {
        WindowTitle = parameters.WindowTitle;
        Export = parameters.Export;
        return base.InitializeAsync(parameters, cancellationToken);
    }

    protected override async Task OnBooleanSubmitAsync(bool inputParameter,
        CancellationToken cancellationToken = default)
    {
        if (inputParameter)
            await clipboard.SetTextAsync(Export);
    }
}

public record ExportWindowViewModelInitializerParameters : IInitializerParameters<ExportWindowViewModel>
{
    public string WindowTitle { get; init; } = string.Empty;
    public string Export { get; init; } = string.Empty;
}