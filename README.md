# StringSearch — Parte 2: Observabilidade com OpenTelemetry

## Stack de Observabilidade

| Sinal   | Ferramenta              | Porta  |
|---------|-------------------------|--------|
| Traces  | Jaeger                  | 16686  |
| Métricas| Prometheus              | 9090   |
| Dashboard | Grafana               | 3000   |
| Logs    | Console / OTLP          | —      |

---

## Como executar

### 1. Subir a stack de observabilidade (Docker)

```bash
docker-compose up -d
```

Aguarde ~10s para todos os containers iniciarem.

### 2. Backend (.NET 8)

```bash
cd Backend/StringSearch.API
dotnet restore
dotnet run
```

O backend sobe em `http://localhost:5000`.
O endpoint de métricas Prometheus fica em `http://localhost:5000/metrics`.

### 3. Frontend (Angular)

```bash
cd Frontend
npm install
npm start
```

Acesse `http://localhost:4200`.

---

## Instrumentação OpenTelemetry

### Traces → Jaeger

Cada requisição gera um trace com spans hierárquicos:

```
SearchFacade.Execute           ← span raiz (HTTP request)
  └─ SearchService.Search      ← span de serviço
       └─ NaiveSearchStrategy  ← (instrumentado via ActivitySource)
```

**Atributos dos spans:**
- `search.algorithm` — id do algoritmo
- `search.pattern` — padrão buscado
- `search.text_length` — tamanho do texto
- `search.occurrences` — total de ocorrências
- `search.comparisons` — comparações realizadas
- `search.duration_ns` — tempo em nanosegundos

Acesse os traces em: **http://localhost:16686**

### Métricas → Prometheus → Grafana

| Métrica | Tipo | Descrição |
|---------|------|-----------|
| `stringsearch_executions_total` | Counter | Total de buscas por algoritmo |
| `stringsearch_execution_duration_ns` | Histogram | Tempo de execução (ns) — permite p50/p99 |
| `stringsearch_comparisons_total` | Histogram | Comparações por execução |
| `stringsearch_occurrences_total` | Counter | Ocorrências encontradas |
| `stringsearch_text_last_length` | Gauge | Tamanho do último texto processado |

**Prometheus:** http://localhost:9090
**Grafana:** http://localhost:3000 (admin/admin)

O dashboard `StringSearch — Observabilidade` é provisionado automaticamente.

### Logs estruturados

Os logs usam propriedades nomeadas (não interpolação de string):

```csharp
// ✅ Correto — propriedade indexada
_logger.LogInformation(
    "[Search] algorithm={Algorithm} duration={DurationNs}ns",
    algorithm, durationNs);

// ❌ Errado — não permite filtragem
_logger.LogInformation($"algorithm: {algorithm}");
```

---

## Arquitetura

```
SearchController
    ↓ ISearchFacade
SearchFacade  ←── Span raiz + log de contexto
    ↓ ISearchService
SearchService ←── Spans filhos + métricas OTel + logs estruturados
    ↓ IEnumerable<ISearchStrategy>
[Naive | KMP | BoyerMoore | RabinKarp]
```

---

## Estrutura de arquivos novos/modificados

```
Parte 2/
├── docker-compose.yml
├── observability/
│   ├── prometheus.yml
│   └── grafana/
│       └── provisioning/
│           ├── datasources/datasources.yml
│           └── dashboards/
│               ├── dashboards.yml
│               └── stringsearch.json      ← dashboard pré-configurado
└── Backend/StringSearch.API/
    ├── StringSearch.API.csproj            ← +OpenTelemetry packages
    ├── Program.cs                         ← configuração OTel completa
    ├── Observability/
    │   └── SearchTelemetry.cs             ← ActivitySource + Meter centralizados
    ├── Services/
    │   └── SearchService.cs               ← instrumentado com traces e métricas
    └── Facade/
        └── SearchFacade.cs                ← spans raiz + RecordException
```
