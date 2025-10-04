using Microsoft.AspNetCore.Mvc;
using PokemonAPI.Models.DTOs;
using PokemonAPI.Services;

namespace PokemonAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PokemonController : ControllerBase
{
    private readonly IPokemonService _pokemonService;
    private readonly ILogger<PokemonController> _logger;

    public PokemonController(IPokemonService pokemonService, ILogger<PokemonController> logger)
    {
        _pokemonService = pokemonService;
        _logger = logger;
    }

    [HttpGet("{nameOrId}")]
    public async Task<ActionResult<PokemonDetail>> GetPokemon(string nameOrId)
    {
        _logger.LogInformation("Fetching Pokemon: {NameOrId}", nameOrId);

        var pokemon = await _pokemonService.GetPokemonDetailAsync(nameOrId);

        if (pokemon == null)
        {
            return NotFound(new { message = $"Pokemon '{nameOrId}' not found" });
        }

        return Ok(pokemon);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PokemonSummary>>> SearchPokemon(
    [FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Search term required" });
        }

        var allPokemon = await _pokemonService.GetPokemonListAsync(1000, 0);
        var filtered = allPokemon
            .Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Ok(filtered);
    }

    [HttpGet("types")]
    public ActionResult<IEnumerable<string>> GetAllTypes()
    {
        var types = new List<string>
    {
        "normal", "fire", "water", "electric", "grass", "ice",
        "fighting", "poison", "ground", "flying", "psychic",
        "bug", "rock", "ghost", "dragon", "dark", "steel", "fairy"
    };

        return Ok(types);
    }

    [HttpGet("type/{type}")]
    public async Task<ActionResult<IEnumerable<PokemonSummary>>> GetPokemonByType(string type)
    {
        _logger.LogInformation("Fetching Pokemon by type: {Type}", type);

        var pokemon = await _pokemonService.GetPokemonByTypeAsync(type);

        if (!pokemon.Any())
        {
            return NotFound(new { message = $"No Pokemon found for type '{type}'" });
        }

        return Ok(pokemon);
    }

    [HttpGet("list")]
    public async Task<ActionResult<IEnumerable<PokemonSummary>>> GetPokemonList(
    [FromQuery] int limit = 20,
    [FromQuery] int offset = 0)
    {
        _logger.LogInformation("Fetching Pokemon list: limit={Limit}, offset={Offset}", limit, offset);
        var pokemonList = await _pokemonService.GetPokemonListAsync(limit, offset);
        return Ok(pokemonList);
    }


    [HttpPost("compare")]
    public async Task<ActionResult<ComparisonResult>> ComparePokemon([FromBody] CompareRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Pokemon1) || string.IsNullOrWhiteSpace(request.Pokemon2))
        {
            return BadRequest(new { message = "Both Pokemon names are required" });
        }

        _logger.LogInformation("Comparing Pokemon: {Pokemon1} vs {Pokemon2}", request.Pokemon1, request.Pokemon2);

        var result = await _pokemonService.ComparePokemonAsync(request.Pokemon1, request.Pokemon2);
        return Ok(result);
    }

    [HttpGet("ability/{ability}")]
    public async Task<ActionResult<IEnumerable<PokemonSummary>>> GetPokemonByAbility(string ability)
    {
        _logger.LogInformation("Fetching Pokemon by ability: {Ability}", ability);

        var pokemon = await _pokemonService.GetPokemonByAbilityAsync(ability);

        if (!pokemon.Any())
        {
            return NotFound(new { message = $"No Pokemon found for ability '{ability}'" });
        }

        return Ok(pokemon);
    }

    [HttpGet("abilities")]
    public async Task<ActionResult<IEnumerable<string>>> GetAllAbilities()
    {
        _logger.LogInformation("Fetching all abilities");
        var abilities = await _pokemonService.GetAllAbilitiesAsync();
        if (!abilities.Any())
            return NotFound(new { message = "No abilities found" });
        return Ok(abilities);
    }

    [HttpGet("filter")]
    public async Task<ActionResult<IEnumerable<PokemonSummary>>> FilterPokemon(
    [FromQuery] FilterRequest filter)
    {
        _logger.LogInformation("Filtering Pokémon with filter: {Filter}", filter);
        var pokemon = await _pokemonService.FilterPokemonAsync(filter);
        if (!pokemon.Any())
            return NotFound(new { message = "No Pokémon found matching the filter criteria" });
        return Ok(pokemon);
    }
}



public record CompareRequest(string Pokemon1, string Pokemon2);