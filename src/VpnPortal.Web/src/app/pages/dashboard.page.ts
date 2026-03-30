import { AsyncPipe, DatePipe, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BehaviorSubject, catchError, of, shareReplay, switchMap } from 'rxjs';
import { NotificationService } from '../core/notification.service';
import { PortalApiService } from '../core/portal-api.service';
import { IssuedVpnDeviceCredential, TrustedDevice, UserDashboard } from '../core/models';
import { SectionMenuComponent, SectionMenuItem } from './section-menu.component';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [AsyncPipe, DatePipe, NgIf, FormsModule, MatTooltipModule, SectionMenuComponent],
  template: `
    <div class="dashboard-page-shell">
    <section class="hero hero-single" *ngIf="dashboard$ | async as dashboard">
      <div class="hero-main">
        <p class="eyebrow">Личный кабинет</p>
        <h1>Устройства и VPN-доступ</h1>
        <p class="lead">Создавайте доступы для своих устройств, храните свежевыданный пароль и при необходимости отвязывайте IP без лишних шагов.</p>

        <div class="hero-inline-summary">
          <span class="hero-summary-pill">Активных устройств: {{ activeDeviceCount(dashboard.devices) }} из {{ dashboard.maxDevices }}</span>
          @if (!dashboard.active) {
            <span class="hero-summary-pill hero-summary-pill-warning">Доступ в кабинет ограничен</span>
          }
        </div>
      </div>
    </section>

    <app-section-menu
      *ngIf="dashboard$ | async as dashboard"
      class="dashboard-tabs"
      [compact]="true"
      [ariaLabel]="'Разделы личного кабинета'"
      [activeSectionId]="activeSectionId()"
      (sectionChange)="activeSectionId.set($event)"
      [sections]="dashboardSections(dashboard)" />

    @if ((dashboard$ | async); as dashboard) {
      @if (activeSectionId() === 'device-access') {
    <section class="content-section section-shell dashboard-tab-content">
      <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">Устройства</p>
            <h2>Ваши устройства</h2>
            <p>Сначала создайте доступ для нового устройства, затем сохраните пароль и используйте его при подключении.</p>
          </div>
        </div>

        <div class="dashboard-create-flow">
          <div class="dashboard-create-card">
            <div>
              <strong>Добавить устройство</strong>
              <p class="detail-copy">Дайте устройству понятное имя, чтобы потом быстро находить его в списке.</p>
            </div>

            <div class="dashboard-create-form">
              <label>
                <span>Название устройства</span>
                <input type="text" [(ngModel)]="newDeviceName" name="newDeviceName" placeholder="Ноутбук дома" />
              </label>
              <button type="button" class="button primary" (click)="issueDeviceCredential()">Создать доступ</button>
            </div>
          </div>
        </div>

        @if (issuedCredential()) {
          <div class="credential-panel credential-panel-inline credential-panel-prominent pending-block">
            <p class="eyebrow">Новый доступ</p>
            <div class="credential-grid pending-block">
              <div class="activation-link">
                <strong>VPN-логин</strong>
                <div>
                  <button
                    type="button"
                    class="copy-code"
                    [matTooltip]="'Скопировать VPN-логин'"
                    matTooltipPosition="above"
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
                    [matTooltip]="'Скопировать пароль устройства'"
                    matTooltipPosition="above"
                    (click)="copyValue(issuedCredential()?.vpnPassword, 'Пароль устройства скопирован.')">
                    <code>{{ issuedCredential()?.vpnPassword }}</code>
                  </button>
                </div>
              </div>
            </div>

            <div class="guide-summary pending-block">
              <div class="guide-summary-card">
                <strong>{{ issuedCredential()?.onboarding?.title }}</strong>
                <p class="detail-copy">{{ issuedCredential()?.onboarding?.summary }}</p>
              </div>
            </div>

            <div class="ordered-steps pending-block">
              @for (step of issuedCredential()?.onboarding?.steps ?? []; track step; let stepIndex = $index) {
                <div class="guide-step-card">
                  <span class="guide-step-index">{{ stepIndex + 1 }}</span>
                  <span>{{ step }}</span>
                </div>
              }
            </div>
          </div>
        }

        <div class="content-section-header content-section-header-single content-section-header-spacious soft-divider">
          <div>
            <p class="eyebrow">Список устройств</p>
            <h2>Все выданные доступы</h2>
            <p>У каждого устройства свой VPN-логин и свой пароль. Привязанный IP появится после первого успешного подключения.</p>
          </div>
        </div>

        <div class="stack-list stack-list-devices" *ngIf="dashboard.devices.length; else noDevicesState">
          @for (device of dashboard.devices; track device.id) {
            <article class="stack-item device-card">
              <div class="device-card-header">
                <div class="list-copy">
                  <strong>{{ device.deviceName }}</strong>
                  <p class="detail-copy">{{ deviceAccessLabel(device) }}</p>
                </div>
                @if (isRevokedDevice(device)) {
                  <span class="status-chip status-chip-danger">Доступ удалён</span>
                }
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
                  <button type="button" class="button danger compact" (click)="revokeDevice(device.id)">Удалить доступ</button>
                </div>
              }
            </article>
          }
        </div>

        <ng-template #noDevicesState>
          <div class="empty-state">
            <h3>Пока нет устройств</h3>
            <p class="muted-note">Создайте первый доступ, чтобы получить VPN-логин и пароль для подключения.</p>
          </div>
        </ng-template>
      </article>
    </section>
      }
    }

    @if ((dashboard$ | async); as dashboard) {
      @if (activeSectionId() === 'setup-guide') {
    <section class="content-section section-shell dashboard-tab-content">
      <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">Настройка</p>
            <h2>Как подключить устройство</h2>
            <p>После первого успешного подключения портал автоматически запомнит текущий IP устройства. Если IP изменился, сначала отвяжите старый IP в разделе устройств.</p>
          </div>
        </div>

          <div class="guide-grid">
            @for (guide of dashboard.platformGuides; track guide.platform) {
              <div class="stack-item guide-card">
                <div class="guide-summary-card">
                  <strong>{{ guide.title }}</strong>
                  <span>{{ guide.summary }}</span>
                </div>
                <div class="ordered-steps pending-block">
                  @for (step of guide.steps; track step; let stepIndex = $index) {
                    <div class="guide-step-card">
                      <span class="guide-step-index">{{ stepIndex + 1 }}</span>
                      <span>{{ step }}</span>
                    </div>
                  }
                </div>
            </div>
          }
        </div>
      </article>
    </section>
      }
    }

    @if ((dashboard$ | async); as dashboard) {
      @if (activeSectionId() === 'vpn-sessions') {
    <section class="content-section section-shell dashboard-tab-content">
      <article class="panel data-panel">
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">История</p>
            <h2>История подключений</h2>
            <p>Здесь отображаются текущие и недавние подключения ваших устройств.</p>
          </div>
        </div>

        <div class="stack-list stack-list-compact" *ngIf="dashboard.sessions.length; else noSessionsState">
          @for (session of dashboard.sessions; track session.id) {
            <article class="session-card">
              <div class="session-card-top">
                <div class="session-card-copy">
                  <strong>{{ session.deviceName || 'Устройство без названия' }}</strong>
                  <p class="detail-copy">{{ session.startedAt | date: 'medium' }}</p>
                </div>
                <span class="status-chip session-status-chip" [class.status-chip-success]="session.active">{{ session.active ? 'Сейчас онлайн' : 'Завершено' }}</span>
              </div>

              <div class="session-meta-row">
                <span><strong>Внешний IP:</strong> {{ session.sourceIp }}</span>
                <span><strong>VPN IP:</strong> {{ session.assignedVpnIp || 'Ещё не назначен' }}</span>
              </div>
            </article>
          }
        </div>

        <ng-template #noSessionsState>
          <div class="empty-state">
            <h3>История пока пуста</h3>
            <p class="muted-note">После первого успешного подключения здесь появятся недавние подключения ваших устройств.</p>
          </div>
        </ng-template>
      </article>
    </section>
      }
    }
    </div>
  `
})
export class DashboardPage {
  private readonly api = inject(PortalApiService);
  private readonly notifications = inject(NotificationService);
  private readonly refresh$ = new BehaviorSubject<void>(undefined);

