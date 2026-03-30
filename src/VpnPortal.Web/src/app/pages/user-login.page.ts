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
          <p class="eyebrow">User sign in</p>
          <h1>Open your VPN workspace</h1>
          <p class="lead">Use the portal credentials created through the activation link. VPN device passwords are managed separately inside the workspace after sign-in.</p>

          <div class="feature-list pending-block">
            <div>
              <strong>Device-aware access</strong>
              <p class="detail-copy">Issue or rotate VPN credentials per device instead of sharing one static secret.</p>
            </div>
            <div>
              <strong>Source IP confirmation</strong>
              <p class="detail-copy">Unexpected connection origins can be reviewed and approved without leaving the portal.</p>
            </div>
          </div>
        </article>

        <article class="auth-panel">
          <div class="panel-heading">
            <div>
              <p class="eyebrow">Credentials</p>
              <h2>Sign in</h2>
            </div>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <label>
              <span>Username or email</span>
              <input type="text" formControlName="login" placeholder="alex or alex@example.com" />
            </label>

            <label>
              <span>Password</span>
              <input type="password" formControlName="password" placeholder="Your portal password" />
            </label>

            <button class="button primary" type="submit" [disabled]="form.invalid || submitting()">{{ submitting() ? 'Signing in...' : 'Continue to workspace' }}</button>
          </form>

          <div *ngIf="error() as error" class="feedback error">{{ error }}</div>

          <div class="inline-actions pending-block">
            <a routerLink="/" fragment="request-access" class="button ghost compact">Request new access</a>
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
        this.error.set('Invalid credentials or inactive account.');
      }
    });
  }
}
