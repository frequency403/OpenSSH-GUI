#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:41

#endregion

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Settings;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ApplicationSettingsViewModel(
    ILogger<ApplicationSettingsViewModel> logger,
    ISettingsFile settingsFile,
    IApplicationSettings applicationSettings) : ViewModelBase(logger)
{
    private bool _convertPpkAutomatically = settingsFile.ConvertPpkAutomatically;

    private List<IConnectionCredentials> _knownServers = settingsFile.LastUsedServers;

    private int _maxServers = settingsFile.MaxSavedServers;

    public ObservableCollection<ISshKey> Keys = [];
    public Interaction<EditSavedServerEntryViewModel, EditSavedServerEntryViewModel?> ShowEditEntry = new();

    public int MaxServers
    {
        get => _maxServers;
        set => this.RaiseAndSetIfChanged(ref _maxServers, value);
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
                    var box = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.ApplicationSettingsEditErrorBoxTitle,
                        StringsAndTexts.ApplicationSettingsEditErrorBoxText);
                    await box.ShowAsync();
                    return null;
                }

                var service = App.ServiceProvider.GetRequiredService<EditSavedServerEntryViewModel>();
                service.SetValues(ref Keys, e);
                var result = await ShowEditEntry.Handle(service);
                if (result is null) return e;
                var list = KnownServers.ToList();
                var index = list.IndexOf(e);
                list.RemoveAt(index);
                list.Insert(index, result.CredentialsToEdit);
                KnownServers = list;
                return result.CredentialsToEdit;
            });

    public ReactiveCommand<string, ApplicationSettingsViewModel> Submit =>
        ReactiveCommand.Create<string, ApplicationSettingsViewModel>(e =>
        {
            if (!bool.TryParse(e, out var realResult)) return this;
            if (!realResult) return this;
            settingsFile.ChangeSettings(new SettingsFile
            {
                Version = settingsFile.Version,
                ConvertPpkAutomatically = ConvertPpkAutomatically,
                MaxSavedServers = MaxServers,
                LastUsedServers = KnownServers
            });
            return this;
        });

    public void GetKeys(ref ObservableCollection<ISshKey> keys)
    {
        Keys = keys;
    }
}