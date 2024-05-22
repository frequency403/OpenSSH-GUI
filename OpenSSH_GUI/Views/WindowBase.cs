// File Created by: Oliver Schantz
// Created: 22.05.2024 - 17:05:31
// Last edit: 22.05.2024 - 17:05:31
using System;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public class WindowBase<T> : ReactiveWindow<T> where T : ViewModelBase<T>, new()
{
    protected WindowBase() => this.WhenActivated(d => d(ViewModel!.Submit.Subscribe(Close))); // @TODO Implement this everywhere.
}