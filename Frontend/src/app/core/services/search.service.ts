import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { API_CONFIG } from '../api.config';
import { MetricsService } from './metrics.service';
import {
  SearchCommand,
  MultiFileSearchCommand,
  SearchResult,
  MultiFileSearchResult,
  AlgorithmInfo,
} from '../models/search.models';

/**
 * SearchService — serviço central de comunicação com a API.
 *
 * Parte 1: intercepta respostas para alimentar o MetricsService local.
 * Parte 2: será complementado com headers de trace (W3C TraceContext).
 */
@Injectable({ providedIn: 'root' })
export class SearchService {
  private readonly base = API_CONFIG.BASE_URL;
  private readonly ep = API_CONFIG.ENDPOINTS.SEARCH;

  constructor(
    private http: HttpClient,
    private metrics: MetricsService
  ) {}

  // ─── POST /search/execute ─────────────────────────────────────────────────
  execute(command: SearchCommand): Observable<SearchResult> {
    return this.http
      .post<SearchResult>(`${this.base}${this.ep.EXECUTE}`, command)
      .pipe(tap(result => this.metrics.record(result)));
  }

  // ─── POST /search/multi-file ──────────────────────────────────────────────
  multiFile(command: MultiFileSearchCommand): Observable<MultiFileSearchResult> {
    return this.http
      .post<MultiFileSearchResult>(`${this.base}${this.ep.MULTI_FILE}`, command)
      .pipe(
        tap(result =>
          result.fileResults.forEach(fr => this.metrics.record(fr.result))
        )
      );
  }

  // ─── POST /search/compare-all ─────────────────────────────────────────────
  compareAll(command: SearchCommand): Observable<SearchResult[]> {
    return this.http
      .post<SearchResult[]>(`${this.base}${this.ep.COMPARE_ALL}`, command)
      .pipe(tap(results => this.metrics.recordBatch(results)));
  }

  // ─── GET /search/algorithms ───────────────────────────────────────────────
  getAlgorithms(): Observable<AlgorithmInfo[]> {
    return this.http.get<AlgorithmInfo[]>(`${this.base}${this.ep.ALGORITHMS}`);
  }
}
