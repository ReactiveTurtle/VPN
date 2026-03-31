import { DatePipe, NgIf } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NotificationService } from '../core/notification.service';
import { PortalApiService } from '../core/portal-api.service';
import { AdminSession, AdminUser, AuditLogEntry, VpnRequest } from '../core/models';
import { actorTypeLabel, auditActionLabel, entityTypeLabel, requestStatusLabel, userStatusLabel } from './admin-shared';
import { SectionMenuComponent, SectionMenuItem } from './section-menu.component';

@Component({
  selector: 'app-admin-dashboard-page',
  standalone: true,
  imports: [DatePipe, NgIf, FormsModule, SectionMenuComponent],
  template: `
    <div class="dashboard-page-shell">
      <section class="hero hero-single">
        <div class="hero-main">
          <p class="eyebrow">Суперадминка</p>
          <h1>Операционный кабинет</h1>
          <p class="lead">Работайте с заявками, аудитом, VPN-сессиями и учетными записями в том же формате, что и в личном кабинете: один экран, секции и быстрые действия по месту.</p>

          <div class="hero-inline-summary">
            <span class="hero-summary-pill">Новых заявок: {{ pendingRequestCount() }}</span>
            <span class="hero-summary-pill">Активных сессий: {{ activeSessionCount() }}</span>
            <span class="hero-summary-pill">Активных пользователей: {{ activeUserCount() }}</span>
            @if (systemEventCount() > 0) {
              <span class="hero-summary-pill hero-summary-pill-warning">Системных событий в аудите: {{ systemEventCount() }}</span>
            }
          </div>
        </div>
      </section>

      <app-section-menu
        class="dashboard-tabs"
        [compact]="true"
        [ariaLabel]="'Разделы суперадминки'"
        [activeSectionId]="activeSectionId()"
        (sectionChange)="activeSectionId.set($event)"
        [sections]="adminSections()" />

      @if (activeSectionId() === 'admin-pending-requests') {
      <section class="content-section section-shell dashboard-tab-content">
        <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">Новые заявки</p>
            <h2>Очередь модерации</h2>
            <p>Здесь остаются только заявки, которые ещё ждут решения.</p>
          </div>
        </div>

        <div class="stack-list" *ngIf="pendingRequests().length; else emptyPendingState">
          @for (request of pendingRequests(); track request.id) {
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
                  <p class="detail-copy">{{ request.submittedAt | date: 'medium' }} <span *ngIf="userTimeZone">({{ userTimeZone }})</span></p>
                </div>
                <div>
                  <strong>Комментарий</strong>
                  <p class="detail-copy">{{ request.adminComment || 'Ожидает рассмотрения' }}</p>
                </div>
              </div>

              @if (request.activationLink) {
                <div class="activation-link"><code>{{ request.activationLink }}</code></div>
              }

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
            </article>
          }
        </div>

        <ng-template #emptyPendingState>
          <div class="empty-state">
            <h3>В очереди нет заявок</h3>
            <p class="muted-note">Публичная форма пока не создала элементов для модерации.</p>
          </div>
        </ng-template>
        </article>
      </section>
      }

      @if (activeSectionId() === 'admin-request-history') {
      <section class="content-section section-shell dashboard-tab-content">
        <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">История</p>
            <h2>Решения по заявкам</h2>
            <p>Здесь собрана только история уже принятых решений.</p>
          </div>
        </div>

        <div class="stack-list" *ngIf="processedRequests().length; else emptyHistoryState">
          @for (request of processedRequests(); track request.id) {
            <article class="stack-item">
              <p>
                Заявка #{{ request.id }} от
                <strong>{{ request.name || request.email }}</strong>
                ({{ request.email }})
                {{ requestStatusLabel(request.status).toLowerCase() }}
                {{ request.processedAt ? (request.processedAt | date: 'medium') : 'без указанного времени' }}.
                Комментарий: {{ request.adminComment || 'не указан' }}.
                @if (request.activationLink) {
                  Ссылка активации: <code>{{ request.activationLink }}</code>
                }
              </p>
            </article>
          }
        </div>

        <ng-template #emptyHistoryState>
          <div class="empty-state">
            <h3>История пока пуста</h3>
            <p class="muted-note">После первого одобрения или отклонения заявки раздел начнет заполняться.</p>
          </div>
        </ng-template>
        </article>
      </section>
      }

      @if (activeSectionId() === 'admin-audit-log') {
      <section class="content-section section-shell dashboard-tab-content">
        <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">Аудит</p>
            <h2>Журнал событий</h2>
            <p>Лента аудита отделена от остальных разделов, чтобы быстрее разбирать инциденты и последствия административных действий.</p>
          </div>
        </div>

        <div class="stack-list" *ngIf="auditLog().length; else emptyAuditState">
          @for (entry of auditLog(); track entry.id) {
            <article class="stack-item">
              <div class="panel-heading">
                <div>
                  <strong>{{ auditActionLabel(entry.action) }}</strong>
                  <p>{{ actorTypeLabel(entry.actorType) }} / {{ entityTypeLabel(entry.entityType) }} / {{ entry.entityId }}</p>
                </div>
                <span class="badge">{{ entry.createdAt | date: 'short' }}</span>
              </div>

              <div class="feature-list">
                <div>
                  <strong>Источник</strong>
                  <p class="detail-copy">{{ entry.ipAddress || 'IP не зафиксирован' }}</p>
                </div>
                <div>
                  <strong>Инициатор</strong>
                  <p class="detail-copy">{{ entry.actorId ?? 'system' }}</p>
                </div>
                <div>
                  <strong>Детали</strong>
                  <p class="detail-copy">{{ entry.detailsJson || 'Дополнительные поля отсутствуют' }}</p>
                </div>
              </div>
            </article>
          }
        </div>

        <ng-template #emptyAuditState>
          <div class="empty-state">
            <h3>Записей аудита пока нет</h3>
            <p class="muted-note">События появятся после действий пользователей, суперадминистратора или внутренних VPN-интеграций.</p>
          </div>
        </ng-template>
        </article>
      </section>
      }

      @if (activeSectionId() === 'admin-sessions') {
      <section class="content-section section-shell dashboard-tab-content">
        <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">Сессии</p>
            <h2>Текущая и недавняя активность</h2>
            <p>Здесь можно просматривать свежие подключения и при необходимости вручную завершать активные сессии.</p>
          </div>
        </div>

        <div class="stack-list" *ngIf="sessions().length; else emptySessionsState">
          @for (session of sessions(); track session.id) {
            <article class="stack-item">
              <div class="panel-heading">
                <div>
                  <strong>{{ session.username }}</strong>
                  <p>{{ session.deviceName || 'Неизвестное устройство' }}</p>
                </div>
                <span class="badge">{{ session.active ? 'Активна' : 'Закрыта' }}</span>
              </div>

              <div class="feature-list">
                <div>
                  <strong>Маршрут</strong>
                  <p class="detail-copy">{{ session.sourceIp }} -> {{ session.assignedVpnIp || 'ожидается адрес' }}</p>
                </div>
                <div>
                  <strong>Начало</strong>
                  <p class="detail-copy">{{ session.startedAt | date: 'medium' }}</p>
                </div>
                <div>
                  <strong>Последняя активность</strong>
                  <p class="detail-copy">{{ session.lastSeenAt ? (session.lastSeenAt | date: 'medium') : 'Нет обновлений' }}</p>
                </div>
              </div>

              <div class="chip-row pending-block">
                <span class="session-pill">{{ session.authorized ? 'Авторизована' : 'Не авторизована' }}</span>
                <button type="button" class="button danger compact" [disabled]="busySessionId() === session.id || !session.active" (click)="disconnectSession(session.id)">
                  {{ busySessionId() === session.id ? 'Отключение...' : (session.active ? 'Отключить' : 'Закрыта') }}
                </button>
              </div>
            </article>
          }
        </div>

        <ng-template #emptySessionsState>
          <div class="empty-state">
            <h3>Нет недавних сессий</h3>
            <p class="muted-note">Активность появится здесь после поступления accounting-событий с VPN-хоста.</p>
          </div>
        </ng-template>
        </article>
      </section>
      }

      @if (activeSectionId() === 'admin-accounts') {
      <section class="content-section section-shell dashboard-tab-content">
        <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">Учетные записи</p>
            <h2>Управление пользователями</h2>
            <p>Здесь редактируется лимит устройств и вручную переключается активность каждой учетной записи.</p>
          </div>
        </div>

        <div class="stack-list" *ngIf="users().length; else emptyUsersState">
          @for (user of users(); track user.id) {
            <article class="stack-item">
              <div class="panel-heading">
                <div>
                  <strong>{{ user.username }}</strong>
                  <p>{{ user.email }}</p>
                </div>
                <span class="badge">{{ userStatusLabel(user.active) }}</span>
              </div>

              <div class="feature-list">
                <div>
                  <strong>Устройства</strong>
                  <p class="detail-copy">Лимит: {{ user.maxDevices }} / привязано: {{ user.deviceCount }}</p>
                </div>
                <div>
                  <strong>Создана</strong>
                  <p class="detail-copy">{{ user.createdAt | date: 'medium' }}</p>
                </div>
                <div>
                  <strong>Последний вход</strong>
                  <p class="detail-copy">{{ user.lastLoginAt ? (user.lastLoginAt | date: 'medium') : 'Еще не входил' }}</p>
                </div>
              </div>

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
            </article>
          }
        </div>

        <ng-template #emptyUsersState>
          <div class="empty-state">
            <h3>Пока нет управляемых пользователей</h3>
            <p class="muted-note">Пользователи появляются здесь после одобрения заявок и активации учетных записей.</p>
          </div>
        </ng-template>
        </article>
      </section>
      }
    </div>
  `
})
export class AdminDashboardPage {
  private readonly api = inject(PortalApiService);
  private readonly notifications = inject(NotificationService);

