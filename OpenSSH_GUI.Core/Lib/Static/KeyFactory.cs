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

/// <summary>
/// Provides static methods for working with SSH keys.
/// </summary>
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
        if (Path.HasExtension(@params.FileName))
            throw new ArgumentException(
                "The parameter \"FileName\" has an extension. Extensions are not allowed as they are determined by the KeyFormat value!",
                nameof(@params.FileName));
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

    /// <summary>
    /// Provides the password for an SSH key.
    /// </summary>
    /// <param name="key">The SSH key for which to provide the password.</param>
    /// <param name="password">The password to set for the SSH key.</param>
    /// <returns>The SSH key with the updated password.</returns>
    /// <exception cref="Exception">Thrown when unable to connect to the database, retrieve the SSH key, or update the password.</exception>
    public static ISshKey? ProvidePasswordForKey(ISshKey key, string password) =>
        ProvidePasswordForKeyAsnyc(key, password).Result;

    /// <summary>
    /// Provides a password for an SSH key asynchronously.
    /// </summary>
    /// <param name="key">The SSH key for which the password needs to be provided.</param>
    /// <param name="password">The password to be provided.</param>
    /// <returns>An ISshKey object representing the SSH key with the provided password.</returns>
    /// <exception cref="Exception">Thrown when unable to create Stream Writer or when unable to write the generated SSH key to the stream.</exception>
    public static async Task<ISshKey?> ProvidePasswordForKeyAsnyc(ISshKey key, string password)
    {
        await using var dbContext = new OpenSshGuiDbContext();
        var keyDto = await dbContext.KeyDtos.FirstAsync(e => e.AbsolutePath == key.AbsoluteFilePath);
        keyDto.Password = password;
        await dbContext.SaveChangesAsync();
        return await FromDtoIdAsync(keyDto.Id);
    }

    /// <summary>
    /// Retrieves an SSH key from the database based on its ID.
    /// </summary>
    /// <param name="id">The ID of the SSH key.</param>
    /// <returns>The retrieved SSH key, or null if no key is found with the given ID.</returns>
    /// <exception cref="Exception">Thrown when unable to create the database context, or when an error occurs during the retrieval process.</exception>
    public static ISshKey? FromDtoId(int id) => FromDtoIdAsync(id).Result;

    /// <summary>
    /// Retrieves an SSH key from the database based on its DTO id asynchronously.
    /// </summary>
    /// <param name="id">The id of the SSH key DTO.</param>
    /// <returns>The SSH key associated with the given id, or null if no matching key is found.</returns>
    public static async Task<ISshKey?> FromDtoIdAsync(int id)
    {
        await using var context = new OpenSshGuiDbContext();
        var dto = await context.KeyDtos.IgnoreAutoIncludes().FirstOrDefaultAsync(k => k.Id == id);
        return dto?.ToKey();
    }

    /// <summary>
    /// Converts the given SSH key to the opposite format asynchronously.
    /// </summary>
    /// <param name="key">The SSH key to convert.</param>
    /// <param name="move">Indicates whether to move the converted key to a new file. The default value is false.</param>
    /// <returns>The converted SSH key.</returns>
    /// <remarks>
    /// This method converts the format of the provided SSH key. If the key is in OpenSSH format, it will be converted to PuTTY format (.ppk),
    /// and if the key is in PuTTY format, it will be converted to OpenSSH format.
    /// The converted key can be optionally moved to a new file location.
    /// </remarks>
    /// <exception cref="Exception">Thrown when unable to create Stream Writer or when unable to write the generated SSH key to the stream.</exception>
    public static ISshKey? ConvertToOppositeFormat(ISshKey key, bool move = false) => ConvertToOppositeFormatAsync(key, move).Result;

    /// <summary>
    /// Converts the format of an SSH key asynchronously.
    /// </summary>
    /// <param name="key">The SSH key to be converted.</param>
    /// <param name="move">Indicates whether to move the key to a different folder after conversion. The default value is false.</param>
    /// <returns>The SSH key in the opposite format.</returns>
    /// <remarks>
    /// Depending on the format of the input key, the method converts it to the opposite format (OpenSSH to PuTTYv3 or vice versa).
    /// If the key format is PuTTYv2 or PuTTYv3, a PuTTY format key (.ppk) will be generated.
    /// If the key format is OpenSSH or other formats, OpenSSH format keys will be generated.
    /// If the move parameter is set to true, the key will be moved to a folder named "PPK" for PuTTY format, or "OPENSSH" for OpenSSH format,
    /// in the same directory as the original key.
    /// </remarks>
    /// <exception cref="Exception">Thrown when unable to create a Stream Writer or when unable to write the generated SSH key to the stream.</exception>
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
    /// <param name="params">The <see cref="SshKeyGenerateParams"/> object providing infos for generation.</param>
    /// <returns>The newly generated SSH key.</returns>
    /// <exception cref="Exception">Thrown when unable to create Stream Writer or when unable to write the generated SSH key to the stream.</exception>
    public static ISshKey GenerateNew(SshKeyGenerateParams @params) => GenerateNewAsync(@params).Result;

    /// <summary>
    /// Generates a new SSH key synchronously.
    /// </summary>
    /// <param name="type">The type of the SSH key.</param>
    /// <param name="format">The format of the SSH key.</param>
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
        int? keyLength = null) =>
        GenerateNew(new SshKeyGenerateParams(type, format, fileName, filePath, password, comment, keyLength));

    /// <summary>
    /// Generates a new SSH key based on the provided file path asynchronously.
    /// </summary>
    /// <param name="path">The file path of the SSH key.</param>
    /// <param name="password">The password for the SSH key if it is protected.</param>
    /// <param name="dbId">The ID of the key in the database (optional).</param>
    /// <returns>The newly generated SSH key.</returns>
    /// <remarks>
    /// This method reads the file extension of the given path to determine the key format.
    /// If the extension contains ".pub", a public key object is created.
    /// If the extension contains ".ppk", a PuTTY key object is created.
    /// If the extension is empty or not recognized, a private key object is created.
    /// The key will be associated with the provided database ID, if available.
    /// </remarks>
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
                var x when x.Contains(".pub") => new SshPublicKey(path, password) { Id = dbId },
                var x when x.Contains(".ppk") => new PpkKey(path, password) { Id = dbId },
                var x when string.IsNullOrWhiteSpace(x) => new SshPrivateKey(path, password) { Id = dbId },
                _ => null
            };
        }
        catch (Exception e)
        {
            return null;
        }
    }
}