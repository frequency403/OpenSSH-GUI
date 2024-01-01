using System.Text;

namespace OpenSSHALib.Model;

public class KnownHostsFile
{
    private FileStream FileStream { get; set; }
    private const string LineEnding = "\r\n";
    private readonly string _filePath;
    
    public IEnumerable<KnownHosts> KnownHosts { get; private set; }
    
    
    public KnownHostsFile(string pathToKnownHosts)
    {
        _filePath = pathToKnownHosts;
        OpenFile();
    }

    private void OpenFile(bool read = true)
    {
        FileStream = read ? File.OpenRead(_filePath) : File.OpenWrite(_filePath);
    }
    
    public async Task ReadContentAsync()
    {
        using var streamReader = new StreamReader(FileStream);
        var fileContent = await streamReader.ReadToEndAsync();
        KnownHosts = fileContent.Split(LineEnding).Select(knownHost => new KnownHosts(knownHost));
    }
    
    public void ReadContent()
    {
        using var streamReader = new StreamReader(FileStream);
        KnownHosts = streamReader.ReadToEnd().Split(LineEnding).Select(knownHost => new KnownHosts(knownHost));
    }

    public void SyncKnownHosts(IEnumerable<KnownHosts> newKnownHosts) => KnownHosts = newKnownHosts;
    
    public void UpdateFile()
    {
        OpenFile(false);
        FileStream.Position = 0;
        var newContent = KnownHosts.Where(e => !e.MarkedForDeletion)
            .Aggregate("", (a, b) => a += $"{b.FullHostEntry}{LineEnding}");
        FileStream.Write(Encoding.Default.GetBytes(newContent));
        FileStream.Flush(true);
    }
}