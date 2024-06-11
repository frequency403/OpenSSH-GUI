#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:41

#endregion

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Converters;

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