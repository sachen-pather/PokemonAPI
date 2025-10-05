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

        bool abilityMatch = filter.Abilities == null ||
                            !filter.Abilities.Any() ||
                            filter.Abilities.All(filterAbility =>
                                pokemon.Abilities.Any(pokemonAbility =>
                                    pokemonAbility.ToLower().Contains(filterAbility.ToLower())));

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

        // Build battle profiles for each Pokemon
        var profile1 = await BuildBattleProfile(p1, p2);
        var profile2 = await BuildBattleProfile(p2, p1);

        // Generate type effectiveness explanations
        var typeExplanation1 = await GenerateTypeEffectivenessExplanation(p1, p2, profile1.typeEffectiveness);
        var typeExplanation2 = await GenerateTypeEffectivenessExplanation(p2, p1, profile2.typeEffectiveness);

        // Calculate ability modifiers for each Pokemon
        var abilityMods1 = CalculateAbilityModifiers(p1, p2);
        var abilityMods2 = CalculateAbilityModifiers(p2, p1);

        // Create effective stats objects
        var effectiveStats1 = new EffectiveStats(
         BaseHP: p1.Stats[0].BaseStat,
         EffectiveOffense: profile1.offense,
         EffectiveDefense: p1.Stats[2].BaseStat * abilityMods1.defenseMultiplier,
         EffectiveSpeed: p1.Stats[5].BaseStat * abilityMods1.speedMultiplier,
         OffenseType: profile1.isPhysicalAttacker ? "Physical" : "Special",
         OffenseMultiplier: abilityMods1.offenseMultiplier,
         DefenseMultiplier: abilityMods1.defenseMultiplier,
         SpeedMultiplier: abilityMods1.speedMultiplier,
         BaseDefense: p1.Stats[2].BaseStat,  // Add: p1's base Defense
         BaseSpecialDefense: p1.Stats[4].BaseStat  // Add: p1's base Special Defense
     );

        var effectiveStats2 = new EffectiveStats(
            BaseHP: p2.Stats[0].BaseStat,
            EffectiveOffense: profile2.offense,
            EffectiveDefense: p2.Stats[2].BaseStat * abilityMods2.defenseMultiplier,
            EffectiveSpeed: p2.Stats[5].BaseStat * abilityMods2.speedMultiplier,
            OffenseType: profile2.isPhysicalAttacker ? "Physical" : "Special",
            OffenseMultiplier: abilityMods2.offenseMultiplier,
            DefenseMultiplier: abilityMods2.defenseMultiplier,
            SpeedMultiplier: abilityMods2.speedMultiplier,
            BaseDefense: p2.Stats[2].BaseStat,  // Add: p2's base Defense
            BaseSpecialDefense: p2.Stats[4].BaseStat  // Add: p2's base Special Defense
        );

        // Simulate the battle
        var battleResult = SimulateBattle(profile1, profile2, p1, p2);

        var statDifferences = new Dictionary<string, int>
        {
            ["HP"] = p1.Stats[0].BaseStat - p2.Stats[0].BaseStat,
            ["Attack"] = p1.Stats[1].BaseStat - p2.Stats[1].BaseStat,
            ["Defense"] = p1.Stats[2].BaseStat - p2.Stats[2].BaseStat,
            ["Special Attack"] = p1.Stats[3].BaseStat - p2.Stats[3].BaseStat,
            ["Special Defense"] = p1.Stats[4].BaseStat - p2.Stats[4].BaseStat,
            ["Speed"] = p1.Stats[5].BaseStat - p2.Stats[5].BaseStat
        };

        return new ComparisonResult(
            Pokemon1: CapitalizeName(p1.Name),
            Pokemon2: CapitalizeName(p2.Name),
            Winner: CapitalizeName(battleResult.winner),
            Score1: battleResult.score1,
            Score2: battleResult.score2,
            Reasoning: battleResult.reasoning,
            StatDifferences: statDifferences,
            TypeMultiplier1Vs2: profile1.typeEffectiveness,
            TypeMultiplier2Vs1: profile2.typeEffectiveness,
            AbilityImpact1: profile1.abilityDescription,
            AbilityImpact2: profile2.abilityDescription,
            TypeEffectivenessExplanation1: typeExplanation1,
            TypeEffectivenessExplanation2: typeExplanation2,
            Pokemon1EffectiveStats: effectiveStats1,
            Pokemon2EffectiveStats: effectiveStats2
        );
    }

    private async Task<string> GenerateTypeEffectivenessExplanation(
    Models.ApiResponses.PokemonApiResponse attacker,
    Models.ApiResponses.PokemonApiResponse defender,
    double finalMultiplier)
    {
        var explanations = new List<string>();
        var attackerTypeNames = attacker.Types.Select(t => CapitalizeName(t.Type.Name)).ToList();
        var defenderTypeNames = defender.Types.Select(t => CapitalizeName(t.Type.Name)).ToList();

        foreach (var atkType in attacker.Types)
        {
            var typeData = await _pokeApiClient.GetTypeAsync(atkType.Type.Name);
            if (typeData == null) continue;

            double typeMultiplier = 1.0;
            var matchupDetails = new List<string>();

            foreach (var defType in defender.Types)
            {
                var defTypeName = CapitalizeName(defType.Type.Name);

                if (typeData.DamageRelations.DoubleDamageTo.Any(d => d.Name == defType.Type.Name))
                {
                    typeMultiplier *= 2.0;
                    matchupDetails.Add($"super effective vs {defTypeName}");
                }
                else if (typeData.DamageRelations.HalfDamageTo.Any(d => d.Name == defType.Type.Name))
                {
                    typeMultiplier *= 0.5;
                    matchupDetails.Add($"not very effective vs {defTypeName}");
                }
                else if (typeData.DamageRelations.NoDamageTo.Any(d => d.Name == defType.Type.Name))
                {
                    typeMultiplier = 0.0;
                    matchupDetails.Add($"no effect on {defTypeName}");
                }
                else
                {
                    matchupDetails.Add($"neutral vs {defTypeName}");
                }
            }

            if (matchupDetails.Any())
            {
                var atkTypeName = CapitalizeName(atkType.Type.Name);
                explanations.Add($"{atkTypeName} attacks are {string.Join(" and ", matchupDetails)} ({typeMultiplier}x)");
            }
        }

        // Build final explanation
        if (finalMultiplier == 0.0)
        {
            return $"IMMUNE: {string.Join("; ", explanations)}";
        }
        else if (finalMultiplier >= 4.0)
        {
            return $"DOUBLE SUPER EFFECTIVE (4x): {string.Join("; ", explanations)}";
        }
        else if (finalMultiplier > 1.0)
        {
            return $"Super Effective ({finalMultiplier}x): {string.Join("; ", explanations)}";
        }
        else if (finalMultiplier < 1.0)
        {
            return $"Not Very Effective ({finalMultiplier}x): {string.Join("; ", explanations)}";
        }
        else
        {
            return $"Neutral damage (1x): {string.Join("; ", explanations)}";
        }
    }

    private async Task<BattleProfile> BuildBattleProfile(
    Models.ApiResponses.PokemonApiResponse attacker,
    Models.ApiResponses.PokemonApiResponse defender)
    {
        var typeEffectiveness = await CalculateTypeEffectivenessAsync(attacker.Types, defender.Types);
        var abilityMods = CalculateAbilityModifiers(attacker, defender);
        var attackRole = DetermineAttackRole(attacker.Stats);
        var effectiveHP = attacker.Stats[0].BaseStat;
        var rawOffense = attackRole.isPhysical
            ? attacker.Stats[1].BaseStat
            : attacker.Stats[3].BaseStat;
        var effectiveOffense = rawOffense * abilityMods.offenseMultiplier * typeEffectiveness;
        var rawDefense = attackRole.isPhysical
            ? defender.Stats[2].BaseStat  // Defender's Physical Defense
            : defender.Stats[4].BaseStat; // Defender's Special Defense
        var effectiveDefense = rawDefense * abilityMods.defenseMultiplier;
        var speed = attacker.Stats[5].BaseStat * abilityMods.speedMultiplier;

        _logger.LogInformation("Building profile for {0} vs {1}: Offense={2:F1}, Defense={3:F1}, HP={4}, TypeEffectiveness={5}",
            attacker.Name, defender.Name, effectiveOffense, effectiveDefense, effectiveHP, typeEffectiveness);

        return new BattleProfile
        {
            hp = effectiveHP,
            offense = effectiveOffense,
            defense = effectiveDefense,
            speed = speed,
            typeEffectiveness = typeEffectiveness,
            isPhysicalAttacker = attackRole.isPhysical,
            abilityDescription = abilityMods.description,
            criticalAbilities = abilityMods.criticalAbilities
        };
    }

    private (bool isPhysical, string explanation) DetermineAttackRole(List<PokemonStat> stats)
    {
        var attack = stats[1].BaseStat;
        var specialAttack = stats[3].BaseStat;
        var difference = attack - specialAttack;

        // If Physical Attack is higher by 15+ points, it's clearly a Physical attacker
        if (difference >= 15)
        {
            return (true, "Physical attacker");
        }

        // If Special Attack is higher by 15+ points, it's clearly a Special attacker
        if (difference <= -15)
        {
            return (false, "Special attacker");
        }

        // Within 15 points: default to Special Attack
        // Rationale: Most balanced Pokémon are special attackers,
        // and this prevents overestimating damage output
        return (false, "Mixed attacker, using Special");
    }

    private (double offenseMultiplier, double defenseMultiplier, double speedMultiplier,
             string description, List<string> criticalAbilities) CalculateAbilityModifiers(
        Models.ApiResponses.PokemonApiResponse pokemon,
        Models.ApiResponses.PokemonApiResponse opponent)
    {
        var offenseMult = 1.0;
        var defenseMult = 1.0;
        var speedMult = 1.0;
        var descriptions = new List<string>();
        var critical = new List<string>();

        foreach (var ability in pokemon.Abilities)
        {
            var name = ability.Ability.Name.ToLower();

            switch (name)
            {
                // GAME-BREAKING ABILITIES
                case "huge-power":
                case "pure-power":
                    offenseMult *= 2.0;
                    descriptions.Add("Attack DOUBLED by " + ability.Ability.Name);
                    critical.Add("huge-power");
                    break;

                case "wonder-guard":
                    // Only super-effective hits land
                    descriptions.Add("CRITICAL: Only super-effective moves can hit");
                    critical.Add("wonder-guard");
                    break;

                // STRONG OFFENSIVE ABILITIES
                case "adaptability":
                    offenseMult *= 1.33; // Enhanced STAB
                    descriptions.Add("+33% damage from Adaptability");
                    break;

                case "guts":
                    offenseMult *= 1.5;
                    descriptions.Add("+50% Attack from Guts");
                    break;

                case "skill-link":
                    offenseMult *= 1.3;
                    descriptions.Add("+30% from multi-hit moves");
                    break;

                // DEFENSIVE ABILITIES
                case "marvel-scale":
                    defenseMult *= 1.5;
                    descriptions.Add("+50% Defense from Marvel Scale");
                    break;

                case "thick-fat":
                    defenseMult *= 1.25;
                    descriptions.Add("+25% bulk vs Fire/Ice");
                    break;

                case "solid-rock":
                case "filter":
                    defenseMult *= 1.25;
                    descriptions.Add("Super-effective damage reduced 25%");
                    break;

                // SPEED ABILITIES
                case "speed-boost":
                    speedMult *= 1.5;
                    descriptions.Add("+50% Speed boost");
                    break;

                case "swift-swim":
                case "chlorophyll":
                case "sand-rush":
                    speedMult *= 1.3;
                    descriptions.Add("+30% Speed in weather");
                    break;

                // UTILITY/IMMUNITY ABILITIES
                case "levitate":
                    if (opponent.Types.Any(t => t.Type.Name == "ground"))
                    {
                        descriptions.Add("IMMUNE to Ground-type");
                        critical.Add("levitate");
                    }
                    break;

                case "water-absorb":
                case "volt-absorb":
                case "flash-fire":
                    descriptions.Add("Heals from certain type attacks");
                    break;

                case "intimidate":
                    descriptions.Add("Lowers opponent Attack 33%");
                    // This affects opponent, handle separately
                    break;

                case "unaware":
                    descriptions.Add("Ignores opponent's stat boosts");
                    break;
            }
        }

        return (offenseMult, defenseMult, speedMult,
                descriptions.Any() ? string.Join("; ", descriptions) : "No significant ability impact",
                critical);
    }

    private (string winner, int score1, int score2, string reasoning) SimulateBattle(
    BattleProfile profile1,
    BattleProfile profile2,
    Models.ApiResponses.PokemonApiResponse p1,
    Models.ApiResponses.PokemonApiResponse p2)
    {
        // PHASE 1: Check for instant-win conditions (Wonder Guard)
        if (profile1.criticalAbilities.Contains("wonder-guard") && profile1.typeEffectiveness <= 1.0)
        {
            return (p1.Name, 999, 0,
                    $"{CapitalizeName(p1.Name)} is INVINCIBLE with Wonder Guard - opponent has no super-effective moves!");
        }
        if (profile2.criticalAbilities.Contains("wonder-guard") && profile2.typeEffectiveness <= 1.0)
        {
            return (p2.Name, 0, 999,
                    $"{CapitalizeName(p2.Name)} is INVINCIBLE with Wonder Guard - opponent has no super-effective moves!");
        }

        // PHASE 2: Check for complete immunity (0x type effectiveness)
        bool p1Immune = profile2.typeEffectiveness == 0.0;
        bool p2Immune = profile1.typeEffectiveness == 0.0;

        if (p1Immune && p2Immune)
        {
            return (p1.Name, 500, 500,
                    $"STALEMATE: Both Pokémon are immune to each other's attacks. No winner can be determined.");
        }
        if (p1Immune)
        {
            return (p1.Name, 999, 0,
                    $"{CapitalizeName(p1.Name)} is IMMUNE to {CapitalizeName(p2.Name)}'s attacks!");
        }
        if (p2Immune)
        {
            return (p2.Name, 0, 999,
                    $"{CapitalizeName(p2.Name)} is IMMUNE to {CapitalizeName(p1.Name)}'s attacks!");
        }

        // PHASE 3: Calculate damage per turn
        // Use profile1.defense for opponent (Charizard) when calculating Pikachu's damage
        var damage1PerTurn = CalculateDamage(profile1.offense, profile1.defense, profile2.hp, profile1.typeEffectiveness);
        // Use profile2.defense for opponent (Pikachu) when calculating Charizard's damage
        var damage2PerTurn = CalculateDamage(profile2.offense, profile2.defense, profile1.hp, profile2.typeEffectiveness);

        // PHASE 4: Calculate turns to KO
        var turnsToKO1 = Math.Ceiling(profile2.hp / Math.Max(damage1PerTurn, 0.1));
        var turnsToKO2 = Math.Ceiling(profile1.hp / Math.Max(damage2PerTurn, 0.1));

        // Validate damage and turns
        if (damage1PerTurn <= 0 || double.IsNaN(damage1PerTurn) || double.IsInfinity(damage1PerTurn))
        {
            _logger.LogError("Invalid damage for {0}: {1}", p1.Name, damage1PerTurn);
            damage1PerTurn = 0.1;
            turnsToKO1 = double.MaxValue;
        }
        if (damage2PerTurn <= 0 || double.IsNaN(damage2PerTurn) || double.IsInfinity(damage2PerTurn))
        {
            _logger.LogError("Invalid damage for {0}: {1}", p2.Name, damage2PerTurn);
            damage2PerTurn = 0.1;
            turnsToKO2 = double.MaxValue;
        }

        _logger.LogInformation("P1 ({0}) damage: {1:F2}, turnsToKO1: {2}, defenseUsed: {3:F1}",
            p1.Name, damage1PerTurn, turnsToKO1, profile1.defense);
        _logger.LogInformation("P2 ({0}) damage: {1:F2}, turnsToKO2: {2}, defenseUsed: {3:F1}",
            p2.Name, damage2PerTurn, turnsToKO2, profile2.defense);

        // PHASE 5: Speed determines first strike
        bool p1StrikesFirst = profile1.speed > profile2.speed;
        bool speedTie = Math.Abs(profile1.speed - profile2.speed) < 0.01;

        // PHASE 6: Calculate weighted battle scores
        int score1 = CalculateOwnPokemonScore(p1, profile1.offense, profile1.speed, (int)turnsToKO1);
        int score2 = CalculateOwnPokemonScore(p2, profile2.offense, profile2.speed, (int)turnsToKO2);

        // PHASE 7: Determine winner with detailed reasoning
        string winner;
        string reasoning;
        // Check for identical Pokémon
        bool identicalPokemon = p1.Name.Equals(p2.Name, StringComparison.OrdinalIgnoreCase) &&
                                Math.Abs(score1 - score2) < 5 &&
                                speedTie;

        if (identicalPokemon)
        {
            return (p1.Name, score1, score2,
                    $"MIRROR MATCH: Both {CapitalizeName(p1.Name)} are identical. Battle outcome would be a coin flip.");
        }

        if (turnsToKO1 < turnsToKO2)
        {
            winner = p1.Name;
            var margin = turnsToKO2 - turnsToKO1;
            reasoning = $"{CapitalizeName(p1.Name)} KOs in {turnsToKO1} turns vs {turnsToKO2} turns ({margin} turn advantage). " +
                       $"Deals {damage1PerTurn:F1} damage/turn with {profile1.typeEffectiveness}x type advantage. " +
                       $"{(p1StrikesFirst ? "Speed advantage ensures first strike. " : "")}";
        }
        else if (turnsToKO2 < turnsToKO1)
        {
            winner = p2.Name;
            var margin = turnsToKO1 - turnsToKO2;
            reasoning = $"{CapitalizeName(p2.Name)} KOs in {turnsToKO2} turns vs {turnsToKO1} turns ({margin} turn advantage). " +
                       $"Deals {damage2PerTurn:F1} damage/turn with {profile2.typeEffectiveness}x type advantage. " +
                       $"{(!p1StrikesFirst ? "Speed advantage ensures first strike. " : "")}";
        }
        else
        {
            // Same turns to KO - use score differential first, speed as tiebreaker
            var scoreDifference = Math.Abs(score1 - score2);
            const int SIGNIFICANT_SCORE_THRESHOLD = 40; // Reduced from 50 for better sensitivity

            if (scoreDifference > SIGNIFICANT_SCORE_THRESHOLD)
            {
                // Significant score difference - higher score wins regardless of speed
                winner = score1 > score2 ? p1.Name : p2.Name;
                var winnerScore = Math.Max(score1, score2);
                var loserScore = Math.Min(score1, score2);

                reasoning = $"Both KO in {turnsToKO1} turns, but {CapitalizeName(winner)} wins with superior combat profile " +
                           $"(Score: {winnerScore} vs {loserScore}). Type advantage and stats outweigh speed difference.";
            }
            else if (speedTie)
            {
                // Speed tie - use score as final arbiter
                winner = score1 >= score2 ? p1.Name : p2.Name;
                reasoning = $"Both KO in {turnsToKO1} turns with equal speed and similar power (Scores: {score1} vs {score2}). " +
                           $"{CapitalizeName(winner)} edges out marginally.";
            }
            else
            {
                // Close scores - speed decides
                winner = p1StrikesFirst ? p1.Name : p2.Name;
                reasoning = $"Both KO in {turnsToKO1} turns with similar power (Scores: {score1} vs {score2}), " +
                           $"but {CapitalizeName(winner)} wins by striking first " +
                           $"(Speed: {(p1StrikesFirst ? profile1.speed : profile2.speed):F0} vs " +
                           $"{(p1StrikesFirst ? profile2.speed : profile1.speed):F0}).";
            }
        }
        _logger.LogInformation("Final reasoning: {0}", reasoning);
        return (winner, score1, score2, reasoning);
    }

    private int CalculateOwnPokemonScore(
    Models.ApiResponses.PokemonApiResponse pokemon,
    double effectiveOffense,
    double effectiveSpeed,
    int turnsToKO)
    {
        var hp = pokemon.Stats[0].BaseStat;
        var defense = pokemon.Stats[2].BaseStat;
        var offenseScore = effectiveOffense * 0.30;
        var survivalScore = (hp + defense) * 0.40;
        var speedScore = effectiveSpeed * 0.20;
        var efficiencyBonus = Math.Max(0, (10 - turnsToKO) * 20);

        if (turnsToKO < 1)
        {
            _logger.LogError("Invalid turnsToKO for {0}: {1}, setting efficiencyBonus to 0", pokemon.Name, turnsToKO);
            efficiencyBonus = 0;
        }

        var total = (int)(offenseScore + survivalScore + speedScore + efficiencyBonus);
        _logger.LogInformation("{0} Score: Offense={1:F1}, Survival={2:F1}, Speed={3:F1}, Efficiency={4}, TurnsToKO={5}, Total={6}",
            pokemon.Name, offenseScore, survivalScore, speedScore, efficiencyBonus, turnsToKO, total);
        return total;
    }

    private double CalculateDamage(double offense, double defense, double hp, double typeEffectiveness)
    {
        // CRITICAL FIX: Handle immunities BEFORE damage calculation
        if (typeEffectiveness == 0.0)
        {
            return 0.0; // True immunity - no damage
        }

        // Type effectiveness is already applied in offense calculation
        // Don't double-apply it here
        var offenseDefenseRatio = offense / Math.Max(defense, 1);

        // Scale to make battles realistic (typically 20-50% HP per turn for balanced matchups)
        var baseDamage = offenseDefenseRatio * 15;

        // Cap maximum damage at 50% HP to prevent constant 1-turn KOs
        var maxDamage = hp * 0.50;
        var cappedDamage = Math.Min(baseDamage, maxDamage);

        // Floor at 3% HP minimum ONLY if not immune
        var minimumDamage = hp * 0.03;

        return Math.Max(cappedDamage, minimumDamage);
    }

    private int CalculateWeightedScore(BattleProfile profile, int turnsToKO, bool includeTypeBonus = false)
    {
        // Weighted scoring system (FIXED to avoid double-counting type effectiveness):
        // - Offensive power: 30% (killing fast is key)
        // - Survivability: 40% (HP + Defense combined) - INCREASED from 35%
        // - Speed: 20% (first strike matters) - INCREASED from 15%
        // - Type advantage: 10% (ONLY if explicitly requested, otherwise already in offense)

        var offenseScore = profile.offense * 0.30;
        var survivalScore = (profile.hp + profile.defense) * 0.40;
        var speedScore = profile.speed * 0.20;

        // Only add type bonus if explicitly requested (for future use cases)
        var typeScore = includeTypeBonus ? (profile.typeEffectiveness * 100 * 0.10) : 0;

        // Bonus for winning quickly
        var efficiencyBonus = Math.Max(0, (10 - turnsToKO) * 20);

        return (int)(offenseScore + survivalScore + speedScore + typeScore + efficiencyBonus);
    }

    private bool IsExtremeMismatch(BattleProfile p1, BattleProfile p2)
    {
        var totalP1 = p1.hp + p1.offense + p1.defense + p1.speed;
        var totalP2 = p2.hp + p2.offense + p2.defense + p2.speed;

        var ratio = Math.Max(totalP1, totalP2) / Math.Max(Math.Min(totalP1, totalP2), 1);

        // If one Pokemon has 2x+ total effective stats, it's an extreme mismatch
        return ratio >= 2.0;
    }

    // Helper class to store battle calculations
    private class BattleProfile
    {
        public double hp { get; set; }
        public double offense { get; set; }
        public double defense { get; set; }
        public double speed { get; set; }
        public double typeEffectiveness { get; set; }
        public bool isPhysicalAttacker { get; set; }
        public string abilityDescription { get; set; } = "";
        public List<string> criticalAbilities { get; set; } = new();
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