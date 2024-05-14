#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:36

#endregion

using Microsoft.Extensions.Logging;

namespace OpenSSH_GUI.ViewModels;

public class ExportWindowViewModel(ILogger<ExportWindowViewModel> logger) : ViewModelBase(logger)
{
    public string WindowTitle { get; set; } = "";
    public string Export { get; set; } = "";
}