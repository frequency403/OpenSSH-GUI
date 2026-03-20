using Avalonia.Media.Imaging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

[UsedImplicitly]
public partial class EditAuthorizedKeysWindow : WindowBase<EditAuthorizedKeysViewModel>
{
    public EditAuthorizedKeysWindow(ILogger<EditAuthorizedKeysWindow> logger,
        [FromKeyedServices(Program.IconServiceKey)] Bitmap icon) : base(logger, icon)
    {
        InitializeComponent();
    }
}