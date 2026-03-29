using StringSearch.API.Facade;
using StringSearch.API.Services;
using StringSearch.API.Strategies;

var builder = WebApplication.CreateBuilder(args);

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ─── Controllers + Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "String Search API",
        Version = "v1",
        Description = "API para comparação de algoritmos de busca de padrões em strings. " +
                      "Padrões arquiteturais: Strategy + Facade. Despacho via switch expression."
    });
});

// ─── Strategy: registra cada algoritmo individualmente ───────────────────────
builder.Services.AddSingleton<NaiveSearchStrategy>();
builder.Services.AddSingleton<RabinKarpSearchStrategy>();
builder.Services.AddSingleton<KMPSearchStrategy>();
builder.Services.AddSingleton<BoyerMooreSearchStrategy>();

// ─── Service: recebe as strategies individualmente via DI ─────────────────────
builder.Services.AddSingleton<SearchService>();

// ─── Facade: ponto único de entrada para o Controller ────────────────────────
builder.Services.AddSingleton<ISearchFacade, SearchFacade>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "String Search API v1"));
}

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

app.Run();
