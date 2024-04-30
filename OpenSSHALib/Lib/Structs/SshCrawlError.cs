namespace OpenSSHALib.Lib.Structs;

public readonly record struct SshCrawlError(string File, Exception Exception);