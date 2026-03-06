using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

public partial class EditSavedServerEntry : WindowBase<EditSavedServerEntryViewModel>
{
    public EditSavedServerEntry(ILogger<EditSavedServerEntry> logger) : base(logger)
    {
        InitializeComponent();
    }
}