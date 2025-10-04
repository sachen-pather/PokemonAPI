using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using PokemonAPI.Models.ApiResponses;
using PokemonAPI.Models.DTOs;
using PokemonAPI.Repositories;
                                
namespace PokemonAPI.Services;

public class PokemonService : IPokemonService
{
    private readonly IPokeApiClient _pokeApiClient;
    private readonly ILogger<PokemonService> _logger;
    private readonly IMemoryCache _cache;

    public PokemonService(IPokeApiClient pokeApiClient, ILogger<PokemonService> logger, IMemoryCache cache)
    {
        _pokeApiClient = pokeApiClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<PokemonDetail?> GetPokemonDetailAsync(string nameOrId)
    {
        var apiResponse = await _pokeApiClient.GetPokemonAsync(nameOrId);

        if (apiResponse == null)
            return null;

        var stats = new PokemonStats(
            HP: apiResponse.Stats[0].BaseStat,
            Attack: apiResponse.Stats[1].BaseStat,
            Defense: apiResponse.Stats[2].BaseStat,
            SpecialAttack: apiResponse.Stats[3].BaseStat,
            SpecialDefense: apiResponse.Stats[4].BaseStat,
            Speed: apiResponse.Stats[5].BaseStat,
            Total: apiResponse.Stats.Sum(s => s.BaseStat)
        );

        return new PokemonDetail(
            Id: apiResponse.Id,
            Name: CapitalizeName(apiResponse.Name),
            Height: apiResponse.Height,
            Weight: apiResponse.Weight,
            Types: apiResponse.Types.Select(t => CapitalizeName(t.Type.Name)).ToArray(),
            Abilities: apiResponse.Abilities.Select(a => CapitalizeName(a.Ability.Name)).ToArray(),
            Stats: stats,
            SpriteUrl: apiResponse.Sprites.Other?.OfficialArtwork?.FrontDefault
                ?? apiResponse.Sprites.FrontDefault ?? ""
        );
    }

    public async Task<IEnumerable<PokemonSummary>> FilterPokemonAsync(FilterRequest filter)
    {
        var allPokemon = new List<PokemonSummary>();
        var offset = 0;
        const int batchSize = 1000;
        PokemonListResponse? listResponse;

        do
        {
            _logger.LogInformation("Fetching Pokémon list: offset={Offset}", offset);
            listResponse = await _pokeApiClient.GetPokemonListAsync(batchSize, offset);
            if (listResponse?.Results != null)
            {
                _logger.LogInformation("Fetched {Count} Pokémon at offset {Offset}, Next: {Next}",
                    listResponse.Results.Count, offset, listResponse.Next);
                var pokemonUrls = listResponse.Results.Select(r => r.Url);
                var pokemonDetails = await _pokeApiClient.GetPokemonDetailsAsync(pokemonUrls);

                var summaries = new List<PokemonSummary>();
                foreach (var detail in pokemonDetails)
                {
                    var spriteUrl = detail.Sprites.Other?.OfficialArtwork?.FrontDefault
                        ?? detail.Sprites.FrontDefault
                        ?? $"https://pokeapi.co/api/v2/pokemon/{detail.Id}/";
                    var pokemon = new PokemonDetail(
                        Id: detail.Id,
                        Name: CapitalizeName(detail.Name),
                        Height: detail.Height,
                        Weight: detail.Weight,
                        Types: detail.Types.Select(t => CapitalizeName(t.Type.Name)).ToArray(),
                        Abilities: detail.Abilities.Select(a => CapitalizeName(a.Ability.Name)).ToArray(),
                        Stats: new PokemonStats(
                            HP: detail.Stats.FirstOrDefault(s => s.Stat.Name == "hp")?.BaseStat ?? 0,
                            Attack: detail.Stats.FirstOrDefault(s => s.Stat.Name == "attack")?.BaseStat ?? 0,
                            Defense: detail.Stats.FirstOrDefault(s => s.Stat.Name == "defense")?.BaseStat ?? 0,
                            SpecialAttack: detail.Stats.FirstOrDefault(s => s.Stat.Name == "special-attack")?.BaseStat ?? 0,
                            SpecialDefense: detail.Stats.FirstOrDefault(s => s.Stat.Name == "special-defense")?.BaseStat ?? 0,
                            Speed: detail.Stats.FirstOrDefault(s => s.Stat.Name == "speed")?.BaseStat ?? 0,
                            Total: detail.Stats.Sum(s => s.BaseStat)
                        ),
                        SpriteUrl: spriteUrl
                    );

                    if (MatchesFilter(pokemon, filter))
                    {
                        summaries.Add(new PokemonSummary(
                            Id: pokemon.Id,
                            Name: pokemon.Name,
                            Url: $"https://pokeapi.co/api/v2/pokemon/{pokemon.Id}/"
                        ));
                    }
                }
                allPokemon.AddRange(summaries);
            }
            else
            {
                _logger.LogWarning("No results or null response at offset {Offset}", offset);
            }
            offset += batchSize;
        } while (listResponse?.Next != null);

        _logger.LogInformation("Filter result generated for: {Filter}, Total Pokémon: {Count}", filter, allPokemon.Count);
        return allPokemon;
    }

    private bool MatchesFilter(PokemonDetail pokemon, FilterRequest filter)
    {
        bool heightMatch = (!filter.MinHeight.HasValue || pokemon.Height >= filter.MinHeight) &&
                           (!filter.MaxHeight.HasValue || pokemon.Height <= filter.MaxHeight);
        bool weightMatch = (!filter.MinWeight.HasValue || pokemon.Weight >= filter.MinWeight) &&
                           (!filter.MaxWeight.HasValue || pokemon.Weight <= filter.MaxWeight);
        bool hpMatch = (!filter.MinHp.HasValue || pokemon.Stats.HP >= filter.MinHp) &&
                       (!filter.MaxHp.HasValue || pokemon.Stats.HP <= filter.MaxHp);
        bool attackMatch = (!filter.MinAttack.HasValue || pokemon.Stats.Attack >= filter.MinAttack) &&
                           (!filter.MaxAttack.HasValue || pokemon.Stats.Attack <= filter.MaxAttack);
        bool defenseMatch = (!filter.MinDefense.HasValue || pokemon.Stats.Defense >= filter.MinDefense) &&
                            (!filter.MaxDefense.HasValue || pokemon.Stats.Defense <= filter.MaxDefense);
        bool specialAttackMatch = (!filter.MinSpecialAttack.HasValue || pokemon.Stats.SpecialAttack >= filter.MinSpecialAttack) &&
                                 (!filter.MaxSpecialAttack.HasValue || pokemon.Stats.SpecialAttack <= filter.MaxSpecialAttack);
        bool specialDefenseMatch = (!filter.MinSpecialDefense.HasValue || pokemon.Stats.SpecialDefense >= filter.MinSpecialDefense) &&
                                  (!filter.MaxSpecialDefense.HasValue || pokemon.Stats.SpecialDefense <= filter.MaxSpecialDefense);
        bool speedMatch = (!filter.MinSpeed.HasValue || pokemon.Stats.Speed >= filter.MinSpeed) &&
                          (!filter.MaxSpeed.HasValue || pokemon.Stats.Speed <= filter.MaxSpeed);
        bool totalMatch = (!filter.MinTotal.HasValue || pokemon.Stats.Total >= filter.MinTotal) &&
                          (!filter.MaxTotal.HasValue || pokemon.Stats.Total <= filter.MaxTotal);

        bool typeMatch = string.IsNullOrEmpty(filter.Type) ||
                         pokemon.Types.Any(t => t.ToLower().Contains(filter.Type.ToLower()));
        bool abilityMatch = string.IsNullOrEmpty(filter.Ability) ||
                            pokemon.Abilities.Any(a => a.ToLower().Contains(filter.Ability.ToLower()));

        return heightMatch && weightMatch && hpMatch && attackMatch && defenseMatch &&
               specialAttackMatch && specialDefenseMatch && speedMatch && totalMatch &&
               typeMatch && abilityMatch;
    }

    public async Task<IEnumerable<PokemonSummary>> GetPokemonByTypeAsync(string type)
    {
        var typeResponse = await _pokeApiClient.GetPokemonByTypeAsync(type);

        if (typeResponse == null)
        {
            return Enumerable.Empty<PokemonSummary>();
        }

        var summaries = new List<PokemonSummary>();
        foreach (var entry in typeResponse.Pokemon)
        {
            // Access Pokemon property with capital P
            var id = ExtractIdFromUrl(entry.Pokemon.Url);
            summaries.Add(new PokemonSummary(
                Id: id,
                Name: CapitalizeName(entry.Pokemon.Name),
                Url: entry.Pokemon.Url
            ));
        }
        return summaries;
    }


    public async Task<IEnumerable<PokemonSummary>> GetPokemonByAbilityAsync(string ability)
    {
        var abilityResponse = await _pokeApiClient.GetPokemonByAbilityAsync(ability);

        if (abilityResponse == null)
        {
            return Enumerable.Empty<PokemonSummary>();
        }

        var summaries = new List<PokemonSummary>();
        foreach (var entry in abilityResponse.Pokemon)
        {
            var id = ExtractIdFromUrl(entry.Pokemon.Url);
            summaries.Add(new PokemonSummary(
                Id: id,
                Name: CapitalizeName(entry.Pokemon.Name),
                Url: entry.Pokemon.Url
            ));
        }
        return summaries;
    }

    public async Task<IEnumerable<string>> GetAllAbilitiesAsync()
    {
        var abilityResponse = await _pokeApiClient.GetAllAbilitiesAsync();
        if (abilityResponse == null || !abilityResponse.Results.Any())
            return Enumerable.Empty<string>();
        return abilityResponse.Results.Select(r => CapitalizeName(r.Name)).OrderBy(name => name);
    }

    public async Task<IEnumerable<PokemonSummary>> GetPokemonListAsync(int limit, int offset)
    {
        var listResponse = await _pokeApiClient.GetPokemonListAsync(limit, offset);
        var summaries = new List<PokemonSummary>();
        foreach (var item in listResponse.Results)
        {
            var id = ExtractIdFromUrl(item.Url);
            summaries.Add(new PokemonSummary(
                Id: id,
                Name: CapitalizeName(item.Name),
                Url: item.Url
            ));
        }
        return summaries;
    }

    public async Task<ComparisonResult> ComparePokemonAsync(string pokemon1, string pokemon2)
    {
        var p1Task = _pokeApiClient.GetPokemonAsync(pokemon1);
        var p2Task = _pokeApiClient.GetPokemonAsync(pokemon2);

        await Task.WhenAll(p1Task, p2Task);

        var p1 = p1Task.Result;
        var p2 = p2Task.Result;

        if (p1 == null || p2 == null)
        {
            throw new ArgumentException("One or both Pokemon not found");
        }

        // Calculate type effectiveness
        double multiplier1Vs2 = await CalculateTypeEffectivenessAsync(p1.Types, p2.Types);
        double multiplier2Vs1 = await CalculateTypeEffectivenessAsync(p2.Types, p1.Types);

        // Calculate offensive power (Attack + Special Attack) with type effectiveness
        var offensivePower1 = (p1.Stats[1].BaseStat + p1.Stats[3].BaseStat) * multiplier1Vs2;
        var offensivePower2 = (p2.Stats[1].BaseStat + p2.Stats[3].BaseStat) * multiplier2Vs1;

        // Calculate defensive stats
        var defensiveTotal1 = p1.Stats[0].BaseStat + p1.Stats[2].BaseStat + p1.Stats[4].BaseStat; // HP + Def + SpDef
        var defensiveTotal2 = p2.Stats[0].BaseStat + p2.Stats[2].BaseStat + p2.Stats[4].BaseStat;

        // Overall battle score combines offense and defense
        double battleScore1 = offensivePower1 + defensiveTotal1 + p1.Stats[5].BaseStat; // Add speed
        double battleScore2 = offensivePower2 + defensiveTotal2 + p2.Stats[5].BaseStat;

        // Ability impacts
        string abilityImpact1 = GetAbilityImpact(p1.Abilities.FirstOrDefault()?.Ability.Name ?? "");
        string abilityImpact2 = GetAbilityImpact(p2.Abilities.FirstOrDefault()?.Ability.Name ?? "");

        var statDifferences = new Dictionary<string, int>
        {
            ["HP"] = p1.Stats[0].BaseStat - p2.Stats[0].BaseStat,
            ["Attack"] = p1.Stats[1].BaseStat - p2.Stats[1].BaseStat,
            ["Defense"] = p1.Stats[2].BaseStat - p2.Stats[2].BaseStat,
            ["Special Attack"] = p1.Stats[3].BaseStat - p2.Stats[3].BaseStat,
            ["Special Defense"] = p1.Stats[4].BaseStat - p2.Stats[4].BaseStat,
            ["Speed"] = p1.Stats[5].BaseStat - p2.Stats[5].BaseStat
        };

        var winner = battleScore1 > battleScore2 ? p1.Name :
                     battleScore2 > battleScore1 ? p2.Name : "Tie";

        var reasoning = battleScore1 == battleScore2
            ? "Both Pokemon have equal battle scores after considering type matchups"
            : $"{CapitalizeName(winner)} wins with a battle score of {Math.Max(battleScore1, battleScore2):F0} vs {Math.Min(battleScore1, battleScore2):F0}. " +
              $"Type advantage: {CapitalizeName(p1.Name)} deals {multiplier1Vs2}x damage, {CapitalizeName(p2.Name)} deals {multiplier2Vs1}x damage.";

        return new ComparisonResult(
            Pokemon1: CapitalizeName(p1.Name),
            Pokemon2: CapitalizeName(p2.Name),
            Winner: CapitalizeName(winner),
            Score1: (int)battleScore1,
            Score2: (int)battleScore2,
            Reasoning: reasoning,
            StatDifferences: statDifferences,
            TypeMultiplier1Vs2: multiplier1Vs2,
            TypeMultiplier2Vs1: multiplier2Vs1,
            AbilityImpact1: abilityImpact1,
            AbilityImpact2: abilityImpact2
        );
    }

    private async Task<double> CalculateTypeEffectivenessAsync(List<PokemonTypeSlot> attackerTypes, List<PokemonTypeSlot> defenderTypes)
    {
        double bestMultiplier = 1.0;

        foreach (var atkType in attackerTypes)
        {
            var typeData = await _pokeApiClient.GetTypeAsync(atkType.Type.Name);
            if (typeData != null)
            {
                double currentMultiplier = 1.0;

                foreach (var defType in defenderTypes)
                {
                    if (typeData.DamageRelations.DoubleDamageTo.Any(d => d.Name == defType.Type.Name))
                        currentMultiplier *= 2.0;
                    else if (typeData.DamageRelations.HalfDamageTo.Any(d => d.Name == defType.Type.Name))
                        currentMultiplier *= 0.5;
                    else if (typeData.DamageRelations.NoDamageTo.Any(d => d.Name == defType.Type.Name))
                    {
                        currentMultiplier = 0.0;
                        break;
                    }
                }

                bestMultiplier = Math.Max(bestMultiplier, currentMultiplier);
            }
        }

        return bestMultiplier;
    }

    public async Task<TypeResponse?> GetTypeAsync(string typeName)
    {
        return await _pokeApiClient.GetTypeAsync(typeName);
    }

    private string GetAbilityImpact(string abilityName)
    {
        return abilityName.ToLower() switch
        {
            "intimidate" => "Reduces opponent's Attack by 50%",
            "huge power" => "Doubles Attack stat",
            "speed boost" => "Increases Speed each turn",
            "levitate" => "Immune to Ground attacks",
            _ => "No special impact"
        };
    }

    private int CalculateBattleScore(Models.ApiResponses.PokemonApiResponse pokemon)
    {
        return pokemon.Stats.Sum(s => s.BaseStat);
    }

    private string CapitalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return char.ToUpper(name[0]) + name.Substring(1).Replace("-", " ");
    }

    private int ExtractIdFromUrl(string url)
    {
        var parts = url.TrimEnd('/').Split('/');
        return int.Parse(parts[^1]);
    }
}