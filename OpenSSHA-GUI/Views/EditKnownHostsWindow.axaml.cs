using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using System;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class EditKnownHostsWindow : ReactiveWindow<EditKnownHostsViewModel>
{
    public EditKnownHostsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.ProcessData.Subscribe(Close)));
    }
}