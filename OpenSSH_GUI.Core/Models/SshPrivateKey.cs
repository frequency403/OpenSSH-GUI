﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:18

#endregion

using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Models;

public class SshPrivateKey(string absoluteFilePath) : SshKey(absoluteFilePath), ISshPrivateKey;