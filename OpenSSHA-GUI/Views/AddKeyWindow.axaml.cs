using System;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class AddKeyWindow : ReactiveWindow<AddKeyWindowViewModel>
{
    public AddKeyWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.AddKey.Subscribe(Close)));
    }
}