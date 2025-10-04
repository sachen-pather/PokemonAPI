using PokemonAPI.Models.ApiResponses;
using PokemonAPI.Models.DTOs;

namespace PokemonAPI.Services;

public interface IPokemonService
{
    Task<PokemonDetail?> GetPokemonDetailAsync(string nameOrId);
    Task<IEnumerable<PokemonSummary>> GetPokemonListAsync(int limit, int offset);
    Task<ComparisonResult> ComparePokemonAsync(string pokemon1, string pokemon2);
    Task<IEnumerable<PokemonSummary>> GetPokemonByTypeAsync(string type);
    Task<IEnumerable<PokemonSummary>> GetPokemonByAbilityAsync(string ability);
    Task<IEnumerable<string>> GetAllAbilitiesAsync();
    Task<IEnumerable<PokemonSummary>> FilterPokemonAsync(FilterRequest filter); 
    Task<TypeResponse?> GetTypeAsync(string typeName); 

}