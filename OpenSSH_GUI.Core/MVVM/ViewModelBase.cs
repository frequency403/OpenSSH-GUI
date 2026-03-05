#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:47

#endregion

using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSH_GUI.Core.MVVM;

public class ViewModelBase<T>(ILogger<T>? logger = null) : ViewModelBase(typeof(T), logger)  where T : class
{
    public ReactiveCommand<T, T?> Submit { get; set; } = ReactiveCommand.Create<T, T?>(e => null);
    public ReactiveCommand<bool, T?> BooleanSubmit { get; set; } = ReactiveCommand.Create<bool, T?>(e => null);
    
    public virtual void Intitialize()
    {
    }
    
    public virtual ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

public class ViewModelBase(Type type, ILogger? logger = null): ReactiveObject
{
    protected ILogger Logger => logger ?? (ILogger)Activator.CreateInstance(typeof(Logger<>).MakeGenericType(type))!;
}