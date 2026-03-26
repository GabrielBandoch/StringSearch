using StringSearch.API.DTOs;

namespace StringSearch.API.Strategies;

/// <summary>
/// Interface Strategy para algoritmos de busca de padrões em strings.
/// Todos os algoritmos devem implementar esta interface.
/// </summary>
public interface ISearchStrategy
{
    string AlgorithmId { get; }
    string AlgorithmDisplayName { get; }
    string TheoreticalComplexity { get; }
    string ComplexityDescription { get; }

    /// <summary>
    /// Executa a busca normal e retorna todas as ocorrências com métricas.
    /// </summary>
    SearchResult Execute(string text, string pattern);

    /// <summary>
    /// Executa a busca passo a passo, retornando cada comparação realizada.
    /// </summary>
    StepSearchResult ExecuteStepByStep(string text, string pattern);
}
