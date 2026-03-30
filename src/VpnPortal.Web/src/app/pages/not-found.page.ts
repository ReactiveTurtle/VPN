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
        <h1>Маршрут не найден</h1>
        <p class="lead">Такого маршрута в портале нет. Вернитесь на главную страницу и продолжите работу через доступный публичный, пользовательский или административный сценарий.</p>
        <div class="inline-actions">
          <a routerLink="/" class="button primary">Вернуться в портал</a>
        </div>
      </article>
    </section>
  `
})
export class NotFoundPage {}
