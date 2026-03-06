using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class ApplicationSettingsWindow : WindowBase<ApplicationSettingsViewModel>
{
    public ApplicationSettingsWindow(ILogger<ApplicationSettingsWindow> logger, IServiceProvider serviceProvider) :
        base(logger)
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(ViewModel!.ShowEditEntry.RegisterHandler(async interaction =>
            {
                var result =
                    await (await serviceProvider.ResolveViewAsync<EditSavedServerEntry, EditSavedServerEntryViewModel>(
                        new EditSavedServerEntryViewModelInitializeParameters
                        {
                            Title = interaction.Input.CredentialsToEdit.Display
                        })).ShowDialog<EditSavedServerEntryViewModel>(this);
                interaction.SetOutput(result);
            }));
        });
    }
}