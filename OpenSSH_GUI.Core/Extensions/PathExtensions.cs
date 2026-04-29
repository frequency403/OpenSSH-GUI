namespace OpenSSH_GUI.Core.Extensions;

public static class PathExtensions
{
    public const string JsonExtension = "json";
    public const string LogExtension = "log";

    extension(Path)
    {
        public static string WithJsonExtension(string baseName)
            => Path.ChangeExtension(baseName, JsonExtension);

        public static string WithLogExtension(string baseName)
            => Path.ChangeExtension(baseName, LogExtension);

        public static bool IsJson(string path)
            => path.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase);

        public static bool IsLog(string path)
            => path.EndsWith(LogExtension, StringComparison.OrdinalIgnoreCase);

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