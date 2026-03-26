using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Lib.Keys;

[DebuggerDisplay("{ToString()}")]
public readonly record struct BasicSshKeyFileInformation()
{
    public SshKeyHashAlgorithmName HashAlgorithmName { get; private init; } = SshKeyHashAlgorithmName.MD5;
    public string FingerPrint { get; private init; } = string.Empty;
    public string Comment { get; private init; } = string.Empty;
    public SshKeyType KeyType { get; private init; } = SshKeyType.RSA;
    private bool IsEmpty => HashAlgorithmName == SshKeyHashAlgorithmName.MD5 && FingerPrint == string.Empty && Comment == string.Empty && KeyType == SshKeyType.RSA;

    private static BasicSshKeyFileInformation Empty { get; } = new();

    private static BasicSshKeyFileInformation FromCommandOutput(string publicKeyInfo)
    {
        if (publicKeyInfo.TrimEnd('\r', '\n')
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is
                not { Length: > 3 } splitted ||
            splitted[1].Split(':') is not { Length: > 1 } fingerprintSplit)
            throw new InvalidOperationException("Invalid public key information");
        if (!Enum.TryParse(fingerprintSplit[0], true, out SshKeyHashAlgorithmName hashAlgorithmName))
            throw new InvalidOperationException($"Invalid hash algorithm name. Valid values are: {string.Join(", ", Enum.GetNames<SshKeyHashAlgorithmName>())}");
        if (!Enum.TryParse(splitted[3][1..^1], true, out SshKeyType keyType))
            throw new InvalidOperationException($"Invalid key type. Valid values are: {string.Join(", ", Enum.GetNames<SshKeyType>())}");
        return new BasicSshKeyFileInformation
        {
            HashAlgorithmName = hashAlgorithmName,
            FingerPrint = fingerprintSplit[1],
            Comment = splitted[2],
            KeyType = keyType
        };
    }

    private static BasicSshKeyFileInformation FromCommandOutput(ReadOnlySpan<char> publicKeyInfo)
    {
        if (publicKeyInfo.IsEmpty)
            throw new InvalidOperationException("Invalid public key information");
        if(new string(publicKeyInfo) is not {Length: > 0} publicKeyInfoString)
            throw new InvalidOperationException("Invalid public key information");
        if (publicKeyInfoString.TrimEnd('\r', '\n')
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is
                not { Length: > 3 } splitted ||
            splitted[1].Split(':') is not { Length: > 1 } fingerprintSplit)
            throw new InvalidOperationException("Invalid public key information");
        if (!Enum.TryParse(fingerprintSplit[0], true, out SshKeyHashAlgorithmName hashAlgorithmName))
            throw new InvalidOperationException($"Invalid hash algorithm name. Valid values are: {string.Join(", ", Enum.GetNames<SshKeyHashAlgorithmName>())}");
        if (!Enum.TryParse(splitted[3][1..^1], true, out SshKeyType keyType))
            throw new InvalidOperationException($"Invalid key type. Valid values are: {string.Join(", ", Enum.GetNames<SshKeyType>())}");
        return new BasicSshKeyFileInformation
        {
            HashAlgorithmName = hashAlgorithmName,
            FingerPrint = fingerprintSplit[1],
            Comment = splitted[2],
            KeyType = keyType
        };
    }
    
    /// <summary>
    ///     Extracts basic information from the SSH key file, such as its fingerprint,
    ///     hash algorithm, comment, and key type, using the <c>ssh-keygen</c> command-line tool
    ///     or in case of a PuTTY key, the public key file itself.
    /// </summary>
    /// <exception cref="FileNotFoundException">
    ///     Thrown if the SSH key file does not exist.
    /// </exception>
    public static BasicSshKeyFileInformation FromKeyFileInfo(SshKeyFileInformation keyFileInformation)
    {
        if (keyFileInformation is { Exists: false }) return Empty;
        if (keyFileInformation.PublicKeyFileName is not null)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ssh-keygen",
                Arguments = $"-lf {keyFileInformation.PublicKeyFileName}",
                CreateNoWindow = true,
                WorkingDirectory = keyFileInformation.DirectoryName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            if (!process.Start()) 
                return Empty;
            process.WaitForExit();
            Span<char> buffer = stackalloc char[0xFF];
            var readChars = (process.ExitCode is 0 ? process.StandardOutput : process.StandardError).Read(buffer);
            return FromCommandOutput(buffer[..readChars]);
        }

        if (keyFileInformation.Files.FirstOrDefault(x =>
                string.Equals(x.Extension, ".ppk", StringComparison.OrdinalIgnoreCase)) is { } ppkFile)
        {
            return FromCommandOutput(ReadPpkAsCommandOutput(ppkFile.FullName));
        }
        return Empty;
    }

    /// <summary>
    /// Reads a PuTTY .ppk file and formats the contained public key information
    /// as a string compatible with <see cref="FromCommandOutput(string)"/>.
    /// </summary>
    /// <param name="ppkPath">Path to the .ppk file.</param>
    /// <returns>
    /// A formatted string in the form: <c>{bits} SHA256:{fingerprint} {comment} ({keyType})</c>
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the key type in the .ppk file is not supported.</exception>
    private static string ReadPpkAsCommandOutput(string ppkPath)
    {
        var lines = File.ReadAllLines(ppkPath);

        var keyTypeRaw = GetField(lines, "PuTTY-User-Key-File");
        var comment = GetField(lines, "Comment");

        var pubLinesIdx = Array.FindIndex(lines, l => l.StartsWith("Public-Lines:"));
        var pubCount = int.Parse(lines[pubLinesIdx].Split(": ", 2)[1].Trim());
        var raw = Convert.FromBase64String(
            string.Concat(lines[(pubLinesIdx + 1)..(pubLinesIdx + 1 + pubCount)]));

        var fingerprint = Convert.ToBase64String(SHA256.HashData(raw)).TrimEnd('=');

        var (keyType, bits) = keyTypeRaw switch
        {
            "ssh-ed25519" => ("ED25519", 256),
            "ssh-rsa" => ("RSA", GetRsaBitLength(raw)),
            "ecdsa-sha2-nistp256" => ("ECDSA", 256),
            "ecdsa-sha2-nistp384" => ("ECDSA", 384),
            "ecdsa-sha2-nistp521" => ("ECDSA", 521),
            "ssh-dss" => ("DSA", 1024),
            _ => throw new NotSupportedException($"Unsupported key type: {keyTypeRaw}")
        };

        return $"{bits} SHA256:{fingerprint} {comment} ({keyType})";
    }

    /// <summary>
    /// Extracts the value of a colon-separated field from the .ppk header lines.
    /// </summary>
    /// <param name="lines">All lines of the .ppk file.</param>
    /// <param name="key">The field name to look for (e.g. "Comment").</param>
    /// <returns>The trimmed value after the colon.</returns>
    private static string GetField(string[] lines, string key) =>
        lines.First(l => l.StartsWith(key)).Split(": ", 2)[1].Trim();

    /// <summary>
    /// Parses the SSH wire-format blob of an RSA public key to determine its bit length via the modulus size.
    /// </summary>
    /// <param name="blob">Raw decoded public key blob.</param>
    /// <returns>Bit length of the RSA modulus.</returns>
    private static int GetRsaBitLength(byte[] blob)
    {
        var span = blob.AsSpan();

        // skip "ssh-rsa" type string
        var typeLen = BinaryPrimitives.ReadInt32BigEndian(span);
        span = span[(4 + typeLen)..];

        // skip public exponent
        var expLen = BinaryPrimitives.ReadInt32BigEndian(span);
        span = span[(4 + expLen)..];

        // read modulus — may carry a leading 0x00 sign byte
        var modLen = BinaryPrimitives.ReadInt32BigEndian(span);
        span = span[4..];
        if (span[0] == 0x00)
        {
            span = span[1..];
            modLen--;
        }

        return (modLen - 1) * 8 + (int)Math.Floor(Math.Log2(span[0]) + 1);
    }
    
    public override string ToString() => !IsEmpty ? $"{HashAlgorithmName.ToString()[^3..]} {HashAlgorithmName}:{FingerPrint} {Comment} ({KeyType})" : string.Empty;
}