using Avalonia.Controls;
using Avalonia.Media.Imaging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

[UsedImplicitly]
public partial class EditKnownHostsWindow : WindowBase<EditKnownHostsWindowViewModel>
{
    public EditKnownHostsWindow()
    {
        InitializeComponent();
    }
}