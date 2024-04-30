namespace OpenSSHALib.Lib;

public static class PpkToOpenSsh
{
    public static string[]? ConvertFile(string path)
    {
        if (!File.Exists(path)) return null;
        var lines = File.ReadAllLines(path);

        var directory = Directory.GetParent(path);
        
        var fileContent = string.Join("\n", lines);

        var privateFilePath = path.Replace(".ppk", "");
        var publicFilePath = privateFilePath + ".pub";
        
        
        
        var definition = lines.First().Split(':').Last().Trim();
        var privateKeyB64 = ExtractLines(lines, "Private-Lines:");
        var publicKeyB64 = ExtractLines(lines, "Public-Lines:");
        
        File.WriteAllBytes(privateFilePath, Convert.FromBase64String(privateKeyB64));
        File.WriteAllBytes(publicFilePath, Convert.FromBase64String(publicKeyB64));
        return [privateFilePath, publicFilePath];
        //todo
    }
    
    static string ExtractKey(string content, string marker)
    {
        var startIndex = content.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var endIndex = content.IndexOf('\n', startIndex);
        return content.Substring(startIndex, endIndex - startIndex).Trim();
    }

    static string ExtractLines(string[] lines, string marker)
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
}