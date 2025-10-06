namespace PokemonAPI.Models.DTOs;

/// Represents a lightweight summary of a Pokemon with minimal information.
/// Used for list/collection endpoints where full details aren't needed (e.g., /api/pokemon/list, filter results).
/// Provides just enough information to identify and fetch the full Pokemon details.
/// Keeps response payloads small when returning many Pokemon at once.

public record PokemonSummary(
    int Id,
    string Name,
    string Url
);