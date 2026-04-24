using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using ReactiveUI.Builder;

namespace OpenSSH_GUI.Tests;

/// <summary>
///     Assembly-wide fixture that initializes ReactiveUI core services
///     before any test runs. Required because <see cref="ReactiveUI.ReactiveCommand" />
///     and related types throw if ReactiveUI has not been bootstrapped.
/// </summary>
/// <summary>
///     Assembly-wide fixture that runs a dedicated Avalonia UI thread with a
///     live dispatcher loop. Required because <see cref="Avalonia.AvaloniaObject" />
///     enforces UI-thread access, deadlocks
///     without a running message loop.
/// </summary>
public sealed class ReactiveUiInitFixture : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ManualResetEventSlim _initialized = new();

    public ReactiveUiInitFixture()
    {
        var uiThread = new Thread(() =>
        {
            AppBuilder.Configure<Application>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();

            RxAppBuilder.CreateReactiveUIBuilder()
                .WithCoreServices()
                .BuildApp();

            _initialized.Set();

            Dispatcher.UIThread.MainLoop(_cts.Token);
        });

        uiThread.IsBackground = true;
        uiThread.Start();

        _initialized.Wait();
    }

    /// <inheritdoc />
    public void Dispose() { _cts.Cancel(); }
}