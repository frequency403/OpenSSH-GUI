using System;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class EditAuthorizedKeysWindow : ReactiveWindow<EditAuthorizedKeysViewModel>
{
    public EditAuthorizedKeysWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.Submit.Subscribe(Close)));
    }
}