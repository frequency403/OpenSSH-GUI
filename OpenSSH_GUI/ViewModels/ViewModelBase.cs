#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:47

#endregion

using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ViewModelBase(ILogger logger) : ReactiveObject
{
    protected ILogger _logger = logger;
}