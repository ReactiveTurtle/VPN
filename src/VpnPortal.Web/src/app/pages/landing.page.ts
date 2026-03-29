import { AsyncPipe, NgClass, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, of, startWith } from 'rxjs';
import { PortalApiService } from '../core/portal-api.service';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [ReactiveFormsModule, AsyncPipe, NgClass, NgIf, RouterLink],
  template: `
    <section class="hero">
      <div>
        <p class="eyebrow">Secure remote access</p>
        <h1>VPN onboarding with approval, device control, and session visibility.</h1>
        <p class="lead">
          Public request intake, admin moderation, account activation, and policy-aware VPN access in one portal.
        </p>
        <div class="hero-actions">
          <a routerLink="/admin" class="button ghost">Open admin preview</a>
          <a routerLink="/dashboard" class="button secondary">Open user dashboard</a>
        </div>
      </div>

      <aside class="status-card" *ngIf="status$ | async as status">
        <h2>Platform status</h2>
        <dl>
          <div>
            <dt>API</dt>
            <dd>{{ status.name }}</dd>
          </div>
          <div>
            <dt>Version</dt>
            <dd>{{ status.version }}</dd>
          </div>
          <div>
            <dt>Database configured</dt>
            <dd [ngClass]="status.databaseConfigured ? 'ok' : 'warn'">{{ status.databaseConfigured ? 'Yes' : 'No' }}</dd>
          </div>
        </dl>
      </aside>
    </section>

    <section class="panel request-panel">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">Request access</p>
          <h2>Submit a VPN access request</h2>
        </div>
        <p>Initial workflow is already connected to the API.</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" class="request-form">
        <label>
          <span>Name</span>
          <input type="text" formControlName="name" placeholder="Jane Doe" />
        </label>

        <label>
          <span>Email</span>
          <input type="email" formControlName="email" placeholder="jane@company.com" />
        </label>

        <button type="submit" class="button primary" [disabled]="form.invalid || submitting()">
          {{ submitting() ? 'Submitting...' : 'Submit request' }}
        </button>
      </form>

      <div *ngIf="message() as message" class="feedback success">{{ message }}</div>
      <div *ngIf="error() as error" class="feedback error">{{ error }}</div>
    </section>
  `
})
export class LandingPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PortalApiService);

  protected readonly form = this.fb.nonNullable.group({
    name: [''],
    email: ['', [Validators.required, Validators.email]]
  });

  protected readonly status$ = this.api.getStatus().pipe(
    catchError(() => of({ name: 'VpnPortal.Api', version: 'offline', databaseConfigured: false, developmentMode: true })),
    startWith(null)
  );

  protected readonly submitting = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.submitting.set(true);
    this.message.set(null);
    this.error.set(null);

    const payload = this.form.getRawValue();
    this.api.submitRequest(payload).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.message.set(`Request #${result.id} is ${result.status}. The admin queue has been updated.`);
        this.form.reset({ name: '', email: '' });
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Could not submit the request. Start the API and try again.');
      }
    });
  }
}
