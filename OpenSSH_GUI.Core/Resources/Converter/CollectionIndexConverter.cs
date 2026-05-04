using System.Collections;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace OpenSSH_GUI.Core.Resources.Converter;

/// <summary>
/// Converts an item and its parent collection into a 1-based index string.
/// </summary>
public class CollectionIndexConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is null || values[1] is not IList collection)
            return string.Empty;

        var index = collection.IndexOf(values[0]);
        return index >= 0 ? (index + 1).ToString() : string.Empty;
    }
}