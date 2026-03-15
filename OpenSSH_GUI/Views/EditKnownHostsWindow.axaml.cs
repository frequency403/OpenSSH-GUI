using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

public partial class EditKnownHostsWindow : WindowBase<EditKnownHostsWindowViewModel>
{
    public EditKnownHostsWindow(ILogger<EditKnownHostsWindow> logger, [FromKeyedServices("AppIcon")] Bitmap icon) : base(logger, icon)
    {
        InitializeComponent();
    }
}