  protected readonly userTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  protected readonly requests = signal<VpnRequest[]>([]);
  protected readonly sessions = signal<AdminSession[]>([]);
  protected readonly auditLog = signal<AuditLogEntry[]>([]);
  protected readonly users = signal<AdminUser[]>([]);
  protected readonly busyRequestId = signal<number | null>(null);
  protected readonly busySessionId = signal<number | null>(null);
  protected readonly busyUserId = signal<number | null>(null);
  protected readonly activeSectionId = signal<string>('admin-pending-requests');
  protected readonly comments: Record<number, string> = {};
  protected readonly userLimits: Record<number, number> = {};

  protected readonly pendingRequests = computed(() => this.requests()
    .filter((request) => request.status === 'pending')
    .slice()
    .sort((left, right) => new Date(right.submittedAt).getTime() - new Date(left.submittedAt).getTime()));
  protected readonly processedRequests = computed(() => this.requests()
    .filter((request) => request.status !== 'pending')
    .slice()
    .sort((left, right) => new Date(right.processedAt ?? right.submittedAt).getTime() - new Date(left.processedAt ?? left.submittedAt).getTime()));
  protected readonly pendingRequestCount = computed(() => this.pendingRequests().length);
  protected readonly processedRequestCount = computed(() => this.processedRequests().length);
  protected readonly activeSessionCount = computed(() => this.sessions().filter((session) => session.active).length);
  protected readonly systemEventCount = computed(() => this.auditLog().filter((entry) => entry.actorType === 'system').length);
  protected readonly activeUserCount = computed(() => this.users().filter((user) => user.active).length);
  protected readonly adminSections = computed<SectionMenuItem[]>(() => [
    {
      id: 'admin-pending-requests',
      label: 'Новые заявки',
      description: 'Очередь модерации запросов, которые ждут решения.',
      count: `${this.pendingRequestCount()}`,
      accent: true
    },
    {
      id: 'admin-request-history',
      label: 'История',
      description: 'Одобренные и отклоненные заявки с итогом обработки.',
      count: `${this.processedRequestCount()}`
    },
    {
      id: 'admin-audit-log',
      label: 'Аудит',
      description: 'Недавние события безопасности и операционные действия.',
      count: `${this.auditLog().length}`
    },
    {
      id: 'admin-sessions',
      label: 'Сессии',
      description: 'Живые и недавние VPN-подключения.',
      count: `${this.activeSessionCount()} / ${this.sessions().length}`
    },
    {
      id: 'admin-accounts',
      label: 'Учетные записи',
      description: 'Статусы пользователей и лимиты устройств.',
      count: `${this.activeUserCount()} / ${this.users().length}`
    }
  ]);

