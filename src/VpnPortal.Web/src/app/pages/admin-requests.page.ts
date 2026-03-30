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
        <p class="eyebrow">Операции администратора</p>
        <h1>Модерация доступа и контроль runtime-состояния</h1>
        <p class="lead">Этот экран объединяет модерацию заявок, управление политиками пользователей, недавние сессии и аудит для суперадминистраторов.</p>

        <div class="summary-grid section-block">
          <article class="summary-card">
            <span class="metric-label">Ожидающие заявки</span>
            <strong>{{ pendingRequestCount() }}</strong>
            <p class="detail-copy">Заявки, которые еще ждут рассмотрения.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Пользователи</span>
            <strong>{{ users().length }}</strong>
            <p class="detail-copy">Пользователи, доступные для администрирования.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Недавние сессии</span>
            <strong>{{ sessions().length }}</strong>
            <p class="detail-copy">Активные и недавно замеченные записи VPN-подключений.</p>
          </article>
        </div>
      </div>

      <aside class="hero-side">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Фокус оператора</p>
            <h2>Что требует внимания сейчас</h2>
          </div>
        </div>

        <div class="feature-list">
          <div>
            <strong>Очередь модерации</strong>
            <p class="detail-copy">Одобряйте ожидающие заявки и сразу отправляйте ссылки активации.</p>
          </div>
          <div>
            <strong>Управление сессиями</strong>
            <p class="detail-copy">Отключайте подозрительные активные сессии, когда доступна runtime-интеграция.</p>
          </div>
          <div>
            <strong>Проверка аудита</strong>
            <p class="detail-copy">Отслеживайте важные события учетных записей, учетных данных и сессий в одном потоке.</p>
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
          <p class="eyebrow">Очередь модерации</p>
          <h2>Проверка заявок на доступ</h2>
        </div>
      </div>

      <div class="stack-list" *ngIf="requests().length; else emptyState">
        @for (request of requests(); track request.id) {
          <article class="stack-item">
            <div class="panel-heading">
              <div>
                <strong>{{ request.name || 'Неизвестный пользователь' }}</strong>
                <p>{{ request.email }}</p>
              </div>
              <span class="badge">{{ requestStatusLabel(request.status) }}</span>
            </div>

            <div class="feature-list">
              <div>
                <strong>Подана</strong>
                <p class="detail-copy">{{ request.submittedAt | date: 'medium' }}</p>
              </div>
              <div>
                <strong>Комментарий проверки</strong>
                <p class="detail-copy">{{ request.adminComment || 'Ожидает рассмотрения' }}</p>
              </div>
            </div>

            @if (request.activationLink) {
              <div class="activation-link"><code>{{ request.activationLink }}</code></div>
            }

            @if (request.status === 'pending') {
              <div class="pending-block auth-form">
                <label>
                  <span>Комментарий администратора</span>
                  <textarea [(ngModel)]="comments[request.id]" [name]="'comment-' + request.id" rows="3" placeholder="Необязательный комментарий администратора"></textarea>
                </label>
                <div class="action-row">
                  <button type="button" class="button primary" [disabled]="busyRequestId() === request.id" (click)="approve(request)">
                    {{ busyRequestId() === request.id ? 'Сохранение...' : 'Одобрить' }}
                  </button>
                  <button type="button" class="button danger" [disabled]="busyRequestId() === request.id" (click)="reject(request)">Отклонить</button>
                </div>
              </div>
            } @else {
              <div class="chip-row pending-block">
                <span class="session-pill">Уже обработана</span>
              </div>
            }
          </article>
        }
      </div>

      <ng-template #emptyState>
        <div class="empty-state">
          <h3>В очереди нет заявок</h3>
          <p class="muted-note">Публичная форма пока не создала элементов для модерации.</p>
        </div>
      </ng-template>
    </section>

    <section class="operations-grid admin-ops-grid">
      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Пользователи</p>
            <h2>Управление учетными записями</h2>
          </div>
        </div>
        <div class="stack-list" *ngIf="users().length; else emptyUsersState">
          @for (user of users(); track user.id) {
            <div class="stack-item">
              <strong>{{ user.username }}</strong>
              <span>{{ user.email }}</span>
              <span>{{ userStatusLabel(user.active) }} / максимум устройств: {{ user.maxDevices }} / привязано: {{ user.deviceCount }}</span>
              <div class="auth-form pending-block">
                <label>
                  <span>Максимум устройств</span>
                  <input type="number" min="1" [(ngModel)]="userLimits[user.id]" [name]="'limit-' + user.id" />
                </label>
                <div class="action-row">
                  <button type="button" class="button primary compact" [disabled]="busyUserId() === user.id" (click)="saveUser(user)">Сохранить</button>
                  <button type="button" class="button ghost compact" [disabled]="busyUserId() === user.id" (click)="toggleUser(user)">
                    {{ user.active ? 'Деактивировать' : 'Активировать' }}
                  </button>
                </div>
              </div>
            </div>
          }
        </div>
        <ng-template #emptyUsersState>
          <div class="empty-state">
            <h3>Пока нет управляемых пользователей</h3>
            <p class="muted-note">Одобренные заявки создают пользователей портала, которыми можно управлять здесь.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">VPN-сессии</p>
            <h2>Активные и недавние подключения</h2>
          </div>
        </div>
        <div class="stack-list" *ngIf="sessions().length; else emptySessionsState">
          @for (session of sessions(); track session.id) {
            <div class="stack-item">
              <strong>{{ session.username }}</strong>
              <span>{{ session.sourceIp }} -> {{ session.assignedVpnIp || 'ожидается адрес' }}</span>
              <span>{{ session.deviceName || 'Неизвестное устройство' }}</span>
              <button type="button" class="button danger compact" [disabled]="busySessionId() === session.id || !session.active" (click)="disconnectSession(session.id)">
                {{ busySessionId() === session.id ? 'Отключение...' : (session.active ? 'Отключить' : 'Закрыта') }}
              </button>
            </div>
          }
        </div>
        <ng-template #emptySessionsState>
          <div class="empty-state">
            <h3>Нет недавних сессий</h3>
            <p class="muted-note">Активность сессий появится здесь после поступления accounting-событий VPN в портал.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Журнал аудита</p>
            <h2>Недавние события безопасности</h2>
          </div>
        </div>
        <div class="stack-list" *ngIf="auditLog().length; else emptyAuditState">
          @for (entry of auditLog(); track entry.id) {
            <div class="stack-item">
              <strong>{{ auditActionLabel(entry.action) }}</strong>
              <span>{{ actorTypeLabel(entry.actorType) }} / {{ entityTypeLabel(entry.entityType) }} / {{ entry.entityId }}</span>
              <span>{{ entry.createdAt | date: 'medium' }}</span>
            </div>
          }
        </div>
        <ng-template #emptyAuditState>
          <div class="empty-state">
            <h3>Записей аудита пока нет</h3>
            <p class="muted-note">Здесь будут накапливаться значимые действия пользователей и администраторов.</p>
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
        this.message.set(`Заявка #${updated.id} одобрена. Ссылка активации готова.`);
      },
      error: () => {
        this.busyRequestId.set(null);
        this.error.set(`Не удалось одобрить заявку #${request.id}.`);
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
        this.message.set(`Заявка #${updated.id} отклонена.`);
      },
      error: () => {
        this.busyRequestId.set(null);
        this.error.set(`Не удалось отклонить заявку #${request.id}.`);
      }
    });
  }

  private loadRequests(): void {
    this.api.getAdminRequests().subscribe({
      next: (requests) => this.requests.set(requests),
      error: () => this.error.set('Не удалось загрузить очередь модерации. Запустите API и обновите страницу.')
    });
  }

  protected disconnectSession(sessionId: number): void {
    this.busySessionId.set(sessionId);
    this.api.disconnectAdminSession(sessionId).subscribe({
      next: () => {
        this.busySessionId.set(null);
        this.message.set(`Сессия #${sessionId} отключена.`);
        this.loadSessions();
        this.loadAuditLog();
      },
      error: () => {
        this.busySessionId.set(null);
        this.error.set(`Не удалось отключить сессию #${sessionId}.`);
      }
    });
  }

  protected saveUser(user: AdminUser): void {
    this.busyUserId.set(user.id);
    this.api.updateAdminUser(user.id, { maxDevices: this.userLimits[user.id] ?? user.maxDevices }).subscribe({
      next: (updated) => {
        this.busyUserId.set(null);
        this.mergeUser(updated);
        this.message.set(`Пользователь ${updated.username} обновлен.`);
        this.loadAuditLog();
      },
      error: () => {
        this.busyUserId.set(null);
        this.error.set(`Не удалось обновить пользователя ${user.username}.`);
      }
    });
  }

  protected toggleUser(user: AdminUser): void {
    this.busyUserId.set(user.id);
    this.api.setAdminUserStatus(user.id, !user.active).subscribe({
      next: (updated) => {
        this.busyUserId.set(null);
        this.mergeUser(updated);
        this.message.set(`Пользователь ${updated.username} теперь ${updated.active ? 'активен' : 'неактивен'}.`);
        this.loadAuditLog();
      },
      error: () => {
        this.busyUserId.set(null);
        this.error.set(`Не удалось изменить статус пользователя ${user.username}.`);
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
      error: () => this.error.set('Не удалось загрузить пользователей.')
    });
  }

  private loadSessions(): void {
    this.api.getAdminSessions().subscribe({
      next: (sessions) => this.sessions.set(sessions),
      error: () => this.error.set('Не удалось загрузить VPN-сессии.')
    });
  }

  private loadAuditLog(): void {
    this.api.getAuditLog().subscribe({
      next: (entries) => this.auditLog.set(entries),
      error: () => this.error.set('Не удалось загрузить журнал аудита.')
    });
  }

  private mergeRequest(updated: VpnRequest): void {
    this.requests.update((requests) => requests.map((request) => request.id === updated.id ? updated : request));
  }

  private mergeUser(updated: AdminUser): void {
    this.userLimits[updated.id] = updated.maxDevices;
    this.users.update((users) => users.map((user) => user.id === updated.id ? updated : user));
  }

  protected requestStatusLabel(status: string): string {
    switch (status) {
      case 'pending':
        return 'Ожидает';
      case 'approved':
        return 'Одобрена';
      case 'rejected':
        return 'Отклонена';
      default:
        return status;
    }
  }

  protected userStatusLabel(active: boolean): string {
    return active ? 'активен' : 'неактивен';
  }

  protected actorTypeLabel(actorType: string): string {
    switch (actorType) {
      case 'user':
        return 'пользователь';
      case 'superadmin':
        return 'суперадминистратор';
      case 'system':
        return 'система';
      default:
        return actorType;
    }
  }

  protected entityTypeLabel(entityType: string): string {
    switch (entityType) {
      case 'vpn_user':
        return 'пользователь VPN';
      case 'vpn_request':
        return 'заявка на VPN';
      case 'vpn_device_credential':
        return 'учетные данные устройства VPN';
      case 'ip_change_confirmation':
        return 'подтверждение смены IP';
      case 'trusted_ip':
        return 'доверенный IP';
      default:
        return entityType;
    }
  }

  protected auditActionLabel(action: string): string {
    switch (action) {
      case 'request_approved':
        return 'Заявка одобрена';
      case 'request_rejected':
        return 'Заявка отклонена';
      case 'user_activated':
        return 'Пользователь активирован';
      case 'user_deactivated':
        return 'Пользователь деактивирован';
      case 'device_credential_issued':
        return 'Выданы учетные данные устройства';
      case 'device_credential_rotated':
        return 'Учетные данные устройства изменены';
      case 'ip_confirmation_requested':
        return 'Запрошено подтверждение IP';
      case 'ip_confirmed':
        return 'IP-адрес подтвержден';
      case 'vpn_new_ip_blocked':
        return 'Новое подключение с IP заблокировано';
      case 'account_activated':
        return 'Учетная запись активирована';
      default:
        return action;
    }
  }
}
