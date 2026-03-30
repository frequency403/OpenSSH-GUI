using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using Avalonia.Media;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;
using OpenSSH_GUI.SshConfig.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public sealed partial class ConnectToServerViewModel : ViewModelBase<ConnectToServerViewModel>
{
    private readonly ServerConnectionService _serverConnectionService;
    private readonly IMessageBoxProvider _messageBoxProvider;

    public ConnectToServerViewModel(ILogger<ConnectToServerViewModel> logger,
        ServerConnectionService serverConnectionService,
        IMessageBoxProvider messageBoxProvider,
        IConfiguration configuration,
        SshKeyManager sshKeyManager) : base(logger)
    {
        _messageBoxProvider = messageBoxProvider;
        _serverConnectionService = serverConnectionService;
        SshKeyManager = sshKeyManager;
        SelectedPublicKey = SshKeyManager.SshKeys.FirstOrDefault();
        TestConnection = ReactiveCommand.CreateFromTask(TestConnectionAsync);
        ResetCommand = ReactiveCommand.Create(Reset);

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
            .WhenAnyValue(viewModel => viewModel.AuthWithPublicKey, model => model.AuthWithAllKeys, model => model._serverConnectionService.IsConnected)
            .Subscribe((tuple) =>
            {
                KeyComboBoxEnabled = tuple is { Item3: false, Item1: true, Item2: false };
            }).DisposeWith(Disposables);

        this.WhenAnyValue(viewModel => viewModel.ConnectionCredentials)
            .Subscribe(credentials =>
            {
                CanConnectToServer = credentials is not null;
            }).DisposeWith(Disposables);
        
        try
        {
            var config = configuration.GetSection("SshConfig").Get<SshConfiguration>();
            SshHostSettings = config?.Hosts.Distinct() ?? [];
        }
        catch (Exception e)
        {
            SshHostSettings = [];
            logger.LogDebug(e, "Config not readable");
        }
    }
    [Reactive]
    private IConnectionCredentials? _connectionCredentials;

    [Reactive] private bool _canConnectToServer;

    public ReactiveCommand<Unit, Unit> TestConnection { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public SshKeyManager SshKeyManager { get; }

    public bool EnablePreConfiguredHosts => SshHostSettings.Any();
    [Reactive] private SshHostSettings? _selectedHostSettings;

    public IEnumerable<SshHostSettings> SshHostSettings { get; }
    
    [Reactive] private bool _authWithPublicKey;

    [Reactive] private bool _authWithAllKeys;

    [Reactive] private SshKeyFile? _selectedPublicKey;

    [Reactive] private string _hostName = string.Empty;

    [Reactive] private string _username = string.Empty;

    [Reactive] private string _password = string.Empty;

    [Reactive] private bool _tryingToConnect;

    [Reactive] private string _statusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerStatusBase,
        StringsAndTexts.ConnectToServerStatusUntested);

    [Reactive] private string _statusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
        StringsAndTexts.ConnectToServerStatusUnknown);

    [Reactive] private IBrush _statusButtonBackground = Brushes.Gray;

    [Reactive] private bool _keyComboBoxEnabled;

    private async Task TestConnectionAsyncBase(CancellationToken cancellationToken = default)
    {
        if (ConnectionCredentials is not null)
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusSuccess);
            StatusButtonToolTip =
                string.Format(StringsAndTexts.ConnectToServerSshConnectionString, Username, HostName);
            StatusButtonBackground = Brushes.Green;
        }
        else
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusFailed);
            StatusButtonBackground = Brushes.Red;
        }

        TryingToConnect = false;

        if (_serverConnectionService.IsConnected)
        {
            await _serverConnectionService.CloseConnection(false, cancellationToken);
            return;
        }

        await _messageBoxProvider!.ShowMessageBoxAsync(StringsAndTexts.Error, StatusButtonToolTip);
    }

    private async Task TestConnectionAsync(SshHostSettings? hostSettings = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hostSettings);
        if (hostSettings.IdentityFiles is null)
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusFailed);
            StatusButtonBackground = Brushes.Red;
            return;
        }

        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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
            
            if(await _serverConnectionService.EstablishConnection(connectionCredentials, linkedTokenSource.Token))
                ConnectionCredentials = connectionCredentials;
        }
        catch (Exception exception)
        {
            StatusButtonToolTip = exception.Message;
        }

        await TestConnectionAsyncBase(cancellationToken);
    }

    private async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            if(string.IsNullOrWhiteSpace(HostName) || string.IsNullOrWhiteSpace(Username) || (SelectedPublicKey is null && string.IsNullOrWhiteSpace(Password)))
                throw new ArgumentException(StringsAndTexts.ConnectToServerValidationError);
            TryingToConnect = true;
            IConnectionCredentials? connectionCredentials = null;
            if (AuthWithPublicKey)
            {
                connectionCredentials = new KeyConnectionCredentials(HostName, Username, SelectedPublicKey);
            } else if (AuthWithAllKeys)
            {
                connectionCredentials = new MultiKeyConnectionCredentials(HostName, Username, SshKeyManager.SshKeys);
            }
            else
            {
                connectionCredentials = new PasswordConnectionCredentials(HostName, Username, Password);
            }
            if(await _serverConnectionService.EstablishConnection(connectionCredentials, linkedTokenSource.Token))
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
            if(!(await _serverConnectionService.EstablishConnection(ConnectionCredentials, cancellationToken)))
            {
                await _messageBoxProvider.ShowMessageBoxAsync(StringsAndTexts.Error, "Connection failed");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Unhandled error during connection");
            await _messageBoxProvider.ShowMessageBoxAsync(StringsAndTexts.Error, e.Message);
        }
    }

    private void Reset()
    {
        HostName = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
            StringsAndTexts.ConnectToServerStatusUnknown);
        StatusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerStatusBase,
            StringsAndTexts.ConnectToServerStatusUntested);
        StatusButtonBackground = Brushes.Gray;
        SelectedHostSettings = null;
        AuthWithAllKeys = false;
        AuthWithPublicKey = false;
        ConnectionCredentials = null;
    }
}