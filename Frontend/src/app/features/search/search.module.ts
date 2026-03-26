import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SearchComponent } from './search.component';
import { MaxComparisonsPipe } from '../../shared/pipes/max-comparisons.pipe';

@NgModule({
  declarations: [
    SearchComponent,
    MaxComparisonsPipe,
  ],
  imports: [
    CommonModule,
    FormsModule,
  ],
  exports: [SearchComponent],
})
export class SearchModule {}
