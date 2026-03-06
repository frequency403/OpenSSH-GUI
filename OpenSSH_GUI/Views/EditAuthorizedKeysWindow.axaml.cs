using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

public partial class EditAuthorizedKeysWindow : WindowBase<EditAuthorizedKeysViewModel>
{
    public EditAuthorizedKeysWindow(ILogger<EditAuthorizedKeysWindow> logger) : base(logger)
    {
        InitializeComponent();
    }
}