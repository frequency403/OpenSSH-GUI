using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Models;
using Renci.SshNet;
using Renci.SshNet.Security.Cryptography.Ciphers;

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

        return string.Join("\n", lines, startPosition, linesToExtract);
    }

    private void WritePrivateKeyToFile(string privateFilePath)
    {
        using var streamWriter = new StreamWriter(File.OpenWrite(privateFilePath));
        streamWriter.Write(string.Join("\n", BeginOpenSshPrivateKey, PrivateKeyString.Replace("\n", "").Wrap(70), EndOpenSshPrivateKey, '\n'));
        // streamWriter.WriteLine(BeginOpenSshPrivateKey);
        // var converted =PrivateKeyString.Replace("\n", "").Wrap(70);
        // streamWriter.WriteLine(converted);
        // streamWriter.WriteLine(EndOpenSshPrivateKey);
    }
    
    public SshPublicKey ConvertToOpenSshKey()
    {
        // var directory = Directory.GetParent(FilePath)!.CreateSubdirectory("PPK");
        // var newFileDestination = Path.Combine(directory.FullName, Path.GetFileName(FilePath));
        // File.Move(FilePath, newFileDestination);
        var privateFilePath = FilePath.Replace(".ppk", "");
        // FilePath = newFileDestination;
        var publicFilePath = privateFilePath + ".pub";
        WritePrivateKeyToFile(privateFilePath);
        var convertProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = $"-y -f \"{privateFilePath}\"",
                FileName = "ssh-keygen"
            }
        };
        convertProcess.Start();
        var error = convertProcess.StandardError.ReadToEnd();
        var output = convertProcess.StandardOutput.ReadToEnd();
        if(string.IsNullOrWhiteSpace(error)) File.WriteAllText(publicFilePath, output);
        return new SshPublicKey(publicFilePath);
    }

    // public override string ToString()
    // {
    //     return $"";
    // }
}