using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.Misc;
using Renci.SshNet;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Services;

/// <summary>
/// Provides functionality to write content or SSH key files to the filesystem.
/// </summary>
public class KeyFileWriterService(ILogger<KeyFileWriterService> logger) : IKeyFileWriterService
{
    /// <inheritdoc />
    public async ValueTask WriteToFile(string filePath, string content,
        bool overwrite = false, Encoding? encoding = null)
    {
        if (encoding is null)
        {
            encoding ??= Encoding.UTF8;
            logger.LogDebug("Using default encoding: {encoding}", encoding.EncodingName);
        }
        else
        {
            logger.LogDebug("Using encoding: {encoding}", encoding.EncodingName);
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Exists && !overwrite)
        {
            logger.LogWarning("File {filePath} already exists. Skipping write operation.", filePath);
            throw new IOException("File already exists");
        }

        var options = new FileStreamOptions
        {
            BufferSize = 0,
            Access = FileAccess.ReadWrite,
            Mode = FileMode.OpenOrCreate,
            Share = FileShare.ReadWrite
        };

        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }
        
        await using var fileStream = fileInfo.Open(options);
        logger.LogDebug("Opened file {filePath}", filePath);

        byte[]? rented = null;
        var maxByteCount = encoding.GetMaxByteCount(content.Length);
        var buffer = maxByteCount <= 256
            ? stackalloc byte[256]
            : rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
        logger.LogDebug("Allocated {byteCount} bytes", buffer.Length);
        try
        {
            var writtenBytes = encoding.GetBytes(content, buffer);
            logger.LogDebug("Writing {byteCount} bytes into file {filePath}", writtenBytes, filePath);
            fileStream.Write(buffer[..writtenBytes]);
            logger.LogDebug("Successfully wrote file {filePath}", filePath);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while writing file {filePath}", filePath);
            throw;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented, true);
                logger.LogDebug("Freeing memory");
            }
        }
    }


    /// <inheritdoc/>
    public async ValueTask<IEnumerable<string>> WriteToFileInSpecificFormat(
        SshKeyFormat format,
        ISshKeyEncryption encryption,
        IPrivateKeySource privateKeySource, string filePath, bool overwrite = false)
    {
        var privateKeyFileContent = format is SshKeyFormat.OpenSSH
            ? privateKeySource.ToOpenSshFormat(encryption)
            : privateKeySource.ToPuttyFormat(encryption, format);
        var writtenFiles = new List<string>();
        switch (format)
        {
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
                break;
            case SshKeyFormat.OpenSSH:
            default:
            {
                var pubKeyFormat = format.ChangeExtension(filePath);
                try
                {
                    await WriteToFile(pubKeyFormat, privateKeySource.ToOpenSshPublicFormat(), overwrite);
                }
                catch (Exception e)
                {
                    logger.LogError("Failed to write public key file {filePath}", pubKeyFormat);
                    throw;
                }

                writtenFiles.Add(pubKeyFormat);
                break;
            }
        }

        var privateFilePath = format.ChangeExtension(filePath, false);
        try
        {
            await WriteToFile(privateFilePath, privateKeyFileContent, overwrite);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to write private key file {filePath}", privateFilePath);
            throw;
        }
        writtenFiles.Add(privateFilePath);
        return writtenFiles;
    }
    
    
    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> WriteToFileInSpecificFormat(
        SshKeyGenerateInfo generateInfo,
        GeneratedPrivateKey createdKey, string filePath, bool overwrite = false) => WriteToFileInSpecificFormat(
        generateInfo.KeyFormat, generateInfo.Encryption, createdKey, filePath,
        overwrite);
}