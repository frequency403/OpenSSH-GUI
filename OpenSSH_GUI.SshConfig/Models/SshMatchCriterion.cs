namespace OpenSSH_GUI.SshConfig.Models;

/// <summary>Identifies the kind of condition expressed by a single <c>Match</c> criterion.</summary>
public enum SshMatchCriterionKind
{
    /// <summary>
    ///     Matches all connections unconditionally (<c>Match all</c>).
    ///     When present, must be the sole criterion or the final one on the line.
    /// </summary>
    All,

    /// <summary>Active only during hostname-canonicalization passes (<c>Match canonical</c>).</summary>
    Canonical,

    /// <summary>Active only during the final configuration-processing pass (<c>Match final</c>).</summary>
    Final,

    /// <summary>
    ///     Matches when the specified shell command exits with status 0 (<c>Match exec</c>).
    ///     Percent-tokens are expanded in the command before execution.
    /// </summary>
    Exec,

    /// <summary>Matches against the (possibly canonicalized) target hostname (<c>Match host</c>).</summary>
    Host,

    /// <summary>Matches against the original, pre-canonicalization hostname (<c>Match originalhost</c>).</summary>
    OriginalHost,

    /// <summary>Matches against the remote username specified for the connection (<c>Match user</c>).</summary>
    User,

    /// <summary>Matches against the local OS username initiating the connection (<c>Match localuser</c>).</summary>
    LocalUser,

    /// <summary>Matches against a named tag associated with the connection (<c>Match tagged</c>).</summary>
    Tagged,

    /// <summary>Matches against the local machine belongs to the specified network subnet (<c>Match localnetwork</c>).</summary>
    LocalNetwork,

    /// <summary>Matches against the address of the remote host (<c>Match address</c>).</summary>
    Address,

    /// <summary>Matches against the group of the remote user (<c>Match group</c>).</summary>
    Group,

    /// <summary>Matches against the local address the connection was received on (<c>Match localaddress</c>).</summary>
    LocalAddress,

    /// <summary>Matches against the local port the connection was received on (<c>Match localport</c>).</summary>
    LocalPort,

    /// <summary>Matches against the remote port (<c>Match port</c>).</summary>
    Port,

    /// <summary>Matches against the remote host's RDNS (<c>Match rdomain</c>).</summary>
    RDomain
}

/// <summary>
///     Represents a single criterion (and its optional pattern or argument) within a <c>Match</c> block header.
/// </summary>
/// <param name="Kind">The type of condition to evaluate.</param>
/// <param name="Pattern">
///     The pattern, glob, or argument for the criterion, or <see langword="null" /> for
///     <see cref="SshMatchCriterionKind.All" />, <see cref="SshMatchCriterionKind.Canonical" />,
///     and <see cref="SshMatchCriterionKind.Final" />.
///     Comma-separated patterns (e.g. <c>*.example.com,!bad.example.com</c>) are stored as a single string.
/// </param>
public sealed record SshMatchCriterion(SshMatchCriterionKind Kind, string? Pattern)
{
    /// <summary>The singleton <c>all</c> criterion.</summary>
    public static SshMatchCriterion All { get; } = new(SshMatchCriterionKind.All, null);

    /// <summary>The singleton <c>canonical</c> criterion.</summary>
    public static SshMatchCriterion Canonical { get; } = new(SshMatchCriterionKind.Canonical, null);

    /// <summary>The singleton <c>final</c> criterion.</summary>
    public static SshMatchCriterion Final { get; } = new(SshMatchCriterionKind.Final, null);

    /// <summary>Creates a <c>host</c> criterion with the given pattern.</summary>
    public static SshMatchCriterion ForHost(string pattern) => new(SshMatchCriterionKind.Host, pattern);

    /// <summary>Creates a <c>user</c> criterion with the given pattern.</summary>
    public static SshMatchCriterion ForUser(string pattern) => new(SshMatchCriterionKind.User, pattern);

    /// <summary>Creates an <c>exec</c> criterion with the given shell command.</summary>
    public static SshMatchCriterion ForExec(string command) => new(SshMatchCriterionKind.Exec, command);

    /// <inheritdoc />
    public override string ToString()
    {
        var keyword = Kind switch
        {
            SshMatchCriterionKind.All => "all",
            SshMatchCriterionKind.Canonical => "canonical",
            SshMatchCriterionKind.Final => "final",
            SshMatchCriterionKind.Exec => "exec",
            SshMatchCriterionKind.Host => "host",
            SshMatchCriterionKind.OriginalHost => "originalhost",
            SshMatchCriterionKind.User => "user",
            SshMatchCriterionKind.LocalUser => "localuser",
            SshMatchCriterionKind.Tagged => "tagged",
            SshMatchCriterionKind.LocalNetwork => "localnetwork",
            SshMatchCriterionKind.Address => "address",
            SshMatchCriterionKind.Group => "group",
            SshMatchCriterionKind.LocalAddress => "localaddress",
            SshMatchCriterionKind.LocalPort => "localport",
            SshMatchCriterionKind.Port => "port",
            SshMatchCriterionKind.RDomain => "rdomain",
            _ => Kind.ToString().ToLowerInvariant()
        };

        return Pattern is null ? keyword : $"{keyword} {Pattern}";
    }
}