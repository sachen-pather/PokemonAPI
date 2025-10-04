namespace PokemonAPI.Models.ApiResponses;

public class AbilityListResponse
{
    public List<NamedApiResource> Results { get; set; } = new();
}