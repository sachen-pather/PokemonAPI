namespace PokemonAPI.Models.DTOs;

/// Represents detailed information about a single Pokemon.
/// Returned by the /api/pokemon/{id-or-name} endpoint.
/// Contains all essential Pokemon data including stats, types, abilities, and images.
/// This is the primary DTO for displaying complete Pokemon information to users.

public record PokemonDetail(
    int Id,
    string Name,
    int Height,
    int Weight,
    string[] Types,
    string[] Abilities,
    PokemonStats Stats,
    string SpriteUrl
);

public record PokemonStats(
    int HP,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed,
    int Total
);