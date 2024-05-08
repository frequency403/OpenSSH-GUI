#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 21.01.2024 - 23:01:53
// Last edit: 08.05.2024 - 22:05:56

#endregion

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenSSHA_GUI.Converters;

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