  protected readonly requestStatusLabel = requestStatusLabel;
  protected readonly userStatusLabel = userStatusLabel;
  protected readonly actorTypeLabel = actorTypeLabel;
  protected readonly entityTypeLabel = entityTypeLabel;
  protected readonly auditActionLabel = auditActionLabel;

  constructor() {
    this.loadRequests();
    this.loadSessions();
    this.loadAuditLog();
    this.loadUsers();
  }

  protected approve(request: VpnRequest): void {
    this.busyRequestId.set(request.id);

    this.api.approveRequest(request.id, this.comments[request.id]).subscribe({
      next: (updated) => {
        this.mergeRequest(updated);
        this.busyRequestId.set(null);
        this.showSuccess(`Заявка #${updated.id} одобрена. Ссылка активации готова.`);
      },
      error: () => {
        this.busyRequestId.set(null);
        this.showError(`Не удалось одобрить заявку #${request.id}.`);
      }
    });
  }

  protected reject(request: VpnRequest): void {
    this.busyRequestId.set(request.id);

    this.api.rejectRequest(request.id, this.comments[request.id]).subscribe({
      next: (updated) => {
        this.mergeRequest(updated);
        this.busyRequestId.set(null);
        this.showSuccess(`Заявка #${updated.id} отклонена.`);
      },
      error: () => {
        this.busyRequestId.set(null);
        this.showError(`Не удалось отклонить заявку #${request.id}.`);
      }
    });
  }

