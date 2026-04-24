using OpenSSH_GUI.Logging.Enricher;
using Serilog;
using Serilog.Configuration;

namespace OpenSSH_GUI.Extensions;

/// <summary>
///     Provides extension methods for enriching Serilog loggers with caller information.
/// </summary>
public static class CallerEnricherExtensions
{
    /// <summary>
    ///     Enriches log events with the caller's class name, method name, and line number.
    /// </summary>
    /// <param name="enrichmentConfiguration">The Serilog enrichment configuration.</param>
    /// <returns>The updated <see cref="LoggerConfiguration" />.</returns>
    public static LoggerConfiguration WithCaller(this LoggerEnrichmentConfiguration enrichmentConfiguration) => enrichmentConfiguration.With<CallerEnricher>();
}