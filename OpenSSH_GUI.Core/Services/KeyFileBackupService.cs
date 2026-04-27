using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.Misc;
using Serilog;
using Serilog.Extensions.Logging;

namespace OpenSSH_GUI.Core.Services;

/// <summary>
///     Default implementation of <see cref="IKeyFileBackupService" />.
///     Manages per-operation backup directories and a scoped Serilog file logger
///     that captures diagnostic output for potentially destructive SSH key file operations.
///     The application logger is intentionally not held by this service — callers are
///     responsible for their own application-level logging channel.
/// </summary>
public sealed class KeyFileBackupService : IKeyFileBackupService, IDisposable
{
    private const string BackupFileExtension = "bak";

    private static readonly string BackupDirectory =
        Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), AppDomain.CurrentDomain.FriendlyName);

    private ILogger<KeyFileBackupService>? _operationLogger;
    private SerilogLoggerFactory? _loggerFactory;

    /// <inheritdoc />
    public void Dispose()
    {
        _loggerFactory?.Dispose();
    }

    /// <inheritdoc />
    public IEnumerable<BackedUpFile> BackupFiles(params FileInfo[] files)
    {
        foreach (var file in files)
        {
            var destination = Path.Combine(BackupDirectory, string.Join(".", file.Name, BackupFileExtension));
            WriteToOperationLog(LogLevel.Debug, "Backing up file {file} to {destination}", file.FullName, destination);
            var backup = new BackedUpFile { InitialFile = file, BackupFile = new FileInfo(destination) };
            backup.Backup();
            WriteToOperationLog(LogLevel.Debug, "Successfully backed up file {file}", file.FullName);
            yield return backup;
        }
    }

    /// <inheritdoc />
    public void RestoreBackupFiles(params BackedUpFile[] files)
    {
        foreach (var file in files)
        {
            WriteToOperationLog(
                LogLevel.Debug, "Restoring backup file {file} to {destination}",
                file.BackupFile.FullName, file.InitialFile.FullName);
            file.Restore();
            WriteToOperationLog(LogLevel.Debug, "Successfully restored backup file {file}", file.BackupFile.FullName);
        }
    }

    /// <inheritdoc />
    public void DeleteBackupFiles(params BackedUpFile[] files)
    {
        foreach (var file in files)
        {
            WriteToOperationLog(LogLevel.Debug, "Deleting backup file {file}", file.BackupFile.FullName);
            file.Delete();
            WriteToOperationLog(LogLevel.Debug, "Successfully deleted backup file {file}", file.BackupFile.FullName);
        }
    }

    /// <inheritdoc />
    public void BeginOperationLog()
    {
        if (_operationLogger is not null) return;

        if (!Directory.Exists(BackupDirectory))
            Directory.CreateDirectory(BackupDirectory);

        var operationLogFile = Path.Combine(BackupDirectory, Path.ChangeExtension("operation_log", "log"));
        _loggerFactory = new SerilogLoggerFactory(
            new LoggerConfiguration()
                .WriteTo.File(operationLogFile)
                .MinimumLevel.Verbose()
                .CreateLogger(), true);
        _operationLogger = _loggerFactory.CreateLogger<KeyFileBackupService>();
    }

    /// <inheritdoc />
    public void EndOperationLog(bool errorsOccurred = false)
    {
        if (_operationLogger is null) return;
        _operationLogger = null;
        _loggerFactory?.Dispose();
        _loggerFactory = null;
        if (errorsOccurred) return;
        try
        {
            Directory.Delete(BackupDirectory, true);
        }
        catch (Exception e)
        {
            // Intentionally swallowed — backup directory cleanup is best-effort.
            // The caller's application logger should have already captured context.
            _ = e;
        }
    }

#pragma warning disable CA2254
    /// <inheritdoc />
    public void WriteToOperationLog(LogLevel level, [StructuredMessageTemplate] string? message,
        params object?[] args)
    {
        _operationLogger?.Log(level, message, args);
    }

    /// <inheritdoc />
    public void WriteToOperationLog(LogLevel level, Exception? exception,
        [StructuredMessageTemplate] string? message, params object?[] args)
    {
        _operationLogger?.Log(level, exception, message, args);
    }
#pragma warning restore CA2254
}