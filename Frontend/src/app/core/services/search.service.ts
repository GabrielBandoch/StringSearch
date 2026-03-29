import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../api.config';
import {
  SearchCommand,
  StepSearchCommand,
  MultiFileSearchCommand,
  SearchResult,
  StepSearchResult,
  MultiFileSearchResult,
  AlgorithmInfo,
} from '../models/search.models';

/**
 * SearchService — serviço central de busca.
 *
 * Uso nos componentes:
 *   constructor(private searchService: SearchService) {}
 *
 *   this.searchService.execute(command).subscribe(...)
 *   this.searchService.stepByStep(command).subscribe(...)
 *   this.searchService.multiFile(command).subscribe(...)
 *   this.searchService.compareAll(command).subscribe(...)
 *   this.searchService.getAlgorithms().subscribe(...)
 */
@Injectable({ providedIn: 'root' })
export class SearchService {
  private readonly base = API_CONFIG.BASE_URL;
  private readonly ep = API_CONFIG.ENDPOINTS.SEARCH;

  constructor(private http: HttpClient) {}

  // ─── POST /search/execute ─────────────────────────────────────────────────
  execute(command: SearchCommand): Observable<SearchResult> {
    return this.http.post<SearchResult>(
      `${this.base}${this.ep.EXECUTE}`,
      command
    );
  }

  // ─── POST /search/multi-file ──────────────────────────────────────────────
  multiFile(command: MultiFileSearchCommand): Observable<MultiFileSearchResult> {
    return this.http.post<MultiFileSearchResult>(
      `${this.base}${this.ep.MULTI_FILE}`,
      command
    );
  }

  // ─── POST /search/compare-all ─────────────────────────────────────────────
  compareAll(command: SearchCommand): Observable<SearchResult[]> {
    return this.http.post<SearchResult[]>(
      `${this.base}${this.ep.COMPARE_ALL}`,
      command
    );
  }

  // ─── GET /search/algorithms ───────────────────────────────────────────────
  getAlgorithms(): Observable<AlgorithmInfo[]> {
    return this.http.get<AlgorithmInfo[]>(
      `${this.base}${this.ep.ALGORITHMS}`
    );
  }
}
