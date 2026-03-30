import { NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-user-login-page',
  standalone: true,
  imports: [ReactiveFormsModule, NgIf, RouterLink],
  template: `
    <section class="auth-shell">
      <div class="auth-layout">
        <article class="auth-panel auth-side">
          <p class="eyebrow">Вход пользователя</p>
          <h1>Откройте свой VPN-кабинет</h1>
          <p class="lead">Используйте учетные данные портала, созданные через ссылку активации. VPN-пароли устройств управляются отдельно внутри кабинета после входа.</p>

          <div class="feature-list pending-block">
            <div>
              <strong>Доступ с учетом устройства</strong>
              <p class="detail-copy">Выдавайте и меняйте VPN-учетные данные для каждого устройства отдельно, без общего статического секрета.</p>
            </div>
            <div>
              <strong>Подтверждение IP-адреса</strong>
              <p class="detail-copy">Неожиданные адреса подключения можно проверить и подтвердить прямо в портале.</p>
            </div>
          </div>
        </article>

        <article class="auth-panel">
          <div class="panel-heading">
            <div>
              <p class="eyebrow">Учетные данные</p>
              <h2>Войти</h2>
            </div>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <label>
              <span>Имя пользователя или email</span>
              <input type="text" formControlName="login" placeholder="alex или alex@example.com" />
            </label>

            <label>
              <span>Пароль</span>
              <input type="password" formControlName="password" placeholder="Ваш пароль от портала" />
            </label>

            <button class="button primary" type="submit" [disabled]="form.invalid || submitting()">{{ submitting() ? 'Вход...' : 'Перейти в кабинет' }}</button>
          </form>

          <div *ngIf="error() as error" class="feedback error">{{ error }}</div>

          <div class="inline-actions pending-block">
            <a routerLink="/" fragment="request-access" class="button ghost compact">Запросить новый доступ</a>
          </div>
        </article>
      </div>
    </section>
  `
})
export class UserLoginPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = this.fb.nonNullable.group({
    login: ['', Validators.required],
    password: ['', Validators.required]
  });

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.auth.loginUser(this.form.controls.login.value, this.form.controls.password.value).subscribe({
      next: async () => {
        this.submitting.set(false);
        await this.router.navigateByUrl('/dashboard');
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Неверные учетные данные или неактивная учетная запись.');
      }
    });
  }
}
