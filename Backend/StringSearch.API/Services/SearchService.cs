using StringSearch.API.DTOs;
using StringSearch.API.Strategies;

namespace StringSearch.API.Services;

/// <summary>
/// Implementação central do serviço de busca.
///
/// Padrão Strategy aplicado corretamente:
///   - Recebe IEnumerable&lt;ISearchStrategy&gt; via DI (não os concretos).
///   - Resolve o algoritmo pelo AlgorithmId em tempo de execução.
///   - Não precisa ser alterado ao adicionar um novo algoritmo.
/// </summary>
public class SearchService : ISearchService
{
    private readonly IReadOnlyDictionary<string, ISearchStrategy> _strategies;

    public SearchService(IEnumerable<ISearchStrategy> strategies)
    {
        // Indexa as strategies pelo AlgorithmId para lookup O(1)
        _strategies = strategies.ToDictionary(
            s => s.AlgorithmId,
            s => s,
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <inheritdoc />
    public SearchResult Search(SearchCommand command)
    {
        var strategy = ResolveStrategy(command.Algorithm);
        return strategy.Execute(command.Text, command.Pattern);
    }

    /// <inheritdoc />
    public MultiFileSearchResult SearchMultipleFiles(MultiFileSearchCommand command)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var fileResults = command.Files
            .Select(file =>
            {
                var result = Search(new SearchCommand(file.Content, command.Pattern, command.Algorithm));
                return new FileSearchResult(file.FileName, result);
            })
            .ToList();

        sw.Stop();

        return new MultiFileSearchResult(
            FileResults: fileResults,
            Algorithm: command.Algorithm,
            TotalExecutionTimeMs: sw.ElapsedMilliseconds
        );
    }

    /// <inheritdoc />
    public List<SearchResult> SearchAllAlgorithms(string text, string pattern)
    {
        return _strategies.Values
            .Select(strategy => strategy.Execute(text, pattern))
            .ToList();
    }

    /// <inheritdoc />
    public List<AlgorithmInfo> GetAlgorithmsInfo()
    {
        return _strategies.Values
            .Select(strategy => strategy.GetInfo())
            .ToList();
    }

    // ─── Privado ─────────────────────────────────────────────────────────────

    private ISearchStrategy ResolveStrategy(string algorithmId)
    {
        if (_strategies.TryGetValue(algorithmId, out var strategy))
            return strategy;

        var available = string.Join(", ", _strategies.Keys);
        throw new ArgumentException(
            $"Algoritmo '{algorithmId}' não reconhecido. Disponíveis: {available}.");
    }
}