  protected readonly dashboard$ = this.refresh$.pipe(
    switchMap(() => this.api.getDashboard()),
    catchError(() => of(null)),
    shareReplay(1)
  );
  protected readonly issuedCredential = signal<IssuedVpnDeviceCredential | null>(null);
  protected readonly activeSectionId = signal<string>('device-access');
  protected newDeviceName = '';

  protected dashboardSections(dashboard: UserDashboard): SectionMenuItem[] {
    return [
      {
        id: 'device-access',
        label: 'Устройства',
        accent: true
      },
      {
        id: 'setup-guide',
        label: 'Настройка'
      },
      {
        id: 'vpn-sessions',
        label: 'История'
      }
    ];
  }

  protected revokeDevice(deviceId: number): void {
    this.api.revokeDevice(deviceId).subscribe({
      next: () => {
        this.reloadDashboard();
        this.issuedCredential.set(null);
        this.showSuccess('Доступ для устройства удалён.');
      },
      error: () => this.showError('Не удалось удалить доступ для устройства.')
    });
  }

  protected issueDeviceCredential(): void {
    if (!this.newDeviceName.trim()) {
      this.showError('Сначала укажите название устройства.');
      return;
    }

    this.api.issueDeviceCredential({
      deviceName: this.newDeviceName.trim()
    }).subscribe({
      next: (result) => {
        this.issuedCredential.set(result);
        this.reloadDashboard();
        this.showSuccess('Доступ создан. Сохраните логин и пароль сейчас.');
        this.newDeviceName = '';
      },
      error: () => this.showError('Не удалось создать доступ для устройства.')
    });
  }

