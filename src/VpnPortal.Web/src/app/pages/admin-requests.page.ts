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
    <section class="hero">
      <div class="hero-main">
        <p class="eyebrow">Admin operations</p>
        <h1>Moderate access and supervise runtime state</h1>
        <p class="lead">This view combines request moderation, user policy controls, recent sessions, and audit visibility for superadmin operators.</p>

        <div class="summary-grid section-block">
          <article class="summary-card">
            <span class="metric-label">Pending requests</span>
            <strong>{{ pendingRequestCount() }}</strong>
            <p class="detail-copy">Requests still waiting for moderation.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Tracked users</span>
            <strong>{{ users().length }}</strong>
            <p class="detail-copy">Provisioned users currently visible to admin operations.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Recent sessions</span>
            <strong>{{ sessions().length }}</strong>
            <p class="detail-copy">Active and recently observed VPN connection records.</p>
          </article>
        </div>
      </div>

      <aside class="hero-side">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Operational focus</p>
            <h2>What to watch now</h2>
          </div>
        </div>

        <div class="feature-list">
          <div>
            <strong>Moderation queue</strong>
            <p class="detail-copy">Approve pending requests and deliver activation links immediately.</p>
          </div>
          <div>
            <strong>Session control</strong>
            <p class="detail-copy">Disconnect suspicious live sessions when runtime integration is available.</p>
          </div>
          <div>
            <strong>Audit review</strong>
            <p class="detail-copy">Track high-value account, credential, and session events in one stream.</p>
          </div>
        </div>
      </aside>
    </section>

    <section class="panel request-panel" *ngIf="message() as message">
      <div class="feedback success">{{ message }}</div>
    </section>

    <section class="panel request-panel" *ngIf="error() as error">
      <div class="feedback error">{{ error }}</div>
    </section>

    <section class="panel data-panel">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">Moderation queue</p>
          <h2>Review access requests</h2>
        </div>
      </div>

      <div class="stack-list" *ngIf="requests().length; else emptyState">
        @for (request of requests(); track request.id) {
          <article class="stack-item">
            <div class="panel-heading">
              <div>
                <strong>{{ request.name || 'Unknown user' }}</strong>
                <p>{{ request.email }}</p>
              </div>
              <span class="badge">{{ request.status }}</span>
            </div>

            <div class="feature-list">
              <div>
                <strong>Submitted</strong>
                <p class="detail-copy">{{ request.submittedAt | date: 'medium' }}</p>
              </div>
              <div>
                <strong>Review note</strong>
                <p class="detail-copy">{{ request.adminComment || 'Waiting for review' }}</p>
              </div>
            </div>

            @if (request.activationLink) {
              <div class="activation-link"><code>{{ request.activationLink }}</code></div>
            }

            @if (request.status === 'pending') {
              <div class="pending-block auth-form">
                <label>
                  <span>Admin comment</span>
                  <textarea [(ngModel)]="comments[request.id]" [name]="'comment-' + request.id" rows="3" placeholder="Optional admin comment"></textarea>
                </label>
                <div class="action-row">
                  <button type="button" class="button primary" [disabled]="busyRequestId() === request.id" (click)="approve(request)">
                    {{ busyRequestId() === request.id ? 'Saving...' : 'Approve' }}
                  </button>
                  <button type="button" class="button danger" [disabled]="busyRequestId() === request.id" (click)="reject(request)">Reject</button>
                </div>
              </div>
            } @else {
              <div class="chip-row pending-block">
                <span class="session-pill">Already processed</span>
              </div>
            }
          </article>
        }
      </div>

      <ng-template #emptyState>
        <div class="empty-state">
          <h3>No requests in queue</h3>
          <p class="muted-note">Public intake has not produced any moderation items yet.</p>
        </div>
      </ng-template>
    </section>

    <section class="operations-grid admin-ops-grid">
      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Users</p>
            <h2>Account control</h2>
          </div>
        </div>
        <div class="stack-list" *ngIf="users().length; else emptyUsersState">
          @for (user of users(); track user.id) {
            <div class="stack-item">
              <strong>{{ user.username }}</strong>
              <span>{{ user.email }}</span>
              <span>{{ user.active ? 'active' : 'inactive' }} / max devices: {{ user.maxDevices }} / bound: {{ user.deviceCount }}</span>
              <div class="auth-form pending-block">
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
        <ng-template #emptyUsersState>
          <div class="empty-state">
            <h3>No managed users yet</h3>
            <p class="muted-note">Approved access requests will create portal users that can be managed here.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">VPN sessions</p>
            <h2>Active and recent connections</h2>
          </div>
        </div>
        <div class="stack-list" *ngIf="sessions().length; else emptySessionsState">
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
        <ng-template #emptySessionsState>
          <div class="empty-state">
            <h3>No recent sessions</h3>
            <p class="muted-note">Session activity will appear here after VPN accounting events reach the portal.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Audit log</p>
            <h2>Recent security events</h2>
          </div>
        </div>
        <div class="stack-list" *ngIf="auditLog().length; else emptyAuditState">
          @for (entry of auditLog(); track entry.id) {
            <div class="stack-item">
              <strong>{{ entry.action }}</strong>
              <span>{{ entry.actorType }} / {{ entry.entityType }} / {{ entry.entityId }}</span>
              <span>{{ entry.createdAt | date: 'medium' }}</span>
            </div>
          }
        </div>
        <ng-template #emptyAuditState>
          <div class="empty-state">
            <h3>No audit entries yet</h3>
            <p class="muted-note">Security-relevant actions will accumulate here as users and admins interact with the portal.</p>
          </div>
        </ng-template>
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
  protected readonly pendingRequestCount = () => this.requests().filter((request) => request.status === 'pending').length;

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
