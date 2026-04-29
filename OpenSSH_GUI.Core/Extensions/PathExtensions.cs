using System.Diagnostics.CodeAnalysis;

namespace OpenSSH_GUI.Core.Extensions;

public static class PathExtensions
{
    /// <summary>
    /// Represents the file extension for JSON files.
    /// </summary>
    public const string JsonExtension = "json";

    /// <summary>
    /// Represents the file extension used for log files.
    /// </summary>
    public const string LogExtension = "log";
    
    /// <summary>
    ///     Represents the file extension for OpenSSH Public Key format.
    /// </summary>
    public const string OpenSshPublicKeyFileExtension = "pub";

    /// <summary>
    ///     Represents the file extension used for PuTTY private key files.
    /// </summary>
    public const string PuttyKeyFileExtension = "ppk";

    extension(Path)
    {
        /// <summary>
        /// Appends the ".json" extension to the specified base file name.
        /// </summary>
        /// <param name="baseName">The base file name to which the ".json" extension will be added.</param>
        /// <returns>A string representing the file name with the ".json" extension appended.</returns>
        public static string WithJsonExtension(string baseName)
            => Path.ChangeExtension(baseName, JsonExtension);

        /// <summary>
        /// Appends the ".log" file extension to the specified base file name.
        /// </summary>
        /// <param name="baseName">The base file name for which the ".log" extension should be added.</param>
        /// <returns>A string representing the base file name combined with the ".log" extension.</returns>
        public static string WithLogExtension(string baseName)
            => Path.ChangeExtension(baseName, LogExtension);

        /// <summary>
        /// Appends the OpenSSH Public Key file extension to the specified base file name.
        /// </summary>
        /// <param name="baseName">The base name of the file to which the extension will be appended.</param>
        /// <returns>The modified file name with the OpenSSH Public Key file extension.</returns>
        public static string WithOpenSshPublicKeyExtension(string baseName)
        => Path.ChangeExtension(baseName, OpenSshPublicKeyFileExtension);

        /// <summary>
        /// Changes the file extension of the given base name to the PuTTY private key extension (.ppk).
        /// </summary>
        /// <param name="baseName">The base name of the file whose extension will be changed.</param>
        /// <returns>The file name with the PuTTY private key file extension (.ppk).</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static string WithPuTTYKeyExtension(string baseName)
        => Path.ChangeExtension(baseName, PuttyKeyFileExtension);

        /// <summary>
        /// Determines whether the provided file path has a JSON file extension.
        /// The comparison is performed in a case-insensitive manner.
        /// </summary>
        /// <param name="path">The file path to evaluate.</param>
        /// <returns><c>true</c> if the file path ends with the ".json" extension; otherwise <c>false</c>.</returns>
        public static bool IsJson(string path)
            => path.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the given path has the ".log" file extension.
        /// Comparison is performed case-insensitively.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns><c>true</c> if the path ends with ".log"; otherwise <c>false</c>.</returns>
        public static bool IsLog(string path)
            => path.EndsWith(LogExtension, StringComparison.OrdinalIgnoreCase);
        /// <summary>
        /// Determines whether the given path represents a PuTTY private key file (.ppk).
        /// Comparison is performed case-insensitively.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns><c>true</c> if the path ends with the PuTTY key file extension; otherwise <c>false</c>.</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static bool IsPuTTYKey(string path)
            => path.EndsWith(PuttyKeyFileExtension, StringComparison.OrdinalIgnoreCase);
        /// <summary>
        /// Determines whether the given path represents an OpenSSH public key file.
        /// Comparison is performed case-insensitively.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns><c>true</c> if the path ends with the OpenSSH public key file extension; otherwise <c>false</c>.</returns>
        public static bool IsOpenSshPublicKey(string path)
        => path.EndsWith(OpenSshPublicKeyFileExtension, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the given path has the specified file extension.
        /// Comparison is performed case-insensitively.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <param name="extension">The extension to compare (e.g. ".json").</param>
        /// <returns><c>true</c> if the path ends with the given extension; otherwise <c>false</c>.</returns>
        public static bool HasExtension(string path, string extension)
            => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the file extension of the specified path in normalized form (always lower-case).
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The normalized file extension including the leading dot, or an empty string.</returns>
        public static string GetNormalizedExtension(string path)
            => Path.GetExtension(path).ToLowerInvariant();
    }

    extension(Directory)
    {
        /// <summary>
        /// Creates a directory at the specified path if it does not already exist.
        /// Automatically ensures that the directory structure exists.
        /// </summary>
        /// <param name="path">The full path of the directory to create.</param>
        public static void CreateIfNotExists(string? path)
        {
            if (!Directory.Exists(path) && !string.IsNullOrWhiteSpace(path))
                Directory.CreateDirectory(path);
        }
    }

    extension(File)
    {
        
        /// <summary>
        /// Creates a file at the specified path if it does not already exist.
        /// Automatically ensures that the directory structure exists.
        /// </summary>
        /// <param name="path">The full path of the file to create.</param>
        /// <param name="content">The content to write to the file, defaults to <see langword="null"/></param>
        public static void CreateIfNotExists(string? path, string? content = null)
        {
            ArgumentNullException.ThrowIfNull(path);
            if (File.Exists(path))
                return;
            Directory.CreateIfNotExists(Path.GetDirectoryName(path));

            using var createdFile = File.Create(path);
            if (content is null)
                return;
            using var writer = new StreamWriter(createdFile);
            writer.Write(content);
        }

        /// <summary>
        /// Deletes the file at the specified path if it exists.
        /// </summary>
        /// <param name="path">The full path of the file to delete.</param>
        public static void RemoveIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}