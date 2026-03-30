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
      <div class="hero-main">
        <p class="eyebrow">Secure remote access</p>
        <h1>Approve access, bind devices, and keep VPN activity visible from one control plane.</h1>
        <p class="lead">
          NorthGate brings request intake, account activation, device-scoped VPN credentials, trusted IP checks, and operational visibility into one portal instead of spreading them across ad hoc runbooks.
        </p>
        <div class="hero-actions">
          <a routerLink="/login" class="button primary">User sign in</a>
          <a routerLink="/admin/login" class="button secondary">Admin operations</a>
        </div>

        <div class="summary-grid section-block">
          <article class="summary-card">
            <span class="metric-label">Access model</span>
            <strong>Per-device VPN credentials</strong>
            <p class="detail-copy">Portal login stays separate from VPN password material issued to each registered device.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Security signal</span>
            <strong>Blocked new-IP visibility</strong>
            <p class="detail-copy">Unexpected source IPs can create confirmation flows instead of silently passing through.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Operations</span>
            <strong>Session-aware moderation</strong>
            <p class="detail-copy">Admins review requests, user limits, audit events, and active VPN sessions from one place.</p>
          </article>
        </div>
      </div>

      <aside class="hero-side" *ngIf="status$ | async as status">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Platform status</p>
            <h2>Deployment readiness snapshot</h2>
          </div>
        </div>

        <div class="status-grid">
          <article class="stat-card">
            <span class="metric-label">API</span>
            <strong>{{ status.name }}</strong>
            <span class="meta-line">HTTP host is responding.</span>
          </article>
          <article class="stat-card">
            <span class="metric-label">Version</span>
            <strong>{{ status.version }}</strong>
            <span class="meta-line">Current backend build marker.</span>
          </article>
          <article class="stat-card">
            <span class="metric-label">Database</span>
            <strong [ngClass]="status.databaseConfigured ? 'ok' : 'warn'">{{ status.databaseConfigured ? 'Configured' : 'Missing' }}</strong>
            <span class="meta-line">Connection string state reported by the API.</span>
          </article>
        </div>

        <div class="feature-list pending-block">
          <div>
            <strong>Public intake</strong>
            <p class="detail-copy">Request capture is already wired to the backend moderation queue.</p>
          </div>
          <div>
            <strong>Cookie auth</strong>
            <p class="detail-copy">User and superadmin sessions are isolated through role-aware portal login.</p>
          </div>
        </div>
      </aside>
    </section>

    <section class="panel request-panel" id="request-access">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">Request access</p>
          <h2>Submit a VPN access request</h2>
        </div>
        <p>Provide a contact email and optional display name. The request enters the admin queue immediately.</p>
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
