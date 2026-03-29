using StringSearch.API.DTOs;

namespace StringSearch.API.Facade;

public interface ISearchFacade
{
    SearchResult Execute(SearchCommand command);
    StepSearchResult ExecuteStepByStep(StepSearchCommand command);
    MultiFileSearchResult ExecuteMultiFile(MultiFileSearchCommand command);
    List<SearchResult> ExecuteCompareAll(SearchCommand command);
    List<AlgorithmInfo> GetAlgorithmsInfo();
}
