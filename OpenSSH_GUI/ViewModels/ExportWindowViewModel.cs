﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:47

#endregion

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OpenSSH_GUI.ViewModels;

public class ExportWindowViewModel : ViewModelBase<ExportWindowViewModel>
{
    public string WindowTitle { get; set; } = "";
    public string Export { get; set; } = "";
}