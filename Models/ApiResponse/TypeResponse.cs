using System.Text.Json.Serialization;

/// Represents the API response when fetching type information from the PokeAPI /type/{type} endpoint.
/// Example: GET https://pokeapi.co/api/v2/type/fire
/// Contains damage relationships that define type effectiveness in Pokemon battles
/// (e.g., Fire is super effective against Grass, but weak against Water).

namespace PokemonAPI.Models.ApiResponses;

public class TypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("damage_relations")]
    public DamageRelations DamageRelations { get; set; } = new();
}

public class DamageRelations
{
    [JsonPropertyName("double_damage_from")]
    public List<NamedApiResource> DoubleDamageFrom { get; set; } = new();

    [JsonPropertyName("double_damage_to")]
    public List<NamedApiResource> DoubleDamageTo { get; set; } = new();

    [JsonPropertyName("half_damage_from")]
    public List<NamedApiResource> HalfDamageFrom { get; set; } = new();

    [JsonPropertyName("half_damage_to")]
    public List<NamedApiResource> HalfDamageTo { get; set; } = new();

    [JsonPropertyName("no_damage_from")]
    public List<NamedApiResource> NoDamageFrom { get; set; } = new();

    [JsonPropertyName("no_damage_to")]
    public List<NamedApiResource> NoDamageTo { get; set; } = new();
}