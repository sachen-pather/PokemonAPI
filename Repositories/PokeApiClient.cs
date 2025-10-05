using Microsoft.Extensions.Caching.Memory;
using PokemonAPI.Models.ApiResponses;

namespace PokemonAPI.Repositories;

public class PokeApiClient : IPokeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PokeApiClient> _logger;

    public PokeApiClient(HttpClient httpClient, IMemoryCache cache, ILogger<PokeApiClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PokemonApiResponse?> GetPokemonAsync(string nameOrId)
    {
        var cacheKey = $"pokemon_{nameOrId.ToLower()}";

        if (_cache.TryGetValue(cacheKey, out PokemonApiResponse? cached))
        {
            _logger.LogInformation("Cache hit for Pokemon: {NameOrId}", nameOrId);
            return cached;
        }

        try
        {
            _logger.LogInformation("Fetching Pokemon from API: {NameOrId}", nameOrId);

            var response = await _httpClient.GetAsync($"pokemon/{nameOrId.ToLower()}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pokemon not found: {NameOrId}", nameOrId);
                return null;
            }

            var pokemon = await response.Content.ReadFromJsonAsync<PokemonApiResponse>();

            if (pokemon != null)
            {
                _cache.Set(cacheKey, pokemon, TimeSpan.FromHours(1));
                _logger.LogInformation("Cached Pokemon: {NameOrId}", nameOrId);
            }

            return pokemon;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching Pokemon: {NameOrId}", nameOrId);
            throw;
        }
    }

    public async Task<PokemonListResponse> GetPokemonListAsync(int limit, int offset)
    {
        var cacheKey = $"pokemon_list_{limit}_{offset}";

        if (_cache.TryGetValue(cacheKey, out PokemonListResponse? cached))
        {
            _logger.LogInformation("Cache hit for Pokemon list");
            return cached!;
        }

        try
        {
            var response = await _httpClient.GetAsync($"pokemon?limit={limit}&offset={offset}");
            response.EnsureSuccessStatusCode();

            var list = await response.Content.ReadFromJsonAsync<PokemonListResponse>();

            if (list != null)
            {
                _cache.Set(cacheKey, list, TimeSpan.FromMinutes(30));
            }

            return list ?? new PokemonListResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching Pokemon list");
            throw;
        }
    }

    public async Task<AbilityResponse?> GetPokemonByAbilityAsync(string ability)
    {
        // Convert display name to API format (lowercase with hyphens)
        var apiAbilityName = ability.ToLower().Replace(" ", "-");
        var cacheKey = $"ability_{apiAbilityName}";

        if (_cache.TryGetValue(cacheKey, out AbilityResponse? cached))
        {
            _logger.LogInformation("Cache hit for ability: {Ability}", ability);
            return cached;
        }

        try
        {
            _logger.LogInformation("Fetching Pokemon by ability from API: {Ability}", ability);

            var response = await _httpClient.GetAsync($"ability/{apiAbilityName}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ability not found: {Ability}", ability);
                return null;
            }

            var abilityData = await response.Content.ReadFromJsonAsync<AbilityResponse>();

            if (abilityData != null)
            {
                _cache.Set(cacheKey, abilityData, TimeSpan.FromHours(2));
                _logger.LogInformation("Cached ability: {Ability}", ability);
            }

            return abilityData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching ability: {Ability}", ability);
            throw;
        }
    }

    public async Task<PokemonTypeResponse?> GetPokemonByTypeAsync(string type)
    {
        var cacheKey = $"pokemon_by_type_{type.ToLower()}";
        if (_cache.TryGetValue(cacheKey, out PokemonTypeResponse? cached))
        {
            _logger.LogInformation("Cache hit for Pokémon by type: {Type}", type);
            return cached;
        }

        try
        {
            _logger.LogInformation("Fetching Pokémon by type from API: {Type}", type);
            var response = await _httpClient.GetAsync($"type/{type.ToLower()}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pokémon by type not found: {Type}", type);
                return null;
            }

            var typeData = await response.Content.ReadFromJsonAsync<PokemonTypeResponse>();
            if (typeData != null)
            {
                _cache.Set(cacheKey, typeData, TimeSpan.FromHours(2));
                _logger.LogInformation("Cached Pokémon by type: {Type}", type);
            }
            return typeData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching Pokémon by type: {Type}", type);
            throw;
        }
    }

    public async Task<TypeResponse?> GetTypeAsync(string typeName)
    {
        var cacheKey = $"type_details_{typeName.ToLower()}";
        if (_cache.TryGetValue(cacheKey, out TypeResponse? cached))
        {
            _logger.LogInformation("Cache hit for type details: {Type}", typeName);
            return cached;
        }

        try
        {
            _logger.LogInformation("Fetching type details from API: {Type}", typeName);
            var response = await _httpClient.GetAsync($"type/{typeName.ToLower()}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Type details not found: {Type}", typeName);
                return null;
            }

            var typeData = await response.Content.ReadFromJsonAsync<TypeResponse>();
            if (typeData != null)
            {
                _cache.Set(cacheKey, typeData, TimeSpan.FromHours(2));
                _logger.LogInformation("Cached type details: {Type}", typeName);
            }
            return typeData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching type details: {Type}", typeName);
            throw;
        }
    }

    public async Task<IEnumerable<PokemonApiResponse>> GetPokemonDetailsAsync(IEnumerable<string> urls)
    {
        var tasks = urls.Select(async url =>
        {
            var cacheKey = $"pokemon_{url}";
            if (_cache.TryGetValue(cacheKey, out PokemonApiResponse? cached))
            {
                _logger.LogInformation("Cache hit for Pokémon: {Url}", url);
                return cached;
            }

            try
            {
                _logger.LogInformation("Fetching Pokémon from API: {Url}", url);
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Pokémon not found: {Url}", url);
                    return null;
                }

                var pokemonData = await response.Content.ReadFromJsonAsync<PokemonApiResponse>();
                if (pokemonData != null)
                {
                    _cache.Set(cacheKey, pokemonData, TimeSpan.FromHours(1));
                    _logger.LogInformation("Cached Pokémon: {Url}", url);
                }
                return pokemonData;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching Pokémon: {Url}", url);
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(p => p != null)!;
    }

    public async Task<AbilityListResponse?> GetAllAbilitiesAsync()
    {
        var cacheKey = "all_abilities";
        if (_cache.TryGetValue(cacheKey, out AbilityListResponse? cached))
        {
            _logger.LogInformation("Cache hit for all abilities");
            return cached;
        }

        try
        {
            _logger.LogInformation("Fetching all abilities from API");
            var response = await _httpClient.GetAsync("ability?limit=1000");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch abilities");
                return null;
            }
            var abilityData = await response.Content.ReadFromJsonAsync<AbilityListResponse>();
            if (abilityData != null)
            {
                _cache.Set(cacheKey, abilityData, TimeSpan.FromHours(2));
                _logger.LogInformation("Cached all abilities");
            }
            return abilityData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching abilities");
            throw;
        }
    }
}