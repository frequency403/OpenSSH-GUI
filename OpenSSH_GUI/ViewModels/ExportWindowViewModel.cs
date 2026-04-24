using Avalonia.Input.Platform;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class ExportWindowViewModel(ILogger<ExportWindowViewModel> logger, IClipboard clipboard)
    : ViewModelBase<(string WindowTitle, string Export)>
{
    [Reactive] private string _export = string.Empty;

    [Reactive] private string _windowTitle = string.Empty;

    public override ValueTask InitializeAsync((string WindowTitle, string Export) parameters,
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

public record ExportWindowViewModelInitializerParameters
{
    public string WindowTitle { get; init; } = string.Empty;
    public string Export { get; init; } = string.Empty;
}