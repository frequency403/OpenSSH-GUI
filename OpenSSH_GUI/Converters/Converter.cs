using Avalonia.Data.Converters;
using OpenSSH_GUI.Resources;
using SshNet.Keygen;

namespace OpenSSH_GUI.Converters;

public static class Converter
{
    private const string Windows = "Windows";
    private const string WindowsShort = "Win";

    public static FuncValueConverter<SshKeyFormat, string?> FormatToStringConverter { get; } = new(EnumToString);

    public static FuncValueConverter<SshKeyFormat, string?> FormatChangeTooltipConverter { get; } =
        new(format => string.Format(StringsAndTexts.FileInfoWindowChangeFormatTo, EnumToString(format)));

    public static FuncValueConverter<PlatformID, string?> PlatformIdToStringConverter { get; } = new(ConvertPlatformId);
    public static FuncValueConverter<object?, int> NullToColumnSpanConverter { get; } = new(o => o is null ? 2 : 1);

    private static string? EnumToString<TEnum>(TEnum value) where TEnum : struct, Enum => Enum.GetName(value);

    private static string? ConvertPlatformId(PlatformID arg) => EnumToString(arg) is { } platformId
        ? platformId.StartsWith(WindowsShort, StringComparison.CurrentCultureIgnoreCase)
            ? Windows
            : platformId
        : null;
}