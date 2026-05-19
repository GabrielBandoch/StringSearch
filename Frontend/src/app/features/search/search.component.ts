import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, finalize, forkJoin, interval } from 'rxjs';
import { startWith, switchMap } from 'rxjs/operators';
import { SearchService } from '../../core/services/search.service';
import { MetricsService } from '../../core/services/metrics.service';
import { PrometheusService, AlgorithmMetricData } from '../../core/services/prometheus.service';
import { API_CONFIG } from '../../core/api.config';
import {
  AlgorithmId,
  AlgorithmInfo,
  FileContent,
  MetricsSummary,
  MultiFileSearchResult,
  SearchResult,
} from '../../core/models/search.models';

@Component({
  selector: 'app-search',
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.scss'],
})
export class SearchComponent implements OnInit, OnDestroy {

  // ─── Config ───────────────────────────────────────────────────────────────
  readonly jaegerUrl     = API_CONFIG.JAEGER_URL;
  readonly prometheusUrl = API_CONFIG.PROMETHEUS_URL;
  readonly grafanaUrl    = 'http://localhost:3000';

  // ─── Estado de busca ─────────────────────────────────────────────────────
  algorithms: AlgorithmInfo[] = [];
  selectedAlgorithm: AlgorithmId = 'kmp';
  pattern = '';
  textInput = '';
  uploadedFiles: FileContent[] = [];

  isLoading = false;
  isCompareLoading = false;
  errorMessage = '';

  searchResult: SearchResult | null = null;
  compareResults: SearchResult[] = [];
  multiFileResult: MultiFileSearchResult | null = null;

  // ─── Métricas locais (fallback quando Prometheus indisponível) ───────────
  metricsSummary: MetricsSummary | null = null;

  // ─── Métricas reais do Prometheus ────────────────────────────────────────
  prometheusMetrics: AlgorithmMetricData[] = [];
  prometheusAvailable = false;
  prometheusLoading = false;
  lastPrometheusRefresh: Date | null = null;

  activeTab: 'search' | 'compare' | 'files' | 'dashboard' = 'search';

  private destroy$ = new Subject<void>();

  constructor(
    private searchService: SearchService,
    private metricsService: MetricsService,
    private prometheusService: PrometheusService
  ) {}

  // ─── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadAlgorithms();

    this.metricsService.summary
      .pipe(takeUntil(this.destroy$))
      .subscribe(summary => (this.metricsSummary = summary));

