using System.Diagnostics;
using StringSearch.API.DTOs;
using StringSearch.API.Observability;
using StringSearch.API.Strategies;

namespace StringSearch.API.Services;

/// <summary>
/// Implementação central do serviço de busca.
///
/// Parte 2 — Instrumentação OpenTelemetry:
///   - Traces: cria um Activity (span) por execução de busca, com atributos ricos.
///   - Métricas: registra contador de execuções, histograma de duração e comparações.
///   - Logs estruturados: usa ILogger com propriedades nomeadas (não interpolação).
/// </summary>
public class SearchService : ISearchService
{
    private readonly IReadOnlyDictionary<string, ISearchStrategy> _strategies;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IEnumerable<ISearchStrategy> strategies,
        ILogger<SearchService> logger)
    {
        _strategies = strategies.ToDictionary(
            s => s.AlgorithmId,
            s => s,
            StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    // ─── Search ───────────────────────────────────────────────────────────────

    public SearchResult Search(SearchCommand command)
    {
        // Trace: abre um span para a execução completa
        using var activity = SearchTelemetry.ActivitySource
            .StartActivity("SearchService.Search", ActivityKind.Internal);

        activity?.SetTag("search.algorithm",    command.Algorithm);
        activity?.SetTag("search.pattern",      command.Pattern);
        activity?.SetTag("search.text_length",  command.Text.Length);
        activity?.SetTag("search.pattern_length", command.Pattern.Length);

        var strategy = ResolveStrategy(command.Algorithm);
        var result   = strategy.Execute(command.Text, command.Pattern);

        // Enriquece o span com o resultado
        activity?.SetTag("search.occurrences",  result.TotalOccurrences);
        activity?.SetTag("search.comparisons",  result.TotalComparisons);
        activity?.SetTag("search.duration_ns",  result.ExecutionTimeNs);
        activity?.SetStatus(ActivityStatusCode.Ok);

        // Métricas
        var tags = new TagList
        {
            { "algorithm", command.Algorithm },
            { "algorithm_display", result.AlgorithmDisplayName }
        };

        SearchTelemetry.SearchCounter.Add(1, tags);
        SearchTelemetry.SearchDurationNs.Record(result.ExecutionTimeNs, tags);
        SearchTelemetry.SearchComparisons.Record(result.TotalComparisons, tags);
        SearchTelemetry.OccurrencesFound.Add(result.TotalOccurrences, tags);
        SearchTelemetry.SetLastTextLength(command.Text.Length);

        // Log estruturado
        _logger.LogInformation(
            "[Search] algorithm={Algorithm} pattern={Pattern} textLen={TextLen} " +
            "occurrences={Occurrences} comparisons={Comparisons} durationNs={DurationNs}",
            command.Algorithm, command.Pattern, command.Text.Length,
            result.TotalOccurrences, result.TotalComparisons, result.ExecutionTimeNs);

        return result;
    }

    // ─── MultiFile ────────────────────────────────────────────────────────────

    public MultiFileSearchResult SearchMultipleFiles(MultiFileSearchCommand command)
    {
        using var activity = SearchTelemetry.ActivitySource
            .StartActivity("SearchService.SearchMultipleFiles", ActivityKind.Internal);

        activity?.SetTag("search.algorithm",   command.Algorithm);
        activity?.SetTag("search.file_count",  command.Files.Count);
        activity?.SetTag("search.pattern",     command.Pattern);

        var sw = Stopwatch.StartNew();

        var fileResults = command.Files.Select(file =>
        {
            // Span filho por arquivo
            using var fileActivity = SearchTelemetry.ActivitySource
                .StartActivity("SearchService.SearchFile", ActivityKind.Internal);
            fileActivity?.SetTag("search.file_name",   file.FileName);
            fileActivity?.SetTag("search.text_length", file.Content.Length);

            var result = Search(new SearchCommand(file.Content, command.Pattern, command.Algorithm));

            fileActivity?.SetTag("search.occurrences", result.TotalOccurrences);
            fileActivity?.SetStatus(ActivityStatusCode.Ok);

            return new FileSearchResult(file.FileName, result);
        }).ToList();

        sw.Stop();

        activity?.SetTag("search.total_duration_ms", sw.ElapsedMilliseconds);
        activity?.SetStatus(ActivityStatusCode.Ok);

        _logger.LogInformation(
            "[MultiFile] algorithm={Algorithm} files={FileCount} pattern={Pattern} totalMs={TotalMs}",
            command.Algorithm, command.Files.Count, command.Pattern, sw.ElapsedMilliseconds);

        return new MultiFileSearchResult(
            FileResults: fileResults,
            Algorithm: command.Algorithm,
            TotalExecutionTimeMs: sw.ElapsedMilliseconds);
    }

    // ─── CompareAll ───────────────────────────────────────────────────────────

    public List<SearchResult> SearchAllAlgorithms(string text, string pattern)
    {
        using var activity = SearchTelemetry.ActivitySource
            .StartActivity("SearchService.SearchAllAlgorithms", ActivityKind.Internal);

        activity?.SetTag("search.pattern",     pattern);
        activity?.SetTag("search.text_length", text.Length);
        activity?.SetTag("search.algo_count",  _strategies.Count);

        var results = _strategies.Values
            .Select(strategy => strategy.Execute(text, pattern))
            .ToList();

        activity?.SetTag("search.fastest_algorithm",
            results.OrderBy(r => r.ExecutionTimeNs).FirstOrDefault()?.Algorithm ?? "n/a");
        activity?.SetStatus(ActivityStatusCode.Ok);

        _logger.LogInformation(
            "[CompareAll] pattern={Pattern} textLen={TextLen} algorithmCount={AlgoCount}",
            pattern, text.Length, _strategies.Count);

        return results;
    }

    // ─── AlgorithmsInfo ───────────────────────────────────────────────────────

    public List<AlgorithmInfo> GetAlgorithmsInfo() =>
        _strategies.Values.Select(s => s.GetInfo()).ToList();

    // ─── Privado ──────────────────────────────────────────────────────────────

    private ISearchStrategy ResolveStrategy(string algorithmId)
    {
        if (_strategies.TryGetValue(algorithmId, out var strategy))
            return strategy;

        var available = string.Join(", ", _strategies.Keys);

        _logger.LogWarning(
            "[Search] Algoritmo desconhecido: {AlgorithmId}. Disponíveis: {Available}",
            algorithmId, available);

        throw new ArgumentException(
            $"Algoritmo '{algorithmId}' não reconhecido. Disponíveis: {available}.");
    }
}
