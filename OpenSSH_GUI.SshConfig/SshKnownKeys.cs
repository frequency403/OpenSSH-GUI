using System.Collections.Frozen;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
/// Registry of recognised SSH client configuration keywords (<c>ssh_config(5)</c>).
/// Provides keyword normalisation, multi-occurrence detection, and multi-token detection.
/// </summary>
public static class SshKnownKeys
{
    /// <summary>
    /// Canonical casing for all known <c>ssh_config(5)</c> client-side keywords.
    /// Lookups are case-insensitive.
    /// </summary>
    private static readonly FrozenDictionary<string, string> CanonicalKeys =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ── Block headers ───────────────────────────────────────────────
            ["Host"]                             = "Host",
            ["Match"]                            = "Match",
            ["Include"]                          = "Include",

            // ── Connection target ───────────────────────────────────────────
            ["HostName"]                         = "HostName",
            ["Port"]                             = "Port",
            ["User"]                             = "User",
            ["AddressFamily"]                    = "AddressFamily",
            ["BindAddress"]                      = "BindAddress",
            ["BindInterface"]                    = "BindInterface",
            ["ConnectTimeout"]                   = "ConnectTimeout",
            ["ConnectionAttempts"]               = "ConnectionAttempts",

            // ── Proxy ────────────────────────────────────────────────────────
            ["ProxyCommand"]                     = "ProxyCommand",
            ["ProxyJump"]                        = "ProxyJump",
            ["ProxyUseFdpass"]                   = "ProxyUseFdpass",

            // ── Authentication ───────────────────────────────────────────────
            ["BatchMode"]                        = "BatchMode",
            ["CertificateFile"]                  = "CertificateFile",
            ["GSSAPIAuthentication"]             = "GSSAPIAuthentication",
            ["GSSAPIClientIdentity"]             = "GSSAPIClientIdentity",
            ["GSSAPIDelegateCredentials"]        = "GSSAPIDelegateCredentials",
            ["GSSAPIKeyExchange"]                = "GSSAPIKeyExchange",
            ["GSSAPIRenewalForcesReauth"]        = "GSSAPIRenewalForcesReauth",
            ["GSSAPIServerIdentity"]             = "GSSAPIServerIdentity",
            ["GSSAPITrustDns"]                   = "GSSAPITrustDns",
            ["HostbasedAcceptedAlgorithms"]      = "HostbasedAcceptedAlgorithms",
            ["HostbasedAuthentication"]          = "HostbasedAuthentication",
            ["IdentitiesOnly"]                   = "IdentitiesOnly",
            ["IdentityAgent"]                    = "IdentityAgent",
            ["IdentityFile"]                     = "IdentityFile",
            ["KbdInteractiveAuthentication"]     = "KbdInteractiveAuthentication",
            ["KbdInteractiveDevices"]            = "KbdInteractiveDevices",
            ["NumberOfPasswordPrompts"]          = "NumberOfPasswordPrompts",
            ["PasswordAuthentication"]           = "PasswordAuthentication",
            ["PKCS11Provider"]                   = "PKCS11Provider",
            ["PreferredAuthentications"]         = "PreferredAuthentications",
            ["PubkeyAcceptedAlgorithms"]         = "PubkeyAcceptedAlgorithms",
            ["PubkeyAuthentication"]             = "PubkeyAuthentication",
            ["SecurityKeyProvider"]              = "SecurityKeyProvider",

            // ── Key management ───────────────────────────────────────────────
            ["AddKeysToAgent"]                   = "AddKeysToAgent",

            // ── Host key verification ────────────────────────────────────────
            ["CASignatureAlgorithms"]            = "CASignatureAlgorithms",
            ["CheckHostIP"]                      = "CheckHostIP",
            ["FingerprintHash"]                  = "FingerprintHash",
            ["GlobalKnownHostsFile"]             = "GlobalKnownHostsFile",
            ["HashKnownHosts"]                   = "HashKnownHosts",
            ["HostKeyAlgorithms"]                = "HostKeyAlgorithms",
            ["HostKeyAlias"]                     = "HostKeyAlias",
            ["KnownHostsCommand"]                = "KnownHostsCommand",
            ["StrictHostKeyChecking"]            = "StrictHostKeyChecking",
            ["UpdateHostKeys"]                   = "UpdateHostKeys",
            ["UserKnownHostsFile"]               = "UserKnownHostsFile",
            ["VerifyHostKeyDNS"]                 = "VerifyHostKeyDNS",

            // ── Ciphers / MACs / algorithms ──────────────────────────────────
            ["Ciphers"]                          = "Ciphers",
            ["KexAlgorithms"]                    = "KexAlgorithms",
            ["MACs"]                             = "MACs",
            ["RequiredRSASize"]                  = "RequiredRSASize",

