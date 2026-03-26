// ─── Request Models ──────────────────────────────────────────────────────────

export interface SearchCommand {
  text: string;
  pattern: string;
  algorithm: AlgorithmId;
}

export interface StepSearchCommand {
  text: string;
  pattern: string;
  algorithm: AlgorithmId;
}

export interface MultiFileSearchCommand {
  files: FileContent[];
  pattern: string;
  algorithm: AlgorithmId;
}

export interface FileContent {
  fileName: string;
  content: string;
}

// ─── Response Models ─────────────────────────────────────────────────────────

export interface SearchResult {
  algorithm: string;
  algorithmDisplayName: string;
  occurrences: number[];
  totalOccurrences: number;
  executionTimeMs: number;
  executionTimeNs: number;
  totalComparisons: number;
  textLength: number;
  patternLength: number;
  theoreticalComplexity: string;
  complexityDescription: string;
  complexityAnalysis: string;
}

export interface StepSearchResult {
  algorithm: string;
  algorithmDisplayName: string;
  steps: SearchStep[];
  occurrences: number[];
  totalOccurrences: number;
  totalComparisons: number;
  auxiliaryStructure: AuxiliaryStructure | null;
}

export interface SearchStep {
  stepNumber: number;
  textIndex: number;
  patternIndex: number;
  textChar: string;
  patternChar: string;
  isMatch: boolean;
  description: string;
  patternOffset: number;
  comparedIndices: number[];
}

export interface AuxiliaryStructure {
  name: string;
  description: string;
  data: Record<string, unknown>;
}

export interface MultiFileSearchResult {
  fileResults: FileSearchResult[];
  algorithm: string;
  totalExecutionTimeMs: number;
}

export interface FileSearchResult {
  fileName: string;
  result: SearchResult;
}

export interface AlgorithmInfo {
  id: AlgorithmId;
  displayName: string;
  description: string;
  bestCase: string;
  averageCase: string;
  worstCase: string;
  spaceComplexity: string;
  useCaseDescription: string;
}

// ─── Type Aliases ─────────────────────────────────────────────────────────────

export type AlgorithmId = 'naive' | 'rabinkarp' | 'kmp' | 'boyermoore';
