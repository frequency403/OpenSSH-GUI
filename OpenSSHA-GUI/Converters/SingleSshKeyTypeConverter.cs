﻿using System;
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