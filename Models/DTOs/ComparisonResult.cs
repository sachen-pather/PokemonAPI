namespace PokemonAPI.Models.DTOs;

// Response model containing the complete results of a Pokemon battle comparison.
/// Returned by the /api/pokemon/compare endpoint after simulating a battle between two Pokemon.
/// Includes winner determination, detailed stats, type effectiveness, ability impacts, and battle reasoning.

public record ComparisonResult(
    string Pokemon1,
    string Pokemon2,
    string Winner,
    int Score1,
    int Score2,
    string Reasoning,
    Dictionary<string, int> StatDifferences,
    double TypeMultiplier1Vs2,
    double TypeMultiplier2Vs1,
    string AbilityImpact1,
    string AbilityImpact2,
    string TypeEffectivenessExplanation1,
    string TypeEffectivenessExplanation2,
    EffectiveStats Pokemon1EffectiveStats,
    EffectiveStats Pokemon2EffectiveStats
);

public record EffectiveStats(
    int BaseHP,
    double EffectiveOffense,      // After ability + type modifiers
    double EffectiveDefense,      // After ability modifiers
    double EffectiveSpeed,        // After ability modifiers
    string OffenseType,           // "Physical" or "Special"
    double OffenseMultiplier,     // From abilities (e.g., 2.0 for Huge Power)
    double DefenseMultiplier,     // From abilities
    double SpeedMultiplier,       // From abilities
    int BaseDefense,              // New: Opponent's base Defense stat
    int BaseSpecialDefense        // New: Opponent's base Special Defense stat
);