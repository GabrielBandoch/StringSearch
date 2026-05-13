import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, finalize } from 'rxjs';
import { SearchService } from '../../core/services/search.service';
import { MetricsService } from '../../core/services/metrics.service';
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

  // ─── State ────────────────────────────────────────────────────────────────
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

  // ─── Métricas (dashboard Parte 1) ────────────────────────────────────────
  metricsSummary: MetricsSummary | null = null;

  activeTab: 'search' | 'compare' | 'files' | 'dashboard' = 'search';

  private destroy$ = new Subject<void>();

  constructor(
    private searchService: SearchService,
    private metricsService: MetricsService
  ) {}

  // ─── Lifecycle ────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.loadAlgorithms();
    this.metricsService.summary
      .pipe(takeUntil(this.destroy$))
      .subscribe(summary => (this.metricsSummary = summary));
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
        next: (data) => (this.algorithms = data),
        error: () =>
          (this.errorMessage =
            'Não foi possível carregar os algoritmos. Verifique se o backend está rodando.'),
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
        next: (result) => (this.searchResult = result),
        error: (err) => (this.errorMessage = err?.error ?? 'Erro ao executar busca.'),
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
        next: (results) => {
          this.compareResults = results;
          this.activeTab = 'compare';
        },
        error: (err) => (this.errorMessage = err?.error ?? 'Erro na comparação.'),
      });
  }

  // ─── Multi-file ───────────────────────────────────────────────────────────
  onSearchFiles(): void {
    if (this.uploadedFiles.length === 0 || !this.pattern.trim()) return;
    this.isLoading = true;
    this.errorMessage = '';
    this.multiFileResult = null;

    this.searchService
      .multiFile({ files: this.uploadedFiles, pattern: this.pattern, algorithm: this.selectedAlgorithm })
      .pipe(takeUntil(this.destroy$), finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (result) => {
          this.multiFileResult = result;
          this.activeTab = 'files';
        },
        error: (err) => (this.errorMessage = err?.error ?? 'Erro na busca em arquivos.'),
      });
  }

  // ─── Upload ───────────────────────────────────────────────────────────────
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    this.uploadedFiles = [];
    Array.from(input.files).forEach((file) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        this.uploadedFiles.push({ fileName: file.name, content: e.target?.result as string });
        if (this.uploadedFiles.length === 1) {
          this.textInput = this.uploadedFiles[0].content.slice(0, 2000);
        }
      };
      reader.readAsText(file);
    });
  }

  // ─── Dashboard ────────────────────────────────────────────────────────────
  resetMetrics(): void {
    this.metricsService.reset();
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────
  isValid(): boolean {
    return !!this.textInput.trim() && !!this.pattern.trim();
  }

  getAlgorithmInfo(): AlgorithmInfo | undefined {
    return this.algorithms.find((a) => a.id === this.selectedAlgorithm);
  }

  getCompareWinner(): SearchResult | null {
    if (!this.compareResults.length) return null;
    return this.compareResults.reduce((a, b) =>
      a.totalComparisons < b.totalComparisons ? a : b
    );
  }

  setActiveTab(tab: typeof this.activeTab): void {
    this.activeTab = tab;
  }

  // ─── Barras do gráfico de comparação ─────────────────────────────────────
  getMaxComparisons(): number {
    if (!this.compareResults.length) return 1;
    return Math.max(...this.compareResults.map(r => r.totalComparisons), 1);
  }

  getMaxTimeNs(): number {
    if (!this.compareResults.length) return 1;
    return Math.max(...this.compareResults.map(r => r.executionTimeNs), 1);
  }

  getBarWidth(value: number, max: number): string {
    return max > 0 ? `${Math.max((value / max) * 100, 2).toFixed(1)}%` : '2%';
  }

  // ─── Dashboard: barras de métricas ───────────────────────────────────────
  getMaxAvgTime(): number {
    if (!this.metricsSummary?.metrics.length) return 1;
    return Math.max(...this.metricsSummary.metrics.map(m => m.avgTimeNs), 1);
  }

  getMaxExecutions(): number {
    if (!this.metricsSummary?.metrics.length) return 1;
    return Math.max(...this.metricsSummary.metrics.map(m => m.totalExecutions), 1);
  }

  formatNs(ns: number): string {
    if (ns < 1_000) return `${ns.toFixed(0)} ns`;
    if (ns < 1_000_000) return `${(ns / 1_000).toFixed(1)} µs`;
    return `${(ns / 1_000_000).toFixed(2)} ms`;
  }
}
