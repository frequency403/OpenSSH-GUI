using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Resources.Wrapper;

namespace OpenSSH_GUI.Core.Extensions;

public static class DependencyInjectionExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> RequiredPropertiesCache = new();

    /// <summary>
    /// Returns all publicly writable properties of <paramref name="type"/> that are
    /// annotated with <see cref="RequiredMemberAttribute"/>, using a per-type cache
    /// to avoid repeated reflection overhead.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>An array of <see cref="PropertyInfo"/> representing the required properties.</returns>
    private static PropertyInfo[] GetRequiredProperties(Type type) =>
        RequiredPropertiesCache.GetOrAdd(type, static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetCustomAttribute<RequiredMemberAttribute>() is not null)
                .ToArray());

    /// <summary>
    /// Validates that <typeparamref name="T1"/> and <typeparamref name="T2"/> follow the
    /// prescribed View/ViewModel naming convention.
    /// </summary>
    /// <typeparam name="T1">The View type.</typeparam>
    /// <typeparam name="T2">The ViewModel type.</typeparam>
    /// <returns>
    /// <see langword="true"/> if the naming convention is satisfied; otherwise <see langword="false"/>.
    /// </returns>
    private static bool ValidateNamingConvention<T1, T2>()
    {
        var t1Name = typeof(T1).Name;
        var t2Name = typeof(T2).Name;
        if (t1Name == t2Name) return false;
        var option1 = string.Equals(t1Name.Replace("Window", ""), t2Name.Replace("ViewModel", ""),
            StringComparison.Ordinal);
        var option2 = t2Name.StartsWith(t1Name) && t2Name.EndsWith("ViewModel");
        return option1 || option2;
    }

    extension(IServiceProvider serviceProvider)
    {
        /// <summary>
        /// Resolves a <typeparamref name="TView"/> from a dedicated <see cref="IServiceScope"/>,
        /// initializes it with the provided <paramref name="initializerParameters"/>, and disposes
        /// the scope automatically when the window closes.
        /// </summary>
        /// <typeparam name="TView">The window type to resolve.</typeparam>
        /// <typeparam name="TViewModel">The ViewModel type associated with the view.</typeparam>
        /// <typeparam name="TViewModelInitializerParameter">The type of the initializer parameter passed to the ViewModel.</typeparam>
        /// <param name="initializerParameters">The parameter passed to <see cref="WindowBase{TViewModel,TViewModelInitializerParameter}.InitializeAsync"/>.</param>
        /// <param name="windowStartupLocation">The startup location of the window.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe during initialization.</param>
        /// <returns>The fully initialized <typeparamref name="TView"/> instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the ViewModel is <see langword="null"/> or was not properly initialized.
        /// </exception>
        public async ValueTask<TView> ResolveViewAsync<TView, TViewModel, TViewModelInitializerParameter>(
            TViewModelInitializerParameter initializerParameters,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterScreen,
            CancellationToken token = default)
            where TView : WindowBase<TViewModel, TViewModelInitializerParameter>
            where TViewModel : ViewModelBase<TViewModelInitializerParameter>
        {
            var scope = serviceProvider.CreateScope();
            IServiceScope? scopeOwnership = scope;
            try
            {
                var resolvedView = scope.ServiceProvider.GetRequiredKeyedService<TView>(typeof(TView).Name);
                await resolvedView.InitializeAsync(initializerParameters, windowStartupLocation, token);
                ArgumentNullException.ThrowIfNull(resolvedView.ViewModel);

                if (!resolvedView.ViewModel.IsInitialized)
                    throw new InvalidOperationException("ViewModel not properly initialized");

                resolvedView.Closed += async (_, _) =>
                {
                    if (scope is IAsyncDisposable asyncScope)
                        await asyncScope.DisposeAsync();
                    else
                        scope.Dispose();
                };

                scopeOwnership = null;
                return resolvedView;
            }
            finally
            {
                scopeOwnership?.Dispose();
            }
        }

        /// <summary>
        /// Resolves a <typeparamref name="TView"/> from a dedicated <see cref="IServiceScope"/>,
        /// initializes it, and disposes the scope automatically when the window closes.
        /// </summary>
        /// <typeparam name="TView">The window type to resolve.</typeparam>
        /// <typeparam name="TViewModel">The ViewModel type associated with the view.</typeparam>
        /// <param name="windowStartupLocation">The startup location of the window.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe during initialization.</param>
        /// <returns>The fully initialized <typeparamref name="TView"/> instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the ViewModel is <see langword="null"/> or was not properly initialized.
        /// </exception>
        public async ValueTask<TView> ResolveViewAsync<TView, TViewModel>(
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterScreen,
            CancellationToken token = default)
            where TView : WindowBase<TViewModel>
            where TViewModel : ViewModelBase
        {
            var scope = serviceProvider.CreateScope();
            IServiceScope? scopeOwnership = scope;
            try
            {
                var resolvedView = scope.ServiceProvider.GetRequiredKeyedService<TView>(typeof(TView).Name);
                await resolvedView.InitializeAsync(windowStartupLocation, token);
                ArgumentNullException.ThrowIfNull(resolvedView.ViewModel);

                if (!resolvedView.ViewModel.IsInitialized)
                    throw new InvalidOperationException("ViewModel not properly initialized");

                resolvedView.Closed += async (_, _) =>
                {
                    if (scope is IAsyncDisposable asyncScope)
                        await asyncScope.DisposeAsync();
                    else
                        scope.Dispose();
                };

                scopeOwnership = null;
                return resolvedView;
            }
            finally
            {
                scopeOwnership?.Dispose();
            }
        }
    }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <typeparamref name="TView"/> and <typeparamref name="TViewModel"/> as a
        /// keyed pair in the service collection, enforcing the View/ViewModel naming convention.
        /// Required properties on the view are resolved and injected via <see cref="IServiceProvider"/>
        /// at activation time.
        /// </summary>
        /// <typeparam name="TView">The window type to register.</typeparam>
        /// <typeparam name="TViewModel">The ViewModel type to register.</typeparam>
        /// <param name="lifetime">The <see cref="ServiceLifetime"/> for both registrations.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the naming convention between <typeparamref name="TView"/> and
        /// <typeparamref name="TViewModel"/> is not satisfied.
        /// </exception>
        public void RegisterViewWithViewModel<TView, TViewModel>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TViewModel : ViewModelBase
            where TView : Window
        {
            var viewType = typeof(TView);
            var viewModelType = typeof(TViewModel);
            if (!ValidateNamingConvention<TView, TViewModel>())
                throw new InvalidOperationException(
                    $"Viewmodels must follow the following convention: $NameOfView + $ViewModel -> in that case your Viewmodel must be renamed to \"{viewType.Name + "ViewModel"}\"");

            var serviceDescriptorView = ServiceDescriptor.DescribeKeyed(viewType, viewType.Name, (provider, _) =>
            {
                if (ActivatorUtilities.CreateInstance(provider, viewType) is not TView view)
                    throw new InvalidOperationException();
                foreach (var requiredProperty in GetRequiredProperties(viewType))
                {
                    if (provider.GetService(requiredProperty.PropertyType) is { } service)
                        requiredProperty.SetValue(view, service);
                }
                return view;
            }, lifetime);

            var serviceDescriptorViewModel =
                ServiceDescriptor.DescribeKeyed(viewModelType, viewModelType.Name, viewModelType, lifetime);

            services.Add(serviceDescriptorView);
            services.Add(serviceDescriptorViewModel);
        }
    }
}