namespace OpenSSH_GUI.Core.MVVM;

public interface IInitializableViewModel
{
    /// <summary>
    ///     Asynchronously initializes the view model, performing necessary setup operations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous initialization operation.</returns>
    public ValueTask InitializeAsync(CancellationToken cancellationToken = default);
}

public interface IInitializableViewModel<in TParam>
{
    /// Asynchronously initializes the ViewModel with the specified parameters and optional cancellation token.
    /// Sets the state of the ViewModel as initialized upon completion.
    /// <param name="parameters">
    ///     The parameters used to initialize the ViewModel.
    /// </param>
    /// <param name="cancellationToken">
    ///     An optional token for observing cancellation requests.
    /// </param>
    /// <returns>
    ///     A ValueTask representing the asynchronous initialization operation.
    /// </returns>
    public ValueTask InitializeAsync(
        TParam parameters,
        CancellationToken cancellationToken = default);
}