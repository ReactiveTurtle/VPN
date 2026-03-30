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
          <p class="eyebrow">Superadmin sign in</p>
          <h1>Moderation and runtime operations</h1>
          <p class="lead">Only manually provisioned superadmins can review requests, manage user limits, inspect audit events, and disconnect live VPN sessions.</p>

          <div class="feature-list pending-block">
            <div>
              <strong>Request queue</strong>
              <p class="detail-copy">Approve pending requests and deliver activation links immediately.</p>
            </div>
            <div>
              <strong>Operational controls</strong>
              <p class="detail-copy">Inspect recent sessions, enforce account state, and review recent security-sensitive audit actions.</p>
            </div>
          </div>
        </article>

        <article class="auth-panel">
          <div class="panel-heading">
            <div>
              <p class="eyebrow">Restricted access</p>
              <h2>Admin sign in</h2>
            </div>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <label>
              <span>Username</span>
              <input type="text" formControlName="login" placeholder="rootadmin" />
            </label>

            <label>
              <span>Password</span>
              <input type="password" formControlName="password" placeholder="Admin password" />
            </label>

            <button class="button primary" type="submit" [disabled]="form.invalid || submitting()">{{ submitting() ? 'Signing in...' : 'Open operations console' }}</button>
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
        this.error.set('Invalid superadmin credentials.');
      }
    });
  }
}
