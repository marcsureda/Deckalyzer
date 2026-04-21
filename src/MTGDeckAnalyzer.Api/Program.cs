using MTGDeckAnalyzer.Application.Services;
using MTGDeckAnalyzer.Infrastructure.Archidekt;
using MTGDeckAnalyzer.Infrastructure.Scryfall;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS for React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Infrastructure
builder.Services.AddMemoryCache();

// HTTP clients — typed registrations also satisfy the concrete type for DI
builder.Services.AddHttpClient<IScryfallService, ScryfallService>();
builder.Services.AddHttpClient<IArchidektService, ArchidektService>();

// Domain services
builder.Services.AddSingleton<IDeckParser, DeckParserService>();
builder.Services.AddSingleton<IPowerLevelAnalyzer, PowerLevelAnalyzer>();

// Application services
builder.Services.AddScoped<IDeckAnalysisService, DeckAnalysisService>();
builder.Services.AddScoped<IPreconService, PreconService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactDev");
app.MapControllers();

app.Run();
