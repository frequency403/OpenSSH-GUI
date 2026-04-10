using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DryIoc;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;
using Renci.SshNet;
using SkiaSharp;
using Svg.Skia;

namespace OpenSSH_GUI;

[UsedImplicitly]
public class App(ILogger<App> logger, IResolver resolver, IRegistrator registrator) : Application
{
    private static readonly Dictionary<float, float> IconSizes = new()
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
            SshNetLoggingConfiguration.InitializeLogging(resolver.Resolve<ILoggerFactory>());
            try
            {
                foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
                {
                    foreach (var (width, height) in IconSizes)
                    {
                        await using var svgStream = AssetLoader.Open(new Uri(
                            $"avares://OpenSSH_GUI/Assets/openssh-gui{(variant is ThemeVariant.Light ? "-light" : string.Empty)}.svg"));
                        var memoryStream = new MemoryStream();
                        using var svg = new SKSvg();
                        svg.Load(svgStream);
                        var bitmap = new SKBitmap((int)width, (int)height, true);
                        using (var canvas = new SKCanvas(bitmap))
                        {
                            if (Math.Min(width / (svg.Picture?.CullRect.Width ?? width),
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
                        var serviceKey = string.Join("_", nameof(Bitmap), width).ToLower();
                        registrator.RegisterInstance(bm, serviceKey: serviceKey,
                            ifAlreadyRegistered: IfAlreadyRegistered.Replace);
                    }

                    registrator.RegisterInstance(
                        new WindowIcon(resolver.Resolve<Bitmap>(string.Join("_", nameof(Bitmap), 32).ToLower())),
                        serviceKey: string.Join("_", nameof(WindowIcon), 32, variant).ToLower());
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error creating app icons");
                throw;
            }

            try
            {
                if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
                desktop.MainWindow = await resolver.ResolveViewAsync<MainWindow, MainWindowViewModel>();
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

    /// <summary>
    ///     Triggers the initial SSH key search after the main window has been presented,
    ///     ensuring the UI is fully ready before background work begins.
    /// </summary>
    private void OnMainWindowOpened(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Window window)
                window.Opened -= OnMainWindowOpened;

            try
            {
                resolver.Resolve<SshKeyManager>().InitialSearchAsync();
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