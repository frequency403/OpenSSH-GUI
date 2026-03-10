using System.Diagnostics.CodeAnalysis;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Keys;

public class SshKeyFileInformation(string filePath)
{
    [MemberNotNullWhen(true, nameof(PublicKeyFileName))]
    private bool IsOpenSshKey => CurrentFormat is SshKeyFormat.OpenSSH;
    private readonly FileInfo _fileInfo = new(filePath);

    public string Name => _fileInfo.Name;
    public string? PublicKeyFileName => IsOpenSshKey ? Path.ChangeExtension(_fileInfo.Name, "pub") : null;
    public string FullName => _fileInfo.FullName;
    public bool Exists => _fileInfo.Exists;
    public string? DirectoryName => _fileInfo.DirectoryName;
    
    public IEnumerable<FileInfo> Files => new[] { FullName, PublicKeyFileName }.Where(e => !string.IsNullOrEmpty(e))
        .Select(e => new FileInfo(e!));

    public SshKeyFormat CurrentFormat => _fileInfo.Extension switch
    {
        ".ppk" => SshKeyFormat.PuTTYv3,
        _ => SshKeyFormat.OpenSSH
    };
    
    public SshKeyFormat DefaultConversionFormat => AvailableFormatsForConversion.Contains(SshKeyFormat.OpenSSH) ? SshKeyFormat.OpenSSH : AvailableFormatsForConversion.OrderDescending().First();
    
    public IEnumerable<SshKeyFormat> AvailableFormatsForConversion => Enum.GetValues<SshKeyFormat>().Where(e => e != CurrentFormat);
}