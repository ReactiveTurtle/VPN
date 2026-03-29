import { DatePipe, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PortalApiService } from '../core/portal-api.service';
import { AdminSession, AdminUser, AuditLogEntry, VpnRequest } from '../core/models';

@Component({
  selector: 'app-admin-requests-page',
  standalone: true,
  imports: [DatePipe, NgIf, FormsModule],
  template: `
    <section class="panel page-header">
      <p class="eyebrow">Admin queue</p>
      <h1>VPN access moderation</h1>
      <p>Approve or reject requests directly from the queue and get the activation link back immediately.</p>
    </section>

    <section class="panel request-panel" *ngIf="message() as message">
      <div class="feedback success">{{ message }}</div>
    </section>

    <section class="panel request-panel" *ngIf="error() as error">
      <div class="feedback error">{{ error }}</div>
    </section>

    <section class="panel data-panel">
      <table *ngIf="requests().length; else emptyState">
        <thead>
          <tr>
            <th>Applicant</th>
            <th>Status</th>
            <th>Submitted</th>
            <th>Comment</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          @for (request of requests(); track request.id) {
            <tr>
              <td>
                <strong>{{ request.name || 'Unknown user' }}</strong>
                <div>{{ request.email }}</div>
              </td>
              <td><span class="badge">{{ request.status }}</span></td>
              <td>{{ request.submittedAt | date: 'medium' }}</td>
              <td>
                {{ request.adminComment || 'Waiting for review' }}
                @if (request.activationLink) {
                  <div class="activation-link"><code>{{ request.activationLink }}</code></div>
                }
              </td>
              <td>
                @if (request.status === 'pending') {
                  <div class="moderation-actions">
                    <textarea
                      [(ngModel)]="comments[request.id]"
                      [name]="'comment-' + request.id"
                      rows="3"
                      placeholder="Optional admin comment"></textarea>
                    <div class="action-row">
                      <button
                        type="button"
                        class="button primary"
                        [disabled]="busyRequestId() === request.id"
                        (click)="approve(request)">
                        {{ busyRequestId() === request.id ? 'Saving...' : 'Approve' }}
                      </button>
                      <button
                        type="button"
                        class="button danger"
                        [disabled]="busyRequestId() === request.id"
                        (click)="reject(request)">
                        Reject
                      </button>
                    </div>
                  </div>
                } @else {
                  <span class="muted-note">Already processed</span>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>

      <ng-template #emptyState>
        <p class="muted-note">No requests in the moderation queue yet.</p>
      </ng-template>
    </section>

    <section class="split-layout admin-ops-grid">
      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Users</p>
            <h2>Account control</h2>
          </div>
        </div>
        <div class="stack-list">
          @for (user of users(); track user.id) {
            <div class="stack-item">
              <strong>{{ user.username }}</strong>
              <span>{{ user.email }}</span>
              <span>{{ user.active ? 'active' : 'inactive' }} / max devices: {{ user.maxDevices }} / bound: {{ user.deviceCount }}</span>
              <div class="moderation-actions compact-grid">
                <label>
                  <span>Max devices</span>
                  <input type="number" min="1" [(ngModel)]="userLimits[user.id]" [name]="'limit-' + user.id" />
                </label>
                <div class="action-row">
                  <button type="button" class="button primary compact" [disabled]="busyUserId() === user.id" (click)="saveUser(user)">Save</button>
                  <button type="button" class="button ghost compact" [disabled]="busyUserId() === user.id" (click)="toggleUser(user)">
                    {{ user.active ? 'Deactivate' : 'Activate' }}
                  </button>
                </div>
              </div>
            </div>
          }
        </div>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">VPN sessions</p>
            <h2>Active and recent connections</h2>
          </div>
        </div>
        <div class="stack-list">
          @for (session of sessions(); track session.id) {
            <div class="stack-item">
              <strong>{{ session.username }}</strong>
              <span>{{ session.sourceIp }} -> {{ session.assignedVpnIp || 'pending lease' }}</span>
              <span>{{ session.deviceName || 'Unknown device' }}</span>
              <button type="button" class="button danger compact" [disabled]="busySessionId() === session.id || !session.active" (click)="disconnectSession(session.id)">
                {{ busySessionId() === session.id ? 'Disconnecting...' : (session.active ? 'Disconnect' : 'Closed') }}
              </button>
            </div>
          }
        </div>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Audit log</p>
            <h2>Recent security events</h2>
          </div>
        </div>
        <div class="stack-list">
          @for (entry of auditLog(); track entry.id) {
            <div class="stack-item">
              <strong>{{ entry.action }}</strong>
              <span>{{ entry.actorType }} / {{ entry.entityType }} / {{ entry.entityId }}</span>
              <span>{{ entry.createdAt | date: 'medium' }}</span>
            </div>
          }
        </div>
      </article>
    </section>
  `
})
export class AdminRequestsPage {
  private readonly api = inject(PortalApiService);

