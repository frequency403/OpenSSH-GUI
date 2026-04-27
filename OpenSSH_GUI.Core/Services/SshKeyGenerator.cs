using System.Text;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.Keys;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Services;

public class SshKeyGenerator(ILogger<SshKeyGenerator> logger, ISshKeyFactory keyFactory, IKeyFileWriterService keyFileWriterService) : ISshKeyGenerator
{
    /// <inheritdoc/>
    public async ValueTask<SshKeyFile> Generate(string fullFilePath, SshKeyGenerateInfo generateParamsInfo, bool overwrite = false)
    {
        GeneratedPrivateKey? createdKey;
        try
        {
            await using var privateStream = new MemoryStream();
            createdKey = SshKey.Generate(privateStream, generateParamsInfo);
            if (createdKey is null)
                throw new InvalidOperationException("Could not generate new key");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while generating key file {filePath}", fullFilePath);
            throw;
        }

        var filePath = generateParamsInfo.KeyFormat.ChangeExtension(fullFilePath, false);

        await keyFileWriterService.WriteToFileInSpecificFormat(generateParamsInfo, createdKey, filePath, overwrite);

        var keyFileSource = SshKeyFileSource.FromDisk(filePath);
        var keyFile = keyFactory.Create();
        if (string.IsNullOrWhiteSpace(generateParamsInfo.Encryption.Passphrase))
            keyFile.Load(keyFileSource);
        else
            keyFile.Load(keyFileSource, Encoding.UTF8.GetBytes(generateParamsInfo.Encryption.Passphrase));
        
        return keyFile;
    }
}