    // Atualiza métricas do Prometheus a cada 15s quando aba Dashboard ativa
    interval(15_000)
      .pipe(startWith(0), takeUntil(this.destroy$))
      .subscribe(() => {
        if (this.activeTab === 'dashboard') {
          this.loadPrometheusMetrics();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Algoritmos ───────────────────────────────────────────────────────────

  loadAlgorithms(): void {
    this.searchService
      .getAlgorithms()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next:  data  => (this.algorithms = data),
        error: ()    => (this.errorMessage = 'Não foi possível carregar os algoritmos. Backend rodando?'),
      });
  }

  // ─── Busca simples ────────────────────────────────────────────────────────

  onExecute(): void {
    if (!this.isValid()) return;
    this.isLoading = true;
    this.errorMessage = '';
    this.searchResult = null;

    this.searchService
      .execute({ text: this.textInput, pattern: this.pattern, algorithm: this.selectedAlgorithm })
      .pipe(takeUntil(this.destroy$), finalize(() => (this.isLoading = false)))
      .subscribe({
        next:  result => (this.searchResult = result),
        error: err    => (this.errorMessage = err?.error ?? 'Erro ao executar busca.'),
      });
  }

  // ─── Comparar todos ───────────────────────────────────────────────────────

  onCompareAll(): void {
    if (!this.isValid()) return;
    this.isCompareLoading = true;
    this.errorMessage = '';
    this.compareResults = [];

    this.searchService
      .compareAll({ text: this.textInput, pattern: this.pattern, algorithm: this.selectedAlgorithm })
      .pipe(takeUntil(this.destroy$), finalize(() => (this.isCompareLoading = false)))
      .subscribe({
        next:  results => { this.compareResults = results; this.activeTab = 'compare'; },
        error: err     => (this.errorMessage = err?.error ?? 'Erro na comparação.'),
      });
  }

  // ─── Multi-file ───────────────────────────────────────────────────────────

  onSearchFiles(): void {
    if (!this.uploadedFiles.length || !this.pattern.trim()) return;
    this.isLoading = true;
    this.errorMessage = '';
    this.multiFileResult = null;

    this.searchService
      .multiFile({ files: this.uploadedFiles, pattern: this.pattern, algorithm: this.selectedAlgorithm })
      .pipe(takeUntil(this.destroy$), finalize(() => (this.isLoading = false)))
      .subscribe({
        next:  result => { this.multiFileResult = result; this.activeTab = 'files'; },
        error: err    => (this.errorMessage = err?.error ?? 'Erro na busca em arquivos.'),
      });
  }

  // ─── Upload ───────────────────────────────────────────────────────────────

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    this.uploadedFiles = [];
    Array.from(input.files).forEach(file => {
      const reader = new FileReader();
      reader.onload = e => {
        this.uploadedFiles.push({ fileName: file.name, content: e.target?.result as string });
        if (this.uploadedFiles.length === 1) this.textInput = this.uploadedFiles[0].content.slice(0, 2000);
      };
      reader.readAsText(file);
    });
  }

  // ─── Prometheus ───────────────────────────────────────────────────────────

  loadPrometheusMetrics(): void {
    this.prometheusLoading = true;

    forkJoin({
      executions:   this.prometheusService.getTotalExecutions(),
      avgDuration:  this.prometheusService.getAvgDurationNs(),
      p50:          this.prometheusService.getP50DurationNs(),
      p99:          this.prometheusService.getP99DurationNs(),
      comparisons:  this.prometheusService.getAvgComparisons(),
      occurrences:  this.prometheusService.getTotalOccurrences(),
    })
    .pipe(finalize(() => (this.prometheusLoading = false)))
    .subscribe({
      next: data => {
        this.prometheusAvailable = true;
        this.lastPrometheusRefresh = new Date();
        this.prometheusMetrics = this.mergePrometheusData(data);
      },
      error: () => {
        this.prometheusAvailable = false;
      }
    });
  }

  private mergePrometheusData(data: any): AlgorithmMetricData[] {
    const map = new Map<string, AlgorithmMetricData>();

    const upsert = (metric: Record<string, string>) => {
      const key = metric['algorithm'] ?? metric['algorithm_display'] ?? 'unknown';
      if (!map.has(key)) {
        map.set(key, {
          algorithm:      key,
          displayName:    metric['algorithm_display'] ?? key,
          totalExecutions: 0,
          avgDurationNs:  0,
          p50DurationNs:  0,
          p99DurationNs:  0,
          avgComparisons: 0,
          totalOccurrences: 0,
        });
      }
      return map.get(key)!;
    };

    data.executions.forEach((r: any) => {
      upsert(r.metric).totalExecutions = Math.round(this.prometheusService.parseValue(r));
    });
    data.avgDuration.forEach((r: any) => {
      upsert(r.metric).avgDurationNs = this.prometheusService.parseValue(r);
    });
    data.p50.forEach((r: any) => {
      upsert(r.metric).p50DurationNs = this.prometheusService.parseValue(r);
    });
    data.p99.forEach((r: any) => {
      upsert(r.metric).p99DurationNs = this.prometheusService.parseValue(r);
    });
    data.comparisons.forEach((r: any) => {
      upsert(r.metric).avgComparisons = this.prometheusService.parseValue(r);
    });
    data.occurrences.forEach((r: any) => {
      upsert(r.metric).totalOccurrences = Math.round(this.prometheusService.parseValue(r));
    });

    return Array.from(map.values()).filter(m => m.totalExecutions > 0 || m.avgDurationNs > 0);
  }

