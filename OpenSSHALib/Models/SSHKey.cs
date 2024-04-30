using System.Diagnostics;
using System.Net;
using System.Text;
using OpenSSHALib.Enums;

namespace OpenSSHALib.Models;

public abstract class SshKey
{
    private void ConvertPpkToOpenSsh(ref string filePath)
    {
        var convertProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = $"-i -f {filePath}",
                FileName = "ssh-keygen"
            }
        };
        convertProcess.Start();
        var originalOutput = convertProcess.StandardOutput.ReadToEnd();
        var err = convertProcess.StandardError.ReadToEnd();
        if (originalOutput.Contains("invalid", StringComparison.InvariantCultureIgnoreCase) ||
            originalOutput.Contains("is not", StringComparison.InvariantCultureIgnoreCase) ||
            err.Contains("invalid", StringComparison.InvariantCultureIgnoreCase) ||
            err.Contains("is not", StringComparison.InvariantCultureIgnoreCase))
        {
            using var streamReader = new StreamReader(File.OpenRead(filePath));
            var tempFilePath = Path.GetTempFileName();
            using var tempFile = new StreamWriter(File.OpenWrite(tempFilePath));
            var fileContent = streamReader.ReadToEnd();
            const string startEnd = "---- {0} OPENSSH PRIVATE KEY ----";
            tempFile.WriteLine(startEnd, "BEGIN");
            var lineCount = 0;
            var feedFile = false;
            foreach (var line in fileContent.Split("\n"))
            {
                if (line.StartsWith("Private-Lines:"))
                {
                    lineCount = int.Parse(line.Replace("Private-Lines:", "").Trim()) -1;
                    feedFile = true;
                    continue;
                }

                if (feedFile)
                {
                    tempFile.WriteLine(line);
                    lineCount--;
                }

                if (lineCount == 0) feedFile = false;
            }
            tempFile.WriteLine(startEnd, "END");
            
            convertProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Arguments = $"-l -f {tempFilePath}",
                    FileName = "ssh-keygen"
                }
            };
            convertProcess.Start();
            originalOutput = convertProcess.StandardOutput.ReadToEnd();
// @todo
        }
        filePath = filePath.Replace(".ppk", "");
        if (File.Exists(filePath)) { File.Delete(filePath); }
        using var writer = new StreamWriter(File.Open(filePath, FileMode.OpenOrCreate));
        writer.WriteLine(originalOutput);
    }
    private string ReadSshFile(ref string filePath)
    {
        if(filePath.Contains(".ppk", StringComparison.InvariantCultureIgnoreCase))
        {
            ConvertPpkToOpenSsh(ref filePath);
        }
        var readerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                Arguments = $"-l -f {filePath}",
                FileName = "ssh-keygen"
            }
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
        if (!string.Equals(AbsoluteFilePath, absoluteFilePath)) AbsoluteFilePath = absoluteFilePath;
        var intToParse = outputOfProcess.First();
        outputOfProcess.Remove(intToParse);
        var currentLastItem = outputOfProcess.Last();
        var keyTypeText = currentLastItem.Replace("(", "").Replace(")", "").Trim();
        outputOfProcess.Remove(currentLastItem);
        Fingerprint = outputOfProcess.First();
        outputOfProcess.Remove(Fingerprint);
        Comment = outputOfProcess.Aggregate("", (a, b) => a += $" {b}").Trim();

        

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