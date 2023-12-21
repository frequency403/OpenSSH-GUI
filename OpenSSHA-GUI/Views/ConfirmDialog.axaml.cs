using System;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class ConfirmDialog : ReactiveWindow<ConfirmDialogViewModel>
{
    public ConfirmDialog()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.DialogResult.Subscribe(Close)));
    }
}