using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using OpenSSH_GUI.Core.Extensions;

namespace OpenSSH_GUI.Core.Resources.Converter;

/// <summary>
/// Converts a path string to a boolean indicating whether it can be deleted.
/// Returns <see langword="false"/> if the path equals the protected default path.
/// </summary>
public sealed class PathDeletableConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string path && path != SshConfigFilesExtension.GetBaseSshPath();

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new BindingNotification(new NotSupportedException(), BindingErrorType.Error);
}