using Microsoft.Extensions.Logging;

namespace OpenSSHA_GUI.ViewModels;

public class ExportWindowViewModel(ILogger<ExportWindowViewModel> logger) : ViewModelBase(logger)
{
    public string WindowTitle { get; set; } = "";
    public string Export { get; set; } = "";
}