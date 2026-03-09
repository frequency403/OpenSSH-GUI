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

    protected override ValueTask<EditSavedServerEntryViewModel?> OnBooleanSubmitAsync(bool inputParameter)
    {
        try
        {
            if (!inputParameter) return ValueTask.FromResult<EditSavedServerEntryViewModel?>(null);
            if (CredentialsToEdit is IPasswordConnectionCredentials pwcc) pwcc.Password = Password;
            if (CredentialsToEdit is IKeyConnectionCredentials kcc) kcc.Key = SelectedKey;
            return ValueTask.FromResult<EditSavedServerEntryViewModel?>(this);
        }
        catch (Exception)
        {
            return ValueTask.FromResult<EditSavedServerEntryViewModel?>(null);
        }
    }

    public override async ValueTask InitializeAsync(IInitializerParameters<EditSavedServerEntryViewModel>? parameters = null, CancellationToken cancellationToken = default)
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

        await base.InitializeAsync(parameters, cancellationToken);
    }
}

public record EditSavedServerEntryViewModelInitializeParameters : IInitializerParameters<EditSavedServerEntryViewModel>
{
    public string Title { get; init; } = "EditSavedServerEntry";
    public ObservableCollection<SshKeyFile> Keys { get; init; }
    public IConnectionCredentials CredentialsToEdit { get; init; }
}