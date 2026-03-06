using System.Reactive;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSH_GUI.Core.MVVM;

public class ViewModelBase<T>(ILogger<T>? logger = null) : ViewModelBase(typeof(T), logger) where T : ViewModelBase
{
    public ReactiveCommand<bool, T?> BooleanSubmit { get; set; } = ReactiveCommand.Create<bool, T?>(e => null);

    public EventHandler Close { get; set; } = delegate { };
    protected void RequestClose()
    {
        Close.Invoke(this, EventArgs.Empty);
    }

    public virtual void Initialize(IInitializerParameters<T>? parameters = null)
    {
        IsInitialized = true;
    }

    public virtual ValueTask InitializeAsync(IInitializerParameters<T>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Initialize(parameters);
        return ValueTask.CompletedTask;
    }
}

public class ViewModelBase(Type type, ILogger? logger = null) : ReactiveObject
{
    public bool IsInitialized { get; protected set; }

    protected ILogger Logger => logger ?? (ILogger)Activator.CreateInstance(typeof(Logger<>).MakeGenericType(type))!;
}

public interface IInitializerParameters<TViewModel> where TViewModel : ViewModelBase
{
}