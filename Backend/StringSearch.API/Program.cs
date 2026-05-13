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
        Description =
            "API para comparação de algoritmos de busca de padrões em strings.\n" +
            "Padrões arquiteturais: Strategy + Facade.\n" +
            "DI via IEnumerable<ISearchStrategy> — adicionar algoritmos sem alterar o serviço."
    });
});

// ─── Strategy Pattern ─────────────────────────────────────────────────────────
// Cada algoritmo é registrado como ISearchStrategy.
// O SearchService recebe IEnumerable<ISearchStrategy> via DI:
//   - Nenhum concreto é referenciado fora desta seção.
//   - Para adicionar um novo algoritmo, basta registrá-lo aqui.
builder.Services.AddSingleton<ISearchStrategy, NaiveSearchStrategy>();
builder.Services.AddSingleton<ISearchStrategy, RabinKarpSearchStrategy>();
builder.Services.AddSingleton<ISearchStrategy, KMPSearchStrategy>();
builder.Services.AddSingleton<ISearchStrategy, BoyerMooreSearchStrategy>();

// ─── Service ──────────────────────────────────────────────────────────────────
// ISearchService para inversão de dependência na Facade.
builder.Services.AddSingleton<ISearchService, SearchService>();

// ─── Facade ───────────────────────────────────────────────────────────────────
// Ponto único de entrada para o Controller.
builder.Services.AddSingleton<ISearchFacade, SearchFacade>();

// ─── Pipeline ─────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "String Search API v1"));
}

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

app.Run();
