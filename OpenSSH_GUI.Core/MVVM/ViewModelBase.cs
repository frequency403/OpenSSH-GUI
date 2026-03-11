using System.Reactive;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSH_GUI.Core.MVVM;

public abstract class ViewModelBase<T> : ViewModelBase where T : ViewModelBase
{
    protected ViewModelBase(ILogger<T> logger) : base(logger)
    {
        BooleanSubmit = ReactiveCommand.CreateFromTask<bool>(OnBooleanSubmitAsync);
        BooleanSubmit.Subscribe(_ =>
        {
            if (CloseOnBooleanSubmit)
                RequestClose();
        });
    }
    
    protected bool CloseOnBooleanSubmit { get; set; } = true;
    public ReactiveCommand<bool, Unit> BooleanSubmit { get; private init; }
    public EventHandler Close { get; set; } = delegate { };
    protected void RequestClose()
    {
        Close.Invoke(this, EventArgs.Empty);
    }

    protected virtual Task OnBooleanSubmitAsync(bool inputParameter, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public virtual ValueTask InitializeAsync(IInitializerParameters<T>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        IsInitialized = true;
        return ValueTask.CompletedTask;
    }
}

public abstract class ViewModelBase : ReactiveObject
{
    protected ViewModelBase(ILogger logger)
    {
        Logger = logger;
        ThrownExceptions.Subscribe(exception => Logger.LogError(exception, "Viewmodel threw an exception"));
    }
    
    public bool IsInitialized { get; protected set; }

    protected ILogger Logger { get; }
}

public interface IInitializerParameters<TViewModel> where TViewModel : ViewModelBase
{
}