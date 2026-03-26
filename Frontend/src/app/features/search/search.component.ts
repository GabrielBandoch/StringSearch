import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, finalize } from 'rxjs';
import { SearchService } from '../../core/services/search.service';
import {
  AlgorithmId,
  AlgorithmInfo,
  FileContent,
  MultiFileSearchResult,
  SearchResult,
  StepSearchResult,
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
  isStepLoading = false;
  isCompareLoading = false;
  errorMessage = '';

  searchResult: SearchResult | null = null;
  stepResult: StepSearchResult | null = null;
  compareResults: SearchResult[] = [];
  multiFileResult: MultiFileSearchResult | null = null;

  currentStepIndex = 0;
  isPlayingSteps = false;
  stepPlayInterval: ReturnType<typeof setInterval> | null = null;
  stepDelay = 400; // ms entre passos

  activeTab: 'search' | 'step' | 'compare' | 'files' = 'search';

  private destroy$ = new Subject<void>();

  // ─── DI seguindo o padrão solicitado ─────────────────────────────────────
  constructor(private searchService: SearchService) {}

  // ─── Lifecycle ────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.loadAlgorithms();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.clearStepInterval();
  }

  // ─── Load algorithms from API ─────────────────────────────────────────────
  loadAlgorithms(): void {
    this.searchService
      .getAlgorithms()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => (this.algorithms = data),
        error: () => (this.errorMessage = 'Não foi possível carregar os algoritmos. Verifique se o backend está rodando.'),
      });
  }

  // ─── Execute normal search ────────────────────────────────────────────────
  onExecute(): void {
    if (!this.isValid()) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.searchResult = null;

    this.searchService
      .execute({
        text: this.textInput,
        pattern: this.pattern,
        algorithm: this.selectedAlgorithm,
      })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => (this.isLoading = false))
      )
      .subscribe({
        next: (result) => (this.searchResult = result),
        error: (err) => (this.errorMessage = err?.error ?? 'Erro ao executar busca.'),
      });
  }

  // ─── Execute step-by-step ─────────────────────────────────────────────────
  onStepByStep(): void {
    if (!this.isValid()) return;

    this.isStepLoading = true;
    this.errorMessage = '';
    this.stepResult = null;
    this.currentStepIndex = 0;
    this.clearStepInterval();

    this.searchService
      .stepByStep({
        text: this.textInput,
        pattern: this.pattern,
        algorithm: this.selectedAlgorithm,
      })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => (this.isStepLoading = false))
      )
      .subscribe({
        next: (result) => {
          this.stepResult = result;
          this.activeTab = 'step';
        },
        error: (err) => (this.errorMessage = err?.error ?? 'Erro na execução passo a passo.'),
      });
  }

  // ─── Compare all algorithms ───────────────────────────────────────────────
  onCompareAll(): void {
    if (!this.isValid()) return;

    this.isCompareLoading = true;
    this.errorMessage = '';
    this.compareResults = [];

    this.searchService
      .compareAll({
        text: this.textInput,
        pattern: this.pattern,
        algorithm: this.selectedAlgorithm,
      })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => (this.isCompareLoading = false))
      )
      .subscribe({
        next: (results) => {
          this.compareResults = results;
          this.activeTab = 'compare';
        },
        error: (err) => (this.errorMessage = err?.error ?? 'Erro na comparação.'),
      });
  }

  // ─── Multi-file search ────────────────────────────────────────────────────
  onSearchFiles(): void {
    if (this.uploadedFiles.length === 0 || !this.pattern.trim()) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.multiFileResult = null;

    this.searchService
      .multiFile({
        files: this.uploadedFiles,
        pattern: this.pattern,
        algorithm: this.selectedAlgorithm,
      })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => (this.isLoading = false))
      )
      .subscribe({
        next: (result) => {
          this.multiFileResult = result;
          this.activeTab = 'files';
        },
        error: (err) => (this.errorMessage = err?.error ?? 'Erro na busca em arquivos.'),
      });
  }

  // ─── File upload ──────────────────────────────────────────────────────────
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    this.uploadedFiles = [];
    const files = Array.from(input.files);

    files.forEach((file) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        this.uploadedFiles.push({
          fileName: file.name,
          content: e.target?.result as string,
        });
        // If all files loaded, auto-set text from first file
        if (this.uploadedFiles.length === 1) {
          this.textInput = this.uploadedFiles[0].content.slice(0, 2000);
        }
      };
      reader.readAsText(file);
    });
  }

  // ─── Step-by-step controls ────────────────────────────────────────────────
  nextStep(): void {
    if (this.stepResult && this.currentStepIndex < this.stepResult.steps.length - 1) {
      this.currentStepIndex++;
    }
  }

  prevStep(): void {
    if (this.currentStepIndex > 0) this.currentStepIndex--;
  }

  goToStep(index: number): void {
    this.currentStepIndex = index;
  }

  togglePlay(): void {
    if (this.isPlayingSteps) {
      this.clearStepInterval();
      this.isPlayingSteps = false;
    } else {
      this.isPlayingSteps = true;
      this.stepPlayInterval = setInterval(() => {
        if (!this.stepResult || this.currentStepIndex >= this.stepResult.steps.length - 1) {
          this.clearStepInterval();
          this.isPlayingSteps = false;
          return;
        }
        this.currentStepIndex++;
      }, this.stepDelay);
    }
  }

  resetSteps(): void {
    this.clearStepInterval();
    this.isPlayingSteps = false;
    this.currentStepIndex = 0;
  }

  private clearStepInterval(): void {
    if (this.stepPlayInterval) {
      clearInterval(this.stepPlayInterval);
      this.stepPlayInterval = null;
    }
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

  auxiliaryEntries(): [string, unknown][] {
    if (!this.stepResult?.auxiliaryStructure) return [];
    return Object.entries(this.stepResult.auxiliaryStructure.data);
  }

  setActiveTab(tab: typeof this.activeTab): void {
    this.activeTab = tab;
  }

  trackByStep(_: number, step: { stepNumber: number }) {
    return step.stepNumber;
  }
}
