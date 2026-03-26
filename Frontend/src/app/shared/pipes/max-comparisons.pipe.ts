import { Pipe, PipeTransform } from '@angular/core';
import { SearchResult } from '../../core/models/search.models';

/**
 * Retorna o maior número de comparações entre todos os resultados.
 * Usado no template para calcular a largura das barras no gráfico.
 *
 * Uso: (results | maxComparisons)
 */
@Pipe({ name: 'maxComparisons' })
export class MaxComparisonsPipe implements PipeTransform {
  transform(results: SearchResult[]): number {
    if (!results?.length) return 1;
    return Math.max(...results.map(r => r.totalComparisons), 1);
  }
}
