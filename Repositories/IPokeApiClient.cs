
using PokemonAPI.Models.ApiResponses;

/// Interface defining the contract for interacting with the external PokeAPI (pokeapi.co).
/// Implementations of this interface handle all HTTP communication with PokeAPI endpoints,
/// including caching, error handling, and response deserialization.
/// This abstraction allows for easier testing (can mock the API) and separates external API
/// concerns from business logic in the service layer.

namespace PokemonAPI.Repositories;

public interface IPokeApiClient
{
    Task<PokemonApiResponse?> GetPokemonAsync(string nameOrId);
    Task<PokemonListResponse> GetPokemonListAsync(int limit, int offset);
    Task<AbilityResponse?> GetPokemonByAbilityAsync(string ability);
    Task<AbilityListResponse?> GetAllAbilitiesAsync();
    Task<TypeResponse?> GetTypeAsync(string typeName);
    Task<IEnumerable<PokemonApiResponse>> GetPokemonDetailsAsync(IEnumerable<string> urls);
    Task<PokemonTypeResponse?> GetPokemonByTypeAsync(string type);
}