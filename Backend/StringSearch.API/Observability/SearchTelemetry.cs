using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace StringSearch.API.Observability;

/// <summary>
/// Ponto central de instrumentação OpenTelemetry.
///
/// Centraliza todos os nomes de ActivitySource e Meter para garantir
/// consistência entre o código de instrumentação e o Program.cs.
///
/// Traces  → ActivitySource "StringSearch.API"
/// Métricas → Meter "StringSearch.API"
/// </summary>
public static class SearchTelemetry
{
    // ─── Identificação ────────────────────────────────────────────────────────
    public const string ServiceName    = "StringSearch.API";
    public const string ServiceVersion = "2.0.0";

    // ─── Traces ───────────────────────────────────────────────────────────────
    // ActivitySource é thread-safe e deve ser um singleton estático.
    public static readonly ActivitySource ActivitySource =
        new(ServiceName, ServiceVersion);

    // ─── Métricas ─────────────────────────────────────────────────────────────
    // Meter é thread-safe e deve ser um singleton estático.
    private static readonly Meter _meter = new(ServiceName, ServiceVersion);

    /// <summary>Contador: total de buscas executadas por algoritmo.</summary>
    public static readonly Counter<long> SearchCounter =
        _meter.CreateCounter<long>(
            name: "stringsearch.executions.total",
            unit: "searches",
            description: "Total de buscas executadas por algoritmo.");

    /// <summary>Histograma: tempo de execução em nanossegundos por algoritmo.</summary>
    public static readonly Histogram<long> SearchDurationNs =
        _meter.CreateHistogram<long>(
            name: "stringsearch.execution.duration_ns",
            unit: "ns",
            description: "Tempo de execução de cada busca em nanossegundos.");

    /// <summary>Histograma: número de comparações realizadas por busca.</summary>
    public static readonly Histogram<long> SearchComparisons =
        _meter.CreateHistogram<long>(
            name: "stringsearch.comparisons.total",
            unit: "comparisons",
            description: "Número de comparações realizadas por execução.");

    /// <summary>Contador: total de ocorrências encontradas.</summary>
    public static readonly Counter<long> OccurrencesFound =
        _meter.CreateCounter<long>(
            name: "stringsearch.occurrences.total",
            unit: "occurrences",
            description: "Total de ocorrências encontradas por algoritmo.");

    /// <summary>ObservableGauge: tamanho médio dos textos processados.</summary>
    private static long _lastTextLength = 0;
    public static void SetLastTextLength(long len) => _lastTextLength = len;
    public static readonly ObservableGauge<long> LastTextLength =
        _meter.CreateObservableGauge<long>(
            name: "stringsearch.text.last_length",
            observeValue: () => _lastTextLength,
            unit: "chars",
            description: "Tamanho do último texto processado.");
}
