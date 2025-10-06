namespace PokemonAPI.Models.ApiResponses;

/// Represents the API response when fetching the complete list of all abilities from the PokeAPI /ability endpoint.
/// This is a paginated list response containing names and URLs for all available Pokemon abilities.
/// Example: GET https://pokeapi.co/api/v2/ability?limit=100&offset=0
/// Used to populate ability dropdowns or to retrieve all abilities for filtering purposes.

public class AbilityListResponse
{
    public List<NamedApiResource> Results { get; set; } = new();
}