import { AsyncPipe, DatePipe, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject, catchError, of, switchMap } from 'rxjs';
import { PortalApiService } from '../core/portal-api.service';
import { IssuedVpnDeviceCredential } from '../core/models';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [AsyncPipe, DatePipe, NgIf, FormsModule],
  template: `
    <section class="hero" *ngIf="dashboard$ | async as dashboard">
      <div class="hero-main">
        <p class="eyebrow">Кабинет пользователя</p>
        <h1>VPN-кабинет пользователя {{ dashboard.username }}</h1>
        <p class="lead">Управляйте VPN-учетными данными устройств, просматривайте недавние сессии и подтверждайте смену IP-адресов прямо в портале.</p>

        <div class="summary-grid section-block">
          <article class="summary-card">
            <span class="metric-label">Учетная запись</span>
            <strong>{{ dashboard.active ? 'Активна' : 'Неактивна' }}</strong>
            <p class="detail-copy">Вход в портал и подключение VPN доступны только пока учетная запись активна.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Политика устройств</span>
            <strong>{{ dashboard.devices.length }} / {{ dashboard.maxDevices }}</strong>
            <p class="detail-copy">Количество активных зарегистрированных устройств относительно текущего лимита.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Активность</span>
            <strong>{{ dashboard.sessions.length }} сессий</strong>
            <p class="detail-copy">Здесь видны недавние сессии и подтвержденные доверенные IP-адреса.</p>
          </article>
        </div>
      </div>

      <aside class="hero-side">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Кратко</p>
            <h2>Состояние безопасности</h2>
          </div>
        </div>

        <div class="status-grid">
          <article class="stat-card">
            <span class="metric-label">Устройства</span>
            <span class="metric-value">{{ dashboard.devices.length }}</span>
          </article>
          <article class="stat-card">
            <span class="metric-label">Доверенные IP</span>
            <span class="metric-value">{{ dashboard.trustedIps.length }}</span>
          </article>
          <article class="stat-card">
            <span class="metric-label">Ожидают подтверждения</span>
            <span class="metric-value">{{ dashboard.pendingIpConfirmations.length }}</span>
          </article>
        </div>
      </aside>
    </section>

    <section class="panel request-panel" *ngIf="message() as message">
      <div class="feedback success">{{ message }}</div>
    </section>

    <section class="panel request-panel" *ngIf="error() as error">
      <div class="feedback error">{{ error }}</div>
    </section>

    <section class="split-layout" *ngIf="dashboard$ | async as dashboard">
      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Доверенные устройства</p>
            <h2>Привязанные устройства</h2>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.devices.length; else noDevicesState">
          @for (device of dashboard.devices; track device.id) {
            <div class="stack-item">
              <strong>{{ device.deviceName }}</strong>
              <span>{{ device.platform }} / {{ device.deviceType }}</span>
              <span>Статус: {{ deviceStatusLabel(device.status) }}</span>
              <span>{{ device.vpnUsername || 'VPN-учетные данные для устройства еще не выданы.' }}</span>
              @if (device.credentialRotatedAt) {
                <span>Изменено {{ device.credentialRotatedAt | date: 'medium' }}</span>
              }
              @if (device.status !== 'revoked') {
                <div class="action-row">
                  @if (device.vpnUsername) {
                    <button type="button" class="button secondary compact" (click)="rotateDeviceCredential(device.id)">Сменить VPN-пароль</button>
                  }
                  <button type="button" class="button ghost compact" (click)="revokeDevice(device.id)">Отозвать устройство</button>
                </div>
              }
              @if (device.onboarding) {
                <div class="feature-list pending-block">
                  <div>
                    <strong>{{ device.onboarding.title }}</strong>
                    <p class="detail-copy">{{ device.onboarding.summary }}</p>
                  </div>
                </div>
              }
            </div>
          }
        </div>

        <ng-template #noDevicesState>
          <div class="empty-state">
            <h3>Пока нет зарегистрированных устройств</h3>
            <p class="muted-note">Выдайте первые VPN-учетные данные, чтобы создать доступ для конкретного устройства.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Доступ для устройства</p>
            <h2>Выдать VPN-учетные данные</h2>
          </div>
        </div>

        <div class="auth-form">
          <label>
            <span>Название устройства</span>
            <input type="text" [(ngModel)]="newDeviceName" name="newDeviceName" placeholder="iPhone Алексея" />
          </label>
          <label>
            <span>Тип устройства</span>
            <input type="text" [(ngModel)]="newDeviceType" name="newDeviceType" placeholder="phone" />
          </label>
          <label>
            <span>Платформа</span>
            <input type="text" [(ngModel)]="newDevicePlatform" name="newDevicePlatform" placeholder="ios" />
          </label>
          <button type="button" class="button primary" (click)="issueDeviceCredential()">Выдать VPN-учетные данные</button>
        </div>

        @if (issuedCredential()) {
          <div class="activation-link"><code>{{ issuedCredential()?.vpnUsername }}</code></div>
          <div class="activation-link"><code>{{ issuedCredential()?.vpnPassword }}</code></div>
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
        }
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">VPN-сессии</p>
            <h2>Текущая активность</h2>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.sessions.length; else noSessionsState">
          @for (session of dashboard.sessions; track session.id) {
            <div class="stack-item">
              <strong>{{ session.assignedVpnIp || 'Ожидается IP-адрес' }}</strong>
              <span>{{ session.sourceIp }}</span>
              <span>{{ session.startedAt | date: 'medium' }}</span>
            </div>
          }
        </div>

        <ng-template #noSessionsState>
          <div class="empty-state">
            <h3>Нет недавних VPN-сессий</h3>
            <p class="muted-note">Информация о сессиях появится здесь после поступления accounting-событий в портал.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Доверенные IP</p>
            <h2>Разрешенные адреса источника</h2>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.trustedIps.length; else noIpsState">
          @for (ip of dashboard.trustedIps; track ip.id) {
            <div class="stack-item">
              <strong>{{ ip.ipAddress }}</strong>
              <span>{{ trustedIpStatusLabel(ip.status) }}</span>
              <span>{{ ip.lastSeenAt ? (ip.lastSeenAt | date: 'medium') : 'Недавней активности нет' }}</span>
            </div>
          }
        </div>

        <ng-template #noIpsState>
          <div class="empty-state">
            <h3>Нет подтвержденных IP-адресов</h3>
            <p class="muted-note">Новые адреса подключения можно подтвердить через письмо со ссылкой.</p>
          </div>
        </ng-template>
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Подтверждение IP</p>
            <h2>Запросить подтверждение нового IP</h2>
          </div>
        </div>

        <div class="auth-form">
          <label>
            <span>Новый IP-адрес источника</span>
            <input type="text" [(ngModel)]="requestedIp" name="requestedIp" placeholder="198.51.100.77" />
          </label>
          <label>
            <span>Устройство</span>
            <select [(ngModel)]="selectedDeviceId" name="selectedDeviceId">
              <option [ngValue]="null">Устройство не выбрано</option>
              @for (device of dashboard.devices; track device.id) {
                <option [ngValue]="device.id">{{ device.deviceName }}</option>
              }
            </select>
          </label>
          <button type="button" class="button primary" (click)="requestIpConfirmation()">Создать ссылку подтверждения</button>
        </div>

        @if (lastConfirmationLink()) {
          <div class="activation-link"><code>{{ lastConfirmationLink() }}</code></div>
        }

        @if (dashboard.pendingIpConfirmations.length) {
          <div class="stack-list pending-block">
            @for (confirmation of dashboard.pendingIpConfirmations; track confirmation.id) {
              <div class="stack-item">
                <strong>{{ confirmation.requestedIp }}</strong>
                <span>Истекает {{ confirmation.expiresAt | date: 'medium' }}</span>
              </div>
            }
          </div>
        }
      </article>

      <article class="panel data-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Инструкции по платформам</p>
            <h2>Справка по ручной настройке</h2>
          </div>
        </div>

        <div class="stack-list" *ngIf="dashboard.platformGuides.length; else noGuidesState">
          @for (guide of dashboard.platformGuides; track guide.platform) {
            <div class="stack-item">
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
        <ng-template #noGuidesState>
          <div class="empty-state">
            <h3>Инструкции по настройке недоступны</h3>
            <p class="muted-note">Инструкции для конкретных платформ появятся здесь после настройки.</p>
          </div>
        </ng-template>
      </article>
    </section>
  `
})
export class DashboardPage {
  private readonly api = inject(PortalApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly refresh$ = new BehaviorSubject<void>(undefined);

  protected readonly dashboard$ = this.refresh$.pipe(
    switchMap(() => this.api.getDashboard()),
    catchError(() => of(null))
  );
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly lastConfirmationLink = signal<string | null>(null);
  protected readonly issuedCredential = signal<IssuedVpnDeviceCredential | null>(null);
  protected requestedIp = '';
  protected selectedDeviceId: number | null = null;
  protected newDeviceName = '';
  protected newDeviceType = '';
  protected newDevicePlatform = '';

  constructor() {
    const token = this.route.snapshot.queryParamMap.get('confirmIpToken');
    if (token) {
      this.confirmIp(token);
    }
  }

  protected revokeDevice(deviceId: number): void {
    this.api.revokeDevice(deviceId).subscribe({
      next: () => {
        this.reloadDashboard();
        this.message.set('Устройство отозвано. Политика доступа обновлена.');
      },
      error: () => this.error.set('Не удалось отозвать устройство.')
    });
  }

  protected issueDeviceCredential(): void {
    if (!this.newDeviceName.trim() || !this.newDeviceType.trim() || !this.newDevicePlatform.trim()) {
      this.error.set('Сначала укажите название устройства, тип и платформу.');
      return;
    }

    this.api.issueDeviceCredential({
      deviceName: this.newDeviceName.trim(),
      deviceType: this.newDeviceType.trim(),
      platform: this.newDevicePlatform.trim()
    }).subscribe({
      next: (result) => {
        this.issuedCredential.set(result);
        this.reloadDashboard();
        this.message.set(result.message);
        this.error.set(null);
        this.newDeviceName = '';
        this.newDeviceType = '';
        this.newDevicePlatform = '';
      },
      error: () => this.error.set('Не удалось выдать VPN-учетные данные для устройства.')
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
      error: () => this.error.set('Не удалось сменить VPN-пароль для этого устройства.')
    });
  }

  protected requestIpConfirmation(): void {
    if (!this.requestedIp.trim()) {
      this.error.set('Сначала укажите IP-адрес.');
      return;
    }

    this.api.requestIpConfirmation({ requestedIp: this.requestedIp.trim(), deviceId: this.selectedDeviceId }).subscribe({
      next: (result) => {
        this.message.set(result.message);
        this.lastConfirmationLink.set(result.confirmationLink);
      },
      error: () => this.error.set('Не удалось создать запрос на подтверждение IP-адреса.')
    });
  }

  private confirmIp(token: string): void {
    this.api.confirmIp(token).subscribe({
      next: () => {
        this.reloadDashboard();
        this.message.set('IP-адрес подтвержден. Список доверенных IP обновлен.');
      },
      error: () => this.error.set('Не удалось подтвердить IP-адрес по этому токену.')
    });
  }

  private reloadDashboard(): void {
    this.refresh$.next();
  }

  protected deviceStatusLabel(status: string): string {
    switch (status) {
      case 'active':
        return 'активно';
      case 'revoked':
        return 'отозвано';
      default:
        return status;
    }
  }

  protected trustedIpStatusLabel(status: string): string {
    switch (status) {
      case 'active':
        return 'разрешен';
      case 'pending':
        return 'ожидает подтверждения';
      case 'revoked':
        return 'отозван';
      default:
        return status;
    }
  }
}
