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
    <section class="panel page-header narrow" *ngIf="status$ | async as status">
      <p class="eyebrow">Account activation</p>
      <h1>{{ status.valid ? 'Create your VPN password' : 'Activation link unavailable' }}</h1>
      <p>{{ status.message }}</p>
      <p *ngIf="status.email"><strong>{{ status.email }}</strong></p>
      <p *ngIf="status.expiresAt">Expires: {{ status.expiresAt | date: 'medium' }}</p>

      <form *ngIf="status.valid" [formGroup]="form" (ngSubmit)="submit()" class="request-form">
        <label>
          <span>Password</span>
          <input type="password" formControlName="password" placeholder="Minimum 10 characters" />
        </label>

        <label>
          <span>Confirm password</span>
          <input type="password" formControlName="confirmPassword" placeholder="Repeat password" />
        </label>

        <button type="submit" class="button primary" [disabled]="form.invalid || submitting()">{{ submitting() ? 'Activating...' : 'Activate account' }}</button>
      </form>

      <div *ngIf="message() as message" class="feedback success">{{ message }}</div>
      <div *ngIf="error() as error" class="feedback error">{{ error }}</div>
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
    catchError(() => of({ valid: false, used: false, email: null, expiresAt: null, message: 'Could not validate activation token.' }))
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
      this.error.set('Passwords do not match.');
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
        this.error.set('Activation failed. The link may be expired or already used.');
      }
    });
  }
}
