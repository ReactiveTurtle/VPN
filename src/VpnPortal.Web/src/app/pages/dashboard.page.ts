import { AsyncPipe, DatePipe, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject, catchError, of, shareReplay, switchMap } from 'rxjs';
import { PortalApiService } from '../core/portal-api.service';
import { IssuedVpnDeviceCredential } from '../core/models';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [AsyncPipe, DatePipe, NgIf, FormsModule],
  template: `
    <section class="hero hero-single" *ngIf="dashboard$ | async as dashboard">
      <div class="hero-main">
        <p class="eyebrow">Личный кабинет</p>
        <h1>{{ dashboard.email }}</h1>
        <p class="lead">Здесь вы управляете доступом для устройств: создаёте пароль, видите привязанный IP и при необходимости меняете доступ без лишних шагов.</p>

        <div class="compact-stats section-block">
          <article class="compact-stat">
            <span class="metric-label">Статус доступа</span>
            <strong>{{ dashboard.active ? 'Активен' : 'Неактивен' }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Устройства</span>
            <strong>{{ dashboard.devices.length }} / {{ dashboard.maxDevices }}</strong>
          </article>
          <article class="compact-stat">
            <span class="metric-label">Активность</span>
            <strong>{{ dashboard.sessions.length }}</strong>
          </article>
        </div>
      </div>
    </section>

    <section class="panel request-panel" *ngIf="message() as message">
      <div class="feedback success">{{ message }}</div>
    </section>

    <section class="panel request-panel" *ngIf="error() as error">
      <div class="feedback error">{{ error }}</div>
    </section>

    <section class="panel data-panel section-subtle">
      <div class="content-section-header content-section-header-single">
        <div>
          <p class="eyebrow">Разделы кабинета</p>
          <h2>Быстрые переходы</h2>
          <p>Основные сценарии вынесены в отдельные секции, чтобы доступы, справка и активность не смешивались на одной длинной ленте.</p>
        </div>
      </div>

      <nav class="section-nav" aria-label="Разделы личного кабинета">
        <a href="#device-access">
          <strong>Доступы</strong>
          <span>Создание устройства, свежевыданные данные и список доступов.</span>
        </a>
        <a href="#setup-guide">
          <strong>Справка</strong>
          <span>Ручная настройка и пошаговые инструкции по платформам.</span>
        </a>
        <a href="#vpn-sessions">
          <strong>VPN-сессии</strong>
          <span>Текущие и недавние подключения после accounting-событий.</span>
        </a>
      </nav>
    </section>

    <section class="content-section" *ngIf="dashboard$ | async as dashboard" id="device-access">
      <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">Доступы</p>
            <h2>Устройства и выданные доступы</h2>
            <p>Создавайте доступы, сохраняйте свежевыданный пароль и управляйте устройствами в одном рабочем блоке.</p>
          </div>
        </div>

        <div class="auth-form">
          <label>
            <span>Название устройства</span>
            <input type="text" [(ngModel)]="newDeviceName" name="newDeviceName" placeholder="Ноутбук дома" />
          </label>
          <button type="button" class="button primary" (click)="issueDeviceCredential()">Создать доступ для устройства</button>
        </div>

        @if (issuedCredential()) {
          <div class="soft-divider credential-panel credential-panel-inline">
            <p class="eyebrow">Свежевыданный доступ</p>
            <div class="credential-grid pending-block">
              <div class="activation-link">
                <strong>VPN-логин</strong>
                <div>
                  <button
                    type="button"
                    class="copy-code"
                    title="Нажмите, чтобы скопировать"
                    (click)="copyValue(issuedCredential()?.vpnUsername, 'VPN-логин скопирован.')">
                    <code>{{ issuedCredential()?.vpnUsername }}</code>
                  </button>
                </div>
              </div>
              <div class="activation-link">
                <strong>Пароль устройства</strong>
                <div>
                  <button
                    type="button"
                    class="copy-code"
                    title="Нажмите, чтобы скопировать"
                    (click)="copyValue(issuedCredential()?.vpnPassword, 'Пароль устройства скопирован.')">
                    <code>{{ issuedCredential()?.vpnPassword }}</code>
                  </button>
                </div>
              </div>
            </div>

            <div class="feature-list pending-block">
              <div>
                <strong>{{ issuedCredential()?.onboarding?.title }}</strong>
                <p class="detail-copy">{{ issuedCredential()?.onboarding?.summary }}</p>
              </div>
              @for (step of issuedCredential()?.onboarding?.steps ?? []; track step) {
                <div>
                  <span>{{ step }}</span>
                </div>
              }
            </div>
          </div>
        }

        <div class="content-section-header content-section-header-single soft-divider">
          <div>
            <p class="eyebrow">Список доступов</p>
            <h2>Выданные устройства</h2>
            <p>У каждого устройства свой логин, пароль и привязанный IP. Если привязки ещё нет, она появится после первого успешного подключения.</p>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.devices.length; else noDevicesState">
          @for (device of dashboard.devices; track device.id) {
            <article class="stack-item device-card">
              <div class="device-card-header">
                <div class="list-copy">
                  <strong>{{ device.deviceName }}</strong>
                  <div class="meta-row">
                    <span class="status-chip">{{ deviceStatusLabel(device.status) }}</span>
                    <span>{{ device.vpnUsername || 'Логин будет показан после выдачи доступа.' }}</span>
                  </div>
                </div>
              </div>

              <div class="device-meta-grid pending-block">
                <div>
                  <span class="metric-label">Привязанный IP</span>
                  <strong>{{ device.boundIpAddress || 'Ещё не привязан' }}</strong>
                  <p class="detail-copy" *ngIf="!device.boundIpAddress">Первое успешное подключение привяжет текущий IP автоматически.</p>
                </div>
                <div>
                  <span class="metric-label">Последняя активность</span>
                  <strong>{{ device.lastSeenAt ? (device.lastSeenAt | date: 'medium') : 'Пока нет' }}</strong>
                  <p class="detail-copy" *ngIf="device.boundIpLastSeenAt">IP замечен {{ device.boundIpLastSeenAt | date: 'medium' }}</p>
                </div>
              </div>

              @if (device.credentialRotatedAt) {
                <p class="detail-copy pending-block">Пароль обновлялся {{ device.credentialRotatedAt | date: 'medium' }}</p>
              }

              @if (device.status !== 'revoked') {
                <div class="action-row pending-block">
                  @if (device.vpnUsername) {
                    <button type="button" class="button secondary compact" (click)="rotateDeviceCredential(device.id)">Сменить пароль</button>
                  }
                  @if (device.boundIpAddress) {
                    <button type="button" class="button ghost compact" (click)="unbindDeviceIp(device.id)">Отвязать IP</button>
                  }
                  <button type="button" class="button danger compact" (click)="revokeDevice(device.id)">Отозвать доступ</button>
                </div>
              }
            </article>
          }
        </div>

        <ng-template #noDevicesState>
          <div class="empty-state">
            <h3>Пока нет доступов для устройств</h3>
            <p class="muted-note">Создайте первый доступ для устройства, чтобы получить логин и пароль для VPN.</p>
          </div>
        </ng-template>
      </article>
    </section>

    <section class="content-section" *ngIf="dashboard$ | async as dashboard" id="setup-guide">
      <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">Справка</p>
            <h2>Ручная настройка</h2>
            <p>Первое успешное подключение автоматически привязывает текущий IP к устройству. Если IP изменился, сначала отвяжите старый IP у нужного устройства.</p>
          </div>
        </div>

        <div class="guide-grid">
          @for (guide of dashboard.platformGuides; track guide.platform) {
            <div class="stack-item guide-card">
              <strong>{{ guide.title }}</strong>
              <span>{{ guide.summary }}</span>
              <div class="feature-list pending-block">
                @for (step of guide.steps; track step) {
                  <div>
                    <span>{{ step }}</span>
                  </div>
                }
              </div>
            </div>
          }
        </div>
      </article>
    </section>

    <section class="content-section" *ngIf="dashboard$ | async as dashboard" id="vpn-sessions">
      <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">Активность</p>
            <h2>VPN-сессии</h2>
            <p>Здесь отображаются текущие и недавние подключения после поступления accounting-событий в портал.</p>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.sessions.length; else noSessionsState">
          @for (session of dashboard.sessions; track session.id) {
            <div class="stack-item">
              <strong>{{ session.deviceName || 'Устройство без названия' }}</strong>
              <div class="meta-row">
                <span>{{ session.sourceIp }}</span>
                <span>{{ session.assignedVpnIp || 'VPN IP ещё не назначен' }}</span>
                <span>{{ session.active ? 'Сессия активна' : 'Сессия завершена' }}</span>
              </div>
              <p class="detail-copy">Начало {{ session.startedAt | date: 'medium' }}</p>
            </div>
          }
        </div>

        <ng-template #noSessionsState>
          <div class="empty-state">
            <h3>Пока нет недавних VPN-сессий</h3>
            <p class="muted-note">После первого успешного подключения здесь появится история активности устройства.</p>
          </div>
        </ng-template>
      </article>
    </section>
  `
})
export class DashboardPage {
  private readonly api = inject(PortalApiService);
  private readonly refresh$ = new BehaviorSubject<void>(undefined);

