namespace PokemonAPI.Models.ApiResponses;


/// Represents the API response when querying Pokemon by ability from the PokeAPI /ability/{ability} endpoint.
/// This response contains a list of all Pokemon that can have the specified ability.
/// Example: GET https://pokeapi.co/api/v2/ability/levitate would return all Pokemon with the Levitate ability.

public class AbilityResponse
{
    public List<PokemonAbilityEntry> Pokemon { get; set; } = new();
}

public class PokemonAbilityEntry
{
    public NamedApiResource Pokemon { get; set; } = new();
}