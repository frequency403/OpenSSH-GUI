using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

public partial class ExportWindow : WindowBase<ExportWindowViewModel>
{
    public ExportWindow(ILogger<ExportWindow> logger) : base(logger)
    {
        InitializeComponent();
    }
}