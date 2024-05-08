#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:56

#endregion

using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class ViewModelBase(ILogger logger) : ReactiveObject
{
    protected ILogger _logger = logger;
}