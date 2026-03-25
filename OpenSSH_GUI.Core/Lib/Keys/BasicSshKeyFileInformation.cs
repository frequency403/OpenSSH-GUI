using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Lib.Keys;


public readonly record struct BasicSshKeyFileInformation()
{
    public SshKeyHashAlgorithmName HashAlgorithmName { get; private init; } = SshKeyHashAlgorithmName.MD5;
    public string FingerPrint { get; private init; } = string.Empty;
    public string Comment { get; private init; } = string.Empty;
    public SshKeyType KeyType { get; private init; } = SshKeyType.RSA;
    private bool IsEmpty => HashAlgorithmName == SshKeyHashAlgorithmName.MD5 && FingerPrint == string.Empty && Comment == string.Empty && KeyType == SshKeyType.RSA;
    
    public static BasicSshKeyFileInformation Empty { get; } = new();
    public static BasicSshKeyFileInformation FromCommandOutput(string publicKeyInfo)
    {
        if (publicKeyInfo.TrimEnd('\r', '\n')
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is
                not { Length: > 3 } splitted ||
            splitted[1].Split(':') is not { Length: > 1 } fingerprintSplit)
            throw new InvalidOperationException("Invalid public key information");
        return new BasicSshKeyFileInformation
        {
            HashAlgorithmName = Enum.Parse<SshKeyHashAlgorithmName>(fingerprintSplit[0]),
            FingerPrint = fingerprintSplit[1],
            Comment = splitted[2],
            KeyType = Enum.Parse<SshKeyType>(splitted[3][1..^1])
        };
    }

    public override string ToString() => !IsEmpty ? $"{HashAlgorithmName.ToString()[^3..]} {HashAlgorithmName}:{FingerPrint} {Comment} ({KeyType}){Environment.NewLine}" : string.Empty;
}