using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.MVVM;

/// <summary>
/// Serves as a base class for all view models in the MVVM pattern within the application.
/// Provides core properties, methods, and initialization logic.
/// </summary>
public abstract class ViewModelBase<TViewModel, TParameters>(ILogger<TViewModel>? logger = null)
    : ViewModelBase<TViewModel>(logger)
    where TViewModel : ViewModelBase<TViewModel>
    where TParameters : class, IInitializerParameters<TViewModel>
{
    /// Asynchronously initializes the ViewModel with the specified parameters and optional cancellation token.
    /// Sets the state of the ViewModel as initialized upon completion.
    /// <param name="parameters">
    /// The parameters used to initialize the ViewModel.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token for observing cancellation requests.
    /// </param>
    /// <returns>
    /// A ValueTask representing the asynchronous initialization operation.
    /// </returns>
    public virtual ValueTask InitializeAsync(
        TParameters parameters,
        CancellationToken cancellationToken = default)
    {
        IsInitialized = true;
        Activator.Activate().DisposeWith(Disposables);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Represents a base class for all ViewModel implementations in the MVVM architecture.
/// This class provides core functionality such as exception handling, initialization,
/// and common command execution logic required by derived ViewModels.
/// </summary>
/// <remarks>
/// Inherits from ReactiveUI.ReactiveObject to facilitate reactive programming.
/// Integrates with ILogger for logging purposes and supports exception handling via a reactive subscription.
/// Defines commands and methods that assist in the management of ViewModel-specific operations.
/// </remarks>
public abstract class ViewModelBase<TViewModel>(ILogger<TViewModel>? logger = null)
    : ViewModelBase(logger), IActivatableViewModel, IViewFor<TViewModel>
    where TViewModel : ViewModelBase
{
    /// <summary>
    /// Asynchronously initializes the view model, performing necessary setup operations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous initialization operation.</returns>
    public virtual ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsInitialized = true;
        Activator.Activate().DisposeWith(Disposables);
        return ValueTask.CompletedTask;
    }
    
    public TViewModel? ViewModel { get; set; }
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as TViewModel;
    }

    public ViewModelActivator Activator { get; } = new();
}

/// <summary>
/// Serves as an abstract base class for view models, providing shared properties, commands,
/// and behaviors for managing the interaction between the view and the application logic.
/// </summary>
public abstract partial class ViewModelBase : ReactiveObject,  IDisposable, IAsyncDisposable
{
    protected readonly CompositeDisposable Disposables;
    
    /// <summary>
    /// Represents an event handler used to signal close requests for the view model.
    /// This private field can be invoked internally to notify subscribers of the close event.
    /// </summary>
    [Reactive]
    private EventHandler _close = delegate { };

    /// <summary>
    /// Indicates whether the ViewModel has been initialized successfully.
    /// This flag is used to track the internal state of the ViewModel and ensure that
    /// initialization processes are not repeated or called prematurely.
    /// </summary>
    [Reactive]
    private bool _isInitialized;

    /// <summary>
    /// Serves as a base class for view models, providing initialization
    /// support, exception logging, and reactive command functionality.
    /// </summary>
    protected ViewModelBase(ILogger? logger)
    {
        Disposables = new CompositeDisposable();
        Logger = logger ?? NullLogger.Instance;
        ThrownExceptions
            .Subscribe(exception => Logger.LogError(exception, "Viewmodel threw an exception"))
            .DisposeWith(Disposables);
        BooleanSubmitCommand.Subscribe(_ =>
        {
            if (CloseOnBooleanSubmit)
                RequestClose();
        }).DisposeWith(Disposables);
    }

    /// <summary>
    /// Provides an instance of the logger.
    /// </summary>
    /// <remarks>
    /// The <see cref="Logger"/> property gives access to the logging functionality
    /// provided by the Microsoft.Extensions.Logging framework. It is used to log
    /// messages, exceptions, and other runtime information throughout the lifecycle
    /// of the ViewModel. This property is primarily intended for internal use by
    /// the ViewModel to handle various events and errors gracefully.
    /// </remarks>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the view model should automatically
    /// request to close when the <see cref="BooleanSubmitCommand"/> command is executed.
    /// </summary>
    /// <remarks>
    /// When set to true, the view model invokes the <see cref="RequestClose"/> method
    /// after the execution of the <see cref="BooleanSubmitCommand"/> command. This behavior
    /// enables automatic closure of the view upon certain operations.
    /// </remarks>
    private protected bool CloseOnBooleanSubmit { get; set; } = true;

    /// <summary>
    /// Triggers a request to close the associated view or component.
    /// </summary>
    /// <remarks>
    /// This method raises the internal <c>Close</c> event, signaling that
    /// the ViewModel intends to close. It is primarily used in scenarios
    /// where the ViewModel is responsible for managing its own lifecycle transitions.
    /// </remarks>
    protected void RequestClose()
    {
        Close.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles the submission of a boolean input asynchronously.
    /// This method can contain custom logic to process the submitted boolean
    /// and perform required asynchronous operations.
    /// </summary>
    /// <param name="inputParameter">The boolean input parameter supplied during submission.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ReactiveCommand]
    protected virtual Task BooleanSubmitAsync(bool inputParameter, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Disposables.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Represents a set of initialization parameters for a specific ViewModel type.
/// This interface is used to define the structure of the data required to initialize a ViewModel.
/// </summary>
/// <typeparam name="TViewModel">
/// The type of the ViewModel that utilizes this initializer parameters implementation.
/// Must inherit from <see cref="ViewModelBase{TViewModel}"/>.
/// </typeparam>
public interface IInitializerParameters<TViewModel> where TViewModel : ViewModelBase
{
}