  protected readonly dashboard$ = this.refresh$.pipe(
    switchMap(() => this.api.getDashboard()),
    catchError(() => of(null)),
    shareReplay(1)
  );
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly issuedCredential = signal<IssuedVpnDeviceCredential | null>(null);
  protected newDeviceName = '';

  protected revokeDevice(deviceId: number): void {
    this.api.revokeDevice(deviceId).subscribe({
      next: () => {
        this.reloadDashboard();
        this.issuedCredential.set(null);
        this.message.set('Доступ для устройства отозван.');
        this.error.set(null);
      },
      error: () => this.error.set('Не удалось отозвать доступ для устройства.')
    });
  }

  protected issueDeviceCredential(): void {
    if (!this.newDeviceName.trim()) {
      this.error.set('Сначала укажите название устройства.');
      return;
    }

    this.api.issueDeviceCredential({
      deviceName: this.newDeviceName.trim()
    }).subscribe({
      next: (result) => {
        this.issuedCredential.set(result);
        this.reloadDashboard();
        this.message.set(result.message);
        this.error.set(null);
        this.newDeviceName = '';
      },
      error: () => this.error.set('Не удалось создать доступ для устройства.')
    });
  }

  protected rotateDeviceCredential(deviceId: number): void {
    this.api.rotateDeviceCredential(deviceId).subscribe({
      next: (result) => {
        this.issuedCredential.set(result);
        this.reloadDashboard();
        this.message.set(result.message);
        this.error.set(null);
      },
      error: () => this.error.set('Не удалось сменить пароль для этого устройства.')
    });
  }

  protected unbindDeviceIp(deviceId: number): void {
    this.api.unbindDeviceIp(deviceId).subscribe({
      next: () => {
        this.reloadDashboard();
        this.message.set('IP отвязан. Следующее успешное подключение привяжет новый IP автоматически.');
        this.error.set(null);
      },
      error: () => this.error.set('Не удалось отвязать IP у этого устройства.')
    });
  }

  protected copyValue(value: string | null | undefined, successMessage: string): void {
    if (!value) {
      return;
    }

    navigator.clipboard.writeText(value).then(() => {
      this.message.set(successMessage);
      this.error.set(null);
    }).catch(() => {
      this.error.set('Не удалось скопировать значение в буфер обмена.');
    });
  }

  private reloadDashboard(): void {
    this.refresh$.next();
  }

  protected deviceStatusLabel(status: string): string {
    switch (status) {
      case 'active':
        return 'доступ активен';
      case 'revoked':
        return 'доступ отозван';
      default:
        return status;
    }
  }
}
