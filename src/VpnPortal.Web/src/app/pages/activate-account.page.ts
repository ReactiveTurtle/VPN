import { AsyncPipe, DatePipe, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { catchError, map, of, switchMap } from 'rxjs';
import { PortalApiService } from '../core/portal-api.service';

@Component({
  selector: 'app-activate-account-page',
  standalone: true,
  imports: [ReactiveFormsModule, AsyncPipe, DatePipe, NgIf],
  template: `
    <section class="auth-shell" *ngIf="status$ | async as status">
      <div class="auth-layout">
        <article class="auth-panel auth-side">
          <p class="eyebrow">Активация учетной записи</p>
          <h1>{{ status.valid ? 'Создайте пароль' : 'Ссылка недоступна' }}</h1>
          <p class="lead">После активации можно будет войти в портал и настроить устройства.</p>

          <div class="feature-list pending-block">
            <div>
              <strong>Статус</strong>
              <p class="detail-copy">{{ status.message }}</p>
            </div>
            <div *ngIf="status.email">
              <strong>Учетная запись</strong>
              <p class="detail-copy">{{ status.email }}</p>
            </div>
            <div *ngIf="status.expiresAt">
              <strong>Истекает</strong>
              <p class="detail-copy">{{ status.expiresAt | date: 'medium' }}</p>
            </div>
          </div>
        </article>

        <article class="auth-panel">
          <div class="content-section-header">
            <div>
              <p class="eyebrow">Пароль портала</p>
              <h2>{{ status.valid ? 'Создать пароль' : 'Проверка токена' }}</h2>
            </div>
          </div>

          <form *ngIf="status.valid" [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <label>
              <span>Пароль</span>
              <input type="password" formControlName="password" placeholder="Минимум 10 символов" />
            </label>

            <label>
              <span>Подтвердите пароль</span>
              <input type="password" formControlName="confirmPassword" placeholder="Повторите пароль" />
            </label>

            <button type="submit" class="button primary" [disabled]="form.invalid || submitting()">{{ submitting() ? 'Активация...' : 'Активировать учетную запись' }}</button>
          </form>

          <div *ngIf="message() as message" class="feedback success">{{ message }}</div>
          <div *ngIf="error() as error" class="feedback error">{{ error }}</div>
        </article>
      </div>
    </section>
  `
})
export class ActivateAccountPage {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PortalApiService);

  protected readonly token = this.route.snapshot.paramMap.get('token') ?? '';
  protected readonly form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(10)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(10)]]
  });

  protected readonly status$ = this.route.paramMap.pipe(
    map((params) => params.get('token') ?? ''),
    switchMap((token) => this.api.getActivationStatus(token)),
    catchError(() => of({ valid: false, used: false, email: null, expiresAt: null, message: 'Не удалось проверить токен активации.' }))
  );

  protected readonly submitting = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    const { password, confirmPassword } = this.form.getRawValue();
    if (password !== confirmPassword) {
      this.error.set('Пароли не совпадают.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.message.set(null);

    this.api.activateAccount({ token: this.token, password }).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.message.set(result.message);
        this.form.reset({ password: '', confirmPassword: '' });
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Не удалось активировать учетную запись. Возможно, ссылка истекла или уже была использована.');
      }
    });
  }
}
