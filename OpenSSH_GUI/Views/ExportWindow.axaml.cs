using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

public partial class ExportWindow : WindowBase<ExportWindowViewModel>
{
    public ExportWindow(ILogger<ExportWindow> logger, [FromKeyedServices(Program.IconServiceKey)] Bitmap icon) :
        base(logger, icon)
    {
        InitializeComponent();
    }
}