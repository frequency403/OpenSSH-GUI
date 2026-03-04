using System.Text;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
/// Expands SSH percent-tokens (<c>%h</c>, <c>%u</c>, <c>%p</c>, …) within raw configuration
/// values, as defined by <c>ssh_config(5)</c>.
/// </summary>
/// <remarks>
/// Percent-token expansion is intentionally kept separate from parsing so that the model
/// always stores raw, unexpanded values.  Call <see cref="Expand"/> at the point where a
/// value is actually used for a connection.
/// </remarks>
public static class SshTokenExpander
{
    /// <summary>
    /// Expands all recognised percent-tokens in <paramref name="value"/> using
    /// the runtime values from <paramref name="context"/>.
    /// Unrecognised tokens (e.g. <c>%z</c>) are passed through unchanged.
    /// <c>%%</c> is always replaced with a literal <c>%</c>.
    /// </summary>
    /// <param name="value">The raw configuration value, potentially containing percent-tokens.</param>
    /// <param name="context">Runtime values used to substitute tokens.</param>
    /// <returns>The fully expanded string.</returns>
    public static string Expand(string value, SshTokenContext context)
    {
        if (!value.Contains('%'))
            return value;

        var sb  = new StringBuilder(value.Length + 32);
        var span = value.AsSpan();

        while (!span.IsEmpty)
        {
            var pct = span.IndexOf('%');
            if (pct < 0)
            {
                sb.Append(span);
                break;
            }

            sb.Append(span[..pct]);
            span = span[(pct + 1)..];

            if (span.IsEmpty)
            {
                sb.Append('%');
                break;
            }

            var expanded = span[0] switch
            {
                '%' => "%",
                'C' => context.ConnectionHash,
                'd' => context.LocalHomeDirectory,
                'h' => context.RemoteHostname,
                'H' => context.LocalHostname,
                'i' => context.LocalUserId?.ToString() ?? "%i",
                'k' => context.HostKeyAlias ?? context.RemoteHostname,
                'l' => context.LocalHostnameFqdn,
                'L' => context.LocalHostnameShort,
                'n' => context.OriginalHostname,
                'p' => context.Port?.ToString() ?? "%p",
                'r' => context.RemoteUser,
                'T' => context.ProxySocketPath ?? "%T",
                'u' => context.LocalUser,
                'U' => context.LocalUserId?.ToString() ?? "%U",
                _   => null,
            };

            if (expanded is not null)
            {
                sb.Append(expanded);
                span = span[1..];
            }
            else
            {
                sb.Append('%');
                // leave span[0] for the next loop iteration
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Supplies the runtime values used by <see cref="SshTokenExpander.Expand"/> to substitute
/// SSH percent-tokens.
/// </summary>
/// <remarks>
/// All string properties default to their literal percent-token so that any unexpanded
/// token remains clearly identifiable in the output.
/// </remarks>
/// <param name="RemoteHostname">
///   The target hostname, after any <c>HostName</c> substitution (<c>%h</c>).
/// </param>
/// <param name="LocalHostname">The local machine hostname (<c>%H</c>).</param>
/// <param name="OriginalHostname">The original hostname before canonicalization (<c>%n</c>).</param>
/// <param name="Port">The remote port number (<c>%p</c>), or <see langword="null"/> when unknown.</param>
/// <param name="RemoteUser">The remote login username (<c>%r</c>).</param>
/// <param name="LocalUser">The local OS username initiating the connection (<c>%u</c>).</param>
/// <param name="LocalUserId">The numeric local user identifier (<c>%U</c>, <c>%i</c>), or <see langword="null"/>.</param>
/// <param name="LocalHostnameFqdn">The fully-qualified local hostname, including domain (<c>%l</c>).</param>
/// <param name="LocalHostnameShort">The first label of the local hostname (<c>%L</c>).</param>
/// <param name="LocalHomeDirectory">Absolute path to the local user's home directory (<c>%d</c>).</param>
/// <param name="HostKeyAlias">The host-key alias if set via <c>HostKeyAlias</c>, otherwise <see langword="null"/> (<c>%k</c>).</param>
/// <param name="ConnectionHash">SHA-1 hash of <c>%l%h%p%r%u</c> for use in <c>ControlPath</c> (<c>%C</c>).</param>
/// <param name="ProxySocketPath">The proxy socket path when using <c>ProxyUseFdpass</c> (<c>%T</c>), or <see langword="null"/>.</param>
public sealed record SshTokenContext(
    string  RemoteHostname      = "%h",
    string  LocalHostname       = "%H",
    string  OriginalHostname    = "%n",
    int?    Port                = null,
    string  RemoteUser          = "%r",
    string  LocalUser           = "%u",
    long?   LocalUserId         = null,
    string  LocalHostnameFqdn   = "%l",
    string  LocalHostnameShort  = "%L",
    string  LocalHomeDirectory  = "%d",
    string? HostKeyAlias        = null,
    string  ConnectionHash      = "%C",
    string? ProxySocketPath     = null);
