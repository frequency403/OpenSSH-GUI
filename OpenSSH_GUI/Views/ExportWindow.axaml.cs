using JetBrains.Annotations;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

[UsedImplicitly]
public partial class ExportWindow : WindowBase<ExportWindowViewModel, ExportWindowViewModelInitializerParameters>
{
    public ExportWindow()
    {
        InitializeComponent();
    }
}