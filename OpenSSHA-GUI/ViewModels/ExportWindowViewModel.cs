#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:57

#endregion

using Microsoft.Extensions.Logging;

namespace OpenSSHA_GUI.ViewModels;

public class ExportWindowViewModel(ILogger<ExportWindowViewModel> logger) : ViewModelBase(logger)
{
    public string WindowTitle { get; set; } = "";
    public string Export { get; set; } = "";
}