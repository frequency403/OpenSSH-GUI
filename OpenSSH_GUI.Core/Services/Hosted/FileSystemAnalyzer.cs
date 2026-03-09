using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;

namespace OpenSSH_GUI.Core.Services.Hosted;

public class FileSystemAnalyzer(ILogger<FileSystemAnalyzer> logger) : IHostedService
{
    private CancellationTokenSource _cts = new();
    private Task _doWorkTask = Task.CompletedTask;

    private async Task DoWork(CancellationToken cancellationToken)
    {
        try
        {
            var rootSshPath = SshConfigFilesExtension.GetRootSshPath();
            var baseSshPath = SshConfigFilesExtension.GetBaseSshPath();
            cancellationToken.ThrowIfCancellationRequested();
            var unixPlatform = Environment.OSVersion.Platform is not PlatformID.Win32NT;
#pragma warning disable CA1416
            if (!Directory.Exists(rootSshPath))
                if (unixPlatform)
                    Directory.CreateDirectory(rootSshPath,
                        UnixFileMode.UserRead   |
                        UnixFileMode.UserWrite  |
                        UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead  |
                        UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead  |
                        UnixFileMode.OtherExecute); // 755
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(baseSshPath))
                if (unixPlatform)
                    Directory.CreateDirectory(baseSshPath,
                        UnixFileMode.UserRead   |
                        UnixFileMode.UserWrite  |
                        UnixFileMode.UserExecute); // 700
                else
                    Directory.CreateDirectory(baseSshPath);
#pragma warning enable CA1416
            
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error creating config path");
        }
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var pathOfFile in Enum.GetValues<SshConfigFiles>().Select(e => e.GetPathOfFile()).Where(e => !string.IsNullOrWhiteSpace(e) && !File.Exists(e)))
        {
            logger.LogInformation("Creating file {pathOfFile}", pathOfFile);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var fileStream = new FileStream(pathOfFile, FileMode.OpenOrCreate, FileAccess.Write);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error creating file {pathOfFile}", pathOfFile);
            }
        }
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _doWorkTask = DoWork(_cts.Token);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cts.CancelAsync();
        await _doWorkTask;
    }
}