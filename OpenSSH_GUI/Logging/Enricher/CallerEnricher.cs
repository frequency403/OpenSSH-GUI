using System.Diagnostics;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace OpenSSH_GUI.Logging.Enricher;

/// <summary>
///     Enriches log events with caller information:
///     the class name, method name, and line number
///     derived from the current stack frame.
/// </summary>
public sealed class CallerEnricher : ILogEventEnricher
{
    private const string LineNumberProperty = "LineNumber";
    private const string ClassNameProperty = "ClassName";
    private const string ClassNameDefaultValue = "<unknown>";

    // Serilog-internal namespaces to skip when walking the stack
    private static readonly string[] SerilogNamespaces =
    [
        "Serilog.",
        "System.",
        "Microsoft."
    ];

    /// <summary>
    ///     Enriches the given log event with caller class, method, file name, and line number.
    /// </summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var frame = FindCallerFrame();
        var lineNumber = frame?.GetFileLineNumber() ?? 0;
        var frameMethod = frame?.GetMethod();
        var className = GetDeclaringClassName(frameMethod);

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LineNumberProperty, lineNumber));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(ClassNameProperty, className));
    }

    private static string GetDeclaringClassName(MethodBase? methodBase)
    {
        if(methodBase == null) return ClassNameDefaultValue;
        var declaringType = methodBase.DeclaringType;
        var declaringTypeName = declaringType?.Name;
        if(declaringTypeName is null) return ClassNameDefaultValue;
        while (declaringTypeName.Contains('<'))
        {
            if (declaringType?.DeclaringType is not null)
            {
                declaringTypeName = declaringType.DeclaringType.Name;
            }
            else
            {
                break;
            }
        }
        return declaringTypeName;
    }
    
    /// <summary>
    ///     Walks the stack to find the first frame outside of Serilog, system namespaces,
    ///     and the enricher itself.
    /// </summary>
    /// <returns>The first relevant <see cref="StackFrame" />, or <c>null</c> if not found.</returns>
    private static StackFrame? FindCallerFrame()
    {
        var stack = new StackTrace(true);

        foreach (var frame in stack.GetFrames())
        {
            var declaringType = frame.GetMethod()?.DeclaringType;

            if (declaringType == null) continue;
            if (declaringType == typeof(CallerEnricher)) continue;
            if (typeof(ILogEventEnricher).IsAssignableFrom(declaringType)) continue;

            var ns = declaringType.Namespace ?? string.Empty;
            if (Array.Exists(SerilogNamespaces, ns.StartsWith)) continue;

            return frame;
        }

        return null;
    }
}