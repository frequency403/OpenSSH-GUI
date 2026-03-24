using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace OpenSSH_GUI.Logging.Enricher;

/// <summary>
/// Enriches log events with caller information:
/// the class name, method name, and line number
/// derived from the current stack frame.
/// </summary>
public sealed class CallerEnricher : ILogEventEnricher
{
    private const string LineNumberProperty = "LineNumber";
    private const string FileNameProperty   = "FileName";

    // Serilog-internal namespaces to skip when walking the stack
    private static readonly string[] serilogNamespaces =
    [
        "Serilog.",
        "System.",
        "Microsoft."
    ];

    /// <summary>
    /// Enriches the given log event with caller class, method, file name, and line number.
    /// </summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var frame = FindCallerFrame();
        var lineNumber = frame?.GetFileLineNumber()               ?? 0;
        var fileName   = Path.GetFileName(frame?.GetFileName())   ?? "<unknown>";

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LineNumberProperty, lineNumber));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(FileNameProperty,   fileName)); 
    }

    /// <summary>
    /// Walks the stack to find the first frame outside of Serilog, system namespaces,
    /// and the enricher itself.
    /// </summary>
    /// <returns>The first relevant <see cref="StackFrame"/>, or <c>null</c> if not found.</returns>
    private static StackFrame? FindCallerFrame()
    {
        var stack = new StackTrace(fNeedFileInfo: true);

        foreach (var frame in stack.GetFrames())
        {
            var declaringType = frame.GetMethod()?.DeclaringType;

            if (declaringType == null)                                          continue;
            if (declaringType == typeof(CallerEnricher))                       continue;
            if (typeof(ILogEventEnricher).IsAssignableFrom(declaringType))     continue;
        
            var ns = declaringType.Namespace ?? string.Empty;
            if (Array.Exists(serilogNamespaces, ns.StartsWith))                continue;

            return frame;
        }

        return null;
    }
}