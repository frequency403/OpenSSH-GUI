using System.Reactive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.MVVM;

public abstract class ViewModelBase<TViewModel, TParameters>(ILogger<TViewModel>? logger = null)
    : ViewModelBase<TViewModel>(logger)
    where TViewModel : ViewModelBase
    where TParameters : class, IInitializerParameters<TViewModel>
{
    public virtual ValueTask InitializeAsync(
        TParameters parameters,
        CancellationToken cancellationToken = default)
    {
        IsInitialized = true;
        return ValueTask.CompletedTask;
    }
}

public abstract class ViewModelBase<TViewModel>(ILogger<TViewModel>? logger = null) : ViewModelBase(logger)
    where TViewModel : ViewModelBase
{
    public virtual ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsInitialized = true;
        return ValueTask.CompletedTask;
    }
}

public abstract partial class ViewModelBase : ReactiveObject
{
    [Reactive]
    private EventHandler _close = delegate { };
    
    [Reactive]
    private bool _isInitialized;
    
    protected ViewModelBase(ILogger? logger)
    {
        Logger = logger ?? NullLogger.Instance;
        ThrownExceptions.Subscribe(exception => Logger.LogError(exception, "Viewmodel threw an exception"));
        BooleanSubmit = ReactiveCommand.CreateFromTask<bool>(OnBooleanSubmitAsync);
        BooleanSubmit.Subscribe(_ =>
        {
            if (CloseOnBooleanSubmit)
                RequestClose();
        });
    }

    protected ILogger Logger { get; }
    private protected bool CloseOnBooleanSubmit { get; set; } = true;
    public ReactiveCommand<bool, Unit> BooleanSubmit { get; }

    protected void RequestClose()
    {
        Close.Invoke(this, EventArgs.Empty);
    }
    protected virtual Task OnBooleanSubmitAsync(bool inputParameter, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public interface IInitializerParameters<TViewModel> where TViewModel : ViewModelBase
{
}