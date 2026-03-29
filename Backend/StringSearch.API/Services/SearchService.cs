using StringSearch.API.DTOs;
using StringSearch.API.Strategies;

namespace StringSearch.API.Services;

public class SearchService
{
    private readonly NaiveSearchStrategy _naive;
    private readonly RabinKarpSearchStrategy _rabinKarp;
    private readonly KMPSearchStrategy _kmp;
    private readonly BoyerMooreSearchStrategy _boyerMoore;

    public SearchService(
        NaiveSearchStrategy naive,
        RabinKarpSearchStrategy rabinKarp,
        KMPSearchStrategy kmp,
        BoyerMooreSearchStrategy boyerMoore)
    {
        _naive = naive;
        _rabinKarp = rabinKarp;
        _kmp = kmp;
        _boyerMoore = boyerMoore;
    }

    public SearchResult Search(SearchCommand command)
    {
        var strategy = ResolveStrategy(command.Algorithm);
        return strategy.Execute(command.Text, command.Pattern);
    }

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

    public List<SearchResult> SearchAllAlgorithms(string text, string pattern)
    {
        return AllStrategies()
            .Select(strategy => strategy.Execute(text, pattern))
            .ToList();
    }

    public List<AlgorithmInfo> GetAlgorithmsInfo()
    {
        return AllStrategies()
            .Select(strategy => strategy.GetInfo())
            .ToList();
    }

    private ISearchStrategy ResolveStrategy(string algorithmId) =>
        algorithmId.ToLower() switch
        {
            "naive"      => _naive,
            "rabinkarp"  => _rabinKarp,
            "kmp"        => _kmp,
            "boyermoore" => _boyerMoore,
            _            => throw new ArgumentException(
                                $"Algoritmo '{algorithmId}' não reconhecido. " +
                                $"Use: naive, rabinkarp, kmp, boyermoore.")
        };

    private IEnumerable<ISearchStrategy> AllStrategies() =>
        [_naive, _rabinKarp, _kmp, _boyerMoore];
}
