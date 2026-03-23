using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;
[UsedImplicitly]
public partial class ApplicationSettingsWindow : WindowBase<ApplicationSettingsViewModel>
{
    public ApplicationSettingsWindow()
    {
        InitializeComponent();
    }
}