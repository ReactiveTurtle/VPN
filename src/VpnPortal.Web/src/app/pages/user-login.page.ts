import { NgClass, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-user-login-page',
  standalone: true,
  imports: [ReactiveFormsModule, NgClass, NgIf, RouterLink],
  template: `
    <section class="auth-shell">
      <div class="auth-layout auth-layout-single">
        <article class="auth-panel auth-panel-centered auth-login-panel">
          <h1 class="auth-title-smaller">Вход в личный кабинет</h1>

          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <label>
              <span>Логин или email</span>
              <input type="text" formControlName="login" placeholder="alex или alex@example.com" [ngClass]="{ 'field-invalid': isInvalid('login') }" />
            </label>

            <label>
              <span>Пароль</span>
              <input type="password" formControlName="password" placeholder="Ваш пароль от портала" [ngClass]="{ 'field-invalid': isInvalid('password') }" />
            </label>

            <button class="button primary" type="submit" [disabled]="submitting()">{{ submitting() ? 'Вход...' : 'Войти' }}</button>
          </form>

          <div *ngIf="error() as error" class="feedback error auth-error">{{ error }}</div>

          <div class="inline-actions pending-block auth-links-row">
            <a routerLink="/">Забыли пароль?</a>
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
      this.form.markAllAsTouched();
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

  protected isInvalid(controlName: 'login' | 'password'): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }
}
