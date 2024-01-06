using System.Diagnostics;
using System.Text;
using OpenSSHALib.Enums;

namespace OpenSSHALib.Models;

public abstract class SshKey
{
    protected SshKey(string absoluteFilePath)
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
        Fingerprint = outputOfProcess[1];
        Comment = outputOfProcess[2];

        var keyTypeText = outputOfProcess[3].Replace("(", "").Replace(")", "").Trim();

        if (Enum.TryParse<KeyType>(keyTypeText, true, out var parsedEnum))
        {
            if (int.TryParse(outputOfProcess[0], out var parsed)) KeyType = new SshKeyType(parsedEnum, parsed);
        }
        else
        {
            throw new ArgumentException($"{keyTypeText} is not a valid enum member of {typeof(KeyType)}");
        }
    }

    public string AbsoluteFilePath { get; protected set; }
    protected bool IsPublicKey => AbsoluteFilePath.EndsWith(".pub");
    public string KeyTypeString => IsPublicKey ? "public" : "private";
    public string Filename { get; protected set; }
    public string Comment { get; protected set; }
    public SshKeyType KeyType { get; } = new(Enums.KeyType.RSA);
    public string Fingerprint { get; protected set; }

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

    public virtual void DeleteKey()
    {
        File.Delete(AbsoluteFilePath);
    }
}