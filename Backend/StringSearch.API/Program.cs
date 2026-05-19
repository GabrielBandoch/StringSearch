using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StringSearch.API.Facade;
using StringSearch.API.Observability;
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
        Title       = "String Search API",
        Version     = "v1",
        Description = "Comparação de algoritmos de busca com OpenTelemetry (Traces + Métricas + Logs)."
    });
});

// ─── Resource OpenTelemetry ───────────────────────────────────────────────────
// Identifica o serviço em todos os sinais (traces, métricas, logs).
var otelResource = ResourceBuilder.CreateDefault()
    .AddService(
        serviceName:    SearchTelemetry.ServiceName,
        serviceVersion: SearchTelemetry.ServiceVersion);

// ─── TRACES ───────────────────────────────────────────────────────────────────
// Exporta para Jaeger (http://localhost:14268/api/traces)
// e também para o console (para debug local sem Docker).
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(otelResource)
        .AddSource(SearchTelemetry.ServiceName)      // nosso ActivitySource
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.RecordException = true;
            // Não rastreia o endpoint de métricas do Prometheus
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/metrics");
        })
        .AddHttpClientInstrumentation()
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri(
        builder.Configuration["Otlp:Endpoint"]
        ?? "http://localhost:4317");
})
        .AddConsoleExporter()                        // visível no terminal durante dev
    );

// ─── MÉTRICAS ─────────────────────────────────────────────────────────────────
// Expõe endpoint /metrics no formato Prometheus (scraping a cada 15s).
// Também coleta métricas de runtime .NET (GC, threads, alocações).
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(otelResource)
        .AddMeter(SearchTelemetry.ServiceName)       // nosso Meter
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter()                     // → /metrics
    );

// ─── LOGS ─────────────────────────────────────────────────────────────────────
// Enriquece o ILogger padrão com exportação OTLP (OpenTelemetry Collector).
// Em dev, os logs continuam aparecendo no console normalmente.
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(otelResource);
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes           = true;
    logging.ParseStateValues        = true;
    logging.AddConsoleExporter();
    // Para produção / Grafana Loki, trocar por:
    // logging.AddOtlpExporter(o => o.Endpoint = new Uri("http://localhost:4317"));
});

// ─── Strategy Pattern ─────────────────────────────────────────────────────────
builder.Services.AddSingleton<ISearchStrategy, NaiveSearchStrategy>();
builder.Services.AddSingleton<ISearchStrategy, RabinKarpSearchStrategy>();
builder.Services.AddSingleton<ISearchStrategy, KMPSearchStrategy>();
builder.Services.AddSingleton<ISearchStrategy, BoyerMooreSearchStrategy>();

// ─── Services & Facade ────────────────────────────────────────────────────────
builder.Services.AddSingleton<ISearchService, SearchService>();
builder.Services.AddSingleton<ISearchFacade,  SearchFacade>();

// ─── Pipeline ─────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "String Search API v1"));
}

// Endpoint /metrics para o Prometheus fazer scraping
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

app.Run();
