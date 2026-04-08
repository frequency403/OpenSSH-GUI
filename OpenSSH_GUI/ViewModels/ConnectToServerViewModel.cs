using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;
using OpenSSH_GUI.SshConfig.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public sealed partial class ConnectToServerViewModel : ViewModelBase<ConnectToServerViewModel>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConnectToServerViewModel> _logger;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly ServerConnectionService _serverConnectionService;

    [Reactive] private bool _authWithAllKeys;

    [Reactive] private bool _authWithPublicKey;

    [Reactive] private bool _canConnectToServer;

    [Reactive] private ConnectionCredentials? _connectionCredentials;

    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _enablePreConfiguredHosts;

    [Reactive] private string _hostName = string.Empty;

    [Reactive] private bool _keyComboBoxEnabled;

    [Reactive] private string _password = string.Empty;
    [Reactive] private SshHostSettings? _selectedHostSettings;

    [Reactive] private SshKeyFile? _selectedPublicKey;

    [ReactiveCollection] private ObservableCollection<SshHostSettings> _sshHostSettings = [];

    [Reactive] private SshKeyManager _sshKeyManager;

    [Reactive] private IBrush _statusButtonBackground =
        Application.Current?.Resources["OverlayBrush"] as IBrush ?? Brushes.Gray;

    [Reactive] private string _statusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
        StringsAndTexts.ConnectToServerStatusUnknown);

    [Reactive] private string _statusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerStatusBase,
        StringsAndTexts.ConnectToServerStatusUntested);

    [Reactive] private bool _tryingToConnect;

    [Reactive] private string _username = string.Empty;

    public ConnectToServerViewModel(ILogger<ConnectToServerViewModel> logger,
        ServerConnectionService serverConnectionService,
        IMessageBoxProvider messageBoxProvider,
        IConfiguration configuration,
        SshKeyManager sshKeyManager) : base(logger)
    {
        _logger = logger;
        _messageBoxProvider = messageBoxProvider;
        _configuration = configuration;
        _serverConnectionService = serverConnectionService;
        SshKeyManager = sshKeyManager;
        SelectedPublicKey = SshKeyManager.SshKeys.FirstOrDefault();

        this
            .WhenAnyValue<ConnectToServerViewModel, SshHostSettings?>(viewModel => viewModel.SelectedHostSettings)
            .Subscribe(async settings =>
            {
                try
                {
                    if (settings is not null)
                        await TestConnectionAsync(settings);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error testing connection");
                }
            }).DisposeWith(Disposables);

        this
            .WhenAnyValue(viewModel => viewModel.AuthWithPublicKey, model => model.AuthWithAllKeys,
                model => model._serverConnectionService.IsConnected)
            .Subscribe(tuple => { KeyComboBoxEnabled = tuple is { Item3: false, Item1: true, Item2: false }; })
            .DisposeWith(Disposables);

        this.WhenAnyValue(viewModel => viewModel.ConnectionCredentials)
            .Subscribe(credentials => { CanConnectToServer = credentials is not null; }).DisposeWith(Disposables);

        Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => ((INotifyCollectionChanged)SshHostSettings).CollectionChanged += h,
                h => ((INotifyCollectionChanged)SshHostSettings).CollectionChanged -= h)
            .Select(_ => SshHostSettings.Count)
            .StartWith(SshHostSettings.Count)
            .Subscribe(count => { EnablePreConfiguredHosts = count > 0; })
            .DisposeWith(Disposables);
    }

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            SshHostSettings.Clear();
            foreach (var hostSettings in _configuration.GetSection("SshConfig").Get<SshConfiguration>()?.Hosts
                         .Distinct() ?? [])
            {
                _logger.LogDebug("Found host {host}", hostSettings.HostName);
                SshHostSettings.Add(hostSettings);
            }
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Config not readable");
        }

        return base.InitializeAsync(cancellationToken);
    }

    private async Task TestConnectionAsyncBase(CancellationToken cancellationToken = default)
    {
        if (ConnectionCredentials is not null)
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusSuccess);
            StatusButtonToolTip =
                string.Format(StringsAndTexts.ConnectToServerSshConnectionString, Username, HostName);
            StatusButtonBackground = Application.Current?.Resources["SuccessBrush"] as IBrush ?? Brushes.Green;
        }
        else
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusFailed);
            StatusButtonBackground = Application.Current?.Resources["ErrorBrush"] as IBrush ?? Brushes.Red;
        }

        TryingToConnect = false;
    }

    private async Task TestConnectionAsync(SshHostSettings? hostSettings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hostSettings);
        if (hostSettings.IdentityFiles is null)
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusFailed);
            StatusButtonBackground = Application.Current?.Resources["ErrorBrush"] as IBrush ?? Brushes.Red;
            return;
        }

        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (!Debugger.IsAttached)
            linkedTokenSource.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            TryingToConnect = true;
            HostName = hostSettings.HostName ?? string.Empty;
            Username = hostSettings.User ?? string.Empty;
            AuthWithAllKeys = true;

            var resolvedPaths = hostSettings.IdentityFiles
                .Select(f => f.ResolvePath())
                .ToHashSet(StringComparer.Ordinal);

            var keys = SshKeyManager.SshKeys
                .Where(e => e.KeyFileInfo?.KeyFileSource?.AbsolutePath is { } path
                            && resolvedPaths.Contains(path));

            var connectionCredentials = new MultiKeyConnectionCredentials(
                hostSettings.HostName ?? string.Empty,
                hostSettings.User ?? string.Empty,
                keys);

            if (await _serverConnectionService.EstablishConnection(connectionCredentials, linkedTokenSource.Token))
                ConnectionCredentials = connectionCredentials;
        }
        catch (Exception exception)
        {
            StatusButtonToolTip = exception.Message;
        }

        await TestConnectionAsyncBase(cancellationToken);
    }

    [ReactiveCommand]
    private async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            if (string.IsNullOrWhiteSpace(HostName) || string.IsNullOrWhiteSpace(Username) ||
                (SelectedPublicKey is null && string.IsNullOrWhiteSpace(Password)))
                throw new ArgumentException(StringsAndTexts.ConnectToServerValidationError);
            TryingToConnect = true;
            ConnectionCredentials? connectionCredentials = null;
            if (AuthWithPublicKey)
                connectionCredentials = new KeyConnectionCredentials(HostName, Username, SelectedPublicKey);
            else if (AuthWithAllKeys)
                connectionCredentials = new MultiKeyConnectionCredentials(HostName, Username, SshKeyManager.SshKeys);
            else
                connectionCredentials = new PasswordConnectionCredentials(HostName, Username, Password);
            if (await _serverConnectionService.EstablishConnection(connectionCredentials, linkedTokenSource.Token))
                ConnectionCredentials = connectionCredentials;
        }
        catch (Exception exception)
        {
            StatusButtonToolTip = exception.Message;
        }

        await TestConnectionAsyncBase(cancellationToken);
    }

    protected override async Task BooleanSubmitAsync(bool inputParameter, CancellationToken cancellationToken = default)
    {
        if (!inputParameter) return;
        if (!CanConnectToServer) return;
        if (ConnectionCredentials is null) return;
        try
        {
            if (!await _serverConnectionService.EstablishConnection(ConnectionCredentials, cancellationToken))
                await _messageBoxProvider.ShowMessageBoxAsync(StringsAndTexts.Error, "Connection failed");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Unhandled error during connection");
            await _messageBoxProvider.ShowMessageBoxAsync(StringsAndTexts.Error, e.Message);
        }
    }

    [ReactiveCommand]
    private void Reset()
    {
        HostName = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
            StringsAndTexts.ConnectToServerStatusUnknown);
        StatusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerStatusBase,
            StringsAndTexts.ConnectToServerStatusUntested);
        StatusButtonBackground = Application.Current?.Resources["OverlayBrush"] as IBrush ?? Brushes.Gray;
        SelectedHostSettings = null;
        AuthWithAllKeys = false;
        AuthWithPublicKey = false;
        ConnectionCredentials = null;
    }
}