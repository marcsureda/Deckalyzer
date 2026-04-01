using MTGDeckAnalyzer.Api.Services;

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

// Register services
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ScryfallService>();
builder.Services.AddHttpClient<ArchidektService>();
builder.Services.AddSingleton<DeckParserService>();
builder.Services.AddSingleton<PowerLevelAnalyzer>();
builder.Services.AddScoped<DeckAnalysisService>();
builder.Services.AddScoped<IArchidektService, ArchidektService>();
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
