using System.Text;
using Renci.SshNet;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Interfaces;

public interface IKeyFileWriterService
{
    /// <summary>
    /// Writes the specified content to a file at the given file path, with optional encoding and overwrite behavior.
    /// </summary>
    /// <param name="filePath">The path of the file to write the content to.</param>
    /// <param name="content">The content to write to the file.</param>
    /// <param name="overwrite">A flag indicating whether to overwrite the file if it already exists. Defaults to false.</param>
    /// <param name="encoding">The encoding to use when writing the file. Defaults to UTF-8 if not specified.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <exception cref="IOException">Thrown if the file already exists and overwrite is set to false.</exception>
    /// <exception cref="Exception">Thrown when an error occurs during the file writing operation.</exception>
    ValueTask WriteToFile(string filePath, string content,
        bool overwrite = false, Encoding? encoding = null);
    /// <summary>
    /// Writes a private key and its corresponding public key (if applicable) to files in a specific SSH key format.
    /// </summary>
    /// <param name="format">
    /// The SSH key format to use for writing the files (e.g., OpenSSH, PuTTYv2, PuTTYv3).
    /// </param>
    /// <param name="encryption">
    /// The encryption strategy to apply to the private key.
    /// </param>
    /// <param name="privateKeySource">
    /// The source of the private key to be written to the file.
    /// </param>
    /// <param name="filePath">
    /// The base file path where the SSH key files will be written. Extensions will be added based on the key format.
    /// </param>
    /// <param name="overwrite">
    /// A boolean value indicating whether to overwrite existing files. Default value is false.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable collection of file paths
    /// to the written SSH key files.
    /// </returns>
    ValueTask<IEnumerable<string>> WriteToFileInSpecificFormat(
        SshKeyFormat format,
        ISshKeyEncryption encryption,
        IPrivateKeySource privateKeySource, string filePath, bool overwrite = false);
    
    /// <summary>
    /// Writes a private key and its corresponding public key (if applicable) to files in a specific SSH key format.
    /// This overload extracts the format and encryption settings from the <see cref="SshKeyGenerateInfo"/> object.
    /// </summary>
    /// <param name="generateInfo">
    /// The SSH key generation information containing the key format and encryption settings.
    /// </param>
    /// <param name="createdKey">
    /// The generated private key to be written to the file.
    /// </param>
    /// <param name="filePath">
    /// The base file path where the SSH key files will be written. Extensions will be added based on the key format.
    /// </param>
    /// <param name="overwrite">
    /// A boolean value indicating whether to overwrite existing files. Default value is false.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable collection of file paths
    /// to the written SSH key files.
    /// </returns>
    ValueTask<IEnumerable<string>> WriteToFileInSpecificFormat(
        SshKeyGenerateInfo generateInfo,
        GeneratedPrivateKey createdKey, string filePath, bool overwrite = false);
}