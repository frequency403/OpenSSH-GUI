using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Lib.Misc;

namespace OpenSSH_GUI.Core.Interfaces;

/// <summary>
///     Provides file backup, restore, and deletion capabilities for SSH key operations,
///     as well as operation-scoped file logging to capture diagnostic output during
///     potentially destructive file system changes.
/// </summary>
public interface IKeyFileBackupService
{
    /// <summary>
    ///     Creates backup copies of the specified files in the backup directory.
    ///     Each backup is named after the original file with the backup extension appended.
    /// </summary>
    /// <param name="files">The files to back up.</param>
    /// <returns>
    ///     A sequence of <see cref="BackedUpFile" /> instances representing the
    ///     original file and its corresponding backup location.
    /// </returns>
    IEnumerable<BackedUpFile> BackupFiles(params FileInfo[] files);

    /// <summary>
    ///     Restores the specified backed-up files to their original locations,
    ///     overwriting any existing files at those paths.
    /// </summary>
    /// <param name="files">The backed-up files to restore.</param>
    void RestoreBackupFiles(params BackedUpFile[] files);

    /// <summary>
    ///     Deletes the backup copies of the specified files from the backup directory.
    ///     Should only be called after a successful operation.
    /// </summary>
    /// <param name="files">The backed-up files whose backup copies should be deleted.</param>
    void DeleteBackupFiles(params BackedUpFile[] files);

    /// <summary>
    ///     Begins an operation-scoped file log session.
    ///     Creates the backup directory if it does not exist and initializes a
    ///     Serilog file sink writing to <c>operation_log.log</c> within that directory.
    ///     Subsequent calls while a session is already active are no-ops.
    /// </summary>
    void BeginOperationLog();

    /// <summary>
    ///     Ends the current operation-scoped file log session and releases all associated resources.
    ///     If <paramref name="errorsOccurred" /> is <see langword="false" />, the entire backup directory
    ///     is deleted on the assumption that no recovery artifacts need to be retained.
    /// </summary>
    /// <param name="errorsOccurred">
    ///     <see langword="true" /> to retain the backup directory and log file for post-mortem inspection;
    ///     <see langword="false" /> to delete the backup directory after the session ends.
    /// </param>
    void EndOperationLog(bool errorsOccurred = false);

    /// <summary>
    ///     Writes a structured log message at the specified level exclusively to the
    ///     active operation-scoped file log. Does not write to the application logger —
    ///     the caller is responsible for that channel separately.
    ///     If no operation log session is currently active, the call is a no-op.
    /// </summary>
    /// <param name="level">The severity level of the log entry.</param>
    /// <param name="message">The structured message template.</param>
    /// <param name="args">Arguments to substitute into the message template.</param>
    void WriteToOperationLog(LogLevel level, [StructuredMessageTemplate] string? message, params object?[] args);

    /// <summary>
    ///     Writes a structured log message with an associated exception at the specified level
    ///     exclusively to the active operation-scoped file log. Does not write to the application
    ///     logger — the caller is responsible for that channel separately.
    ///     If no operation log session is currently active, the call is a no-op.
    /// </summary>
    /// <param name="level">The severity level of the log entry.</param>
    /// <param name="exception">The exception to associate with the log entry.</param>
    /// <param name="message">The structured message template.</param>
    /// <param name="args">Arguments to substitute into the message template.</param>
    void WriteToOperationLog(LogLevel level, Exception? exception, [StructuredMessageTemplate] string? message,
        params object?[] args);
}