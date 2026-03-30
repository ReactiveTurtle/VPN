import { DatePipe, NgIf } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { PortalApiService } from '../core/portal-api.service';
import { AdminSession } from '../core/models';
import { AdminSectionNavComponent } from './admin-section-nav.component';

@Component({
  selector: 'app-admin-sessions-page',
  standalone: true,
  imports: [DatePipe, NgIf, AdminSectionNavComponent],
  template: `
    <section class="hero hero-single">
      <div class="hero-main">
        <p class="eyebrow">Суперадминка</p>
        <h1>Активные сессии и недавние подключения</h1>
        <p class="lead">Раздел сессий теперь изолирован от модерации заявок, чтобы быстрее контролировать живые подключения и последние события VPN.</p>

        <div class="compact-stats section-block">
          <article class="compact-stat">
            <span class="metric-label">Всего</span>
            <strong>{{ sessions().length }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Активные</span>
            <strong>{{ activeSessionCount() }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Авторизованные</span>
            <strong>{{ authorizedSessionCount() }}</strong>
          </article>
        </div>
      </div>
    </section>

    <app-admin-section-nav />

    <section class="panel request-panel" *ngIf="message() as message">
      <div class="feedback success">{{ message }}</div>
    </section>

    <section class="panel request-panel" *ngIf="error() as error">
      <div class="feedback error">{{ error }}</div>
    </section>

    <section class="panel data-panel">
      <div class="content-section-header">
        <div>
          <p class="eyebrow">VPN-сессии</p>
          <h2>Текущая и недавняя активность</h2>
          <p>Здесь можно просматривать свежие подключения и при необходимости вручную завершать активные сессии.</p>
        </div>
      </div>

      <div class="stack-list" *ngIf="sessions().length; else emptyState">
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

      <ng-template #emptyState>
        <div class="empty-state">
          <h3>Нет недавних сессий</h3>
          <p class="muted-note">Активность появится здесь после поступления accounting-событий с VPN-хоста.</p>
        </div>
      </ng-template>
    </section>
  `
})
export class AdminSessionsPage {
  private readonly api = inject(PortalApiService);

  protected readonly sessions = signal<AdminSession[]>([]);
  protected readonly busySessionId = signal<number | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly activeSessionCount = computed(() => this.sessions().filter((session) => session.active).length);
  protected readonly authorizedSessionCount = computed(() => this.sessions().filter((session) => session.authorized).length);

  constructor() {
    this.loadSessions();
  }

  protected disconnectSession(sessionId: number): void {
    this.busySessionId.set(sessionId);
    this.message.set(null);
    this.error.set(null);

    this.api.disconnectAdminSession(sessionId).subscribe({
      next: () => {
        this.busySessionId.set(null);
        this.message.set(`Сессия #${sessionId} отключена.`);
        this.loadSessions();
      },
      error: () => {
        this.busySessionId.set(null);
        this.error.set(`Не удалось отключить сессию #${sessionId}.`);
      }
    });
  }

  private loadSessions(): void {
    this.api.getAdminSessions().subscribe({
      next: (sessions) => this.sessions.set(sessions),
      error: () => this.error.set('Не удалось загрузить VPN-сессии.')
    });
  }
}
