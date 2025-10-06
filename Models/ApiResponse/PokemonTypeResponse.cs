namespace PokemonAPI.Models.ApiResponses;

/// Represents the API response when querying Pokemon by type from the PokeAPI /type/{type} endpoint.
/// This response contains a list of all Pokemon that have the specified type.
/// Example: GET https://pokeapi.co/api/v2/type/fire would return all Fire-type Pokemon.

public class PokemonTypeResponse
{
    public List<PokemonEntry> Pokemon { get; set; } = new();
}

public class PokemonEntry
{
    public NamedApiResource Pokemon { get; set; } = new();
}