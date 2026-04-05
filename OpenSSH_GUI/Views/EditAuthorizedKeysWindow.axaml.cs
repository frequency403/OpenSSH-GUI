using JetBrains.Annotations;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

[UsedImplicitly]
public partial class EditAuthorizedKeysWindow : WindowBase<EditAuthorizedKeysViewModel>
{
    public EditAuthorizedKeysWindow()
    {
        InitializeComponent();
    }
}