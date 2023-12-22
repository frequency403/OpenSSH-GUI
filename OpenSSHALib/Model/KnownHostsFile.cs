using System.Text;

namespace OpenSSHALib.Model;

public class KnownHostsFile
{
    private FileStream _fileStream { get; }
    private string _lineEnding = "\r\n";
    
    public IEnumerable<KnownHosts> KnownHosts { get; private set; }
    
    
    public KnownHostsFile(string pathToKnownHosts)
    {
        _fileStream = File.Open(pathToKnownHosts, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
    }

    public async Task ReadContentAsync()
    {
        using var streamReader = new StreamReader(_fileStream);
        var fileContent = await streamReader.ReadToEndAsync();
        KnownHosts = fileContent.Split(_lineEnding).Select(knownHost => new KnownHosts(knownHost));
    }
    
    public void ReadContent()
    {
        using var streamReader = new StreamReader(_fileStream);
        KnownHosts = streamReader.ReadToEnd().Split(_lineEnding).Select(knownHost => new KnownHosts(knownHost));
    }

    public void UpdateFile()
    {
        _fileStream.Position = 0;
        var newContent = KnownHosts.Where(e => !e.MarkedForDeletion)
            .Aggregate("", (a, b) => a += $"{b.FullHostEntry}{_lineEnding}");
        _fileStream.Write(Encoding.Default.GetBytes(newContent));
        _fileStream.Flush(true);
    }
}