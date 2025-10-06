namespace PokemonAPI.Models.DTOs;

/// Request model for filtering Pokemon based on multiple criteria including physical attributes and battle stats.
/// Used by the /api/pokemon/filter endpoint to search for Pokemon matching specific characteristics.
/// All properties are optional (nullable) - only specified filters are applied.
/// Example: Find all Fire-type Pokemon with Attack > 100 and Speed between 80-120.
/// Note: For best performance, include Type or Abilities filters to narrow the search scope.

public class FilterRequest
{
    public int? MinHeight { get; set; }
    public int? MaxHeight { get; set; }
    public int? MinWeight { get; set; }
    public int? MaxWeight { get; set; }
    public int? MinHp { get; set; }
    public int? MaxHp { get; set; }
    public int? MinAttack { get; set; }
    public int? MaxAttack { get; set; }
    public int? MinDefense { get; set; }
    public int? MaxDefense { get; set; }
    public int? MinSpecialAttack { get; set; }
    public int? MaxSpecialAttack { get; set; }
    public int? MinSpecialDefense { get; set; }
    public int? MaxSpecialDefense { get; set; }
    public int? MinSpeed { get; set; }
    public int? MaxSpeed { get; set; }
    public string? Type { get; set; }
    public List<string>? Abilities { get; set; }
    public int? MinTotal { get; set; }        
    public int? MaxTotal { get; set; }        
}