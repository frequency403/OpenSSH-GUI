#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:47

#endregion

using System.Reactive;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSH_GUI.Core.MVVM;

public class ViewModelBase<T>(ILogger<T>? logger = null) : ViewModelBase(typeof(T), logger)  where T : ViewModelBase
{
    public ReactiveCommand<T, T?> Submit { get; set; } = ReactiveCommand.Create<T, T?>(e => null);
    public ReactiveCommand<bool, T?> BooleanSubmit { get; set; } = ReactiveCommand.Create<bool, T?>(e => null);
    
    public EventHandler RequestCose { get; set; } = delegate { };
    protected void RequestClose() => RequestCose.Invoke(this, EventArgs.Empty);
    
    public ReactiveCommand<Unit? ,Unit?> Close { get; set; } = ReactiveCommand.Create<Unit?, Unit?>(_ => null);
    
    public virtual void Initialize(IInitializerParameters<T>?  parameters = null)
    {
        IsInitialized = true;
    }
    
    public virtual ValueTask InitializeAsync(IInitializerParameters<T>?  parameters = null, CancellationToken cancellationToken = default)
    {
        Initialize(parameters);
        return ValueTask.CompletedTask;
    }
}

public class ViewModelBase(Type type, ILogger? logger = null): ReactiveObject
{
    public bool IsInitialized { get; protected set; } = false;
    
    protected ILogger Logger => logger ?? (ILogger)Activator.CreateInstance(typeof(Logger<>).MakeGenericType(type))!;
}

public interface IInitializerParameters<TViewModel> where TViewModel : ViewModelBase
{
    
}