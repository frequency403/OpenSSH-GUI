// File Created by: Oliver Schantz
// Created: 18.05.2024 - 13:05:52
// Last edit: 18.05.2024 - 13:05:53

using Microsoft.EntityFrameworkCore;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using Renci.SshNet;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.PuttyKeyFile;

namespace OpenSSH_GUI.Core.Lib.Static;

public static class KeyFactory
{
    /// <summary>
    /// Generates a new SSH key asynchronously.
    /// </summary>
    /// <param name="params">SshKeyGenerateParams object containing the parameters for the new SSH key.</param>
    /// <returns>The newly generated SSH key</returns>
    /// <remarks>
    /// Depending on the KeyFormat specified in the SshKeyGenerateParams object, different formats of SSH keys will be generated.
    /// For key formats PuTTYv2 and PuTTYv3, a PuTTY format key (.ppk) will be generated.
    /// For other formats (OpenSSH, etc.), OpenSSH format keys will be generated.
    /// </remarks>
    /// <exception cref="Exception">Thrown when unable to create Stream Writer or when unable to write the generated SSH key to the stream.</exception>
    public static async Task<ISshKey> GenerateNewAsync(SshKeyGenerateParams @params)
    {
        if (Path.HasExtension(@params.FileName)) throw new ArgumentException("The parameter \"FileName\" has an extension. Extensions are not allowed as they are determined by the KeyFormat value!", nameof(@params.FileName));
        await using var privateStream = new MemoryStream();
        var generated = SshNet.Keygen.SshKey.Generate(privateStream, @params.ToInfo());
        switch (@params.KeyFormat)
        {
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
                var puttyFileName = @params.KeyFormat.ChangeExtension(@params.FullFilePath);
                await using (var privateStreamWriter = new StreamWriter(File.Create(puttyFileName)))
                {
                    await privateStreamWriter.WriteAsync(generated.ToPuttyFormat());
                }

                return new PpkKey(puttyFileName, @params.Password);
            case SshKeyFormat.OpenSSH:
            default:
                var pubPath = @params.KeyFormat.ChangeExtension(@params.FullFilePath);
                await using (var privateStreamWriter = new StreamWriter(File.Create(@params.KeyFormat.ChangeExtension(@params.FullFilePath, false))))
                {
                    await privateStreamWriter.WriteAsync(generated.ToOpenSshFormat());
                }

                await using (var publicStreamWriter = new StreamWriter(File.Create(pubPath)))
                {
                    await publicStreamWriter.WriteAsync(generated.ToOpenSshPublicFormat());
                }

                return new SshPublicKey(pubPath,@params.Password);
        }
    }

    public static ISshKey? FromDtoId(int id)
    {
        var context = new OpenSshGuiDbContext();
        var dto = context.KeyDtos.Include(k => k.ConnectionCredentialsDto).FirstOrDefault(k => k.Id == id);
        return dto?.ToKey();
    }
    
    /// <summary>
    /// Generates a new SSH key.
    /// </summary>
    /// <param name="params">The <see cref="SshKeyGenerateParams"/> object providing infos for generation</param>    /// <returns>The newly generated SSH key.</returns>
    /// <exception cref="Exception">Thrown when unable to create Stream Writer or when unable to write the generated SSH key to the stream.</exception>
    public static ISshKey GenerateNew(SshKeyGenerateParams @params) => GenerateNewAsync(@params).Result;

    /// <summary>
    /// Generates a new SSH key synchronously.
    /// </summary>
    /// <param name="format">The format of the SSH key.</param>
    /// <param name="type">The type of the SSH key.</param>
    /// <param name="fileName">The name of the file to save the SSH key. If not provided, a temporary file name will be used.</param>
    /// <param name="filePath">The path to save the SSH key file. If not provided, the default SSH path will be used.</param>
    /// <param name="password">The password to protect the SSH key with. If not provided, the SSH key will not be password protected.</param>
    /// <param name="comment">The comment to associate with the SSH key. If not provided, a default comment will be used.</param>
    /// <param name="keyLength">The length of the SSH key. If not provided, the default length for the key type will be used.</param>
    /// <returns>The newly generated SSH key.</returns>
    /// <remarks>
    /// Depending on the KeyFormat specified, different formats of SSH keys will be generated.
    /// For key formats PuTTYv2 and PuTTYv3, a PuTTY format key (.ppk) will be generated.
    /// For other formats (OpenSSH, etc.), OpenSSH format keys will be generated.
    /// </remarks>
    /// <exception cref="Exception">Thrown when unable to create Stream Writer or when unable to write the generated SSH key to the stream.</exception>
    public static ISshKey GenerateNew(KeyType type,
        SshKeyFormat format, 
        string? fileName = null, 
        string? filePath = null, 
        string? password = null,
        string? comment = null,
        int? keyLength = null) => GenerateNew(new SshKeyGenerateParams(type, format, fileName, filePath, password, comment, keyLength));

    public static ISshKey? FromPath(string path, string? password = null, int dbId = 0)
    {
        try
        {
            return Path.GetExtension(path) switch
            {
                var x when x.Contains(".pub") => new SshPublicKey(path, password) {Id = dbId},
                var x when x.Contains(".ppk") => new PpkKey(path, password){Id = dbId},
                var x when string.IsNullOrWhiteSpace(x) => new SshPrivateKey(path, password){Id = dbId},
                _ => null
            };
        }
        catch (Exception e)
        {
            return null;
        }
    }
}