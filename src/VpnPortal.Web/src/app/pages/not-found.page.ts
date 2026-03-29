import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="panel page-header narrow">
      <p class="eyebrow">404</p>
      <h1>Route not found</h1>
      <p>Go back to the public request page and continue from there.</p>
      <a routerLink="/" class="button primary">Back to portal</a>
    </section>
  `
})
export class NotFoundPage {}
