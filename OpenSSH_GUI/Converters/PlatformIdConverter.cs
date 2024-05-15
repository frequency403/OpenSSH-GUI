#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:40

#endregion

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenSSH_GUI.Converters;

public class PlatformIdConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Enum.GetName((PlatformID)value).Contains("Win", StringComparison.CurrentCultureIgnoreCase))
            return "Windows";
        return value is null ? value : Enum.GetName((PlatformID)value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? value : Enum.Parse<PlatformID>((string)value);
    }
}