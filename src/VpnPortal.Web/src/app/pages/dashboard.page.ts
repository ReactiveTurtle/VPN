import { AsyncPipe, DatePipe, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject, catchError, of, switchMap } from 'rxjs';
import { PortalApiService } from '../core/portal-api.service';
import { IssuedVpnDeviceCredential } from '../core/models';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [AsyncPipe, DatePipe, NgIf, FormsModule],
  template: `
    <section class="hero" *ngIf="dashboard$ | async as dashboard">
      <div class="hero-main">
        <p class="eyebrow">User workspace</p>
        <h1>{{ dashboard.username }}'s VPN control surface</h1>
        <p class="lead">Manage device-bound VPN credentials, inspect recent sessions, and approve source IP changes without leaving the portal.</p>

        <div class="summary-grid section-block">
          <article class="summary-card">
            <span class="metric-label">Account</span>
            <strong>{{ dashboard.active ? 'Active' : 'Inactive' }}</strong>
            <p class="detail-copy">Portal sign-in and VPN onboarding are enabled only while the account stays active.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Device policy</span>
            <strong>{{ dashboard.devices.length }} / {{ dashboard.maxDevices }}</strong>
            <p class="detail-copy">Registered active devices versus the current device allowance.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Observed activity</span>
            <strong>{{ dashboard.sessions.length }} sessions</strong>
            <p class="detail-copy">Recent session records and trusted IP approvals stay visible from this workspace.</p>
          </article>
        </div>
      </div>

      <aside class="hero-side">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">At a glance</p>
            <h2>Security posture</h2>
          </div>
        </div>

        <div class="status-grid">
          <article class="stat-card">
            <span class="metric-label">Devices</span>
            <span class="metric-value">{{ dashboard.devices.length }}</span>
          </article>
          <article class="stat-card">
            <span class="metric-label">Trusted IPs</span>
            <span class="metric-value">{{ dashboard.trustedIps.length }}</span>
          </article>
          <article class="stat-card">
            <span class="metric-label">Pending IP approvals</span>
            <span class="metric-value">{{ dashboard.pendingIpConfirmations.length }}</span>
          </article>
        </div>
      </aside>
    </section>

    <section class="panel request-panel" *ngIf="message() as message">
      <div class="feedback success">{{ message }}</div>
    </section>

    <section class="panel request-panel" *ngIf="error() as error">
      <div class="feedback error">{{ error }}</div>
    </section>

    <section class="split-layout" *ngIf="dashboard$ | async as dashboard">
      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Trusted devices</p>
            <h2>Bound hardware</h2>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.devices.length; else noDevicesState">
          @for (device of dashboard.devices; track device.id) {
            <div class="stack-item">
              <strong>{{ device.deviceName }}</strong>
              <span>{{ device.platform }} / {{ device.deviceType }}</span>
              <span>State: {{ device.status }}</span>
              <span>{{ device.vpnUsername || 'VPN credential has not been issued yet.' }}</span>
              @if (device.credentialRotatedAt) {
                <span>Rotated {{ device.credentialRotatedAt | date: 'medium' }}</span>
              }
              @if (device.status !== 'revoked') {
                <div class="action-row">
                  @if (device.vpnUsername) {
                    <button type="button" class="button secondary compact" (click)="rotateDeviceCredential(device.id)">Rotate VPN password</button>
                  }
                  <button type="button" class="button ghost compact" (click)="revokeDevice(device.id)">Revoke device</button>
                </div>
              }
              @if (device.onboarding) {
                <div class="feature-list pending-block">
                  <div>
                    <strong>{{ device.onboarding.title }}</strong>
                    <p class="detail-copy">{{ device.onboarding.summary }}</p>
                  </div>
                </div>
              }
            </div>
          }
        </div>

        <ng-template #noDevicesState>
          <div class="empty-state">
            <h3>No registered devices yet</h3>
            <p class="muted-note">Issue the first VPN credential to create a device-bound access path.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Device access</p>
            <h2>Issue a VPN credential</h2>
          </div>
        </div>

        <div class="auth-form">
          <label>
            <span>Device name</span>
            <input type="text" [(ngModel)]="newDeviceName" name="newDeviceName" placeholder="Alex iPhone" />
          </label>
          <label>
            <span>Device type</span>
            <input type="text" [(ngModel)]="newDeviceType" name="newDeviceType" placeholder="phone" />
          </label>
          <label>
            <span>Platform</span>
            <input type="text" [(ngModel)]="newDevicePlatform" name="newDevicePlatform" placeholder="ios" />
          </label>
          <button type="button" class="button primary" (click)="issueDeviceCredential()">Issue VPN credential</button>
        </div>

        @if (issuedCredential()) {
          <div class="activation-link"><code>{{ issuedCredential()?.vpnUsername }}</code></div>
          <div class="activation-link"><code>{{ issuedCredential()?.vpnPassword }}</code></div>
          <div class="feature-list pending-block">
            <div>
              <strong>{{ issuedCredential()?.onboarding?.title }}</strong>
              <p class="detail-copy">{{ issuedCredential()?.onboarding?.summary }}</p>
            </div>
            @for (step of issuedCredential()?.onboarding?.steps ?? []; track step) {
              <div>
                <span>{{ step }}</span>
              </div>
            }
          </div>
        }
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">VPN sessions</p>
            <h2>Current activity</h2>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.sessions.length; else noSessionsState">
          @for (session of dashboard.sessions; track session.id) {
            <div class="stack-item">
              <strong>{{ session.assignedVpnIp || 'Pending IP lease' }}</strong>
              <span>{{ session.sourceIp }}</span>
              <span>{{ session.startedAt | date: 'medium' }}</span>
            </div>
          }
        </div>

        <ng-template #noSessionsState>
          <div class="empty-state">
            <h3>No recent VPN sessions</h3>
            <p class="muted-note">Session visibility appears here after accounting events reach the portal.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Trusted IPs</p>
            <h2>Approved source addresses</h2>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.trustedIps.length; else noIpsState">
          @for (ip of dashboard.trustedIps; track ip.id) {
            <div class="stack-item">
              <strong>{{ ip.ipAddress }}</strong>
              <span>{{ ip.status }}</span>
              <span>{{ ip.lastSeenAt ? (ip.lastSeenAt | date: 'medium') : 'No recent activity' }}</span>
            </div>
          }
        </div>

        <ng-template #noIpsState>
          <div class="empty-state">
            <h3>No approved IPs</h3>
            <p class="muted-note">New connection origins can be approved from email-driven confirmation flows.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">IP approval</p>
            <h2>Request a new IP confirmation</h2>
          </div>
        </div>

        <div class="auth-form">
          <label>
            <span>New source IP</span>
            <input type="text" [(ngModel)]="requestedIp" name="requestedIp" placeholder="198.51.100.77" />
          </label>
          <label>
            <span>Device</span>
            <select [(ngModel)]="selectedDeviceId" name="selectedDeviceId">
              <option [ngValue]="null">No device selected</option>
              @for (device of dashboard.devices; track device.id) {
                <option [ngValue]="device.id">{{ device.deviceName }}</option>
              }
            </select>
          </label>
          <button type="button" class="button primary" (click)="requestIpConfirmation()">Create confirmation link</button>
        </div>

        @if (lastConfirmationLink()) {
          <div class="activation-link"><code>{{ lastConfirmationLink() }}</code></div>
        }

        @if (dashboard.pendingIpConfirmations.length) {
          <div class="stack-list pending-block">
            @for (confirmation of dashboard.pendingIpConfirmations; track confirmation.id) {
              <div class="stack-item">
                <strong>{{ confirmation.requestedIp }}</strong>
                <span>Expires {{ confirmation.expiresAt | date: 'medium' }}</span>
              </div>
            }
          </div>
        }
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Platform guides</p>
            <h2>Manual onboarding reference</h2>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.platformGuides.length; else noGuidesState">
          @for (guide of dashboard.platformGuides; track guide.platform) {
            <div class="stack-item">
              <strong>{{ guide.title }}</strong>
              <span>{{ guide.summary }}</span>
              <div class="feature-list pending-block">
                @for (step of guide.steps; track step) {
                  <div>
                    <span>{{ step }}</span>
                  </div>
                }
              </div>
            </div>
          }
        </div>
        <ng-template #noGuidesState>
          <div class="empty-state">
            <h3>No onboarding guides available</h3>
            <p class="muted-note">Platform-specific connection guidance will appear here once configured.</p>
          </div>
        </ng-template>
      </article>
    </section>
  `
})
export class DashboardPage {
  private readonly api = inject(PortalApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly refresh$ = new BehaviorSubject<void>(undefined);

  protected readonly dashboard$ = this.refresh$.pipe(
    switchMap(() => this.api.getDashboard()),
    catchError(() => of(null))
  );
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly lastConfirmationLink = signal<string | null>(null);
  protected readonly issuedCredential = signal<IssuedVpnDeviceCredential | null>(null);
  protected requestedIp = '';
  protected selectedDeviceId: number | null = null;
  protected newDeviceName = '';
  protected newDeviceType = '';
  protected newDevicePlatform = '';

  constructor() {
    const token = this.route.snapshot.queryParamMap.get('confirmIpToken');
    if (token) {
      this.confirmIp(token);
    }
  }

  protected revokeDevice(deviceId: number): void {
    this.api.revokeDevice(deviceId).subscribe({
      next: () => {
        this.reloadDashboard();
        this.message.set('Device revoked. Policy state updated.');
      },
      error: () => this.error.set('Could not revoke device.')
    });
  }

  protected issueDeviceCredential(): void {
    if (!this.newDeviceName.trim() || !this.newDeviceType.trim() || !this.newDevicePlatform.trim()) {
      this.error.set('Enter device name, type, and platform first.');
      return;
    }

    this.api.issueDeviceCredential({
      deviceName: this.newDeviceName.trim(),
      deviceType: this.newDeviceType.trim(),
      platform: this.newDevicePlatform.trim()
    }).subscribe({
      next: (result) => {
        this.issuedCredential.set(result);
        this.reloadDashboard();
        this.message.set(result.message);
        this.error.set(null);
        this.newDeviceName = '';
        this.newDeviceType = '';
        this.newDevicePlatform = '';
      },
      error: () => this.error.set('Could not issue a VPN device credential.')
    });
  }

  protected rotateDeviceCredential(deviceId: number): void {
    this.api.rotateDeviceCredential(deviceId).subscribe({
      next: (result) => {
        this.issuedCredential.set(result);
        this.reloadDashboard();
        this.message.set(result.message);
        this.error.set(null);
      },
      error: () => this.error.set('Could not rotate the VPN password for this device.')
    });
  }

  protected requestIpConfirmation(): void {
    if (!this.requestedIp.trim()) {
      this.error.set('Enter an IP address first.');
      return;
    }

    this.api.requestIpConfirmation({ requestedIp: this.requestedIp.trim(), deviceId: this.selectedDeviceId }).subscribe({
      next: (result) => {
        this.message.set(result.message);
        this.lastConfirmationLink.set(result.confirmationLink);
      },
      error: () => this.error.set('Could not create IP confirmation request.')
    });
  }

  private confirmIp(token: string): void {
    this.api.confirmIp(token).subscribe({
      next: () => {
        this.reloadDashboard();
        this.message.set('IP address confirmed. Trusted IP list updated.');
      },
      error: () => this.error.set('Could not confirm IP address from this token.')
    });
  }

  private reloadDashboard(): void {
    this.refresh$.next();
  }
}
