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

    public StepSearchResult ExecuteStepByStep(string text, string pattern)
    {
        int n = text.Length;
        int m = pattern.Length;
        var steps = new List<SearchStep>();
        var occurrences = new List<int>();
        int comparisons = 0;
        int stepNumber = 0;

        if (m > n) return EmptyStepResult();

        int[] badChar = BuildBadCharTable(pattern);
        int s = 0;

        while (s <= n - m)
        {
            int j = m - 1;

            while (j >= 0 && pattern[j] == text[s + j])
            {
                comparisons++;
                steps.Add(new SearchStep(
                    StepNumber: ++stepNumber,
                    TextIndex: s + j,
                    PatternIndex: j,
                    TextChar: text[s + j],
                    PatternChar: pattern[j],
                    IsMatch: true,
                    Description: $"✓ texto[{s + j}]='{text[s + j]}' == padrão[{j}]='{pattern[j]}' (comparando RTL)",
                    PatternOffset: s,
                    ComparedIndices: new List<int> { s + j }
                ));
                j--;
            }

            if (j < 0)
            {
                occurrences.Add(s);
                int nextShift = (s + m < n) ? m - badChar[text[s + m]] : 1;
                steps.Add(new SearchStep(
                    StepNumber: ++stepNumber,
                    TextIndex: s,
                    PatternIndex: 0,
                    TextChar: text[s],
                    PatternChar: pattern[0],
                    IsMatch: true,
                    Description: $"✓ Padrão encontrado na posição {s}! Avança {nextShift} posição(ões).",
                    PatternOffset: s,
                    ComparedIndices: Enumerable.Range(s, m).ToList()
                ));
                s += nextShift;
            }
            else
            {
                comparisons++;
                int bcShift = j - badChar[text[s + j]];
                int shift = Math.Max(1, bcShift);

                steps.Add(new SearchStep(
                    StepNumber: ++stepNumber,
                    TextIndex: s + j,
                    PatternIndex: j,
                    TextChar: text[s + j],
                    PatternChar: pattern[j],
                    IsMatch: false,
                    Description: $"✗ texto[{s + j}]='{text[s + j]}' != padrão[{j}]='{pattern[j]}'. " +
                                 $"Bad Char '{text[s + j]}' → salta {shift} posição(ões).",
                    PatternOffset: s,
                    ComparedIndices: new List<int> { s + j }
                ));
                s += shift;
            }
        }

        var bcDisplay = new Dictionary<string, object>();
        for (int c = 32; c < 127; c++)
        {
            if (badChar[c] >= 0)
                bcDisplay[$"'{(char)c}'"] = badChar[c];
        }

        return new StepSearchResult(
            Algorithm: AlgorithmId,
            AlgorithmDisplayName: AlgorithmDisplayName,
            Steps: steps,
            Occurrences: occurrences,
            TotalOccurrences: occurrences.Count,
            TotalComparisons: comparisons,
            AuxiliaryStructure: new AuxiliaryStructure(
                "Tabela Bad Character",
                "Última ocorrência de cada caractere no padrão. Permite saltar quando há mismatch.",
                bcDisplay
            )
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

    private StepSearchResult EmptyStepResult() =>
        new(AlgorithmId, AlgorithmDisplayName, [], [], 0, 0, null);
}
