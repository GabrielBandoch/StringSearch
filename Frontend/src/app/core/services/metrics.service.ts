import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { AlgorithmId, AlgorithmMetric, MetricsSummary, SearchResult } from '../models/search.models';

/**
 * MetricsService — coleta métricas de execução localmente no frontend.
 *
 * Objetivo da Parte 1:
 *   Simular observabilidade sem OpenTelemetry ainda.
 *   Na Parte 2, este serviço será complementado com dados reais
 *   exportados pelo backend via Prometheus/OTLP.
 *
 * Responsabilidades:
 *   - Acumular métricas por algoritmo a cada execução.
 *   - Expor um Observable de MetricsSummary para o dashboard.
 *   - Persistir em sessionStorage para sobreviver a navegações internas.
 */
@Injectable({ providedIn: 'root' })
export class MetricsService {
  private readonly STORAGE_KEY = 'stringsearch_metrics';
  private metricsMap = new Map<string, AlgorithmMetric>();

  private summary$ = new BehaviorSubject<MetricsSummary>(this.buildSummary());

  readonly summary = this.summary$.asObservable();

  constructor() {
    this.loadFromStorage();
  }

  // ─── Registro ────────────────────────────────────────────────────────────

  record(result: SearchResult): void {
    const existing = this.metricsMap.get(result.algorithm);

    if (existing) {
      existing.totalExecutions++;
      existing.totalComparisons += result.totalComparisons;
      existing.totalTimeNs += result.executionTimeNs;
      existing.avgTimeNs = existing.totalTimeNs / existing.totalExecutions;
      existing.avgComparisons = existing.totalComparisons / existing.totalExecutions;
      existing.lastExecutedAt = new Date();
    } else {
      this.metricsMap.set(result.algorithm, {
        algorithmId: result.algorithm,
        displayName: result.algorithmDisplayName,
        totalExecutions: 1,
        totalComparisons: result.totalComparisons,
        totalTimeNs: result.executionTimeNs,
        avgTimeNs: result.executionTimeNs,
        avgComparisons: result.totalComparisons,
        lastExecutedAt: new Date(),
      });
    }

    this.saveToStorage();
    this.summary$.next(this.buildSummary());
  }

  recordBatch(results: SearchResult[]): void {
    results.forEach(r => this.record(r));
  }

  reset(): void {
    this.metricsMap.clear();
    sessionStorage.removeItem(this.STORAGE_KEY);
    this.summary$.next(this.buildSummary());
  }

  // ─── Internos ────────────────────────────────────────────────────────────

  private buildSummary(): MetricsSummary {
    const metrics = Array.from(this.metricsMap.values());
    const totalSearches = metrics.reduce((acc, m) => acc + m.totalExecutions, 0);

    let fastestAlgorithm: string | null = null;
    let mostUsedAlgorithm: string | null = null;

    if (metrics.length > 0) {
      fastestAlgorithm = metrics.reduce((a, b) =>
        a.avgTimeNs < b.avgTimeNs ? a : b
      ).displayName;
      mostUsedAlgorithm = metrics.reduce((a, b) =>
        a.totalExecutions > b.totalExecutions ? a : b
      ).displayName;
    }

    return { totalSearches, metrics, fastestAlgorithm, mostUsedAlgorithm };
  }

  private saveToStorage(): void {
    try {
      const data = Array.from(this.metricsMap.entries());
      sessionStorage.setItem(this.STORAGE_KEY, JSON.stringify(data));
    } catch { /* sessionStorage indisponível */ }
  }

  private loadFromStorage(): void {
    try {
      const raw = sessionStorage.getItem(this.STORAGE_KEY);
      if (!raw) return;
      const data: [string, AlgorithmMetric][] = JSON.parse(raw);
      data.forEach(([key, value]) => {
        value.lastExecutedAt = new Date(value.lastExecutedAt);
        this.metricsMap.set(key, value);
      });
      this.summary$.next(this.buildSummary());
    } catch { /* dados corrompidos, ignora */ }
  }
}
