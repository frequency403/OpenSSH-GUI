using System.Text;
using ReactiveUI;

namespace OpenSSHALib.Models;

public class KnownHostsFile : ReactiveObject
{
    internal const string LineEnding = "\r\n";
    private readonly string _filePath;
    
    public List<KnownHost> KnownHosts { get; private set; }
    
    
    public KnownHostsFile(string pathToKnownHosts)
    {
        _filePath = pathToKnownHosts;
        ReadContent();
    }
    
    
    private void SetKnownHosts(string fileContent) => KnownHosts = fileContent
        .Split(LineEnding)
        .Where(e => !string.IsNullOrEmpty(e))
        .GroupBy(e => e.Split(' ')[0])
        .Select(e => new KnownHost(e)).ToList();
    
    public async Task ReadContentAsync(FileStream? stream = null)
    {
        if (stream is null)
        {
            await using var fileStream = File.OpenRead(_filePath);
            using var streamReader = new StreamReader(fileStream);
            SetKnownHosts(await streamReader.ReadToEndAsync());
        }
        else
        {
            using var streamReader = new StreamReader(stream);
            SetKnownHosts(await streamReader.ReadToEndAsync());
        }
        
    }
    
    public void ReadContent(FileStream? stream = null)
    {
        if (stream is null)
        {
            using var fileStream = File.OpenRead(_filePath);
            using var streamReader = new StreamReader(fileStream);
            SetKnownHosts(streamReader.ReadToEnd());
        }
        else
        {
            using var streamReader = new StreamReader(stream);
            SetKnownHosts(streamReader.ReadToEnd());
        }
    }

    public void SyncKnownHosts(IEnumerable<KnownHost> newKnownHosts) => KnownHosts = newKnownHosts.ToList();
    
    public async Task UpdateFile()
    {
        await using var fileStream = File.OpenWrite(_filePath);
        fileStream.SetLength(0);
        await fileStream.FlushAsync();
        var newContent = KnownHosts
            .Where(e => !e.DeleteWholeHost)
            .Aggregate("", (current, host) => current + host.GetAllEntries());
        var newContentBytes = Encoding.Default.GetBytes(newContent);
        fileStream.SetLength(newContentBytes.Length);
        await fileStream.WriteAsync(newContentBytes);
        await fileStream.FlushAsync(); 
        SetKnownHosts(newContent);
    }
}