  protected disconnectSession(sessionId: number): void {
    this.busySessionId.set(sessionId);

    this.api.disconnectAdminSession(sessionId).subscribe({
      next: () => {
        this.busySessionId.set(null);
        this.showSuccess(`Сессия #${sessionId} отключена.`);
        this.loadSessions();
      },
      error: () => {
        this.busySessionId.set(null);
        this.showError(`Не удалось отключить сессию #${sessionId}.`);
      }
    });
  }

  protected saveUser(user: AdminUser): void {
    this.busyUserId.set(user.id);

    this.api.updateAdminUser(user.id, { maxDevices: this.userLimits[user.id] ?? user.maxDevices }).subscribe({
      next: (updated) => {
        this.busyUserId.set(null);
        this.mergeUser(updated);
        this.showSuccess(`Пользователь ${updated.username} обновлен.`);
      },
      error: () => {
        this.busyUserId.set(null);
        this.showError(`Не удалось обновить пользователя ${user.username}.`);
      }
    });
  }

  protected toggleUser(user: AdminUser): void {
    this.busyUserId.set(user.id);

    this.api.setAdminUserStatus(user.id, !user.active).subscribe({
      next: (updated) => {
        this.busyUserId.set(null);
        this.mergeUser(updated);
        this.showSuccess(`Пользователь ${updated.username} теперь ${updated.active ? 'активен' : 'неактивен'}.`);
      },
      error: () => {
        this.busyUserId.set(null);
        this.showError(`Не удалось изменить статус пользователя ${user.username}.`);
      }
    });
  }

  private loadRequests(): void {
    this.api.getAdminRequests().subscribe({
      next: (requests) => this.requests.set(requests),
      error: () => this.showError('Не удалось загрузить очередь модерации.')
    });
  }

  private loadSessions(): void {
    this.api.getAdminSessions().subscribe({
      next: (sessions) => this.sessions.set(sessions),
      error: () => this.showError('Не удалось загрузить VPN-сессии.')
    });
  }

  private loadAuditLog(): void {
    this.api.getAuditLog().subscribe({
      next: (entries) => this.auditLog.set(entries),
      error: () => this.showError('Не удалось загрузить журнал аудита.')
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
      error: () => this.showError('Не удалось загрузить пользователей.')
    });
  }

  private mergeRequest(updated: VpnRequest): void {
    this.requests.update((requests) => requests.map((request) => request.id === updated.id ? updated : request));
  }

  private mergeUser(updated: AdminUser): void {
    this.userLimits[updated.id] = updated.maxDevices;
    this.users.update((users) => users.map((user) => user.id === updated.id ? updated : user));
  }

  private showSuccess(message: string): void {
    this.notifications.success(message);
  }

  private showError(message: string): void {
    this.notifications.error(message);
  }
}
