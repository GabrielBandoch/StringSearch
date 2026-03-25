using StringSearch.API.DTOs;
using StringSearch.API.Strategies;

namespace StringSearch.API.Services;

/// <summary>
/// Serviço principal que orquestra a seleção e execução dos algoritmos de busca.
/// Implementa o padrão Strategy através do ISearchStrategy.
/// </summary>
public class SearchService
{
    private readonly Dictionary<string, ISearchStrategy> _strategies;

    public SearchService(IEnumerable<ISearchStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.AlgorithmId, s => s);
    }

    /// <summary>
    /// Executa a busca normal com o algoritmo especificado.
    /// </summary>
    public SearchResult Search(SearchCommand command)
    {
        var strategy = GetStrategy(command.Algorithm);
        return strategy.Execute(command.Text, command.Pattern);
    }

    /// <summary>
    /// Executa a busca passo a passo com o algoritmo especificado.
    /// </summary>
    public StepSearchResult SearchStepByStep(StepSearchCommand command)
    {
        var strategy = GetStrategy(command.Algorithm);
        return strategy.ExecuteStepByStep(command.Text, command.Pattern);
    }

    /// <summary>
    /// Executa a busca em múltiplos arquivos.
    /// </summary>
    public MultiFileSearchResult SearchMultipleFiles(MultiFileSearchCommand command)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var fileResults = new List<FileSearchResult>();

        foreach (var file in command.Files)
        {
            var searchCommand = new SearchCommand(file.Content, command.Pattern, command.Algorithm);
            var result = Search(searchCommand);
            fileResults.Add(new FileSearchResult(file.FileName, result));
        }

        sw.Stop();

        return new MultiFileSearchResult(
            FileResults: fileResults,
            Algorithm: command.Algorithm,
            TotalExecutionTimeMs: sw.ElapsedMilliseconds
        );
    }

    /// <summary>
    /// Executa todos os algoritmos para comparação.
    /// </summary>
    public List<SearchResult> SearchAllAlgorithms(string text, string pattern)
    {
        return _strategies.Values
            .Select(strategy => strategy.Execute(text, pattern))
            .ToList();
    }

    /// <summary>
    /// Retorna informações sobre todos os algoritmos disponíveis.
    /// </summary>
    public List<AlgorithmInfo> GetAlgorithmsInfo()
    {
        return
        [
            new AlgorithmInfo(
                Id: "naive",
                DisplayName: "Busca Naive (Força Bruta)",
                Description: "Compara o padrão com cada posição possível do texto sequencialmente.",
                BestCase: "O(n)",
                AverageCase: "O(n × m)",
                WorstCase: "O(n × m)",
                SpaceComplexity: "O(1)",
                UseCaseDescription: "Simples de implementar. Adequado para textos e padrões pequenos."
            ),
            new AlgorithmInfo(
                Id: "rabinkarp",
                DisplayName: "Rabin-Karp",
                Description: "Usa função de hash (rolling hash) para comparar janelas do texto com o padrão.",
                BestCase: "O(n + m)",
                AverageCase: "O(n + m)",
                WorstCase: "O(n × m)",
                SpaceComplexity: "O(1)",
                UseCaseDescription: "Excelente para busca de múltiplos padrões. Bom desempenho médio."
            ),
            new AlgorithmInfo(
                Id: "kmp",
                DisplayName: "Knuth-Morris-Pratt (KMP)",
                Description: "Pré-processa o padrão para construir a tabela LPS e evita comparações redundantes.",
                BestCase: "O(n)",
                AverageCase: "O(n + m)",
                WorstCase: "O(n + m)",
                SpaceComplexity: "O(m)",
                UseCaseDescription: "Garante O(n + m) sempre. Ótimo para padrões com repetições."
            ),
            new AlgorithmInfo(
                Id: "boyermoore",
                DisplayName: "Boyer-Moore",
                Description: "Compara da direita para esquerda usando heurísticas Bad Character e Good Suffix.",
                BestCase: "O(n/m)",
                AverageCase: "O(n/m)",
                WorstCase: "O(n × m)",
                SpaceComplexity: "O(m + σ)",
                UseCaseDescription: "O mais rápido na prática para textos em linguagem natural com alfabeto grande."
            )
        ];
    }

    private ISearchStrategy GetStrategy(string algorithmId)
    {
        if (!_strategies.TryGetValue(algorithmId.ToLower(), out var strategy))
            throw new ArgumentException($"Algoritmo '{algorithmId}' não encontrado. Use: {string.Join(", ", _strategies.Keys)}");
        return strategy;
    }
}
