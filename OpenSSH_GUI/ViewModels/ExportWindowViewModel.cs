using Avalonia.Input.Platform;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class ExportWindowViewModel(ILogger<ExportWindowViewModel> logger, IClipboard clipboard)
    : ViewModelBase<ExportWindowViewModel, ExportWindowViewModelInitializerParameters>(logger)
{
    [Reactive] private string _export = "";

    [Reactive] private string _windowTitle = "";

    public override ValueTask InitializeAsync(ExportWindowViewModelInitializerParameters parameters,
        CancellationToken cancellationToken = default)
    {
        WindowTitle = parameters.WindowTitle;
        Export = parameters.Export;
        return base.InitializeAsync(parameters, cancellationToken);
    }

    protected override async Task BooleanSubmitAsync(bool inputParameter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (inputParameter)
                await clipboard.SetTextAsync(Export);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error submitting export to clipboard");
        }
    }
}

public record ExportWindowViewModelInitializerParameters : IInitializerParameters<ExportWindowViewModel>
{
    public string WindowTitle { get; init; } = string.Empty;
    public string Export { get; init; } = string.Empty;
}