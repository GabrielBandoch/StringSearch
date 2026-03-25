using StringSearch.API.Facade;
using StringSearch.API.Services;
using StringSearch.API.Strategies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "String Search API",
        Version = "v1",
        Description = "API para comparação de algoritmos de busca de padrões em strings. " +
                      "Padrões arquiteturais: Strategy + Facade."
    });
});

builder.Services.AddSingleton<ISearchStrategy, NaiveSearchStrategy>();
builder.Services.AddSingleton<ISearchStrategy, RabinKarpSearchStrategy>();
builder.Services.AddSingleton<ISearchStrategy, KMPSearchStrategy>();
builder.Services.AddSingleton<ISearchStrategy, BoyerMooreSearchStrategy>();

builder.Services.AddSingleton<SearchService>();

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