  // ─── Tab ─────────────────────────────────────────────────────────────────

  setActiveTab(tab: typeof this.activeTab): void {
    this.activeTab = tab;
    if (tab === 'dashboard') this.loadPrometheusMetrics();
  }

  resetMetrics(): void {
    this.metricsService.reset();
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  isValid(): boolean { return !!this.textInput.trim() && !!this.pattern.trim(); }

  getAlgorithmInfo(): AlgorithmInfo | undefined {
    return this.algorithms.find(a => a.id === this.selectedAlgorithm);
  }

  getCompareWinner(): SearchResult | null {
    if (!this.compareResults.length) return null;
    return this.compareResults.reduce((a, b) => a.totalComparisons < b.totalComparisons ? a : b);
  }

  getMaxComparisons(): number {
    return Math.max(...(this.compareResults.map(r => r.totalComparisons) || [1]), 1);
  }

  getMaxTimeNs(): number {
    return Math.max(...(this.compareResults.map(r => r.executionTimeNs) || [1]), 1);
  }

  getBarWidth(value: number, max: number): string {
    return max > 0 ? `${Math.max((value / max) * 100, 2).toFixed(1)}%` : '2%';
  }

  // Barras do dashboard local
  getMaxAvgTime(): number {
    const src = this.prometheusAvailable
      ? this.prometheusMetrics.map(m => m.avgDurationNs)
      : (this.metricsSummary?.metrics.map(m => m.avgTimeNs) ?? []);
    return Math.max(...src, 1);
  }

  getMaxExecutions(): number {
    const src = this.prometheusAvailable
      ? this.prometheusMetrics.map(m => m.totalExecutions)
      : (this.metricsSummary?.metrics.map(m => m.totalExecutions) ?? []);
    return Math.max(...src, 1);
  }

  formatNs(ns: number): string {
    if (!ns || isNaN(ns)) return '—';
    if (ns < 1_000)       return `${ns.toFixed(0)} ns`;
    if (ns < 1_000_000)   return `${(ns / 1_000).toFixed(1)} µs`;
    return `${(ns / 1_000_000).toFixed(2)} ms`;
  }

  // Métricas unificadas (Prometheus ou local)
  get displayMetrics() {
    if (this.prometheusAvailable && this.prometheusMetrics.length) {
      return this.prometheusMetrics.map(m => ({
        displayName:     m.displayName,
        totalExecutions: m.totalExecutions,
        avgTimeNs:       m.avgDurationNs,
        avgComparisons:  m.avgComparisons,
        totalComparisons: 0,
        lastExecutedAt:  this.lastPrometheusRefresh ?? new Date(),
      }));
    }
    return this.metricsSummary?.metrics ?? [];
  }

  get totalSearches(): number {
    if (this.prometheusAvailable && this.prometheusMetrics.length) {
      return this.prometheusMetrics.reduce((a, m) => a + m.totalExecutions, 0);
    }
    return this.metricsSummary?.totalSearches ?? 0;
  }

  get fastestAlgorithm(): string | null {
    if (this.prometheusAvailable && this.prometheusMetrics.length) {
      return this.prometheusMetrics
        .filter(m => m.avgDurationNs > 0)
        .reduce((a, b) => a.avgDurationNs < b.avgDurationNs ? a : b, this.prometheusMetrics[0])
        ?.displayName ?? null;
    }
    return this.metricsSummary?.fastestAlgorithm ?? null;
  }

  get mostUsedAlgorithm(): string | null {
    if (this.prometheusAvailable && this.prometheusMetrics.length) {
      return this.prometheusMetrics
        .reduce((a, b) => a.totalExecutions > b.totalExecutions ? a : b, this.prometheusMetrics[0])
        ?.displayName ?? null;
    }
    return this.metricsSummary?.mostUsedAlgorithm ?? null;
  }
}
