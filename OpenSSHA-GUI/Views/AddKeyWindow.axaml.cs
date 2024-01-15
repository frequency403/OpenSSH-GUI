using System;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace OpenSSHA_GUI.Views;

public partial class AddKeyWindow : ReactiveWindow<AddKeyWindowViewModel>
{
    public AddKeyWindow()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.BindValidation(ViewModel, model => model.KeyName, window => window.KeyFileNameValidation.Text);
            d(ViewModel!.AddKey.Subscribe(Close));
        });
    }
}