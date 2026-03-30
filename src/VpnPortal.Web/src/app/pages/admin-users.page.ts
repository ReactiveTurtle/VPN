import { DatePipe, NgIf } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PortalApiService } from '../core/portal-api.service';
import { AdminUser } from '../core/models';
import { AdminSectionNavComponent } from './admin-section-nav.component';
import { userStatusLabel } from './admin-shared';

@Component({
  selector: 'app-admin-users-page',
  standalone: true,
  imports: [DatePipe, NgIf, FormsModule, AdminSectionNavComponent],
  template: `
    <section class="hero hero-single">
      <div class="hero-main">
        <p class="eyebrow">Суперадминка</p>
        <h1>Управление учетными записями</h1>
        <p class="lead">Учетные записи выделены в отдельный экран, чтобы статус пользователей и лимиты устройств не терялись среди заявок, аудита и VPN-сессий.</p>

        <div class="compact-stats section-block">
          <article class="compact-stat">
            <span class="metric-label">Пользователи</span>
            <strong>{{ users().length }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Активные</span>
            <strong>{{ activeUserCount() }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Неактивные</span>
            <strong>{{ inactiveUserCount() }}</strong>
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
          <p class="eyebrow">Пользователи</p>
          <h2>Администрирование учетных записей</h2>
          <p>Здесь редактируется лимит устройств и вручную переключается активность каждой учетной записи.</p>
        </div>
      </div>

      <div class="stack-list" *ngIf="users().length; else emptyState">
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

      <ng-template #emptyState>
        <div class="empty-state">
          <h3>Пока нет управляемых пользователей</h3>
          <p class="muted-note">Пользователи появляются здесь после одобрения заявок и активации учетных записей.</p>
        </div>
      </ng-template>
    </section>
  `
})
export class AdminUsersPage {
  private readonly api = inject(PortalApiService);

  protected readonly users = signal<AdminUser[]>([]);
  protected readonly busyUserId = signal<number | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly activeUserCount = computed(() => this.users().filter((user) => user.active).length);
  protected readonly inactiveUserCount = computed(() => this.users().filter((user) => !user.active).length);
  protected readonly userLimits: Record<number, number> = {};
  protected readonly userStatusLabel = userStatusLabel;

  constructor() {
    this.loadUsers();
  }

  protected saveUser(user: AdminUser): void {
    this.busyUserId.set(user.id);
    this.message.set(null);
    this.error.set(null);

    this.api.updateAdminUser(user.id, { maxDevices: this.userLimits[user.id] ?? user.maxDevices }).subscribe({
      next: (updated) => {
        this.busyUserId.set(null);
        this.mergeUser(updated);
        this.message.set(`Пользователь ${updated.username} обновлен.`);
      },
      error: () => {
        this.busyUserId.set(null);
        this.error.set(`Не удалось обновить пользователя ${user.username}.`);
      }
    });
  }

  protected toggleUser(user: AdminUser): void {
    this.busyUserId.set(user.id);
    this.message.set(null);
    this.error.set(null);

    this.api.setAdminUserStatus(user.id, !user.active).subscribe({
      next: (updated) => {
        this.busyUserId.set(null);
        this.mergeUser(updated);
        this.message.set(`Пользователь ${updated.username} теперь ${updated.active ? 'активен' : 'неактивен'}.`);
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

  private mergeUser(updated: AdminUser): void {
    this.userLimits[updated.id] = updated.maxDevices;
    this.users.update((users) => users.map((user) => user.id === updated.id ? updated : user));
  }
}
