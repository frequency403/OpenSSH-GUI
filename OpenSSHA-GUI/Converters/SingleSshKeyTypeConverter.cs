﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 21.01.2024 - 23:01:53
// Last edit: 08.05.2024 - 22:05:03

#endregion

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using OpenSSHALib.Enums;

namespace OpenSSHA_GUI.Converters;

public class SingleSshKeyTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null ? Enum.GetName(typeof(KeyType), (KeyType)value) : "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null ? Enum.Parse<KeyType>((string)value) : "";
    }
}