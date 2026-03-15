using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Resources.Wrapper;

namespace OpenSSH_GUI.Core.Extensions;

public static class ServiceCollectionExtensions
{
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

    public static IServiceCollection RegisterViewWithViewModel<TView, TViewModel>(this IServiceCollection services,
        bool registerAsSingleton = false, Action<IServiceCollection>? configure = null)
        where TViewModel : ViewModelBase<TViewModel>
        where TView : Window
    {
        if (!ValidateNamingConvention<TView, TViewModel>())
            throw new InvalidOperationException(
                $"Viewmodels must follow the following convention: $NameOfView + $ViewModel -> in that case your Viewmodel must be renamed to \"{typeof(TView).Name + "ViewModel"}\"");
        configure?.Invoke(services);
        if (registerAsSingleton)
        {
            services.AddKeyedSingleton<TView>(typeof(TView).Name);
            services.AddKeyedSingleton<TViewModel>(typeof(TViewModel).Name);
        }
        else
        {
            services.AddKeyedTransient<TView>(typeof(TView).Name);
            services.AddKeyedTransient<TViewModel>(typeof(TViewModel).Name);
        }

        return services;
    }

    public static async ValueTask<TView> ResolveViewAsync<TView, TViewModel>(this IServiceProvider provider,
        IInitializerParameters<TViewModel>? initializerParameters = null,
        WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterScreen,
        CancellationToken token = default)
        where TView : WindowBase<TViewModel>
        where TViewModel : ViewModelBase<TViewModel>
    {
        var viewName = typeof(TView).Name;
        var resolvedView = provider.GetRequiredKeyedService<TView>(viewName);
        var viewModelName = typeof(TViewModel).Name;
        var viewModel = provider.GetRequiredKeyedService<TViewModel>(viewModelName);
        await viewModel.InitializeAsync(initializerParameters, token);
        if (!viewModel.IsInitialized)
            throw new InvalidOperationException("ViewModel not properly initialized");
        resolvedView.DataContext = viewModel;
        resolvedView.WindowStartupLocation = windowStartupLocation;
        resolvedView.AttachCloseRequest();
        return resolvedView;
    }
}