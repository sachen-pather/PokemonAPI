namespace PokemonAPI.Models.DTOs;

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