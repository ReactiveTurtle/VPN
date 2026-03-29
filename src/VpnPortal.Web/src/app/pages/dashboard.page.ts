import { AsyncPipe, DatePipe, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { PortalApiService } from '../core/portal-api.service';
import { IssuedVpnDeviceCredential, UserDashboard } from '../core/models';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [AsyncPipe, DatePipe, NgIf, FormsModule],
  template: `
    <section class="panel page-header" *ngIf="dashboard$ | async as dashboard">
      <p class="eyebrow">User dashboard</p>
      <h1>{{ dashboard.username }}'s VPN workspace</h1>
      <div class="stats-grid">
        <article>
          <span>Account</span>
          <strong>{{ dashboard.active ? 'Active' : 'Inactive' }}</strong>
        </article>
        <article>
          <span>Devices allowed</span>
          <strong>{{ dashboard.maxDevices }}</strong>
        </article>
        <article>
          <span>Devices registered</span>
          <strong>{{ dashboard.devices.length }}</strong>
        </article>
        <article>
          <span>Live sessions</span>
          <strong>{{ dashboard.sessions.length }}</strong>
        </article>
        <article>
          <span>Trusted IPs</span>
          <strong>{{ dashboard.trustedIps.length }}</strong>
        </article>
      </div>
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
        <div class="stack-list">
          @for (device of dashboard.devices; track device.id) {
            <div class="stack-item">
              <strong>{{ device.deviceName }}</strong>
              <span>{{ device.platform }} / {{ device.deviceType }}</span>
              <span>{{ device.status }}</span>
              <span>{{ device.vpnUsername || 'VPN credential not issued yet' }}</span>
              @if (device.status !== 'revoked') {
                <div class="action-row">
                  @if (device.vpnUsername) {
                    <button type="button" class="button secondary compact" (click)="rotateDeviceCredential(device.id)">Rotate VPN password</button>
                  }
                  <button type="button" class="button ghost compact" (click)="revokeDevice(device.id)">Revoke device</button>
                </div>
              }
            </div>
          }
        </div>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Device access</p>
            <h2>Issue a VPN credential</h2>
          </div>
        </div>
        <div class="request-form auth-form">
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
          <div class="activation-link">
            <code>{{ issuedCredential()?.vpnUsername }}</code>
          </div>
          <div class="activation-link">
            <code>{{ issuedCredential()?.vpnPassword }}</code>
          </div>
        }
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Trusted IPs</p>
            <h2>Approved source addresses</h2>
          </div>
        </div>
        <div class="stack-list">
          @for (ip of dashboard.trustedIps; track ip.id) {
            <div class="stack-item">
              <strong>{{ ip.ipAddress }}</strong>
              <span>{{ ip.status }}</span>
              <span>{{ ip.lastSeenAt ? (ip.lastSeenAt | date: 'medium') : 'No recent activity' }}</span>
            </div>
          }
        </div>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">VPN sessions</p>
            <h2>Current activity</h2>
          </div>
        </div>
        <div class="stack-list">
          @for (session of dashboard.sessions; track session.id) {
            <div class="stack-item">
              <strong>{{ session.assignedVpnIp || 'Pending IP lease' }}</strong>
              <span>{{ session.sourceIp }}</span>
              <span>{{ session.startedAt | date: 'medium' }}</span>
            </div>
          }
        </div>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">IP approval</p>
            <h2>Request a new IP confirmation</h2>
          </div>
        </div>
        <div class="request-form auth-form">
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
          <div class="activation-link">
            <code>{{ lastConfirmationLink() }}</code>
          </div>
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
    </section>
  `
})
export class DashboardPage {
  private readonly api = inject(PortalApiService);
  private readonly route = inject(ActivatedRoute);

  protected readonly dashboard$ = this.api.getDashboard().pipe(catchError(() => of(null)));
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
        this.message.set('Device revoked. Refresh the page to see updated policy state.');
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
        this.message.set(`${result.message} Refresh the page to see the new device in the dashboard.`);
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
        this.message.set(result.message + ' Refresh the page to see the updated credential state.');
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
      next: () => this.message.set('IP address confirmed. Refresh the page to see it in the trusted list.'),
      error: () => this.error.set('Could not confirm IP address from this token.')
    });
  }
}
