using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSH_GUI.Core.MVVM;

public class ViewModelBase<T> : ViewModelBase where T : ViewModelBase
{
    public ViewModelBase(ILogger<T>? logger = null) : base(typeof(T), logger)
    {
        BooleanSubmit = ReactiveCommand.CreateFromTask<bool, T?>(async e => await OnBooleanSubmitAsync(e));
    }

    public ReactiveCommand<bool, T?> BooleanSubmit { get; private init; }

    public EventHandler Close { get; set; } = delegate { };

    protected virtual ValueTask<T?> OnBooleanSubmitAsync(bool inputParameter)
    {
        return ValueTask.FromResult(default(T?));
    }

    protected void RequestClose()
    {
        Close.Invoke(this, EventArgs.Empty);
    }

    public virtual ValueTask InitializeAsync(IInitializerParameters<T>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        IsInitialized = true;
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