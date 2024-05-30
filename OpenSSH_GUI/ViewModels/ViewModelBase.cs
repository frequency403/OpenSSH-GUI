﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:47

#endregion

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ViewModelBase<T>() : ViewModelBase(App.ServiceProvider.GetRequiredService<ILogger<T>>()) where T : class
{
    public ReactiveCommand<T, T?> Submit { get; set; } = ReactiveCommand.Create<T, T?>(e => null);
    public ReactiveCommand<bool, T?> BooleanSubmit { get; set; } = ReactiveCommand.Create<bool, T?>(e => null);
}

public class ViewModelBase(ILogger? logger = null) : ReactiveObject
{
    protected ILogger Logger => logger ?? NullLogger.Instance;
}