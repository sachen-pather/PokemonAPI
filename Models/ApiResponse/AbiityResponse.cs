namespace PokemonAPI.Models.ApiResponses;

public class AbilityResponse
{
    public List<PokemonAbilityEntry> Pokemon { get; set; } = new();
}

public class PokemonAbilityEntry
{
    public NamedApiResource Pokemon { get; set; } = new();
}