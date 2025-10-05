# Pokémon API - Comparator & Information Viewer

A RESTful API built with ASP.NET Core that interacts with [PokeAPI](https://pokeapi.co/) to retrieve, filter, compare, and analyze Pokémon data. This project demonstrates clean architecture principles, performance optimization through caching, and custom battle logic for Pokémon comparisons.

##  Features

- **Retrieve Pokémon Details**: Get comprehensive information about any Pokémon by name or ID
- **Advanced Filtering**: Filter Pokémon by type, ability, stats ranges (HP, Attack, Defense, etc.), height, and weight
- **Battle Comparison**: Compare two Pokémon using custom logic that considers type effectiveness, stats, and abilities
- **Search Functionality**: Find Pokémon by partial name matching
- **Type & Ability Queries**: Browse Pokémon by specific types or abilities
- **Performance Caching**: In-memory caching with strategic TTLs to minimize API calls
- **Comprehensive Error Handling**: Global exception handler with consistent error responses
- **API Documentation**: Interactive Swagger/OpenAPI documentation

##  Technology Stack

- **Language**: C# (.NET 6.0+)
- **Framework**: ASP.NET Core Web API
- **Caching**: IMemoryCache
- **Documentation**: Swagger/OpenAPI
- **External API**: PokeAPI v2
- **Architecture**: Three-layer architecture (Controller → Service → Repository)

##  Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download) or later
- Visual Studio 2022, Visual Studio Code, or JetBrains Rider
- Internet connection (for PokeAPI access)

##  Installation & Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd PokemonAPI
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access Swagger UI**
   - HTTPS: `https://localhost:7087/swagger`
   - HTTP: `http://localhost:5243/swagger`

##  API Endpoints

### Pokémon Information

#### Get Pokémon Details
```http
GET /api/Pokemon/{nameOrId}
```
Retrieve detailed information about a specific Pokémon.

**Example:**
```bash
curl https://localhost:7087/api/Pokemon/pikachu
```

**Response:**
```json
{
  "id": 25,
  "name": "Pikachu",
  "height": 4,
  "weight": 60,
  "types": ["Electric"],
  "abilities": ["Static", "Lightning rod"],
  "stats": {
    "hp": 35,
    "attack": 55,
    "defense": 40,
    "specialAttack": 50,
    "specialDefense": 50,
    "speed": 90,
    "total": 320
  },
  "spriteUrl": "https://raw.githubusercontent.com/PokeAPI/sprites/..."
}
```

#### Search Pokémon
```http
GET /api/Pokemon/search?name={searchTerm}
```
Search for Pokémon by partial name match (case-insensitive).

**Example:**
```bash
curl https://localhost:7087/api/Pokemon/search?name=char
```

#### List Pokémon
```http
GET /api/Pokemon/list?limit={limit}&offset={offset}
```
Get a paginated list of Pokémon.

**Parameters:**
- `limit` (optional, default: 20): Number of results
- `offset` (optional, default: 0): Starting position

### Type & Ability Queries

#### Get All Types
```http
GET /api/Pokemon/types
```
Returns all 18 Pokémon types.

#### Get Pokémon by Type
```http
GET /api/Pokemon/type/{type}
```
Get all Pokémon of a specific type.

**Example:**
```bash
curl https://localhost:7087/api/Pokemon/type/electric
```

#### Get All Abilities
```http
GET /api/Pokemon/abilities
```
Returns a list of all Pokémon abilities (sorted alphabetically).

#### Get Pokémon by Ability
```http
GET /api/Pokemon/ability/{ability}
```
Get all Pokémon with a specific ability.

**Example:**
```bash
curl https://localhost:7087/api/Pokemon/ability/levitate
```

### Advanced Features

#### Compare Pokémon
```http
POST /api/Pokemon/compare
Content-Type: application/json

{
  "pokemon1": "charizard",
  "pokemon2": "blastoise"
}
```

Compares two Pokémon using custom battle logic that considers:
- Type effectiveness (super effective, not very effective, immune)
- Base stats (HP, Attack, Defense, Special Attack, Special Defense, Speed)
- Special abilities
- Overall battle score calculation

**Response:**
```json
{
  "pokemon1": "Charizard",
  "pokemon2": "Blastoise",
  "winner": "Charizard",
  "score1": 534,
  "score2": 530,
  "reasoning": "Charizard wins with a battle score of 534 vs 530...",
  "statDifferences": {
    "HP": -1,
    "Attack": 24,
    ...
  },
  "typeMultiplier1Vs2": 0.5,
  "typeMultiplier2Vs1": 2.0,
  "abilityImpact1": "No special impact",
  "abilityImpact2": "No special impact"
}
```

#### Filter Pokémon
```http
GET /api/Pokemon/filter?{parameters}
```

Advanced filtering with multiple criteria:

**Available Parameters:**
- `MinHeight` / `MaxHeight`: Height range
- `MinWeight` / `MaxWeight`: Weight range
- `MinHp` / `MaxHp`: HP stat range
- `MinAttack` / `MaxAttack`: Attack stat range
- `MinDefense` / `MaxDefense`: Defense stat range
- `MinSpecialAttack` / `MaxSpecialAttack`: Special Attack range
- `MinSpecialDefense` / `MaxSpecialDefense`: Special Defense range
- `MinSpeed` / `MaxSpeed`: Speed stat range
- `MinTotal` / `MaxTotal`: Total base stats range
- `Type`: Pokémon type
- `Ability`: Pokémon ability

**Example:**
```bash
curl "https://localhost:7087/api/Pokemon/filter?MinAttack=100&Type=dragon&MaxSpeed=80"
```

##  Architecture

The project follows a clean, three-layer architecture:

```
┌─────────────────────────────────────────┐
│         Controllers Layer               │
│  (HTTP Request/Response Handling)       │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│          Services Layer                 │
│     (Business Logic & Data              │
│      Transformation)                    │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│       Repository Layer                  │
│  (External API Communication &          │
│   Caching)                              │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│           PokeAPI                       │
│    (External Data Source)               │
└─────────────────────────────────────────┘
```

### Design Patterns

- **Dependency Injection**: All dependencies injected through constructors
- **Repository Pattern**: Data access abstracted through `IPokeApiClient`
- **Service Pattern**: Business logic encapsulated in `IPokemonService`
- **DTO Pattern**: Separation between API models and domain models
- **Middleware Pattern**: Global exception handling

##  Performance Optimization

### Caching Strategy

The application implements a cache-first approach using `IMemoryCache`:

| Resource Type | Cache Duration | Cache Key Pattern |
|--------------|----------------|-------------------|
| Pokémon Data | 1 hour | `pokemon_{name/id}` |
| Type Data | 2 hours | `type_{typename}` |
| Ability Data | 2 hours | `ability_{abilityname}` |
| List Data | 30 minutes | `pokemon_list_{limit}_{offset}` |

### Parallel Processing

The `GetPokemonDetailsAsync` method uses `Task.WhenAll` for parallel fetching, significantly improving performance for batch operations like filtering.

##  Error Handling

All errors are handled by a global exception handler middleware that returns consistent error responses:

```json
{
  "error": "Error message",
  "statusCode": 500,
  "timestamp": "2025-10-04T12:00:00Z"
}
```

**HTTP Status Codes:**
- `200 OK`: Successful request
- `400 Bad Request`: Invalid input
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

## 🧪 Testing with Swagger

1. Run the application
2. Navigate to `https://localhost:7087/swagger`
3. Expand any endpoint
4. Click "Try it out"
5. Enter parameters
6. Click "Execute"
7. View the response

### Recommended Test Scenarios

1. **Get Pokémon**: Try `pikachu`, `25`, or `charizard`
2. **Search**: Use partial names like `char` or `pika`
3. **Compare**: Compare type-advantaged matchups (e.g., Pikachu vs Gyarados)
4. **Filter**: Try `MinTotal=500&Type=fire&MinAttack=80`
5. **Cache Testing**: Run the same query twice and check console logs for cache hits

##  Known Limitations

- **Filter Endpoint Timeout**: The `/filter` endpoint may timeout with very broad criteria due to the need to fetch all Pokémon data. Use specific filters to improve performance.
- **Rate Limiting**: No rate limiting implemented. Relies on PokeAPI's rate limits.
- **Authentication**: No authentication required (public API).

##  Future Enhancements

- Unit and integration tests
- Database caching (Redis/SQL Server)
- Rate limiting middleware
- User authentication and favorites system
- Move effectiveness calculation
- Evolution chain tracking
- GraphQL endpoint
- Docker containerization
- CI/CD pipeline

##  Project Structure

```
PokemonAPI/
├── Controllers/
│   └── PokemonController.cs
├── Services/
│   ├── IPokemonService.cs
│   └── PokemonService.cs
├── Repositories/
│   ├── IPokeApiClient.cs
│   └── PokeApiClient.cs
├── Models/
│   ├── DTOs/
│   │   ├── PokemonDetail.cs
│   │   ├── ComparisonResult.cs
│   │   └── FilterRequest.cs
│   └── ApiResponses/
│       ├── PokemonApiResponse.cs
│       ├── TypeResponse.cs
│       └── AbilityResponse.cs
├── Middleware/
│   └── GlobalExceptionHandler.cs
└── Program.cs
```

##  Contributing

This is a demonstration project for a coding assessment. Contributions are not currently accepted.

##  License

This project is created for educational and assessment purposes.

##  Contact

For questions about this project, please contact the repository owner.

---

**Note**: This project uses the free PokeAPI service. Please be respectful of their resources and implement appropriate caching in production environments.
