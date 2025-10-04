
using PokemonAPI.Models.ApiResponses;

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