  protected rotateDeviceCredential(deviceId: number): void {
    this.api.rotateDeviceCredential(deviceId).subscribe({
      next: (result) => {
        this.issuedCredential.set(result);
        this.reloadDashboard();
        this.showSuccess('Пароль обновлён. Сохраните новый пароль сейчас.');
      },
      error: () => this.showError('Не удалось сменить пароль для этого устройства.')
    });
  }

  protected unbindDeviceIp(deviceId: number): void {
    this.api.unbindDeviceIp(deviceId).subscribe({
      next: () => {
        this.reloadDashboard();
        this.showSuccess('IP отвязан. Следующее подключение привяжет новый адрес автоматически.');
      },
      error: () => this.showError('Не удалось отвязать IP у этого устройства.')
    });
  }

  protected copyValue(value: string | null | undefined, successMessage: string): void {
    if (!value) {
      return;
    }

    navigator.clipboard.writeText(value).then(() => {
      this.notifications.info(successMessage);
    }).catch(() => {
      this.notifications.error('Не удалось скопировать значение в буфер обмена.');
    });
  }

  private reloadDashboard(): void {
    this.refresh$.next();
  }

  private showSuccess(message: string): void {
    this.notifications.success(message);
  }

  private showError(message: string): void {
    this.notifications.error(message);
  }

  protected deviceAccessLabel(device: TrustedDevice): string {
    if (this.isRevokedDevice(device)) {
      return device.vpnUsername ? `VPN-логин: ${device.vpnUsername}` : 'Доступ удалён';
    }

    return device.vpnUsername ? `VPN-логин: ${device.vpnUsername}` : 'VPN-логин появится после выдачи доступа.';
  }

  protected isRevokedDevice(device: TrustedDevice): boolean {
    return device.status === 'revoked' || device.credentialStatus === 'revoked';
  }

  protected activeDeviceCount(devices: TrustedDevice[]): number {
    return devices.filter((device) => !this.isRevokedDevice(device)).length;
  }
}
