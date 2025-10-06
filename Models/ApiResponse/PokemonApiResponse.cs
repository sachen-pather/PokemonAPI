using System.Text.Json.Serialization;

/// Represents the complete API response when fetching detailed information about a specific Pokemon 
/// from the PokeAPI /pokemon/{id-or-name} endpoint.
/// Example: GET https://pokeapi.co/api/v2/pokemon/25 (for Pikachu)
/// Contains all core Pokemon data including stats, types, abilities, and sprite images.
/// </summary>

namespace PokemonAPI.Models.ApiResponses;

public class PokemonApiResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Height { get; set; }
    public int Weight { get; set; }

    [JsonPropertyName("base_experience")]
    public int BaseExperience { get; set; }

    public List<PokemonTypeSlot> Types { get; set; } = new();
    public List<PokemonAbilitySlot> Abilities { get; set; } = new();
    public List<PokemonStat> Stats { get; set; } = new();
    public PokemonSprites Sprites { get; set; } = new();
}

public class PokemonTypeSlot
{
    public int Slot { get; set; }
    public NamedApiResource Type { get; set; } = new();
}

public class PokemonAbilitySlot
{
    public NamedApiResource Ability { get; set; } = new();

    [JsonPropertyName("is_hidden")]
    public bool IsHidden { get; set; }
}

public class PokemonStat
{
    [JsonPropertyName("base_stat")]
    public int BaseStat { get; set; }

    public NamedApiResource Stat { get; set; } = new();
}

public class PokemonSprites
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }

    public OtherSprites? Other { get; set; }
}

public class OtherSprites
{
    [JsonPropertyName("official-artwork")]
    public ArtworkSprites? OfficialArtwork { get; set; }
}

public class ArtworkSprites
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }
}

public class NamedApiResource
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class PokemonListResponse
{
    public int Count { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
    public List<NamedApiResource> Results { get; set; } = new();
}