// File Created by: Oliver Schantz
// Created: 21.05.2024 - 11:05:25
// Last edit: 21.05.2024 - 11:05:25

using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Resources.Wrapper;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ConnectionViewModel : ViewModelBase
{
    public ConnectionViewModel(ILogger<ConnectionViewModel> logger, OpenSshGuiDbContext context) : base(logger)
    {
        CredentialGroups = context.ConnectionCredentialsDtos.GroupBy(e => new
        {
            e.Hostname,
            e.Username
        }).ToList().Select(e => new CredentialGroup
        {
            Username = e.Key.Username,
            Hostname = e.Key.Hostname,
            Credentials = e.GroupBy(f => f.AuthType).ToDictionary(f => f.Key, f => f.Select(g => new ManagedCredential { Credentials = g.ToCredentials()}).ToList())
        }).ToList();
    }

    public ReactiveCommand<IConnectionCredentials, IConnectionCredentials> Remove =>
        ReactiveCommand.CreateFromTask<IConnectionCredentials, IConnectionCredentials>(
            async dto =>
            {
                await dto.RemoveDtoFromDatabase();
                return dto;
            });
    
    public ReactiveCommand<ManagedCredential,ManagedCredential> Connect =>
        ReactiveCommand.Create<ManagedCredential,ManagedCredential>(
            dto =>
            {
                CredentialInUse = dto;
                ServerConnection = new ServerConnection(CredentialInUse.Credentials);
                CredentialInUse.InUse = ServerConnection.IsConnected;
                return dto;
            });
    
    public List<CredentialGroup> CredentialGroups { get; set; }
    public IServerConnection ServerConnection { get; set; }
    public ManagedCredential CredentialInUse { get; set; }
    
    
    public ReactiveCommand<Unit, ConnectionViewModel?> Submit => ReactiveCommand.Create<Unit, ConnectionViewModel?>(a => this);
}