using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using OpenSSHALib.Enums;
using OpenSSHALib.Lib.Structs;

namespace OpenSSHALib.Models;

public abstract partial class SshKey
{
    
    [GeneratedRegex(@"\(([^)]*)\)")]
    private static partial Regex BracesRegex();
    
    private string ReadSshFile(ref string filePath)
    {
        using var readerProcess = new Process();
        readerProcess.StartInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Arguments = $"-l -f {filePath}",
            FileName = "ssh-keygen"
        };
        readerProcess.Start();
        return readerProcess.StandardOutput.ReadToEnd();
    }
    protected SshKey(string absoluteFilePath)
    {
        AbsoluteFilePath = absoluteFilePath;
        if (!File.Exists(AbsoluteFilePath)) throw new FileNotFoundException($"No such file: {AbsoluteFilePath}");
        Filename = Path.GetFileName(AbsoluteFilePath);
        var outputOfProcess = ReadSshFile(ref absoluteFilePath).Split(' ').ToList();
        var intToParse = outputOfProcess.First();
        outputOfProcess.RemoveAt(0);

        Fingerprint = outputOfProcess.First();
        outputOfProcess.RemoveAt(0);

        var keyTypeText = BracesRegex().Replace(outputOfProcess.Last().Trim(), "$1");
        outputOfProcess.RemoveAt(outputOfProcess.Count - 1);

        Comment = string.Join(" ", outputOfProcess);

        if (Enum.TryParse<KeyType>(keyTypeText, true, out var parsedEnum))
        {
            if (int.TryParse(intToParse, out _)) KeyType = new SshKeyType(parsedEnum);
        }
        else
        {
            throw new ArgumentException($"{keyTypeText} is not a valid enum member of {typeof(KeyType)}");
        }
    }

    public string AbsoluteFilePath { get; protected set; }
    private bool IsPublicKey => AbsoluteFilePath.EndsWith(".pub");
    public string KeyTypeString => IsPublicKey ? "public" : "private";
    public string Filename { get; protected set; }
    public string Comment { get; protected set; }
    public SshKeyType KeyType { get; } = new(Enums.KeyType.RSA);
    public string Fingerprint { get; protected set; }

    public async Task<string?> ExportKeyAsync()
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

    public string? ExportKey()
    {
        try
        {
            using var fileStream = File.OpenRead(AbsoluteFilePath);
            using var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            return Encoding.Default.GetString(memoryStream.ToArray());
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return null;
        }
    }

}