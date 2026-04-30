using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Configuration;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Resources;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;
using Renci.SshNet;
using Serilog.Core;
using SkiaSharp;
using Svg.Skia;

namespace OpenSSH_GUI;

internal class DoubleToleranceComparer(double epsilon) : IEqualityComparer<double>
{
    public bool Equals(double x, double y) => Math.Abs(x - y) < epsilon;

    public int GetHashCode(double obj) => 0;
}

[UsedImplicitly]
public class App(
    ILogger<App> logger,
    IServiceProvider serviceProvider,
    AppIconStore iconStore,
    IHostApplicationLifetime hostApplicationLifetime) : Application
{
    private const string RessourceUri = "avares://OpenSSH_GUI/Assets/openssh-gui{0}.svg";
    private const string Underline = "_";
    internal const string SystemFontSize = "SystemFontSize";
    internal const string BaseFontSize = "BaseFontSize";
    private const string MaterialIconSize = "MaterialIconSize";
    private static readonly CompositeDisposable Disposables = new();

    private static readonly Dictionary<int, int> IconSizes = new()
    {
        { 16, 16 },
        { 32, 32 },
        { 48, 48 },
        { 64, 64 },
        { 128, 128 },
        { 256, 256 },
        { 512, 512 }
    };

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            base.OnFrameworkInitializationCompleted();
            SshNetLoggingConfiguration.InitializeLogging(serviceProvider.GetRequiredService<ILoggerFactory>());
            try
            {
                foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
                {
                    foreach (var (width, height) in IconSizes)
                    {
                        await using var svgStream = AssetLoader.Open(
                            new Uri(
                                string.Format(RessourceUri, variant is ThemeVariant.Light ? "-light" : string.Empty)));
                        var memoryStream = new MemoryStream();
                        using var svg = new SKSvg();
                        svg.Load(svgStream);
                        var bitmap = new SKBitmap(width, height, true);
                        using (var canvas = new SKCanvas(bitmap))
                        {
                            if (Math.Min(
                                    width / (svg.Picture?.CullRect.Width ?? width),
                                    height / (svg.Picture?.CullRect.Height ?? height)) is { } scale and > 0)
                                canvas.Scale(scale);
                            canvas.Clear(SKColors.Transparent);
                            canvas.DrawPicture(svg.Picture);
                        }

                        using (var data = SKImage.FromBitmap(bitmap))
                        {
                            if (data != null)
                            {
                                using var dataToEncode = data.Encode(SKEncodedImageFormat.Png, 100);
                                if (dataToEncode is null) continue;
                                memoryStream.Write(dataToEncode.AsSpan());
                                memoryStream.Seek(0, SeekOrigin.Begin);
                            }
                        }

                        var bm = new Bitmap(memoryStream);
                        var bitmapKey = string.Join(Underline, nameof(Bitmap), width, variant).ToLower();
                        iconStore.AddBitmap(bitmapKey, bm);
                    }

                    var iconKey = string.Join(Underline, nameof(WindowIcon), 32, variant).ToLower();
                    var bitmapRef = iconStore.GetBitmap(string.Join(Underline, nameof(Bitmap), 32, variant).ToLower());
                    if (bitmapRef is not null)
                        iconStore.AddWindowIcon(iconKey, new WindowIcon(bitmapRef));
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error creating app icons");
                throw;
            }

            try
            {
                ApplyConfiguration();
                if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
                desktop.MainWindow = await serviceProvider.ResolveViewAsync<MainWindow, MainWindowViewModel>();

                if (Current is not null)
                {
                    Current.Resources
                        .GetResourceObservable(SystemFontSize)
                        .Select(fs => fs as double?)
                        .Where(fs => fs.HasValue)
                        .Select(fs => fs!.Value)
                        .DistinctUntilChanged(new DoubleToleranceComparer(0.1))
                        .Subscribe(FontSizeChanged)
                        .DisposeWith(Disposables);
                    if (Current.TryFindResource(BaseFontSize, out var fontSize) &&
                        fontSize is double fontSizeValueDouble)
                    {
                        var fontSizeValue = fontSizeValueDouble * desktop.MainWindow.RenderScaling;
                        Current.Resources[SystemFontSize] = fontSizeValue;
                    }
                }

                logger.LogInformation("MainWindow created");
                desktop.MainWindow.Opened += OnMainWindowOpened;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during application initialization");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled error during application initialization");
        }
    }

    private void ApplyConfiguration()
    {
        var configuration = serviceProvider.GetRequiredService<IMutableConfiguration<ApplicationConfiguration>>().Current;
        Resources[SystemFontSize] = configuration.FontSize;
        RequestedThemeVariant = configuration.PreferredTheme switch
        {
            ThemeVariant.Light => Avalonia.Styling.ThemeVariant.Light,
            ThemeVariant.Dark => Avalonia.Styling.ThemeVariant.Dark,
            _ => Avalonia.Styling.ThemeVariant.Default
        };

        var levelSwitch = serviceProvider.GetRequiredService<LoggingLevelSwitch>();
        levelSwitch.MinimumLevel = configuration.LogLevel;
    }

    private static void FontSizeChanged(double fontSize)
    {
        if (Current is null) return;
        var materialIconSize = fontSize + 4;
        Current.Resources[MaterialIconSize] = materialIconSize;
    }

    /// <summary>
    ///     Triggers the initial SSH key search after the main window has been presented,
    ///     ensuring the UI is fully ready before background work begins.
    /// </summary>
    private async void OnMainWindowOpened(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Window window)
            {
                window.Opened -= OnMainWindowOpened;
                window.Topmost = true;
                logger.LogDebug("Trying to bring {WindowName} to front", sender?.GetType().Name ?? "null");
                window.Activate();
                
                Dispatcher.Post(() =>
                {
                    logger.LogDebug("Window is not set as topmost anymore");
                    window.Topmost = false;
                }, DispatcherPriority.Background);
            }

            try
            {
                await serviceProvider.GetRequiredService<SshKeyManager>().InitialSearchAsync(hostApplicationLifetime.ApplicationStopping);
                logger.LogInformation("Initial key search completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Initial key search failed");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error handling MainWindowOpened event");
        }
    }
}