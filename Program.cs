
using PokemonAPI.Middleware;
using PokemonAPI.Repositories;
using PokemonAPI.Services;

namespace PokemonAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add memory cache
            builder.Services.AddMemoryCache();

            // Register HttpClient for PokeAPI
            builder.Services.AddHttpClient<IPokeApiClient, PokeApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Register application services
            builder.Services.AddScoped<IPokemonService, PokemonService>();

            // Add CORS for React frontend
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReact", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<GlobalExceptionHandler>();
            app.UseCors("AllowReact");
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
