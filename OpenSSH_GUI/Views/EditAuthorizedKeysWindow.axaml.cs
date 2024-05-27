﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:38

#endregion

using OpenSSH_GUI.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

public partial class EditAuthorizedKeysWindow : WindowBase<EditAuthorizedKeysViewModel>
{
    public EditAuthorizedKeysWindow()
    {
        InitializeComponent();
    }
}