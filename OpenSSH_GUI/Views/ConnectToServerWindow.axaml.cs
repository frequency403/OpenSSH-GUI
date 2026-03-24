using Avalonia.Media.Imaging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

[UsedImplicitly]
public partial class ConnectToServerWindow : WindowBase<ConnectToServerViewModel>
{
    public ConnectToServerWindow()
    {
        InitializeComponent();
    }
}