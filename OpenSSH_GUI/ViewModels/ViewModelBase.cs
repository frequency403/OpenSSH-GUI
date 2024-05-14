#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:37

#endregion

using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ViewModelBase(ILogger logger) : ReactiveObject
{
    protected ILogger _logger = logger;
}