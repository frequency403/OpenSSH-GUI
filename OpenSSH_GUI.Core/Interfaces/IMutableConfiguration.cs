using System.Linq.Expressions;

namespace OpenSSH_GUI.Core.Interfaces;

/// <summary>
/// Represents a writable configuration that allows dynamic updates and overrides of configuration values at runtime.
/// </summary>
/// <typeparam name="T">The type of the configuration class.</typeparam>
public interface IMutableConfiguration<T> : IDisposable where T : class
{
    /// <summary>
    /// Gets the current instance of the configuration object of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// This property provides access to the current state of the configuration as managed by the underlying options mechanism.
    /// It reflects the current configuration values without the need to manually reload or retrieve them.
    /// </remarks>
    T Current { get; }

    /// <summary>
    /// Asynchronously updates the configuration object by applying the specified update action.
    /// </summary>
    /// <param name="update">
    /// An action that performs updates on the configuration object.
    /// The current configuration is passed to this action.
    /// </param>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// </returns>
    Task ExecuteConfigurationUpdateAsync(Action<T> update, CancellationToken ct = default);

    /// <summary>
    /// Sets the value of a specific property in the writable configuration using an expression to target the property.
    /// </summary>
    /// <typeparam name="TValue">The type of the value being set.</typeparam>
    /// <param name="property">An expression representing the property to set.</param>
    /// <param name="value">The new value to assign to the specified property.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetPropertyValueAsync<TValue>(Expression<Func<T, TValue>> property, TValue value, CancellationToken ct = default);

    /// Asynchronously updates the configuration by setting a specific key to the provided value.
    /// <param name="key">The key in the configuration to set the value for.</param>
    /// <param name="value">The value to assign to the specified key.</param>
    /// <param name="ct">The optional cancellation token to cancel the operation.</param>
    /// <typeparam name="TValue">The type of the value being set.</typeparam>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetPropertyValueAsync<TValue>(string key, TValue value, CancellationToken ct = default);


    /// <summary>
    /// Occurs when the configuration is updated, signaling that changes have been applied to the configuration values.
    /// </summary>
    /// <remarks>
    /// Subscribing to this event allows components to react to configuration changes dynamically at runtime.
    /// This can be particularly useful for scenarios where live updates to settings or parameters require immediate processing.
    /// </remarks>
    event EventHandler<T> ConfigurationChanged;
}