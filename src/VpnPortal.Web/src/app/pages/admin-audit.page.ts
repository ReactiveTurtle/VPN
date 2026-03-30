import { DatePipe, NgIf } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { PortalApiService } from '../core/portal-api.service';
import { AuditLogEntry } from '../core/models';
import { AdminSectionNavComponent } from './admin-section-nav.component';
import { actorTypeLabel, auditActionLabel, entityTypeLabel } from './admin-shared';

@Component({
  selector: 'app-admin-audit-page',
  standalone: true,
  imports: [DatePipe, NgIf, AdminSectionNavComponent],
  template: `
    <section class="hero hero-single">
      <div class="hero-main">
        <p class="eyebrow">Суперадминка</p>
        <h1>Аудит</h1>
        <p class="lead">Раздел аудита показывает недавние события безопасности и операционные действия без смешивания с очередью заявок или управлением пользователями.</p>

        <div class="compact-stats section-block">
          <article class="compact-stat">
            <span class="metric-label">Записей</span>
            <strong>{{ auditLog().length }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Системных</span>
            <strong>{{ systemEventCount() }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Админ-действий</span>
            <strong>{{ superadminEventCount() }}</strong>
          </article>
        </div>
      </div>
    </section>

    <app-admin-section-nav />

    <section class="panel request-panel" *ngIf="error() as error">
      <div class="feedback error">{{ error }}</div>
    </section>

    <section class="panel data-panel">
      <div class="content-section-header">
        <div>
          <p class="eyebrow">Журнал аудита</p>
          <h2>Недавние события</h2>
          <p>Лента аудита отделена от остальных разделов, чтобы быстрее разбирать инциденты и проверять последствия административных действий.</p>
        </div>
      </div>

      <div class="stack-list" *ngIf="auditLog().length; else emptyState">
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

      <ng-template #emptyState>
        <div class="empty-state">
          <h3>Записей аудита пока нет</h3>
          <p class="muted-note">События появятся после действий пользователей, суперадминистратора или внутренних VPN-интеграций.</p>
        </div>
      </ng-template>
    </section>
  `
})
export class AdminAuditPage {
  private readonly api = inject(PortalApiService);

  protected readonly auditLog = signal<AuditLogEntry[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly systemEventCount = computed(() => this.auditLog().filter((entry) => entry.actorType === 'system').length);
  protected readonly superadminEventCount = computed(() => this.auditLog().filter((entry) => entry.actorType === 'superadmin').length);
  protected readonly actorTypeLabel = actorTypeLabel;
  protected readonly entityTypeLabel = entityTypeLabel;
  protected readonly auditActionLabel = auditActionLabel;

  constructor() {
    this.loadAuditLog();
  }

  private loadAuditLog(): void {
    this.api.getAuditLog().subscribe({
      next: (entries) => this.auditLog.set(entries),
      error: () => this.error.set('Не удалось загрузить журнал аудита.')
    });
  }
}
