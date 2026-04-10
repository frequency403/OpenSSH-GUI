using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using OpenSSH_GUI.Core.Extensions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Lib.Keys;

[DebuggerDisplay("{ToString()}")]
public readonly record struct BasicSshKeyFileInformation()
{
    private static readonly ReadOnlyMemory<byte> OpensshMagic = "openssh-key-v1\0"u8.ToArray();
    private const string OpensshPrivateHeader = "-----BEGIN OPENSSH PRIVATE KEY-----";
    private const string OpensshPrivateFooter = "-----END OPENSSH PRIVATE KEY-----";
    private const string PuttyFileStart = "PuTTY-User-Key-File-";
    private const string OutputFormat = "{0} {1}:{2} {3} ({4})";
    internal ReadOnlyMemory<byte> KeyBlob { get; init; }


    /// <summary>The hash algorithm used to compute the fingerprint. Always SHA256 for parsed keys.</summary>
    public SshKeyHashAlgorithmName HashAlgorithmName { get; private init; } = SshKeyHashAlgorithmName.SHA256;

    /// <summary>Base64-encoded SHA256 fingerprint of the public key blob (without trailing padding).</summary>
    public string FingerPrint { get; private init; } = string.Empty;

    /// <summary>Key comment as stored in the key file.</summary>
    public string Comment { get; private init; } = string.Empty;

    /// <summary>Logical SSH key algorithm type.</summary>
    public SshKeyType KeyType { get; private init; } = SshKeyType.RSA;

    /// <summary>Effective bit length of the key (e.g. 256, 384, 521, 2048, 4096).</summary>
    public int BitLength { get; private init; } = 0;

    /// <summary>Storage format of the key, independent of whether it is split across one or two files.</summary>
    public SshKeyFormat Format { get; private init; } = SshKeyFormat.OpenSSH;
    
    private bool IsEmpty => FingerPrint.Length == 0;
    private static BasicSshKeyFileInformation Empty { get; } = new();
    
    /// <summary>
    ///     Extracts metadata from any supported SSH key file without requiring a passphrase.
    ///     Supports OpenSSH public keys (.pub), OpenSSH private keys, and PuTTY keys (PPK v1/v2/v3).
    ///     The comment will be empty for passphrase-protected OpenSSH private keys
    ///     when no corresponding .pub file is present.
    /// </summary>
    /// <param name="keyFileInformation">Descriptor of the key file(s) on disk.</param>
    /// <returns>Parsed metadata, or an empty instance if the key cannot be read.</returns>
    public static BasicSshKeyFileInformation FromKeyFileInfo(SshKeyFileInformation keyFileInformation)
    {
        if (keyFileInformation is { Exists: false })
            return Empty;

        // .pub file is always preferred — richest source, comment always present
        if (keyFileInformation.PublicKeyFileName is { } pubPath)
            return TryParseOrEmpty(() => ParseOpenSshPublicKey(File.ReadAllText(pubPath).Trim()));

        var files = keyFileInformation.Files.ToList();

        // PPK — comment lives in the unencrypted plaintext header regardless of encryption
        if (files.FirstOrDefault(f => f.Extension.Equals(SshKeyFormatExtension.PuttyKeyFileExtension, StringComparison.OrdinalIgnoreCase)) is { } ppkFile)
            return TryParseOrEmpty(() => ParsePpkFile(File.ReadAllText(ppkFile.FullName)));

        // OpenSSH private key — public key blob is always stored unencrypted
        if (files.FirstOrDefault(LooksLikeOpensshPrivateKey) is { } privateFile)
            return TryParseOrEmpty(() => ParseOpensshPrivateKey(File.ReadAllText(privateFile.FullName)));

        return Empty;
    }
    
    /// <summary>
    ///     Parses a single-line OpenSSH public key in the format:
    ///     &lt;keytype&gt; &lt;base64blob&gt; [comment]
    /// </summary>
    private static BasicSshKeyFileInformation ParseOpenSshPublicKey(string content)
    {
        var parts = content.Split(' ', 3);
        if (parts.Length < 2)
            throw new FormatException("Not a valid OpenSSH public key line.");

        var keyTypeRaw = parts[0];
        var keyBlob    = Convert.FromBase64String(parts[1]);
        var comment    = parts.Length == 3 ? parts[2] : string.Empty;

        return new BasicSshKeyFileInformation
        {
            Format            = SshKeyFormat.OpenSSH,
            HashAlgorithmName = SshKeyHashAlgorithmName.SHA256,
            FingerPrint       = ComputeFingerprint(keyBlob),
            Comment           = comment,
            KeyType           = MapKeyType(keyTypeRaw),
            BitLength         = ComputeBitLength(keyTypeRaw, keyBlob)
        };
    }

    /// <summary>
    ///     Parses the unencrypted public-key section of an OpenSSH private key file.
    ///     The public key blob is stored in plaintext even when the private key is passphrase-protected.
    ///     The comment field will be empty because it resides in the encrypted section.
    /// </summary>
    private static BasicSshKeyFileInformation ParseOpensshPrivateKey(string pem)
    {
        var base64 = pem
            .Replace(OpensshPrivateHeader, "")
            .Replace(OpensshPrivateFooter, "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();

        ReadOnlyMemory<byte> blob = Convert.FromBase64String(base64);

        if (!blob.Span[..OpensshMagic.Length].SequenceEqual(OpensshMagic.Span))
            throw new FormatException("Invalid OpenSSH private key magic bytes.");

        var reader = new BlobReader(blob, OpensshMagic.Length);
        reader.ReadString(); // ciphername
        reader.ReadString(); // kdfname
        reader.ReadString(); // kdfoptions

        if (reader.ReadUInt32() == 0)
            throw new FormatException("No keys found in OpenSSH private key file.");

        var pubKeyBlob = reader.ReadBlob();
        var inner      = new BlobReader(pubKeyBlob, 0);
        var keyTypeRaw = inner.ReadString();

        return new BasicSshKeyFileInformation
        {
            KeyBlob = pubKeyBlob,
            Format            = SshKeyFormat.OpenSSH,
            HashAlgorithmName = SshKeyHashAlgorithmName.SHA256,
            FingerPrint       = ComputeFingerprint(pubKeyBlob.Span),
            Comment           = string.Empty,
            KeyType           = MapKeyType(keyTypeRaw),
            BitLength         = ComputeBitLength(keyTypeRaw, pubKeyBlob.Span)
        };
    }

    /// <summary>
    ///     Parses a PuTTY private key file (PPK v1, v2, or v3).
    ///     All versions store the public key blob and comment in unencrypted plaintext headers.
    ///     PPK v1 is mapped to <see cref="SshKeyFormat.PuTTYv2"/> as no dedicated enum value exists.
    /// </summary>
    private static BasicSshKeyFileInformation ParsePpkFile(string content)
    {
        var firstLine = content.Split('\n', 2)[0].Trim();
        
        var format = int.TryParse(firstLine.Replace(PuttyFileStart, "")[0].ToString(), out var version)
            ? version is 3 ? SshKeyFormat.PuTTYv3 : SshKeyFormat.PuTTYv2
            : SshKeyFormat.PuTTYv2;

        var keyTypeRaw   = string.Empty;
        var comment      = string.Empty;
        var publicBase64 = string.Empty;

        using var sr = new StringReader(content);
        while (sr.ReadLine() is { } line)
        {
            if (line.StartsWith(PuttyFileStart))
                keyTypeRaw = SplitPpkField(line);
            else if (line.StartsWith("Comment:"))
                comment = SplitPpkField(line);
            else if (line.StartsWith("Public-Lines:") &&
                     int.TryParse(SplitPpkField(line), out var pubLineCount))
            {
                for (var i = 0; i < pubLineCount; i++)
                    if (sr.ReadLine() is { } pubLine)
                        publicBase64 += pubLine.Trim();

                break; // everything we need has been read
            }
        }

        if (publicBase64.Length == 0)
            throw new FormatException("PPK file contains no public key data.");

        var keyBlob = Convert.FromBase64String(publicBase64);

        return new BasicSshKeyFileInformation
        {
            KeyBlob = keyBlob,
            Format            = format,
            HashAlgorithmName = SshKeyHashAlgorithmName.SHA256,
            FingerPrint       = ComputeFingerprint(keyBlob),
            Comment           = comment,
            KeyType           = MapKeyType(keyTypeRaw),
            BitLength         = ComputeBitLength(keyTypeRaw, keyBlob)
        };
    }
    
    /// <summary>
    ///     Maps an OpenSSH wire-format key type string to the <see cref="SshKeyType"/> enum.
    ///     DSA and unrecognized types fall back to <see cref="SshKeyType.RSA"/>.
    /// </summary>
    private static SshKeyType MapKeyType(string keyTypeRaw) => keyTypeRaw switch
    {
        "ssh-ed25519"
            or "ssh-ed448"
            or "sk-ssh-ed25519@openssh.com"               => SshKeyType.ED25519,
        "ecdsa-sha2-nistp256"
            or "ecdsa-sha2-nistp384"
            or "ecdsa-sha2-nistp521"
            or "sk-ecdsa-sha2-nistp256@openssh.com"       => SshKeyType.ECDSA,
        _                                                  => SshKeyType.RSA
    };

    /// <summary>
    ///     Returns the effective bit length of the key.
    ///     For RSA and DSA the modulus size is read directly from the key blob.
    /// </summary>
    private static int ComputeBitLength(string keyTypeRaw, ReadOnlySpan<byte> keyBlob) => keyTypeRaw switch
    {
        "ssh-ed25519"
            or "sk-ssh-ed25519@openssh.com"               => 256,
        "ssh-ed448"                                        => 448,
        "ecdsa-sha2-nistp256"
            or "sk-ecdsa-sha2-nistp256@openssh.com"       => 256,
        "ecdsa-sha2-nistp384"                              => 384,
        "ecdsa-sha2-nistp521"                              => 521,
        "ssh-rsa"                                          => GetRsaBitLength(keyBlob),
        "ssh-dss"                                          => GetDsaBitLength(keyBlob),
        _                                                  => 0
    };

    /// <summary>
    ///     Reads the RSA modulus from an SSH wire-format blob to determine the key's bit length.
    ///     Layout: [keytype][exponent e][modulus n] — all uint32-length-prefixed.
    /// </summary>
    private static int GetRsaBitLength(ReadOnlySpan<byte> span)
    {
        var typeLen = BinaryPrimitives.ReadInt32BigEndian(span);
        span = span[(4 + typeLen)..];

        var expLen = BinaryPrimitives.ReadInt32BigEndian(span);
        span = span[(4 + expLen)..];

        var modLen = BinaryPrimitives.ReadInt32BigEndian(span);
        span = span[4..];

        if (span[0] == 0x00) { span = span[1..]; modLen--; }

        return (modLen - 1) * 8 + (int)Math.Floor(Math.Log2(span[0]) + 1);
    }

    /// <summary>
    ///     Reads the DSA prime p from an SSH wire-format blob to determine the key's bit length.
    ///     Layout: [keytype][p][q][g][y] — all uint32-length-prefixed.
    /// </summary>
    private static int GetDsaBitLength(ReadOnlySpan<byte> span)
    {
        var typeLen = BinaryPrimitives.ReadInt32BigEndian(span);
        span = span[(4 + typeLen)..];

        var pLen = BinaryPrimitives.ReadInt32BigEndian(span);
        span = span[4..];

        if (span[0] == 0x00) pLen--;

        return pLen * 8;
    }

    /// <summary>Computes a SHA256 fingerprint and returns it as unpadded Base64.</summary>
    private static string ComputeFingerprint(ReadOnlySpan<byte> keyBlob, SshKeyHashAlgorithmName hashAlgorithmName = SshKeyHashAlgorithmName.SHA256)
    {
        IDigest digest = hashAlgorithmName switch
        {
            SshKeyHashAlgorithmName.SHA256 => new Sha256Digest(),
            SshKeyHashAlgorithmName.SHA512 => new Sha512Digest(),
            SshKeyHashAlgorithmName.SHA384 => new Sha384Digest(),
            SshKeyHashAlgorithmName.SHA1 => new Sha1Digest(),
            SshKeyHashAlgorithmName.MD5 => new MD5Digest(),
            _ => new Sha256Digest()
        };
        digest.BlockUpdate(keyBlob);
        byte[]? rented = null;
        var buffer = digest.GetDigestSize() <= 256
            ? stackalloc byte[digest.GetDigestSize()]
            : rented = ArrayPool<byte>.Shared.Rent(digest.GetDigestSize());
        try
        {
            var digested = digest.DoFinal(buffer);
            return Convert.ToBase64String(buffer[..digested]).TrimEnd('=');
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented, true);
            }
        }
    }
    /// <summary>Peeks at the first line of a file to check for the OpenSSH private key header.</summary>
    private static bool LooksLikeOpensshPrivateKey(FileInfo file)
    {
        try
        {
            using var fs = file.OpenText();
            return fs.ReadLine()?.TrimStart().StartsWith(OpensshPrivateHeader) == true;
        }
        catch { return false; }
    }

    /// <summary>Splits a PPK header line of the form "Key: Value" and returns the trimmed value.</summary>
    private static string SplitPpkField(string line)
        => line.Split(": ", 2) is { Length: 2 } parts ? parts[1].Trim() : string.Empty;

    /// <summary>
    ///     Wraps a parse call and returns <see cref="Empty"/> on any exception,
    ///     so that a malformed or unsupported key file never crashes the caller.
    /// </summary>
    private static BasicSshKeyFileInformation TryParseOrEmpty(Func<BasicSshKeyFileInformation> parse)
    {
        try   { return parse(); }
        catch { return Empty;   }
    }
    
    public string ToString(SshKeyHashAlgorithmName hashAlgorithmName, string outputFormat = OutputFormat)
    {
        return IsEmpty
            ? hashAlgorithmName == HashAlgorithmName
                ? string.Format(outputFormat, BitLength, HashAlgorithmName, FingerPrint, Comment, KeyType)
                : string.Format(outputFormat, BitLength, hashAlgorithmName, ComputeFingerprint([], hashAlgorithmName),
                    Comment, KeyType)
            : string.Empty;
    }

    /// <summary>
    ///     Returns a human-readable string matching the output format of <c>ssh-keygen -lf</c>:
    ///     <c>{bits} SHA256:{fingerprint} {comment} ({keyType})</c>
    /// </summary>
    public override string ToString()
        => ToString(HashAlgorithmName);
}

/// <summary>
///     Reads SSH binary protocol fields encoded as big-endian uint32-length-prefixed byte arrays.
/// </summary>
file sealed class BlobReader(ReadOnlyMemory<byte> data, int offset)
{
    private int _position = offset;

    public uint ReadUInt32()
    {
        var value = (uint)(
            (data.Span[_position]     << 24) |
            (data.Span[_position + 1] << 16) |
            (data.Span[_position + 2] <<  8) |
             data.Span[_position + 3]);
        _position += 4;
        return value;
    }

    public ReadOnlyMemory<byte> ReadBlob()
    {
        var length = (int)ReadUInt32();
        var result = data[_position..(_position + length)];
        _position += length;
        return result;
    }

    public string ReadString(Encoding? encoding = null)
        => (encoding ?? Encoding.ASCII).GetString(ReadBlob().Span);
}