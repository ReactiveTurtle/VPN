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
    <section class="panel page-header narrow auth-card">
      <p class="eyebrow">Superadmin sign in</p>
      <h1>Moderation and VPN operations</h1>
      <p>Only manually provisioned superadmins can access the admin queue.</p>

      <form [formGroup]="form" (ngSubmit)="submit()" class="request-form auth-form">
        <label>
          <span>Username</span>
          <input type="text" formControlName="login" placeholder="rootadmin" />
        </label>

        <label>
          <span>Password</span>
          <input type="password" formControlName="password" placeholder="Admin password" />
        </label>

        <button class="button primary" type="submit" [disabled]="form.invalid || submitting()">{{ submitting() ? 'Signing in...' : 'Sign in as superadmin' }}</button>
      </form>

      <div *ngIf="error() as error" class="feedback error">{{ error }}</div>
      <p class="muted-note demo-note">Demo admin: <code>rootadmin</code> / <code>TestPassword123!</code></p>
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
