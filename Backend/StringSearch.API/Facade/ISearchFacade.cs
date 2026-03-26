using StringSearch.API.DTOs;

namespace StringSearch.API.Facade;

/// <summary>
/// Facade para o subsistema de busca.
/// O Controller só conhece esta interface — toda a complexidade fica encapsulada aqui.
/// </summary>
public interface ISearchFacade
{
    SearchResult Execute(SearchCommand command);
    StepSearchResult ExecuteStepByStep(StepSearchCommand command);
    MultiFileSearchResult ExecuteMultiFile(MultiFileSearchCommand command);
    List<SearchResult> ExecuteCompareAll(SearchCommand command);
    List<AlgorithmInfo> GetAlgorithmsInfo();
}
