using System;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class UploadToServerWindow : ReactiveWindow<UploadToServerViewModel>
{
    public UploadToServerWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.UploadAction.Subscribe(Close)));
    }
}