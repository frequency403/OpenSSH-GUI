using System.Diagnostics;
using System.Text;
using OpenSSHALib.Enums;
using OpenSSHALib.Models;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;
using SshNet.PuttyKeyFile;
using PemReader = Org.BouncyCastle.OpenSsl.PemReader;
using PemWriter = Org.BouncyCastle.Utilities.IO.Pem.PemWriter;

namespace OpenSSHALib.Lib.Structs;

public record struct PpkKey
{
    private const string EncryptionLineStart    = "Encryption:";
    private const string PrivateKeyLineStart    = "Private-Lines:";
    private const string PublicKeyLineStart     = "Public-Lines:";
    private const string DefinitionLineStart    = "PuTTY-User-Key-File";
    private const string CommentLineStart       = "Comment:";
    private const string MacLineStart           = "Private-MAC:";

    private const string BeginOpenSshPrivateKey = "-----BEGIN OPENSSH PRIVATE KEY-----";
    private const string EndOpenSshPrivateKey   = "-----END OPENSSH PRIVATE KEY-----";
    
    public PpkKey(string filePath)
    {
        if (!File.Exists(filePath)) return;
        FilePath = filePath;
        var lines = File.ReadAllLines(FilePath);
        EncryptionType = Enum.TryParse<EncryptionType>(
            lines.FirstOrDefault(e => e.StartsWith(EncryptionLineStart)).Replace(EncryptionLineStart, "").Trim(),
            out var parsedEncryptionType) 
            ? parsedEncryptionType 
            : EncryptionType.NONE;
        KeyType = Enum.TryParse<KeyType>(
            lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart)).Split('-')[0].Trim(),
            out var parsedKeyType)
            ? parsedKeyType
            : KeyType.RSA;
        Comment = (lines.FirstOrDefault(e => e.StartsWith(CommentLineStart)) ?? "").Replace(CommentLineStart, "").Trim();
        PrivateKeyString = ExtractLines(lines, PrivateKeyLineStart);
        PublicKeyString = ExtractLines(lines, PublicKeyLineStart);
        PrivateMAC = (lines.FirstOrDefault(e => e.StartsWith(MacLineStart)) ?? "").Replace(MacLineStart, "").Trim();
    }
    
    public string FilePath { get; private set; }

    public KeyType KeyType { get; }
    
    public EncryptionType EncryptionType { get; }
    
    public string Comment { get; }
    
    public string PublicKeyString { get; }
    
    public string PrivateKeyString { get; }
    
    public string PrivateMAC { get; }

    private string ExtractLines(string[] lines, string marker)
    {
        var startPosition = 0;
        var linesToExtract = 0;
        foreach (var line in lines.Select((content, index) => (content, index)))
        {
            if (startPosition == 0)
            {
                if (line.content.Contains(marker))
                {
                    linesToExtract = int.Parse(line.content.Replace(marker, "").Trim());
                    startPosition = line.index + 1;
                    break;
                }
            }
        }

        return string.Join("", lines, startPosition, linesToExtract);
    }
    
    public SshPublicKey? ConvertToOpenSshKey()
    {
        return null;
        // var privateFilePath = FilePath.Replace(".ppk", "");
        //
        // var publicFilePath = privateFilePath + ".pub";
        //
        // var convertFromPpk = new Process
        // {
        //     StartInfo = !OperatingSystem.IsWindows() ? new ProcessStartInfo
        //     {
        //         WindowStyle = ProcessWindowStyle.Hidden,
        //         RedirectStandardOutput = true,
        //         RedirectStandardError = true,
        //         CreateNoWindow = true,
        //         Arguments = $"{FilePath} -O private-openssh -o \"{privateFilePath}\"",
        //         FileName = "puttygen"
        //     } : new ProcessStartInfo
        //     {
        //         WindowStyle = ProcessWindowStyle.Hidden,
        //         RedirectStandardOutput = true,
        //         RedirectStandardError = true,
        //         CreateNoWindow = true,
        //         Arguments = $"/keygen {FilePath} -o \"{privateFilePath}\"",
        //         FileName = "winscp.com"
        //     }
        // };
        // convertFromPpk.Start();
        //
        // var convertError = convertFromPpk.StandardError.ReadToEnd();
        // var convertSuccess = convertFromPpk.StandardOutput.ReadToEnd();
        //
        // if (!string.IsNullOrWhiteSpace(convertError)) return null;
        //
        // var directory = Directory.GetParent(FilePath)!.CreateSubdirectory("PPK");
        // var newFileDestination = Path.Combine(directory.FullName, Path.GetFileName(FilePath));
        // File.Move(FilePath, newFileDestination);
        // FilePath = newFileDestination;
        //
        //
        // var extractPubKey = new Process
        // {
        //     StartInfo = new ProcessStartInfo
        //     {
        //         WindowStyle = ProcessWindowStyle.Hidden,
        //         RedirectStandardOutput = true,
        //         RedirectStandardError = true,
        //         CreateNoWindow = true,
        //         Arguments = $"-y -f \"{privateFilePath}\"",
        //         FileName = "ssh-keygen"
        //     }
        // };
        // extractPubKey.Start();
        // var error = extractPubKey.StandardError.ReadToEnd();
        // var output = extractPubKey.StandardOutput.ReadToEnd();
        // if(string.IsNullOrWhiteSpace(error)) File.WriteAllText(publicFilePath, output);
        // return new SshPublicKey(publicFilePath);
    }

    // public override string ToString()
    // {
    //     return $"";
    // }
}