#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:36

#endregion

using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.Settings;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ApplicationSettingsViewModel(
    ILogger<ApplicationSettingsViewModel> logger,
    IApplicationSettings applicationSettings) : ViewModelBase(logger)
{
    public ReactiveCommand<string, ApplicationSettingsViewModel?> Submit =>
        ReactiveCommand.Create<string, ApplicationSettingsViewModel?>(e =>
            bool.TryParse(e, out var realResult) ? realResult ? this : null : null);
}