using StringSearch.API.DTOs;
using StringSearch.API.Services;

namespace StringSearch.API.Facade;

/// <summary>
/// Facade: ponto único de entrada para o Controller.
///
/// Responsabilidades:
///   - Logar operações no nível da facade (contexto de request).
///   - Delegar toda lógica de busca ao ISearchService.
///   - Isolar o Controller de qualquer detalhe de implementação.
///
/// Correção de boas práticas:
///   - Dependência agora é ISearchService (interface), não SearchService (concreto).
/// </summary>
public class SearchFacade : ISearchFacade
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchFacade> _logger;

    public SearchFacade(ISearchService searchService, ILogger<SearchFacade> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    public SearchResult Execute(SearchCommand command)
    {
        _logger.LogInformation(
            "[Facade] Execute | algorithm={Alg} | pattern='{Pat}' | textLen={Len}",
            command.Algorithm, command.Pattern, command.Text.Length);

        return _searchService.Search(command);
    }

    public MultiFileSearchResult ExecuteMultiFile(MultiFileSearchCommand command)
    {
        _logger.LogInformation(
            "[Facade] MultiFile | algorithm={Alg} | files={Count} | pattern='{Pat}'",
            command.Algorithm, command.Files.Count, command.Pattern);

        return _searchService.SearchMultipleFiles(command);
    }

    public List<SearchResult> ExecuteCompareAll(SearchCommand command)
    {
        _logger.LogInformation(
            "[Facade] CompareAll | pattern='{Pat}' | textLen={Len}",
            command.Pattern, command.Text.Length);

        return _searchService.SearchAllAlgorithms(command.Text, command.Pattern);
    }

    public List<AlgorithmInfo> GetAlgorithmsInfo()
    {
        return _searchService.GetAlgorithmsInfo();
    }
}
