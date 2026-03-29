using StringSearch.API.DTOs;

namespace StringSearch.API.Strategies;

public interface ISearchStrategy : IAlgorithmInfo
{
    SearchResult Execute(string text, string pattern);
    StepSearchResult ExecuteStepByStep(string text, string pattern);
}
