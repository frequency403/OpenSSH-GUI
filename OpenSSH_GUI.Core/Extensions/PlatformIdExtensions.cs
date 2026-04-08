namespace OpenSSH_GUI.Core.Extensions;

/// <summary>
/// Provides extension methods for <see cref="PlatformID"/>.
/// </summary>
public static class PlatformIdExtensions
{
    private const string UnixLineSeparator = "\n";
    private const string WindowsLineSeparator = "\r\n";

    /// <summary>
    /// Returns the line separator string used by the given platform.
    /// </summary>
    /// <param name="platformId">The target platform identifier.</param>
    /// <returns><c>\n</c> for Unix-like platforms, <c>\r\n</c> for all Windows variants.</returns>
    public static string GetLineSeparator(this PlatformID platformId) => platformId switch
    {
        PlatformID.Win32NT or
            PlatformID.Win32Windows or
            PlatformID.Win32S or
            PlatformID.WinCE => WindowsLineSeparator,
        _ => UnixLineSeparator
    };
}