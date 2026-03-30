import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="auth-shell">
      <article class="auth-panel auth-side">
        <p class="eyebrow">404</p>
        <h1>Route not found</h1>
        <p class="lead">This portal route does not exist. Return to the main entry point and continue from a supported public, user, or admin flow.</p>
        <div class="inline-actions">
          <a routerLink="/" class="button primary">Back to portal</a>
        </div>
      </article>
    </section>
  `
})
export class NotFoundPage {}
