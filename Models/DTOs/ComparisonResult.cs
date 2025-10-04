namespace PokemonAPI.Models.DTOs;

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
    string AbilityImpact2       
);