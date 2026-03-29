namespace StringSearch.API.DTOs;

// ─── Request DTOs ───────────────────────────────────────────────────────────

public record SearchCommand(
    string Text,
    string Pattern,
    string Algorithm
);

public record StepSearchCommand(
    string Text,
    string Pattern,
    string Algorithm
);

public record MultiFileSearchCommand(
    List<FileContent> Files,
    string Pattern,
    string Algorithm
);

public record FileContent(string FileName, string Content);

// ─── Response DTOs ──────────────────────────────────────────────────────────

public record SearchResult(
    string Algorithm,
    string AlgorithmDisplayName,
    List<int> Occurrences,
    int TotalOccurrences,
    long ExecutionTimeMs,
    long ExecutionTimeNs,
    int TotalComparisons,
    int TextLength,
    int PatternLength,
    string TheoreticalComplexity,
    string ComplexityDescription,
    string ComplexityAnalysis
);

public record StepSearchResult(
    string Algorithm,
    string AlgorithmDisplayName,
    List<SearchStep> Steps,
    List<int> Occurrences,
    int TotalOccurrences,
    int TotalComparisons,
    AuxiliaryStructure? AuxiliaryStructure
);

public record SearchStep(
    int StepNumber,
    int TextIndex,
    int PatternIndex,
    char TextChar,
    char PatternChar,
    bool IsMatch,
    string Description,
    int PatternOffset,
    List<int> ComparedIndices
);

public record AuxiliaryStructure(
    string Name,
    string Description,
    Dictionary<string, object> Data
);

public record MultiFileSearchResult(
    List<FileSearchResult> FileResults,
    string Algorithm,
    long TotalExecutionTimeMs
);

public record FileSearchResult(
    string FileName,
    SearchResult Result
);

public record AlgorithmInfo(
    string Id,
    string DisplayName,
    string Description,
    string BestCase,
    string AverageCase,
    string WorstCase,
    string SpaceComplexity,
    string UseCaseDescription
);
