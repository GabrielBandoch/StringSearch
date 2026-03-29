using System.Diagnostics;
using StringSearch.API.DTOs;

namespace StringSearch.API.Strategies;

public class NaiveSearchStrategy : ISearchStrategy
{
    public string AlgorithmId => "naive";
    public string AlgorithmDisplayName => "Busca Naive (Força Bruta)";
    public string TheoreticalComplexity => "O(n × m)";
    public string ComplexityDescription =>
        "n = tamanho do texto, m = tamanho do padrão. " +
        "No pior caso (ex: texto 'AAAA' e padrão 'AAB'), cada posição requer m comparações.";

    public AlgorithmInfo GetInfo() => new(
        Id: AlgorithmId,
        DisplayName: AlgorithmDisplayName,
        Description: "Compara o padrão com cada posição possível do texto sequencialmente.",
        BestCase: "O(n)",
        AverageCase: "O(n × m)",
        WorstCase: "O(n × m)",
        SpaceComplexity: "O(1)",
        UseCaseDescription: "Simples de implementar. Adequado para textos e padrões pequenos."
    );

    public SearchResult Execute(string text, string pattern)
    {
        var occurrences = new List<int>();
        int comparisons = 0;
        int n = text.Length;
        int m = pattern.Length;

        var sw = Stopwatch.StartNew();

        for (int i = 0; i <= n - m; i++)
        {
            int j;
            for (j = 0; j < m; j++)
            {
                comparisons++;
                if (text[i + j] != pattern[j])
                    break;
            }
            if (j == m)
                occurrences.Add(i);
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
            ComplexityAnalysis: AnalyzeComplexity(n, m, comparisons)
        );
    }

    private static string AnalyzeComplexity(int n, int m, int actualComparisons)
    {
        long worstCase = (long)(n - m + 1) * m;
        double ratio = worstCase > 0 ? (double)actualComparisons / worstCase * 100 : 0;
        return $"Pior caso teórico: {worstCase} comparações. Real: {actualComparisons} ({ratio:F1}% do pior caso).";
    }
}
