using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

public partial class ConnectToServerWindow : WindowBase<ConnectToServerViewModel>
{
    public ConnectToServerWindow(ILogger<ConnectToServerWindow> logger) : base(logger)
    {
        InitializeComponent();
    }
}