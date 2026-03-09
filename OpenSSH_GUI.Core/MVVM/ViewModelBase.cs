using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSH_GUI.Core.MVVM;

public class ViewModelBase<T> : ViewModelBase where T : ViewModelBase
{
    public ViewModelBase(ILogger<T>? logger = null) : base(typeof(T), logger)
    {
        BooleanSubmit = ReactiveCommand.CreateFromTask<bool, T?>(OnBooleanSubmit);
    }

    public ReactiveCommand<bool, T?> BooleanSubmit { get; private init; }

    public EventHandler Close { get; set; } = delegate { };

    protected virtual Task<T?> OnBooleanSubmit(bool inputParameter)
    {
        return Task.FromResult(default(T?));
    }

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