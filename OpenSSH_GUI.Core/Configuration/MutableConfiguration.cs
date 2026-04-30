using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSSH_GUI.Core.Interfaces;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Configuration;

/// <inheritdoc/>
public sealed class MutableConfiguration<T> : IMutableConfiguration<T>
    where T : class
{
    private readonly ILogger<MutableConfiguration<T>> _logger;
    private readonly JsonFileConfigurationWriter<T> _writer;
    private readonly IOptionsMonitor<T> _options;
    private readonly IDisposable? _optionsMonitor;

    
    public MutableConfiguration(ILogger<MutableConfiguration<T>> logger,
        JsonFileConfigurationWriter<T> writer,
        IOptionsMonitor<T> options)
    {
        _logger = logger;
        _writer = writer;
        _options = options;
        _optionsMonitor = _options.OnChange(conf =>
        {
            _logger.LogDebug("Configuration changed triggered");
            ConfigurationChanged?.Invoke(this, conf);
        });
    }

    /// <inheritdoc/>
    public T Current => _options.CurrentValue;

    /// <inheritdoc/>
    public Task ExecuteConfigurationUpdateAsync(Action<T> update, CancellationToken ct = default) =>
        _writer.UpdateAsync(current =>
        {
            try
            {
                var config = current ?? throw new InvalidOperationException("Configuration could not be read.");
                update(config);
                return Task.FromResult(config);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating configuration.");
                throw;
            }
        }, ct);

    /// <inheritdoc/>
    public Task SetPropertyValueAsync<TValue>(Expression<Func<T, TValue>> property, TValue value, CancellationToken ct = default) =>
        _writer.UpdateAsync(current =>
        {
            try
            {
                var config = current ?? throw new InvalidOperationException("Configuration could not be read.");

                // Unwrap expression (handle Convert)
                var memberExpression = property.Body switch
                {
                    MemberExpression m => m,
                    UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression m } => m,
                    _ => throw new InvalidOperationException(
                        $"Expression '{property}' does not refer to a property.")
                };

                if (memberExpression.Member is not PropertyInfo { CanWrite: true } propertyInfo)
                    throw new InvalidOperationException(
                        $"Expression '{property}' does not refer to a writable property.");

                // Convert value if necessary
                var targetType = propertyInfo.PropertyType;
                object? convertedValue = value;

                if (value is not null && !targetType.IsAssignableFrom(typeof(TValue)))
                {
                    convertedValue = ConvertValue(value, targetType);
                }

                SetPropertyValue(propertyInfo, config, convertedValue);
                return Task.FromResult(config);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating configuration.");
                throw;
            }
        }, ct);

    /// <inheritdoc/>
    public Task SetPropertyValueAsync<TValue>(string key, TValue value, CancellationToken ct = default) =>
        _writer.UpdateAsync(current =>
        {
            try
            {
                var config = current ?? throw new InvalidOperationException("Configuration could not be read.");
                SetPropertyValue(typeof(T).GetProperty(
                                     key,
                                     BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                 ?? throw new InvalidOperationException($"Property '{key}' was not found on type '{typeof(T).Name}'."), config, value);
                return Task.FromResult(config);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating configuration.");
                throw;
            }
        }, ct);
    
    /// <inheritdoc/>
    public event EventHandler<T>? ConfigurationChanged;
    
    /// <inheritdoc/>
    public void Dispose()
    {
        _optionsMonitor?.Dispose();
    }
    
    
    private void SetPropertyValue<TValue>(PropertyInfo propertyInfo, T config, TValue value)
    {
        if (!propertyInfo.CanWrite)
            throw new InvalidOperationException($"Property '{propertyInfo.Name}' on type '{typeof(T).Name}' is not writable.");
        var initialValue = propertyInfo.GetValue(config);
        propertyInfo.SetValue(config, value);
        _logger.LogDebug("Updated property {PropertyName} of configuration object '{ConfigurationType}' from {InitialValue} to {CurrentValue}", propertyInfo.Name, typeof(T).Name, initialValue, value);
    }
    
    private static object? ConvertValue(object value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (underlyingType.IsEnum)
                return Enum.ToObject(underlyingType, value);

            return Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            // fallback: try direct cast
            if (targetType.IsInstanceOfType(value))
                return value;

            throw new InvalidCastException(
                $"Cannot convert value '{value}' to type '{targetType}'.");
        }
    }
}