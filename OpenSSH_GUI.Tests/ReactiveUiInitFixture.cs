using Avalonia;
using Avalonia.Headless;
using ReactiveUI.Builder;

namespace OpenSSH_GUI.Tests;

/// <summary>
/// Assembly-wide fixture that initializes ReactiveUI core services
/// before any test runs. Required because <see cref="ReactiveUI.ReactiveCommand"/>
/// and related types throw if ReactiveUI has not been bootstrapped.
/// </summary>
public sealed class ReactiveUiInitFixture
{
    public ReactiveUiInitFixture()
    {
        AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();

        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }
}