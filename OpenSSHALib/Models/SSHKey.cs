using System.Diagnostics;
using System.Text;
using OpenSSHALib.Enums;

namespace OpenSSHALib.Models;

public class SshKey
{
    public SshKey(string absoluteFilePath)
    {
        AbsoluteFilePath = absoluteFilePath;
        Filename = Path.GetFileName(AbsoluteFilePath);
        var readerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                Arguments = $"-l -f {AbsoluteFilePath}",
                FileName = "ssh-keygen"
            }
        };
        readerProcess.Start();
        var outputOfProcess = readerProcess.StandardOutput.ReadToEnd().Split(' ');
        // if (int.TryParse(outputOfProcess[0], out var parsed)) KeySize = parsed;
        Fingerprint = outputOfProcess[1];
        Comment = outputOfProcess[2];
        // KeyType = outputOfProcess[3].Replace("(", "").Replace(")", "").Trim();

        //

        var keyTypeText = outputOfProcess[3].Replace("(", "").Replace(")", "").Trim();

        if (Enum.TryParse<KeyType>(keyTypeText, true, out var parsedEnum))
        {
            if (int.TryParse(outputOfProcess[0], out var parsed)) KeyType = new SshKeyType(parsedEnum, parsed);
        }
        else
        {
            throw new ArgumentException($"{keyTypeText} is not a valid enum member of {typeof(KeyType)}");
        }

        //

        if (IsPublicKey) PrivateKey = new SshKey(AbsoluteFilePath.Replace(".pub", ""));
    }

    public string AbsoluteFilePath { get; }
    private bool IsPublicKey => AbsoluteFilePath.EndsWith(".pub");
    public string Filename { get; }

    public string Comment { get; private set; }

    // public SshKeyType KeySize { get; private set; }
    // public int KeySize { get; private set; }
    public SshKeyType KeyType { get; private set; }

    // public string KeyType { get; private set; }
    public string Fingerprint { get; private set; }
    public SshKey? PrivateKey { get; }

    public async Task<string?> ExportKey()
    {
        try
        {
            await using var fileStream = File.OpenRead(AbsoluteFilePath);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            return Encoding.Default.GetString(memoryStream.ToArray());
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return null;
        }
    }

    public void DeleteKeys()
    {
        File.Delete(AbsoluteFilePath);
        File.Delete(PrivateKey.AbsoluteFilePath);
    }
}