            // ── Forwarding ───────────────────────────────────────────────────
            ["AllowStreamLocalForwarding"]       = "AllowStreamLocalForwarding",
            ["DynamicForward"]                   = "DynamicForward",
            ["ExitOnForwardFailure"]             = "ExitOnForwardFailure",
            ["ForwardAgent"]                     = "ForwardAgent",
            ["ForwardX11"]                       = "ForwardX11",
            ["ForwardX11Timeout"]                = "ForwardX11Timeout",
            ["ForwardX11Trusted"]                = "ForwardX11Trusted",
            ["GatewayPorts"]                     = "GatewayPorts",
            ["LocalForward"]                     = "LocalForward",
            ["RemoteForward"]                    = "RemoteForward",
            ["StreamLocalBindMask"]              = "StreamLocalBindMask",
            ["StreamLocalBindUnlink"]            = "StreamLocalBindUnlink",

            // ── Environment ──────────────────────────────────────────────────
            ["SendEnv"]                          = "SendEnv",
            ["SetEnv"]                           = "SetEnv",

            // ── Multiplexing ─────────────────────────────────────────────────
            ["ControlMaster"]                    = "ControlMaster",
            ["ControlPath"]                      = "ControlPath",
            ["ControlPersist"]                   = "ControlPersist",

            // ── Keep-alive / timeouts ────────────────────────────────────────
            ["ServerAliveCountMax"]              = "ServerAliveCountMax",
            ["ServerAliveInterval"]              = "ServerAliveInterval",
            ["TCPKeepAlive"]                     = "TCPKeepAlive",

            // ── Logging ──────────────────────────────────────────────────────
            ["LogLevel"]                         = "LogLevel",
            ["LogVerbose"]                       = "LogVerbose",
            ["SyslogFacility"]                   = "SyslogFacility",

            // ── Canonicalization ─────────────────────────────────────────────
            ["CanonicalDomains"]                 = "CanonicalDomains",
            ["CanonicalizeFallbackLocal"]        = "CanonicalizeFallbackLocal",
            ["CanonicalizeHostname"]             = "CanonicalizeHostname",
            ["CanonicalizeMaxDots"]              = "CanonicalizeMaxDots",
            ["CanonicalizePermittedCNAMEs"]      = "CanonicalizePermittedCNAMEs",

            // ── Tunnel ───────────────────────────────────────────────────────
            ["Tunnel"]                           = "Tunnel",
            ["TunnelDevice"]                     = "TunnelDevice",

            // ── Miscellaneous ────────────────────────────────────────────────
            ["Banner"]                           = "Banner",
            ["ClearAllForwardings"]              = "ClearAllForwardings",
            ["Compression"]                      = "Compression",
            ["EnableEscapeCommandline"]          = "EnableEscapeCommandline",
            ["EnableSSHKeysign"]                 = "EnableSSHKeysign",
            ["EscapeChar"]                       = "EscapeChar",
            ["ForkAfterAuthentication"]          = "ForkAfterAuthentication",
            ["IgnoreUnknown"]                    = "IgnoreUnknown",
            ["IPQoS"]                            = "IPQoS",
            ["LocalCommand"]                     = "LocalCommand",
            ["NoHostAuthenticationForLocalhost"] = "NoHostAuthenticationForLocalhost",
            ["ObscureKeystrokeTiming"]           = "ObscureKeystrokeTiming",
            ["PermitLocalCommand"]               = "PermitLocalCommand",
            ["PermitRemoteOpen"]                 = "PermitRemoteOpen",
            ["RekeyLimit"]                       = "RekeyLimit",
            ["RemoteCommand"]                    = "RemoteCommand",
            ["RequestTTY"]                       = "RequestTTY",
            ["SessionType"]                      = "SessionType",
            ["StdinNull"]                        = "StdinNull",
            ["Tag"]                              = "Tag",
            ["VisualHostKey"]                    = "VisualHostKey",
            ["XAuthLocation"]                    = "XAuthLocation",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Keywords that support multiple occurrences within the same block with additive semantics
    /// (i.e. later occurrences accumulate rather than override earlier ones).
    /// </summary>
    private static readonly FrozenSet<string> MultiOccurrenceKeys =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
            "CertificateFile",
            "DynamicForward",
            "IdentityFile",
            "LocalForward",
            "RemoteForward");

    /// <summary>
    /// Keywords that accept multiple space-separated value tokens on a single directive line.
    /// </summary>
    private static readonly FrozenSet<string> MultiTokenKeys =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
            "SendEnv",
            "SetEnv",
            "Host",
            "Match");

    /// <summary>
    /// Returns the canonical casing for <paramref name="key"/> as used by OpenSSH,
    /// or returns <paramref name="key"/> unchanged if it is not a recognised keyword.
    /// </summary>
    /// <param name="key">A configuration keyword in any casing.</param>
    public static string Normalize(string key) =>
        CanonicalKeys.TryGetValue(key, out var canonical) ? canonical : key;

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="key"/> supports multiple occurrences
    /// within the same block with additive (accumulative) semantics.
    /// </summary>
    public static bool IsMultiOccurrenceKey(string key) =>
        MultiOccurrenceKeys.Contains(key);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="key"/> accepts multiple
    /// space-separated value tokens on a single directive line.
    /// </summary>
    public static bool IsMultiTokenKey(string key) =>
        MultiTokenKeys.Contains(key);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="key"/> is a recognised
    /// <c>ssh_config(5)</c> client keyword.
    /// </summary>
    public static bool IsKnownKey(string key) =>
        CanonicalKeys.ContainsKey(key);
}
