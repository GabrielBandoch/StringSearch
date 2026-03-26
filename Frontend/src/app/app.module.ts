import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';

import { AppComponent } from './app.component';
import { SearchModule } from './features/search/search.module';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    HttpClientModule,  // ← necessário para o SearchService usar HttpClient
    SearchModule,
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