  protected readonly requests = signal<VpnRequest[]>([]);
  protected readonly busyRequestId = signal<number | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly sessions = signal<AdminSession[]>([]);
  protected readonly users = signal<AdminUser[]>([]);
  protected readonly auditLog = signal<AuditLogEntry[]>([]);
  protected readonly busySessionId = signal<number | null>(null);
  protected readonly busyUserId = signal<number | null>(null);
  protected readonly comments: Record<number, string> = {};
  protected readonly userLimits: Record<number, number> = {};

  constructor() {
    this.loadRequests();
    this.loadUsers();
    this.loadSessions();
    this.loadAuditLog();
  }

  protected approve(request: VpnRequest): void {
    this.busyRequestId.set(request.id);
    this.message.set(null);
    this.error.set(null);

    this.api.approveRequest(request.id, this.comments[request.id]).subscribe({
      next: (updated) => {
        this.mergeRequest(updated);
        this.busyRequestId.set(null);
        this.message.set(`Request #${updated.id} approved. Activation link is ready.`);
      },
      error: () => {
        this.busyRequestId.set(null);
        this.error.set(`Could not approve request #${request.id}.`);
      }
    });
  }

  protected reject(request: VpnRequest): void {
    this.busyRequestId.set(request.id);
    this.message.set(null);
    this.error.set(null);

    this.api.rejectRequest(request.id, this.comments[request.id]).subscribe({
      next: (updated) => {
        this.mergeRequest(updated);
        this.busyRequestId.set(null);
        this.message.set(`Request #${updated.id} rejected.`);
      },
      error: () => {
        this.busyRequestId.set(null);
        this.error.set(`Could not reject request #${request.id}.`);
      }
    });
  }

  private loadRequests(): void {
    this.api.getAdminRequests().subscribe({
      next: (requests) => this.requests.set(requests),
      error: () => this.error.set('Could not load moderation queue. Start the API and refresh.')
    });
  }

  protected disconnectSession(sessionId: number): void {
    this.busySessionId.set(sessionId);
    this.api.disconnectAdminSession(sessionId).subscribe({
      next: () => {
        this.busySessionId.set(null);
        this.message.set(`Session #${sessionId} disconnected.`);
        this.loadSessions();
        this.loadAuditLog();
      },
      error: () => {
        this.busySessionId.set(null);
        this.error.set(`Could not disconnect session #${sessionId}.`);
      }
    });
  }

  protected saveUser(user: AdminUser): void {
    this.busyUserId.set(user.id);
    this.api.updateAdminUser(user.id, { maxDevices: this.userLimits[user.id] ?? user.maxDevices }).subscribe({
      next: (updated) => {
        this.busyUserId.set(null);
        this.mergeUser(updated);
        this.message.set(`User ${updated.username} updated.`);
        this.loadAuditLog();
      },
      error: () => {
        this.busyUserId.set(null);
        this.error.set(`Could not update user ${user.username}.`);
      }
    });
  }

  protected toggleUser(user: AdminUser): void {
    this.busyUserId.set(user.id);
    this.api.setAdminUserStatus(user.id, !user.active).subscribe({
      next: (updated) => {
        this.busyUserId.set(null);
        this.mergeUser(updated);
        this.message.set(`User ${updated.username} is now ${updated.active ? 'active' : 'inactive'}.`);
        this.loadAuditLog();
      },
      error: () => {
        this.busyUserId.set(null);
        this.error.set(`Could not change status for ${user.username}.`);
      }
    });
  }

  private loadUsers(): void {
    this.api.getAdminUsers().subscribe({
      next: (users) => {
        this.users.set(users);
        users.forEach((user) => {
          this.userLimits[user.id] = user.maxDevices;
        });
      },
      error: () => this.error.set('Could not load users.')
    });
  }

  private loadSessions(): void {
    this.api.getAdminSessions().subscribe({
      next: (sessions) => this.sessions.set(sessions),
      error: () => this.error.set('Could not load VPN sessions.')
    });
  }

  private loadAuditLog(): void {
    this.api.getAuditLog().subscribe({
      next: (entries) => this.auditLog.set(entries),
      error: () => this.error.set('Could not load audit log.')
    });
  }

  private mergeRequest(updated: VpnRequest): void {
    this.requests.update((requests) => requests.map((request) => request.id === updated.id ? updated : request));
  }

  private mergeUser(updated: AdminUser): void {
    this.userLimits[updated.id] = updated.maxDevices;
    this.users.update((users) => users.map((user) => user.id === updated.id ? updated : user));
  }
}
