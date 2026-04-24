namespace OpenSSH_GUI.Core.Lib.KnownHosts;

public readonly record struct KnownHostHost
{
    private readonly string _originalHostEntry;

    public KnownHostHost(string host)
    {
        _originalHostEntry = host;
        if (host.Split(':') is not { Length: 2 } split)
        {
            Host = host;
        }
        else
        {
            Port = int.Parse(split[1]);
            Host = split[0];
        }
        Host = Host.Trim('[', ']');
    }

    public int Port { get; } = 22;
    public string Host { get; } = string.Empty;

    public override string ToString() => _originalHostEntry;
}