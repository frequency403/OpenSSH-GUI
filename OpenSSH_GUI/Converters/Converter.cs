using Avalonia.Data.Converters;
using SshNet.Keygen;

namespace OpenSSH_GUI.Converters;

public static class Converter
{
    public static FuncValueConverter<SshKeyFormat, string?> FormatToStringConverter { get; } = new(Enum.GetName);
}