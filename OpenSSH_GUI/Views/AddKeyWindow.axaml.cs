#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:37

#endregion

using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace OpenSSH_GUI.Views;

public partial class AddKeyWindow : WindowBase<AddKeyWindowViewModel>
{
    public AddKeyWindow(ILogger<AddKeyWindow> logger) : base(logger)
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.BindValidation<AddKeyWindow, AddKeyWindowViewModel, string, string>(ViewModel, model => model.KeyName,
                window => window.KeyFileNameValidation.Text);
            // d(ViewModel!.Submit.Subscribe(Close));
        });
    }
}