using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.MVVM;

/// <summary>
///     Serves as a base class for all view models in the MVVM pattern within the application.
///     Provides core properties, methods, and initialization logic.
/// </summary>
public abstract class ViewModelBase<TParameters> : ViewModelBase, IInitializableViewModel<TParameters>
{
    /// <inheritdoc />
    public virtual ValueTask InitializeAsync(
        TParameters? parameters,
        CancellationToken cancellationToken = default)
    {
        IsInitialized = true;
        Activator.Activate().DisposeWith(Disposables);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public sealed override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        return InitializeAsync(default, cancellationToken);
    }
}

/// <summary>
///     Serves as an abstract base class for view models, providing shared properties, commands,
///     and behaviors for managing the interaction between the view and the application logic.
/// </summary>
public abstract partial class ViewModelBase : ReactiveObject, IDisposable, IAsyncDisposable, IActivatableViewModel,
    IInitializableViewModel
{
    protected readonly CompositeDisposable Disposables;

    /// <summary>
    ///     Represents an event handler used to signal close requests for the view model.
    ///     This private field can be invoked internally to notify subscribers of the close event.
    /// </summary>
    [Reactive] private EventHandler _close = delegate { };

    /// <summary>
    ///     Indicates whether the ViewModel has been initialized successfully.
    ///     This flag is used to track the internal state of the ViewModel and ensure that
    ///     initialization processes are not repeated or called prematurely.
    /// </summary>
    [Reactive] private bool _isInitialized;

    /// <summary>
    ///     Serves as a base class for view models, providing initialization
    ///     support, exception logging, and reactive command functionality.
    /// </summary>
    protected ViewModelBase()
    {
        Disposables = new CompositeDisposable();
        BooleanSubmitCommand.Subscribe(_ =>
        {
            if (CloseOnBooleanSubmit)
                RequestClose();
        }).DisposeWith(Disposables);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the view model should automatically
    ///     request to close when the <see cref="BooleanSubmitCommand" /> command is executed.
    /// </summary>
    /// <remarks>
    ///     When set to true, the view model invokes the <see cref="RequestClose" /> method
    ///     after the execution of the <see cref="BooleanSubmitCommand" /> command. This behavior
    ///     enables automatic closure of the view upon certain operations.
    /// </remarks>
    protected bool CloseOnBooleanSubmit { get; set; } = true;

    /// <summary>
    ///     Provides the activator for the view model, enabling activation and deactivation
    ///     of reactive components tied to the lifecycle of the view model. This property
    ///     supports managing subscriptions and other reactive resources.
    /// </summary>
    public ViewModelActivator Activator { get; } = new();

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Disposables.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public virtual ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsInitialized = true;
        Activator.Activate().DisposeWith(Disposables);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     Triggers a request to close the associated view or component.
    /// </summary>
    /// <remarks>
    ///     This method raises the internal <c>Close</c> event, signaling that
    ///     the ViewModel intends to close. It is primarily used in scenarios
    ///     where the ViewModel is responsible for managing its own lifecycle transitions.
    /// </remarks>
    protected void RequestClose()
    {
        Close.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Handles the submission of a boolean input asynchronously.
    ///     This method can contain custom logic to process the submitted boolean
    ///     and perform required asynchronous operations.
    /// </summary>
    /// <param name="inputParameter">The boolean input parameter supplied during submission.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ReactiveCommand]
    protected virtual Task BooleanSubmitAsync(bool inputParameter, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Displays the attached <see cref="FlyoutBase" /> for a specified control.
    /// </summary>
    /// <param name="parameter">
    ///     The control for which the flyout will be displayed. This parameter must be of type <see cref="Control" />.
    /// </param>
    [ReactiveCommand]
    private void OpenFlyout(object? parameter)
    {
        if (parameter is Control control)
            FlyoutBase.ShowAttachedFlyout(control);
    }
}