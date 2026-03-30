import { DatePipe, NgIf } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { PortalApiService } from '../core/portal-api.service';
import { VpnRequest } from '../core/models';
import { AdminSectionNavComponent } from './admin-section-nav.component';
import { requestStatusLabel } from './admin-shared';

@Component({
  selector: 'app-admin-request-history-page',
  standalone: true,
  imports: [DatePipe, NgIf, AdminSectionNavComponent],
  template: `
    <section class="hero hero-single">
      <div class="hero-main">
        <p class="eyebrow">Суперадминка</p>
        <h1>История одобрения и отклонения</h1>
        <p class="lead">Здесь собраны уже обработанные заявки с итоговым решением, временем обработки и административным комментарием.</p>

        <div class="compact-stats section-block">
          <article class="compact-stat">
            <span class="metric-label">Обработано</span>
            <strong>{{ processedRequests().length }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Одобрено</span>
            <strong>{{ approvedCount() }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Отклонено</span>
            <strong>{{ rejectedCount() }}</strong>
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
          <p class="eyebrow">История модерации</p>
          <h2>Обработанные заявки</h2>
          <p>Новые запросы остаются в очереди ожидания, а здесь история показана короткими записями без отдельного карточного интерфейса.</p>
        </div>
      </div>

      <div class="stack-list" *ngIf="processedRequests().length; else emptyState">
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

      <ng-template #emptyState>
        <div class="empty-state">
          <h3>История пока пуста</h3>
          <p class="muted-note">После первого одобрения или отклонения заявки раздел начнет заполняться.</p>
        </div>
      </ng-template>
    </section>
  `
})
export class AdminRequestHistoryPage {
  private readonly api = inject(PortalApiService);

  protected readonly userTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  protected readonly requests = signal<VpnRequest[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly processedRequests = computed(() => this.requests()
    .filter((request) => request.status !== 'pending')
    .slice()
    .sort((left, right) => new Date(right.processedAt ?? right.submittedAt).getTime() - new Date(left.processedAt ?? left.submittedAt).getTime()));
  protected readonly approvedCount = computed(() => this.processedRequests().filter((request) => request.status === 'approved').length);
  protected readonly rejectedCount = computed(() => this.processedRequests().filter((request) => request.status === 'rejected').length);
  protected readonly requestStatusLabel = requestStatusLabel;

  constructor() {
    this.loadRequests();
  }

  private loadRequests(): void {
    this.api.getAdminRequests().subscribe({
      next: (requests) => this.requests.set(requests),
      error: () => this.error.set('Не удалось загрузить историю обработки заявок.')
    });
  }
}
