using System.Diagnostics;
using StringSearch.API.DTOs;
using StringSearch.API.Observability;
using StringSearch.API.Services;

namespace StringSearch.API.Facade;

/// <summary>
/// Extensões para Activity (diagnóstico de traces)
/// </summary>
internal static class ActivityExtensions
{
    /// <summary>
    /// Registra uma exceção no span
    /// </summary>
    internal static void RecordException(this Activity? activity, Exception exception)
    {
        if (activity == null) return;
        
        activity.AddEvent(new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.StackTrace }
            }
        ));
    }
}

/// <summary>
/// Facade: ponto único de entrada para o Controller.
///
/// Parte 2 — Instrumentação:
///   - Cria o span raiz de cada operação (o SearchService cria spans filhos).
///   - Propaga contexto de trace para toda a cadeia de chamadas.
///   - Registra erros no span com SetStatus(Error) + RecordException.
/// </summary>
public class SearchFacade : ISearchFacade
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchFacade> _logger;

    public SearchFacade(ISearchService searchService, ILogger<SearchFacade> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    public SearchResult Execute(SearchCommand command)
    {
        using var activity = SearchTelemetry.ActivitySource
            .StartActivity("SearchFacade.Execute", ActivityKind.Server);

        activity?.SetTag("facade.operation",  "execute");
        activity?.SetTag("search.algorithm",  command.Algorithm);
        activity?.SetTag("search.pattern",    command.Pattern);

        try
        {
            _logger.LogInformation(
                "[Facade] Execute | algorithm={Alg} | pattern={Pat} | textLen={Len}",
                command.Algorithm, command.Pattern, command.Text.Length);

            var result = _searchService.Search(command);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "[Facade] Erro em Execute | algorithm={Alg}", command.Algorithm);
            throw;
        }
    }

    public MultiFileSearchResult ExecuteMultiFile(MultiFileSearchCommand command)
    {
        using var activity = SearchTelemetry.ActivitySource
            .StartActivity("SearchFacade.ExecuteMultiFile", ActivityKind.Server);

        activity?.SetTag("facade.operation", "multi-file");
        activity?.SetTag("search.algorithm", command.Algorithm);
        activity?.SetTag("search.file_count", command.Files.Count);

        try
        {
            _logger.LogInformation(
                "[Facade] MultiFile | algorithm={Alg} | files={Count} | pattern={Pat}",
                command.Algorithm, command.Files.Count, command.Pattern);

            var result = _searchService.SearchMultipleFiles(command);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "[Facade] Erro em ExecuteMultiFile | algorithm={Alg}", command.Algorithm);
            throw;
        }
    }

    public List<SearchResult> ExecuteCompareAll(SearchCommand command)
    {
        using var activity = SearchTelemetry.ActivitySource
            .StartActivity("SearchFacade.ExecuteCompareAll", ActivityKind.Server);

        activity?.SetTag("facade.operation", "compare-all");
        activity?.SetTag("search.pattern",   command.Pattern);

        try
        {
            _logger.LogInformation(
                "[Facade] CompareAll | pattern={Pat} | textLen={Len}",
                command.Pattern, command.Text.Length);

            var results = _searchService.SearchAllAlgorithms(command.Text, command.Pattern);
            activity?.SetTag("search.algo_count", results.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return results;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "[Facade] Erro em ExecuteCompareAll");
            throw;
        }
    }

    public List<AlgorithmInfo> GetAlgorithmsInfo() =>
        _searchService.GetAlgorithmsInfo();
}
