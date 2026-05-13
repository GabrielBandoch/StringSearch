using StringSearch.API.DTOs;

namespace StringSearch.API.Services;

/// <summary>
/// Contrato do serviço de busca.
/// Separa a interface da implementação, permitindo
/// substituição, mock em testes e inversão de dependência.
/// </summary>
public interface ISearchService
{
    SearchResult Search(SearchCommand command);
    MultiFileSearchResult SearchMultipleFiles(MultiFileSearchCommand command);
    List<SearchResult> SearchAllAlgorithms(string text, string pattern);
    List<AlgorithmInfo> GetAlgorithmsInfo();
}
