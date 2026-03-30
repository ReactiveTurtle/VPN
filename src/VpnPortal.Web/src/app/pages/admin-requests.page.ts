import { DatePipe, NgIf } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PortalApiService } from '../core/portal-api.service';
import { VpnRequest } from '../core/models';
import { AdminSectionNavComponent } from './admin-section-nav.component';
import { requestStatusLabel } from './admin-shared';

@Component({
  selector: 'app-admin-requests-page',
  standalone: true,
  imports: [DatePipe, NgIf, FormsModule, AdminSectionNavComponent],
  template: `
    <section class="hero hero-single">
      <div class="hero-main">
        <p class="eyebrow">Суперадминка</p>
        <h1>Ожидающие заявки</h1>
        <p class="lead">Основной рабочий экран модерации. Здесь остались только новые запросы, которые еще требуют решения.</p>

        <div class="compact-stats section-block">
          <article class="compact-stat">
            <span class="metric-label">Ожидают</span>
            <strong>{{ pendingRequestCount() }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Всего заявок</span>
            <strong>{{ requests().length }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Обработано</span>
            <strong>{{ processedRequestCount() }}</strong>
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
          <p class="eyebrow">Очередь модерации</p>
          <h2>Проверка заявок на доступ</h2>
          <p>Обработанные заявки вынесены в отдельную историю, поэтому в списке ниже остались только элементы, которые ждут решения.</p>
        </div>
      </div>

      <div class="stack-list" *ngIf="pendingRequests().length; else emptyState">
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

      <ng-template #emptyState>
        <div class="empty-state">
          <h3>В очереди нет заявок</h3>
          <p class="muted-note">Публичная форма пока не создала элементов для модерации.</p>
        </div>
      </ng-template>
    </section>
  `
})
export class AdminRequestsPage {
  private readonly api = inject(PortalApiService);

  protected readonly userTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  protected readonly requests = signal<VpnRequest[]>([]);
  protected readonly busyRequestId = signal<number | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly comments: Record<number, string> = {};
  protected readonly pendingRequests = computed(() => this.requests()
    .filter((request) => request.status === 'pending')
    .slice()
    .sort((left, right) => new Date(right.submittedAt).getTime() - new Date(left.submittedAt).getTime()));
  protected readonly pendingRequestCount = computed(() => this.pendingRequests().length);
  protected readonly processedRequestCount = computed(() => this.requests().filter((request) => request.status !== 'pending').length);
  protected readonly requestStatusLabel = requestStatusLabel;

  constructor() {
    this.loadRequests();
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

  private mergeRequest(updated: VpnRequest): void {
    this.requests.update((requests) => requests.map((request) => request.id === updated.id ? updated : request));
  }
}
