namespace PokemonAPI.Models.DTOs;

/// Request model for comparing two Pokemon in a battle simulation.
/// Used by the /api/pokemon/compare endpoint to determine which Pokemon would win in a hypothetical battle.
/// Example: { "Pokemon1": "pikachu", "Pokemon2": "charizard" }
/// Can accept either Pokemon names (case-insensitive) or Pokedex IDs.

public record CompareRequest(string Pokemon1, string Pokemon2);