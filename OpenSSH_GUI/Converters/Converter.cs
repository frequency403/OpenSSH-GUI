using Avalonia.Data.Converters;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Converters;

public static class Converter
{
    private const string Windows = "Windows";
    private const string WindowsShort = "Win";

    public static FuncValueConverter<SshKeyFormat, string?> FormatToStringConverter { get; } = new(EnumToString);
    public static FuncValueConverter<SshKeyType, string?> KeyTypeToStringConverter { get; } = new(EnumToString);
    public static FuncValueConverter<PlatformID, string?> PlatformIdToStringConverter { get; } = new(ConvertPlatformId);

    private static string? EnumToString<TEnum>(TEnum value) where TEnum : struct, Enum 
        => Enum.GetName(value);

    private static string? ConvertPlatformId(PlatformID arg) =>
        EnumToString(arg) is { } platformId
            ? platformId.StartsWith(WindowsShort, StringComparison.CurrentCultureIgnoreCase)
                ? Windows
                : platformId
            : null;
}