namespace PokemonAPI.Models.ApiResponses;

public class PokemonTypeResponse
{
    public List<PokemonEntry> Pokemon { get; set; } = new();
}

public class PokemonEntry
{
    public NamedApiResource Pokemon { get; set; } = new();
}