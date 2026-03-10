using Avalonia.Data.Converters;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Converters;

public static class Converter
{
    private static string? EnumToString<TEnum>(TEnum value) where TEnum : struct, Enum => Enum.GetName(value);
    
    public static FuncValueConverter<SshKeyFormat, string?> FormatToStringConverter { get; } = new(EnumToString);
    public static FuncValueConverter<SshKeyType, string?> KeyTypeToStringConverter { get; } = new(EnumToString);
    public static FuncValueConverter<SshKeyHashAlgorithmName, string?> HashAlgorithmNameToStringConverter { get; } = new(EnumToString);
}