using Microsoft.AspNetCore.Mvc;
using StringSearch.API.DTOs;
using StringSearch.API.Facade;

namespace StringSearch.API.Controllers;

/// <summary>
/// Controller limpo: recebe HTTP, valida entrada, delega ao Facade, retorna resposta.
/// Não conhece Service, Strategy nem nenhuma lógica de busca.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly ISearchFacade _searchFacade;

    public SearchController(ISearchFacade searchFacade)
    {
        _searchFacade = searchFacade;
    }

    /// POST /api/search/execute
    [HttpPost("execute")]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SearchResult> Execute([FromBody] SearchCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Text) || string.IsNullOrWhiteSpace(command.Pattern))
            return BadRequest("Texto e padrão não podem ser vazios.");

        return Ok(_searchFacade.Execute(command));
    }

    /// POST /api/search/step-by-step
    [HttpPost("step-by-step")]
    [ProducesResponseType(typeof(StepSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<StepSearchResult> StepByStep([FromBody] StepSearchCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Text) || string.IsNullOrWhiteSpace(command.Pattern))
            return BadRequest("Texto e padrão não podem ser vazios.");

        if (command.Text.Length > 500)
            return BadRequest("Para execução passo a passo, limite o texto a 500 caracteres.");

        return Ok(_searchFacade.ExecuteStepByStep(command));
    }

    /// POST /api/search/multi-file
    [HttpPost("multi-file")]
    [ProducesResponseType(typeof(MultiFileSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<MultiFileSearchResult> MultiFile([FromBody] MultiFileSearchCommand command)
    {
        if (command.Files == null || command.Files.Count == 0)
            return BadRequest("Nenhum arquivo enviado.");

        if (string.IsNullOrWhiteSpace(command.Pattern))
            return BadRequest("Padrão de busca não pode ser vazio.");

        return Ok(_searchFacade.ExecuteMultiFile(command));
    }

    /// POST /api/search/compare-all
    [HttpPost("compare-all")]
    [ProducesResponseType(typeof(List<SearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<SearchResult>> CompareAll([FromBody] SearchCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Text) || string.IsNullOrWhiteSpace(command.Pattern))
            return BadRequest("Texto e padrão não podem ser vazios.");

        return Ok(_searchFacade.ExecuteCompareAll(command));
    }

    /// GET /api/search/algorithms
    [HttpGet("algorithms")]
    [ProducesResponseType(typeof(List<AlgorithmInfo>), StatusCodes.Status200OK)]
    public ActionResult<List<AlgorithmInfo>> GetAlgorithms()
    {
        return Ok(_searchFacade.GetAlgorithmsInfo());
    }
}
