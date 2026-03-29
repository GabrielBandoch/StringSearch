using System.Diagnostics;
using StringSearch.API.DTOs;

namespace StringSearch.API.Strategies;

public class RabinKarpSearchStrategy : ISearchStrategy
{
    private const int Base = 256;
    private const int Mod = 101;

    public string AlgorithmId => "rabinkarp";
    public string AlgorithmDisplayName => "Rabin-Karp";
    public string TheoreticalComplexity => "O(n + m)";
    public string ComplexityDescription =>
        "Caso médio O(n + m) usando hash. Pior caso O(n × m) se muitas colisões de hash. " +
        "Excelente para busca de múltiplos padrões simultaneamente.";

    public AlgorithmInfo GetInfo() => new(
        Id: AlgorithmId,
        DisplayName: AlgorithmDisplayName,
        Description: "Usa função de hash (rolling hash) para comparar janelas do texto com o padrão.",
        BestCase: "O(n + m)",
        AverageCase: "O(n + m)",
        WorstCase: "O(n × m)",
        SpaceComplexity: "O(1)",
        UseCaseDescription: "Excelente para busca de múltiplos padrões. Bom desempenho médio."
    );

    public SearchResult Execute(string text, string pattern)
    {
        int n = text.Length;
        int m = pattern.Length;
        var occurrences = new List<int>();
        int comparisons = 0;

        if (m > n) return EmptyResult(n, m);

        var sw = Stopwatch.StartNew();

        int patternHash = 0;
        int windowHash = 0;
        int h = 1;

        for (int i = 0; i < m - 1; i++)
            h = (h * Base) % Mod;

        for (int i = 0; i < m; i++)
        {
            patternHash = (Base * patternHash + pattern[i]) % Mod;
            windowHash = (Base * windowHash + text[i]) % Mod;
        }

        for (int i = 0; i <= n - m; i++)
        {
            comparisons++;

            if (patternHash == windowHash)
            {
                int j;
                for (j = 0; j < m; j++)
                {
                    comparisons++;
                    if (text[i + j] != pattern[j]) break;
                }
                if (j == m) occurrences.Add(i);
            }

            if (i < n - m)
            {
                windowHash = (Base * (windowHash - text[i] * h) + text[i + m]) % Mod;
                if (windowHash < 0) windowHash += Mod;
            }
        }

        sw.Stop();

        return new SearchResult(
            Algorithm: AlgorithmId,
            AlgorithmDisplayName: AlgorithmDisplayName,
            Occurrences: occurrences,
            TotalOccurrences: occurrences.Count,
            ExecutionTimeMs: sw.ElapsedMilliseconds,
            ExecutionTimeNs: sw.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency),
            TotalComparisons: comparisons,
            TextLength: n,
            PatternLength: m,
            TheoreticalComplexity: TheoreticalComplexity,
            ComplexityDescription: ComplexityDescription,
            ComplexityAnalysis: $"Hash base={Base}, mod={Mod}. Comparações reais: {comparisons} (inclui verificações de hash e confirmações char-a-char)."
        );
    }

    public StepSearchResult ExecuteStepByStep(string text, string pattern)
    {
        int n = text.Length;
        int m = pattern.Length;
        var steps = new List<SearchStep>();
        var occurrences = new List<int>();
        int comparisons = 0;
        int stepNumber = 0;

        if (m > n) return EmptyStepResult();

        int patternHash = 0, windowHash = 0, h = 1;
        for (int i = 0; i < m - 1; i++) h = (h * Base) % Mod;
        for (int i = 0; i < m; i++)
        {
            patternHash = (Base * patternHash + pattern[i]) % Mod;
            windowHash = (Base * windowHash + text[i]) % Mod;
        }

        for (int i = 0; i <= n - m; i++)
        {
            comparisons++;
            bool hashMatch = patternHash == windowHash;

            steps.Add(new SearchStep(
                StepNumber: ++stepNumber,
                TextIndex: i,
                PatternIndex: 0,
                TextChar: text[i],
                PatternChar: pattern[0],
                IsMatch: hashMatch,
                Description: hashMatch
                    ? $"Hash match na posição {i} (hash={windowHash}) → verificando char-a-char..."
                    : $"Hash diferente na posição {i} (janela={windowHash} ≠ padrão={patternHash}) → avança",
                PatternOffset: i,
                ComparedIndices: Enumerable.Range(i, m).ToList()
            ));

            if (hashMatch)
            {
                int j;
                for (j = 0; j < m; j++)
                {
                    comparisons++;
                    bool isMatch = text[i + j] == pattern[j];
                    steps.Add(new SearchStep(
                        StepNumber: ++stepNumber,
                        TextIndex: i + j,
                        PatternIndex: j,
                        TextChar: text[i + j],
                        PatternChar: pattern[j],
                        IsMatch: isMatch,
                        Description: isMatch
                            ? $"  ✓ Confirmação: texto[{i + j}]='{text[i + j]}' == padrão[{j}]='{pattern[j]}'"
                            : $"  ✗ Colisão de hash! texto[{i + j}]='{text[i + j]}' != padrão[{j}]='{pattern[j]}'",
                        PatternOffset: i,
                        ComparedIndices: new List<int> { i + j }
                    ));
                    if (!isMatch) break;
                }
                if (j == m) occurrences.Add(i);
            }

            if (i < n - m)
            {
                windowHash = (Base * (windowHash - text[i] * h) + text[i + m]) % Mod;
                if (windowHash < 0) windowHash += Mod;
            }
        }

        var hashData = new Dictionary<string, object>
        {
            ["patternHash"] = patternHash,
            ["base"] = Base,
            ["mod"] = Mod
        };

        return new StepSearchResult(
            Algorithm: AlgorithmId,
            AlgorithmDisplayName: AlgorithmDisplayName,
            Steps: steps,
            Occurrences: occurrences,
            TotalOccurrences: occurrences.Count,
            TotalComparisons: comparisons,
            AuxiliaryStructure: new AuxiliaryStructure(
                "Função de Hash",
                $"Hash do padrão = {patternHash} (base={Base}, mod={Mod}). Rolling hash atualiza em O(1).",
                hashData
            )
        );
    }

    private SearchResult EmptyResult(int n, int m) =>
        new(AlgorithmId, AlgorithmDisplayName, [], 0, 0, 0, 0, n, m,
            TheoreticalComplexity, ComplexityDescription, "Padrão maior que texto.");

    private StepSearchResult EmptyStepResult() =>
        new(AlgorithmId, AlgorithmDisplayName, [], [], 0, 0, null);
}
