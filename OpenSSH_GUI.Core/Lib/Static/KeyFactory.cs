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
        await using var dbContext = new OpenSshGuiDbContext();
        var generated = SshNet.Keygen.SshKey.Generate(privateStream, @params.ToInfo());
        ISshKey key;
        switch (@params.KeyFormat)
        {
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
                var puttyFileName = @params.KeyFormat.ChangeExtension(@params.FullFilePath);
                await using (var privateStreamWriter = new StreamWriter(File.Create(puttyFileName)))
                {
                    await privateStreamWriter.WriteAsync(generated.ToPuttyFormat());
                }

                key = new PpkKey(puttyFileName, @params.Password);
                break;
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

                key = new SshPublicKey(pubPath,@params.Password);
                break;
        }

        var entity = dbContext.KeyDtos.Add(key.ToDto());
        await dbContext.SaveChangesAsync();
        key.Id = entity.Entity.Id;
        return key;
    }

    public static ISshKey? ProvidePasswordForKey(ISshKey key, string password) =>
        ProvidePasswordForKeyAsnyc(key, password).Result;

    public static async Task<ISshKey?> ProvidePasswordForKeyAsnyc(ISshKey key, string password)
    {
        await using var dbContext = new OpenSshGuiDbContext();
        var keyDto = await dbContext.KeyDtos.FirstAsync(e => e.AbsolutePath == key.AbsoluteFilePath);
        keyDto.Password = password;
        await dbContext.SaveChangesAsync();
        return await FromDtoIdAsync(keyDto.Id);
    }
    
    public static ISshKey? FromDtoId(int id) => FromDtoIdAsync(id).Result;
    public static async Task<ISshKey?> FromDtoIdAsync(int id)
    {
        await using var context = new OpenSshGuiDbContext();
        var dto = await context.KeyDtos.IgnoreAutoIncludes().FirstOrDefaultAsync(k => k.Id == id);
        return dto?.ToKey();
    }

    public static ISshKey? ConvertToOppositeFormat(ISshKey key, bool move = false) => ConvertToOppositeFormatAsync(key, move).Result;
    public static async Task<ISshKey?> ConvertToOppositeFormatAsync(ISshKey key, bool move = false)
    {
        await using var dbContext = new OpenSshGuiDbContext();
        var dtoOfKey = await dbContext.KeyDtos.FirstAsync(e => e.AbsolutePath == key.AbsoluteFilePath);
        dtoOfKey.Format = dtoOfKey.Format is SshKeyFormat.OpenSSH ? SshKeyFormat.PuTTYv3 : SshKeyFormat.OpenSSH;
        await dbContext.SaveChangesAsync();
        if (move)
        {
            var folderName = key.Format is not SshKeyFormat.OpenSSH ? "PPK" : "OPENSSH";
            var target  = Path.Combine(Path.GetDirectoryName(key.AbsoluteFilePath), folderName, Path.GetFileName(key.AbsoluteFilePath));
            File.Move(key.AbsoluteFilePath, target);
            if (key is ISshPublicKey publicKey)
            {
                target = Path.Combine(Path.GetDirectoryName(publicKey.PrivateKey.AbsoluteFilePath), folderName, Path.GetFileName(publicKey.PrivateKey.AbsoluteFilePath));
                File.Move(publicKey.PrivateKey.AbsoluteFilePath, target);
            }
        }
        var privateFilePath = dtoOfKey.Format.ChangeExtension(key.AbsoluteFilePath, false);
        switch (dtoOfKey.Format)
        {
            case SshKeyFormat.OpenSSH:
                await using (var privateWriter = new StreamWriter(privateFilePath, false))
                {
                    await privateWriter.WriteAsync(key.ExportOpenSshPrivateKey());
                }
                var publicFilePath = dtoOfKey.Format.ChangeExtension(privateFilePath);
                await using (var publicWriter = new StreamWriter(publicFilePath, false))
                {
                    await publicWriter.WriteAsync(key.ExportOpenSshPublicKey());
                }
                dtoOfKey.AbsolutePath = publicFilePath;
                break;
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
            default:
                await using (var privateWriter = new StreamWriter(privateFilePath, false))
                {
                    await privateWriter.WriteAsync(key.ExportPuttyPpkKey());
                }

                dtoOfKey.AbsolutePath = privateFilePath;
                break;
        }
        await dbContext.SaveChangesAsync();
        return await FromDtoIdAsync(dtoOfKey.Id);
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
        if (dbId == 0)
        {
            using var dbContext = new OpenSshGuiDbContext();
            var keyDto = dbContext.KeyDtos.FirstOrDefault(e => e.AbsolutePath == path);
            if (keyDto is not null)
            {
                dbId = keyDto.Id;
            }
        }
        
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