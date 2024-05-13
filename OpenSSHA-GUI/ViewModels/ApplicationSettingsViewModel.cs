// File Created by: Oliver Schantz
// Created: 13.05.2024 - 13:05:40
// Last edit: 13.05.2024 - 13:05:40

using Microsoft.Extensions.Logging;
using OpenSSHALib.Interfaces;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class ApplicationSettingsViewModel(
    ILogger<ApplicationSettingsViewModel> logger,
    IApplicationSettings applicationSettings) : ViewModelBase(logger)
{
    public ReactiveCommand<string, ApplicationSettingsViewModel?> Submit =>
        ReactiveCommand.Create<string, ApplicationSettingsViewModel?>(e =>
            bool.TryParse(e, out var realResult) ? realResult ? this : null : null);
}