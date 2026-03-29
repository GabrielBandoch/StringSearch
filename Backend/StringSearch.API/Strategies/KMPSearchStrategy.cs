using System.Diagnostics;
using StringSearch.API.DTOs;

namespace StringSearch.API.Strategies;

public class KMPSearchStrategy : ISearchStrategy
{
    public string AlgorithmId => "kmp";
    public string AlgorithmDisplayName => "Knuth-Morris-Pratt (KMP)";
    public string TheoreticalComplexity => "O(n + m)";
    public string ComplexityDescription =>
        "Pré-processa o padrão em O(m) para construir a tabela LPS. " +
        "Busca em O(n) nunca retrocedendo no texto. Total: O(n + m).";

    public AlgorithmInfo GetInfo() => new(
        Id: AlgorithmId,
        DisplayName: AlgorithmDisplayName,
        Description: "Pré-processa o padrão para construir a tabela LPS e evita comparações redundantes.",
        BestCase: "O(n)",
        AverageCase: "O(n + m)",
        WorstCase: "O(n + m)",
        SpaceComplexity: "O(m)",
        UseCaseDescription: "Garante O(n + m) sempre. Ótimo para padrões com repetições."
    );

    public SearchResult Execute(string text, string pattern)
    {
        int n = text.Length;
        int m = pattern.Length;
        var occurrences = new List<int>();
        int comparisons = 0;

        if (m == 0 || m > n) return EmptyResult(n, m);

        var sw = Stopwatch.StartNew();

        int[] lps = BuildLPS(pattern, ref comparisons);
        int i = 0, j = 0;

        while (i < n)
        {
            comparisons++;
            if (text[i] == pattern[j])
            {
                i++; j++;
            }

            if (j == m)
            {
                occurrences.Add(i - j);
                j = lps[j - 1];
            }
            else if (i < n && text[i] != pattern[j])
            {
                if (j != 0)
                    j = lps[j - 1];
                else
                    i++;
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
            ComplexityAnalysis: $"Tabela LPS pré-calculada permite retorno inteligente. Nunca retrocede no texto (ponteiro i monotonicamente crescente)."
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

        if (m == 0 || m > n) return EmptyStepResult();

        int dummyComparisons = 0;
        int[] lps = BuildLPS(pattern, ref dummyComparisons);

        int i = 0, j = 0;

        while (i < n)
        {
            comparisons++;
            bool isMatch = text[i] == pattern[j];

            steps.Add(new SearchStep(
                StepNumber: ++stepNumber,
                TextIndex: i,
                PatternIndex: j,
                TextChar: text[i],
                PatternChar: pattern[j],
                IsMatch: isMatch,
                Description: isMatch
                    ? $"✓ texto[{i}]='{text[i]}' == padrão[{j}]='{pattern[j]}'"
                    : j != 0
                        ? $"✗ Mismatch! Usa LPS[{j - 1}]={lps[j - 1]} → salta padrão para j={lps[j - 1]}"
                        : $"✗ texto[{i}]='{text[i]}' != padrão[{j}]='{pattern[j]}' → avança texto",
                PatternOffset: i - j,
                ComparedIndices: new List<int> { i }
            ));

            if (isMatch)
            {
                i++; j++;
            }

            if (j == m)
            {
                occurrences.Add(i - j);
                steps[^1] = steps[^1] with
                {
                    Description = $"✓ Padrão encontrado na posição {i - j}! Usa LPS[{j - 1}]={lps[j - 1]} para continuar."
                };
                j = lps[j - 1];
            }
            else if (i < n && !isMatch)
            {
                if (j != 0)
                    j = lps[j - 1];
                else
                    i++;
            }
        }

        var lpsDict = new Dictionary<string, object>();
        for (int k = 0; k < m; k++)
            lpsDict[$"lps[{k}] ('{pattern[k]}')"] = lps[k];

        return new StepSearchResult(
            Algorithm: AlgorithmId,
            AlgorithmDisplayName: AlgorithmDisplayName,
            Steps: steps,
            Occurrences: occurrences,
            TotalOccurrences: occurrences.Count,
            TotalComparisons: comparisons,
            AuxiliaryStructure: new AuxiliaryStructure(
                "Tabela LPS (Longest Proper Prefix-Suffix)",
                "Indica o comprimento do maior prefixo do padrão que também é sufixo. Permite saltar comparações redundantes.",
                lpsDict
            )
        );
    }

    private static int[] BuildLPS(string pattern, ref int comparisons)
    {
        int m = pattern.Length;
        int[] lps = new int[m];
        int len = 0;
        int i = 1;
        lps[0] = 0;

        while (i < m)
        {
            comparisons++;
            if (pattern[i] == pattern[len])
            {
                lps[i++] = ++len;
            }
            else if (len != 0)
            {
                len = lps[len - 1];
            }
            else
            {
                lps[i++] = 0;
            }
        }
        return lps;
    }

    private SearchResult EmptyResult(int n, int m) =>
        new(AlgorithmId, AlgorithmDisplayName, [], 0, 0, 0, 0, n, m,
            TheoreticalComplexity, ComplexityDescription, "Padrão inválido ou maior que texto.");

    private StepSearchResult EmptyStepResult() =>
        new(AlgorithmId, AlgorithmDisplayName, [], [], 0, 0, null);
}
