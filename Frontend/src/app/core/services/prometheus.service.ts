import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { API_CONFIG } from '../api.config';

export interface PrometheusResult {
  metric: Record<string, string>;
  value: [number, string]; // [timestamp, value]
}

export interface PrometheusRangeResult {
  metric: Record<string, string>;
  values: [number, string][];
}

export interface AlgorithmMetricData {
  algorithm: string;
  displayName: string;
  totalExecutions: number;
  avgDurationNs: number;
  p50DurationNs: number;
  p99DurationNs: number;
  avgComparisons: number;
  totalOccurrences: number;
}

/**
 * PrometheusService — consulta métricas reais do Prometheus.
 *
 * Usa a Prometheus HTTP API (query instantânea e range queries).
 * Se o Prometheus não estiver disponível, retorna dados vazios
 * sem quebrar a aplicação.
 */
@Injectable({ providedIn: 'root' })
export class PrometheusService {
  private readonly base = `${API_CONFIG.PROMETHEUS_URL}/api/v1`;

  constructor(private http: HttpClient) {}

  // ─── Query instantânea ────────────────────────────────────────────────────

  query(promql: string): Observable<PrometheusResult[]> {
    return this.http
      .get<any>(`${this.base}/query`, { params: { query: promql } })
      .pipe(
        map(r => r.data?.result ?? []),
        catchError(() => of([]))
      );
  }

  // ─── Range query (série temporal) ────────────────────────────────────────

  queryRange(promql: string, start: string, end: string, step: string): Observable<PrometheusRangeResult[]> {
    return this.http
      .get<any>(`${this.base}/query_range`, {
        params: { query: promql, start, end, step }
      })
      .pipe(
        map(r => r.data?.result ?? []),
        catchError(() => of([]))
      );
  }

  // ─── Helpers pré-montados ────────────────────────────────────────────────

  getTotalExecutions(): Observable<PrometheusResult[]> {
    return this.query('sum by (algorithm_display) (stringsearch_executions_total)');
  }

  getAvgDurationNs(): Observable<PrometheusResult[]> {
    return this.query(
      'rate(stringsearch_execution_duration_ns_sum[5m]) / rate(stringsearch_execution_duration_ns_count[5m])'
    );
  }

  getP50DurationNs(): Observable<PrometheusResult[]> {
    return this.query(
      'histogram_quantile(0.5, rate(stringsearch_execution_duration_ns_bucket[5m]))'
    );
  }

  getP99DurationNs(): Observable<PrometheusResult[]> {
    return this.query(
      'histogram_quantile(0.99, rate(stringsearch_execution_duration_ns_bucket[5m]))'
    );
  }

  getAvgComparisons(): Observable<PrometheusResult[]> {
    return this.query(
      'rate(stringsearch_comparisons_total_sum[5m]) / rate(stringsearch_comparisons_total_count[5m])'
    );
  }

  getTotalOccurrences(): Observable<PrometheusResult[]> {
    return this.query('sum by (algorithm_display) (stringsearch_occurrences_total)');
  }

  getExecutionsOverTime(): Observable<PrometheusRangeResult[]> {
    const now   = Math.floor(Date.now() / 1000);
    const start = String(now - 3600); // última hora
    const end   = String(now);
    return this.queryRange(
      'rate(stringsearch_executions_total[1m])',
      start, end, '15'
    );
  }

  parseValue(result: PrometheusResult): number {
    return parseFloat(result.value[1]) || 0;
  }
}
