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
    <section class="panel page-header narrow auth-card">
      <p class="eyebrow">User sign in</p>
      <h1>Access your VPN workspace</h1>
      <p>Use your portal username or email and the password created from the activation link.</p>

      <form [formGroup]="form" (ngSubmit)="submit()" class="request-form auth-form">
        <label>
          <span>Username or email</span>
          <input type="text" formControlName="login" placeholder="alex or alex@example.com" />
        </label>

        <label>
          <span>Password</span>
          <input type="password" formControlName="password" placeholder="Your password" />
        </label>

        <button class="button primary" type="submit" [disabled]="form.invalid || submitting()">{{ submitting() ? 'Signing in...' : 'Sign in' }}</button>
      </form>

      <div *ngIf="error() as error" class="feedback error">{{ error }}</div>
      <p class="muted-note demo-note">Demo user: <code>alex</code> / <code>TestPassword123!</code></p>
      <a routerLink="/activate/demo-token" class="button ghost">Open activation demo</a>
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
        this.error.set('Invalid credentials or inactive account.');
      }
    });
  }
}
