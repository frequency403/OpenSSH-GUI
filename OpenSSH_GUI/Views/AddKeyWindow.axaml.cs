using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace OpenSSH_GUI.Views;

public partial class AddKeyWindow : WindowBase<AddKeyWindowViewModel>
{
    public AddKeyWindow(ILogger<AddKeyWindow> logger) : base(logger)
    {
        InitializeComponent();
        this.WhenActivated(_ =>
        {
            this.BindValidation<AddKeyWindow, AddKeyWindowViewModel, string, string>(ViewModel, model => model.KeyName,
                window => window.KeyFileNameValidation.Text);
        });
    }
}