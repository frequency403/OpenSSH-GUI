// File Created by: Oliver Schantz
// Created: 27.05.2024 - 08:05:50
// Last edit: 27.05.2024 - 08:05:51

using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Resources.Wrapper;

public class WindowBase<T> : ReactiveWindow<T> where T : ViewModelBase<T>
{
    
    protected WindowBase()
    {
        Icon = App.WindowIcon;
        this.WhenActivated(d =>
        {
            d(ViewModel!.Submit.Subscribe(Close));
            d(ViewModel!.BooleanSubmit.Subscribe(Close));
        });
    }
}