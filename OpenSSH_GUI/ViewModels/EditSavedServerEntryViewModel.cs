using System.Collections.ObjectModel;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public sealed class EditSavedServerEntryViewModel
    : ViewModelBase<EditSavedServerEntryViewModel>
{
    public string WindowTitle
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public IConnectionCredentials CredentialsToEdit { get; set; }
    public ObservableCollection<SshKeyFile> Keys { get; set; }

    public SshKeyFile SelectedKey
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string Password
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public bool IsPasswordKey => CredentialsToEdit is IPasswordConnectionCredentials;

    protected override Task<EditSavedServerEntryViewModel?> OnBooleanSubmit(bool inputParameter)
    {
        try
        {
            if (!inputParameter) return Task.FromResult<EditSavedServerEntryViewModel?>(null);
            if (CredentialsToEdit is IPasswordConnectionCredentials pwcc) pwcc.Password = Password;
            if (CredentialsToEdit is IKeyConnectionCredentials kcc) kcc.Key = SelectedKey;
            return Task.FromResult<EditSavedServerEntryViewModel?>(this);
        }
        catch (Exception exception)
        {
            return Task.FromException<EditSavedServerEntryViewModel?>(exception);
        }
    }

    public override void Initialize(IInitializerParameters<EditSavedServerEntryViewModel>? parameters = null)
    {
        if (parameters is EditSavedServerEntryViewModelInitializeParameters initParams)
        {
            Keys = initParams.Keys;
            WindowTitle = initParams.Title;
            CredentialsToEdit = initParams.CredentialsToEdit;
            if (CredentialsToEdit is IPasswordConnectionCredentials pwcc) Password = pwcc.Password;
            SelectedKey = CredentialsToEdit is IKeyConnectionCredentials kcc
                ? Keys.FirstOrDefault(e => string.Equals(kcc.Key.Fingerprint(), e.Fingerprint()))
                : Keys.FirstOrDefault();
        }

        base.Initialize(parameters);
    }
}

public record EditSavedServerEntryViewModelInitializeParameters : IInitializerParameters<EditSavedServerEntryViewModel>
{
    public string Title { get; init; } = "EditSavedServerEntry";
    public ObservableCollection<SshKeyFile> Keys { get; init; }
    public IConnectionCredentials CredentialsToEdit { get; init; }
}