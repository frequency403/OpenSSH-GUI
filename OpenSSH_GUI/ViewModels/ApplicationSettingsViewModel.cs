#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:41

#endregion

using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using OpenSSH_GUI.Core;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Settings;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Views;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public sealed class ApplicationSettingsViewModel(ILogger<ApplicationSettingsViewModel> logger, KeyLocatorService locatorService, IServiceProvider serviceProvider, IDialogHost dialogHost, OpenSshGuiDbContext dbContext) : ViewModelBase<ApplicationSettingsViewModel>(logger)
{
    private bool _convertPpkAutomatically;

    private List<IConnectionCredentials> _knownServers;

    private Settings _settings;

    public ObservableCollection<ISshKey> Keys = []; // = locatorSerivce.SshKeys
    public Interaction<EditSavedServerEntryViewModel, EditSavedServerEntryViewModel?> ShowEditEntry = new();

    public override async ValueTask InitializeAsync(IInitializerParameters<ApplicationSettingsViewModel>? initializerParameters = null, CancellationToken cancellationToken = default)
    {
        _settings = dbContext.Settings.First();
        _convertPpkAutomatically = _settings.ConvertPpkAutomatically;
        _knownServers = await dbContext.ConnectionCredentialsDtos.Select(e => e.ToCredentials()).ToListAsync(cancellationToken: cancellationToken);

        BooleanSubmit = ReactiveCommand.CreateFromTask<bool, ApplicationSettingsViewModel?>(async e =>
        {
            if (!e) return this;
            await using var context = new OpenSshGuiDbContext();
            var file = context.Settings.Update(_settings).Entity;
            file.ConvertPpkAutomatically = ConvertPpkAutomatically;
            var knownServerIds = KnownServers.Select(s => s.Id).ToList();
            await context.ConnectionCredentialsDtos.Where(f => !knownServerIds.Contains(f.Id)).ExecuteDeleteAsync(cancellationToken: cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return this;
        });
        await base.InitializeAsync(initializerParameters, cancellationToken);
    }


    public List<IConnectionCredentials> KnownServers
    {
        get => _knownServers;
        set => this.RaiseAndSetIfChanged(ref _knownServers, value);
    }

    public bool ConvertPpkAutomatically
    {
        get => _convertPpkAutomatically;
        set => this.RaiseAndSetIfChanged(ref _convertPpkAutomatically, value);
    }

    public ReactiveCommand<IConnectionCredentials, ApplicationSettingsViewModel?> RemoveServer =>
        ReactiveCommand.Create<IConnectionCredentials, ApplicationSettingsViewModel?>(input =>
        {
            var index = KnownServers.IndexOf(input);
            var copy = KnownServers.ToList();
            copy.RemoveAt(index);
            KnownServers = copy;
            return this;
        });

    public ReactiveCommand<IConnectionCredentials, IConnectionCredentials?> EditEntry =>
        ReactiveCommand.CreateFromTask<IConnectionCredentials, IConnectionCredentials?>(
            async e =>
            {
                if (e is IMultiKeyConnectionCredentials)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(
                        StringsAndTexts.ApplicationSettingsEditErrorBoxTitle,
                        StringsAndTexts.ApplicationSettingsEditErrorBoxText);
                    await box.ShowAsync();
                    return null;
                }

                var s = await serviceProvider.ResolveViewAsync<EditSavedServerEntry, EditSavedServerEntryViewModel>(
                    new EditSavedServerEntryViewModelInitializeParameters()
                    {
                        Keys = Keys,
                        CredentialsToEdit = e
                    });
                var result = await dialogHost.ShowDialog<EditSavedServerEntry, EditSavedServerEntryViewModel>(s);
                if (result is null) return e;
                var list = KnownServers.ToList();
                var index = list.IndexOf(e);
                list.RemoveAt(index);
                list.Insert(index, result.CredentialsToEdit);
                KnownServers = list;
                return result.CredentialsToEdit; // @TODO DoesNotShowKeys
            });
}