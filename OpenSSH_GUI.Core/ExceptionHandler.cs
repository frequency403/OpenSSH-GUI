using System.Diagnostics;
using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;
using ReactiveUI.Avalonia;

namespace OpenSSH_GUI.Core;

public class ExceptionHandler(ILogger<ExceptionHandler> logger) : IObserver<Exception>
{
    public void OnCompleted()
    {
        if (Debugger.IsAttached)
            Debugger.Break();
    }

    public void OnError(Exception error)
    {
        if (Debugger.IsAttached)
            Debugger.Break();
        logger.LogError(error, "Unhandled Exception:");
        AvaloniaScheduler.Instance.Schedule(error, HandleException);
    }

    public void OnNext(Exception value)
    {
        if (Debugger.IsAttached)
            Debugger.Break();
        logger.LogError(value, "Unhandled Exception:");
        AvaloniaScheduler.Instance.Schedule(value, HandleException);
    }

    private static IDisposable HandleException(IScheduler arg1, Exception arg2) => throw arg2;
}