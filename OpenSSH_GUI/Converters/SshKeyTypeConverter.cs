#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:36

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Models;

namespace OpenSSH_GUI.Converters;

public class SshKeyTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<ISshKeyType> && value is ISshKeyType type) return type.BaseType;
        return (value as IEnumerable<ISshKeyType>)!.Select(e => e.BaseType);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<KeyType> && value is KeyType type) return new SshKeyType(type);
        return (value as IEnumerable<KeyType>)!.Select(e => new SshKeyType(e));
    }
}