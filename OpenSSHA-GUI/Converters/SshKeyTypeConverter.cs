using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using OpenSSHALib.Enums;
using OpenSSHALib.Models;

namespace OpenSSHA_GUI.Converters;

public class SshKeyTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!(value is IEnumerable<SshKeyType>) && value is SshKeyType type) return type.BaseType;
        return (value as IEnumerable<SshKeyType>).Select(e => e.BaseType);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!(value is IEnumerable<KeyType>) && value is KeyType type) return new SshKeyType(type);
        return (value as IEnumerable<KeyType>).Select(e => new SshKeyType(e));
    }
}