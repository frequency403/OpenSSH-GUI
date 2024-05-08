using System.Diagnostics;
using OpenSSHALib.Enums;
using OpenSSHALib.Interfaces;
using Renci.SshNet;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.PuttyKeyFile;

namespace OpenSSHALib.Models;


public class PpkKey : IPpkKey
{
    private const string EncryptionLineStart = "Encryption:";
    private const string PrivateKeyLineStart = "Private-Lines:";
    private const string PublicKeyLineStart = "Public-Lines:";
    private const string DefinitionLineStart = "PuTTY-User-Key-File-";
    private const string CommentLineStart = "Comment:";
    private const string MacLineStart = "Private-MAC:";
    private readonly PuttyKeyFile _keyFile;
    public SshKeyFormat Format { get; }
    public PpkKey(string absoluteFilePath)
    {
        if (!File.Exists(absoluteFilePath)) return;
        AbsoluteFilePath = absoluteFilePath;
        Filename = Path.GetFileNameWithoutExtension(AbsoluteFilePath);
        _keyFile = new PuttyKeyFile(AbsoluteFilePath);
        var lines = File.ReadAllLines(AbsoluteFilePath);

        Format = int.TryParse(
            lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart)).Replace(DefinitionLineStart, "")[0].ToString(),
            out var parsed)
            ? parsed switch
            {
                2 => SshKeyFormat.PuTTYv2,
                3 => SshKeyFormat.PuTTYv3,
                _ => SshKeyFormat.OpenSSH
            }
            : SshKeyFormat.OpenSSH;
        KeyTypeString = lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart)).Split('-')[0].Trim();
        KeyType = new Models.SshKeyType(Enum.TryParse<KeyType>(lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart)).Split('-')[0].Trim(), out var parsedKeyType)
            ? parsedKeyType
            : OpenSSHALib.Enums.KeyType.RSA);
        EncryptionType = Enum.TryParse<EncryptionType>(
            lines.FirstOrDefault(e => e.StartsWith(EncryptionLineStart)).Replace(EncryptionLineStart, "").Trim(),
            out var parsedEncryptionType)
            ? parsedEncryptionType
            : EncryptionType.NONE;
        Comment = (lines.FirstOrDefault(e => e.StartsWith(CommentLineStart)) ?? "").Replace(CommentLineStart, "")
            .Trim();
        PrivateKeyString = ExtractLines(lines, PrivateKeyLineStart);
        PublicKeyString = ExtractLines(lines, PublicKeyLineStart);
        PrivateMAC = (lines.FirstOrDefault(e => e.StartsWith(MacLineStart)) ?? "").Replace(MacLineStart, "").Trim();
        
    }

    public string AbsoluteFilePath { get; private set; }
    public string KeyTypeString { get; }
    public string Filename { get; } 
    public ISshKeyType KeyType { get; }
    public string Fingerprint { get; }
    public bool IsPublicKey { get; }
    public Task<string> ExportKeyAsync(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        return Task.FromResult(publicKey
            ? format switch
            {
                SshKeyFormat.OpenSSH => _keyFile.ToOpenSshPublicFormat(),
                SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => _keyFile.ToPuttyPublicFormat(),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            }
            : format switch
            {
                SshKeyFormat.OpenSSH => _keyFile.ToOpenSshFormat(),
                SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => _keyFile.ToPuttyFormat(format),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            });
    }
    public string ExportKey(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH) => ExportKeyAsync(publicKey, format).Result;
    public Task<string> ExportKeyAsync(SshKeyFormat format = SshKeyFormat.OpenSSH) => ExportKeyAsync(true, format);
    public string ExportKey(SshKeyFormat format = SshKeyFormat.OpenSSH) => ExportKeyAsync(format).Result;

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

    public ISshPublicKey? ConvertToOpenSshKey(out string errorMessage, bool temp = false)
    {
        try
        {
            var key = new PuttyKeyFile(AbsoluteFilePath);
            var privateFilePath = AbsoluteFilePath.Replace(".ppk", "");
            var publicFilePath = AbsoluteFilePath.Replace(".ppk", ".pub");
            if (File.Exists(privateFilePath)) privateFilePath += $"_{DateTime.Now:yy_MM_dd_HH_mm}";
            if (File.Exists(publicFilePath)) publicFilePath = publicFilePath.Replace(".pub", $"{DateTime.Now:yy_MM_dd_HH_mm}.pub");
            File.WriteAllText(privateFilePath, key.ToOpenSshFormat());
            var extractPubKey = new Process
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
            extractPubKey.Start();
            errorMessage = extractPubKey.StandardError.ReadToEnd();
            var output = extractPubKey.StandardOutput.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(errorMessage)) throw new Exception(errorMessage);
            File.WriteAllText(publicFilePath, output);
            var directory = Directory.GetParent(AbsoluteFilePath)!.CreateSubdirectory("PPK");
            var newFileDestination = Path.Combine(directory.FullName, Path.GetFileName(AbsoluteFilePath));
            File.Move(AbsoluteFilePath, newFileDestination);
            AbsoluteFilePath = newFileDestination;
            return new SshPublicKey(publicFilePath);
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            return null;
        }
    }

    public IPrivateKeySource GetRenciKeyType() => _keyFile;
    public void DeleteKey()
    {
        File.Delete(AbsoluteFilePath);
    }

    // public override string ToString()
    // {
    //     return $"";
    // }
}