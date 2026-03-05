using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using OpenSSH_GUI.Core.MVVM;

namespace OpenSSH_GUI.Core.Extensions;

public static class ServiceCollectionExtensions
{
    private static bool ValidateNamingConvention<T1, T2>()
    {
        var t1Name  = typeof(T1).Name;
        var t2Name = typeof(T2).Name;
        if (t1Name == t2Name) return false;
        var option1 = string.Equals(t1Name.Replace("Window", ""), t2Name.Replace("ViewModel", ""),
            StringComparison.Ordinal);
        var option2 = t2Name.StartsWith(t1Name) && t2Name.EndsWith("ViewModel");
        return  option1 || option2;
    }
    
    public static IServiceCollection RegisterViewWithViewModel<TView, TViewModel>(this IServiceCollection services, bool registerAsSingleton = true)
        where TViewModel : ViewModelBase<TViewModel>
    where TView : Window
    {
        if(!ValidateNamingConvention<TView, TViewModel>())
            throw new InvalidOperationException($"Viewmodels must follow the following convention: $NameOfView + $ViewModel -> in that case your Viewmodel must be renamed to \"{typeof(TView).Name+"ViewModel"}\"");
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

    public static TView ResolveView<TView>(this IServiceProvider provider, WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterScreen) where TView : Window
    {
        var viewName = typeof(TView).Name;
        var resolvedView = provider.GetRequiredKeyedService<TView>(viewName);

        var viewModelType = typeof(TView).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == viewName + "ViewModel");

        if (viewModelType == null)
            throw new InvalidOperationException($"Could not find ViewModel for View '{viewName}'");

        resolvedView.DataContext = provider.GetRequiredKeyedService(viewModelType, viewModelType.Name);
        resolvedView.WindowStartupLocation = windowStartupLocation;
        return resolvedView;
    }
}