using StringSearch.API.DTOs;

namespace StringSearch.API.Strategies;

public interface IAlgorithmInfo
{
    string AlgorithmId { get; }
    string AlgorithmDisplayName { get; }
    string TheoreticalComplexity { get; }
    string ComplexityDescription { get; }

    AlgorithmInfo GetInfo();
}
