using System.Globalization;
using Avalonia.Data.Converters;
using SshNet.Keygen;

namespace OpenSSH_GUI.Converters;

public class SingleSshKeyTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null ? Enum.GetName(typeof(SshKeyType), (SshKeyType)value) : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null ? Enum.Parse<SshKeyType>((string)value) : string.Empty;
    }
}