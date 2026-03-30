import { NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-admin-login-page',
  standalone: true,
  imports: [ReactiveFormsModule, NgIf],
  template: `
    <section class="auth-shell">
      <div class="auth-layout">
        <article class="auth-panel auth-side">
          <p class="eyebrow">Вход суперадминистратора</p>
          <h1>Модерация и операционное управление</h1>
          <p class="lead">Только вручную созданные суперадминистраторы могут рассматривать заявки, управлять лимитами пользователей, просматривать аудит и отключать активные VPN-сессии.</p>

          <div class="feature-list pending-block">
            <div>
              <strong>Очередь заявок</strong>
              <p class="detail-copy">Одобряйте ожидающие заявки и сразу выдавайте ссылки активации.</p>
            </div>
            <div>
              <strong>Операционное управление</strong>
              <p class="detail-copy">Просматривайте недавние сессии, управляйте состоянием учетных записей и отслеживайте важные события аудита.</p>
            </div>
          </div>
        </article>

        <article class="auth-panel">
          <div class="panel-heading">
            <div>
              <p class="eyebrow">Ограниченный доступ</p>
              <h2>Вход администратора</h2>
            </div>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <label>
              <span>Имя пользователя</span>
              <input type="text" formControlName="login" placeholder="rootadmin" />
            </label>

            <label>
              <span>Пароль</span>
              <input type="password" formControlName="password" placeholder="Пароль администратора" />
            </label>

            <button class="button primary" type="submit" [disabled]="form.invalid || submitting()">{{ submitting() ? 'Вход...' : 'Открыть консоль операций' }}</button>
          </form>

          <div *ngIf="error() as error" class="feedback error">{{ error }}</div>
        </article>
      </div>
    </section>
  `
})
export class AdminLoginPage {
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

    this.auth.loginAdmin(this.form.controls.login.value, this.form.controls.password.value).subscribe({
      next: async () => {
        this.submitting.set(false);
        await this.router.navigateByUrl('/admin');
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Неверные учетные данные суперадминистратора.');
      }
    });
  }
}
