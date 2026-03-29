using System.Diagnostics;
using StringSearch.API.DTOs;

namespace StringSearch.API.Strategies;

public class BoyerMooreSearchStrategy : ISearchStrategy
{
    private const int AlphabetSize = 256;
    public string AlgorithmId => "boyermoore";
    public string AlgorithmDisplayName => "Boyer-Moore";
    public string TheoreticalComplexity => "O(n/m)";
    public string ComplexityDescription =>
        "Melhor caso O(n/m) pois pode pular blocos de texto. Pré-processamento O(m + σ). " +
        "Compara o padrão da direita para a esquerda, usando Bad Character Heuristic.";

    public AlgorithmInfo GetInfo() => new(
        Id: AlgorithmId,
        DisplayName: AlgorithmDisplayName,
        Description: "Compara da direita para esquerda usando heurísticas Bad Character e Good Suffix.",
        BestCase: "O(n/m)",
        AverageCase: "O(n/m)",
        WorstCase: "O(n × m)",
        SpaceComplexity: "O(m + σ)",
        UseCaseDescription: "O mais rápido na prática para textos em linguagem natural com alfabeto grande."
    );

    public SearchResult Execute(string text, string pattern)
    {
        int n = text.Length;
        int m = pattern.Length;
        var occurrences = new List<int>();
        int comparisons = 0;

        if (m > n) return EmptyResult(n, m);

        var sw = Stopwatch.StartNew();

        int[] badChar = BuildBadCharTable(pattern);
        int s = 0;

        while (s <= n - m)
        {
            int j = m - 1;

            while (j >= 0 && pattern[j] == text[s + j])
            {
                comparisons++;
                j--;
            }

            if (j < 0)
            {
                occurrences.Add(s);
                s += (s + m < n) ? m - badChar[text[s + m]] : 1;
            }
            else
            {
                comparisons++;
                int shift = Math.Max(1, j - badChar[text[s + j]]);
                s += shift;
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
            ComplexityAnalysis: $"Compara da direita para a esquerda. Saltos maiores em textos com alfabeto grande. Ideal para buscas em linguagem natural."
        );
    }

    private static int[] BuildBadCharTable(string pattern)
    {
        int[] table = new int[AlphabetSize];
        Array.Fill(table, -1);
        for (int i = 0; i < pattern.Length; i++)
            table[pattern[i]] = i;
        return table;
    }

    private SearchResult EmptyResult(int n, int m) =>
        new(AlgorithmId, AlgorithmDisplayName, [], 0, 0, 0, 0, n, m,
            TheoreticalComplexity, ComplexityDescription, "Padrão maior que texto.");
}
