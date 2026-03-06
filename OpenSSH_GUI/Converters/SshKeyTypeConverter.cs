using System.Globalization;
using Avalonia.Data.Converters;
using SshNet.Keygen;

namespace OpenSSH_GUI.Converters;

public class SshKeyTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<SshKeyType> && value is SshKeyType type) return type;
        return value as IEnumerable<SshKeyType>;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<SshKeyType> && value is SshKeyType type) return type;
        return value as IEnumerable<SshKeyType>;
    }
}