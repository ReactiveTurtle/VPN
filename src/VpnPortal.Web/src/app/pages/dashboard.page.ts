import { AsyncPipe, DatePipe, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BehaviorSubject, catchError, of, shareReplay, switchMap } from 'rxjs';
import { NotificationService } from '../core/notification.service';
import { PortalApiService } from '../core/portal-api.service';
import { IssuedVpnDeviceCredential, TrustedDevice, UserDashboard, VpnOnboardingField } from '../core/models';
import { SectionMenuComponent, SectionMenuItem } from './section-menu.component';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [AsyncPipe, DatePipe, NgIf, FormsModule, MatTooltipModule, SectionMenuComponent],
  template: `
    <div class="dashboard-page-shell">
    <section class="hero hero-single" *ngIf="dashboard$ | async as dashboard">
      <div class="hero-main">
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

        <div class="content-section-header content-section-header-single content-section-header-spacious soft-divider">
          <div>
            <p class="eyebrow">Список устройств</p>
            <h2>Все выданные доступы</h2>
            <p>У каждого устройства свои VPN-учетные данные. Привязанный IP появится после первого успешного подключения.</p>
          </div>
        </div>

        <div class="stack-list stack-list-devices" *ngIf="dashboard.devices.length; else noDevicesState">
          @for (device of dashboard.devices; track device.id) {
            <article class="stack-item device-card">
              <div class="device-card-header">
                <div class="list-copy">
                  <strong>{{ device.deviceName }}</strong>
                </div>
              </div>

              @if (issuedCredential()?.deviceId === device.id) {
                <div class="credential-panel credential-panel-inline credential-panel-prominent pending-block">
                  <p class="eyebrow">Новый доступ</p>
                  <div class="credential-inline-summary pending-block">
                    <p>
                      <strong>Имя пользователя:</strong>
                      <button
                        type="button"
                        class="copy-code copy-code-inline"
                        [matTooltip]="'Скопировать имя пользователя VPN'"
                        matTooltipPosition="above"
                        (click)="copyValue(issuedCredential()?.vpnUsername, 'Имя пользователя VPN скопировано.')">
                        <code>{{ issuedCredential()?.vpnUsername }}</code>
                      </button>
                    </p>
                    <p>
                      <strong>Пароль устройства:</strong>
                      <button
                        type="button"
                        class="copy-code copy-code-inline"
                        [matTooltip]="'Скопировать пароль устройства'"
                        matTooltipPosition="above"
                        (click)="copyValue(issuedCredential()?.vpnPassword, 'Пароль устройства скопирован.')">
                        <code>{{ issuedCredential()?.vpnPassword }}</code>
                      </button>
                    </p>
                    @if (fieldValue(issuedCredential()?.onboarding?.fields ?? [], 'Сервер'); as server) {
                      <p>
                        <strong>Сервер:</strong>
                        <button
                          type="button"
                          class="copy-code copy-code-inline"
                          [matTooltip]="'Скопировать адрес сервера'"
                          matTooltipPosition="above"
                          (click)="copyValue(server, 'Адрес сервера скопирован.')">
                          <code>{{ server }}</code>
                        </button>
                      </p>
                    }
                    @if (fieldValue(issuedCredential()?.onboarding?.fields ?? [], 'Remote ID'); as remoteId) {
                      <p>
                        <strong>Remote ID:</strong>
                        <button
                          type="button"
                          class="copy-code copy-code-inline"
                          [matTooltip]="'Скопировать Remote ID'"
                          matTooltipPosition="above"
                          (click)="copyValue(remoteId, 'Remote ID скопирован.')">
                          <code>{{ remoteId }}</code>
                        </button>
                      </p>
                    }
                  </div>

                  <div class="credential-panel-actions pending-block">
                    <button type="button" class="button secondary compact" (click)="openInstructionDialog()">Инструкции по подключению</button>
                  </div>
                </div>
              }

              <div class="device-facts pending-block">
                <div class="device-fact-block">
                  @if (device.vpnUsername) {
                    <div class="copy-code-inline">
                      <span class="copy-code-label">Имя пользователя:</span>
                      <button
                        type="button"
                        class="copy-code"
                        [matTooltip]="'Скопировать логин для подключения'"
                        matTooltipPosition="above"
                        (click)="copyValue(device.vpnUsername, 'Имя пользователя VPN скопировано.')">
                        <code>{{ device.vpnUsername }}</code>
                      </button>
                    </div>
                  } @else {
                    <p class="detail-copy">Имя пользователя VPN появится после выдачи доступа.</p>
                  }
                </div>

                <div class="device-fact-block">
                  <p class="device-fact-line">Привязанный IP: <span>{{ device.boundIpAddress || 'Ещё не привязан' }}</span></p>
                  @if (!device.boundIpAddress) {
                    <p class="detail-copy">Первое успешное подключение привяжет текущий IP автоматически.</p>
                  }
                </div>

                <div class="device-fact-block">
                  <p class="device-fact-line">Последняя активность: <span>{{ device.lastSeenAt ? (device.lastSeenAt | date: 'medium') : 'Пока нет' }}</span></p>
                  @if (device.boundIpLastSeenAt) {
                    <p class="detail-copy">IP замечен {{ device.boundIpLastSeenAt | date: 'medium' }}</p>
                  }
                </div>

                @if (device.credentialRotatedAt) {
                  <div class="device-fact-block">
                    <p class="device-fact-line">Пароль обновлялся: <span>{{ device.credentialRotatedAt | date: 'medium' }}</span></p>
                  </div>
                }
              </div>

              <div class="action-row pending-block">
                @if (device.vpnUsername) {
                  <button type="button" class="button secondary compact" (click)="rotateDeviceCredential(device.id)">Сменить пароль</button>
                }
                @if (device.boundIpAddress) {
                  <button type="button" class="button ghost compact" (click)="unbindDeviceIp(device.id)">Отвязать IP</button>
                }
                <button type="button" class="button danger compact" (click)="revokeDevice(device.id)">Удалить</button>
              </div>
            </article>
          }
        </div>

        <ng-template #noDevicesState>
          <div class="empty-state">
            <h3>Пока нет устройств</h3>
            <p class="muted-note">Создайте первый доступ, чтобы получить логин и пароль для подключения.</p>
          </div>
        </ng-template>
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

    @if ((dashboard$ | async); as dashboard) {
      @if (instructionDialogOpen()) {
        @if (issuedCredential(); as credential) {
        <div class="dialog-backdrop" (click)="closeInstructionDialog()">
          <section class="dialog-panel dialog-panel-wide" (click)="$event.stopPropagation()">
            <div class="dialog-header">
              <div>
                <p class="eyebrow">Инструкции</p>
                <h2>Подключение устройства {{ credential.deviceName }}</h2>
                <p>Выберите платформу и используйте текущие учетные данные устройства.</p>
              </div>
              <button type="button" class="dialog-close" (click)="closeInstructionDialog()" aria-label="Закрыть">x</button>
            </div>

            <div class="platform-tabs">
              @for (guide of modalGuides(dashboard); track guide.platform) {
                <button
                  type="button"
                  class="platform-tab"
                  [class.active]="activeInstructionPlatform() === guide.platform"
                  (click)="selectInstructionPlatform(guide.platform)">
                  <span class="platform-icon" [class.platform-icon-android]="guide.platform === 'android'" [class.platform-icon-ios]="guide.platform === 'ios'" [class.platform-icon-windows]="guide.platform === 'windows'">
                    {{ platformIconText(guide.platform) }}
                  </span>
                  <span>{{ platformLabel(guide.platform) }}</span>
                </button>
              }
            </div>

            @if (selectedModalGuide(dashboard); as guide) {
              <div class="dialog-body">
                <div class="guide-summary-card">
                  <strong>{{ guide.title }}</strong>
                  <p class="detail-copy">{{ guide.summary }}</p>
                </div>

                <div class="credential-inline-summary pending-block">
                  <p>
                    <strong>Имя пользователя:</strong>
                    <button type="button" class="copy-code copy-code-inline" (click)="copyValue(credential.vpnUsername, 'Имя пользователя VPN скопировано.')"><code>{{ credential.vpnUsername }}</code></button>
                  </p>
                  <p>
                    <strong>Пароль устройства:</strong>
                    <button type="button" class="copy-code copy-code-inline" (click)="copyValue(credential.vpnPassword, 'Пароль устройства скопирован.')"><code>{{ credential.vpnPassword }}</code></button>
                  </p>
                  @if (fieldValue(resolvedGuideFields(guide, credential.onboarding.fields), 'Сервер'); as server) {
                    <p>
                      <strong>Сервер:</strong>
                      <button type="button" class="copy-code copy-code-inline" (click)="copyValue(server, 'Адрес сервера скопирован.')"><code>{{ server }}</code></button>
                    </p>
                  }
                  @if (fieldValue(resolvedGuideFields(guide, credential.onboarding.fields), 'Remote ID'); as remoteId) {
                    <p>
                      <strong>Remote ID:</strong>
                      <button type="button" class="copy-code copy-code-inline" (click)="copyValue(remoteId, 'Remote ID скопирован.')"><code>{{ remoteId }}</code></button>
                    </p>
                  }
                </div>

                @if (guide.platform === 'ios') {
                  <div class="credential-panel-actions pending-block">
                    <button type="button" class="button primary compact" (click)="downloadMobileConfig(credential, guide)">Скачать .mobileconfig</button>
                  </div>
                }

                <div class="ordered-steps pending-block">
                  @for (step of resolvedGuideSteps(guide, credential); track step; let stepIndex = $index) {
                    <div class="guide-step-card">
                      <span class="guide-step-index">{{ stepIndex + 1 }}</span>
                      <span class="guide-step-copy-text">
                        @for (segment of stepSegments(step, resolvedGuideFields(guide, credential.onboarding.fields)); track $index) {
                          @if (segment.copyValue) {
                            <button
                              type="button"
                              class="copy-code guide-step-inline-copy"
                              [matTooltip]="'Скопировать ' + segment.label.toLowerCase()"
                              matTooltipPosition="above"
                              (click)="copyValue(segment.copyValue, 'Значение «' + segment.label + '» скопировано.')">
                              <code>{{ segment.text }}</code>
                            </button>
                          } @else {
                            <span>{{ segment.text }}</span>
                          }
                        }
                      </span>
                    </div>
                  }
                </div>
              </div>
            }
          </section>
        </div>
      }
      }
    }
    </div>
  `
})
export class DashboardPage {
  private readonly api = inject(PortalApiService);
  private readonly notifications = inject(NotificationService);
  private readonly refresh$ = new BehaviorSubject<void>(undefined);
  private readonly mobileConfigUsernamePlaceholder = '<device-username>';

  protected readonly dashboard$ = this.refresh$.pipe(
    switchMap(() => this.api.getDashboard()),
    catchError(() => of(null)),
    shareReplay(1)
  );
  protected readonly issuedCredential = signal<IssuedVpnDeviceCredential | null>(null);
  protected readonly activeSectionId = signal<string>('device-access');
  protected readonly instructionDialogOpen = signal(false);
  protected readonly activeInstructionPlatform = signal<'android' | 'ios' | 'windows'>('ios');
  protected newDeviceName = '';

  protected dashboardSections(dashboard: UserDashboard): SectionMenuItem[] {
    return [
      {
        id: 'device-access',
        label: 'Устройства',
        accent: true
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

  protected visibleOnboardingFields(fields: VpnOnboardingField[]): VpnOnboardingField[] {
    return fields.filter((field) => field.label !== 'Имя пользователя');
  }

  protected fieldValue(fields: VpnOnboardingField[], label: string): string | null {
    return fields.find((field) => field.label === label)?.value ?? null;
  }

  protected openInstructionDialog(platform?: 'android' | 'ios' | 'windows'): void {
    const preferredPlatform = platform ?? this.detectPreferredInstructionPlatform();
    this.activeInstructionPlatform.set(preferredPlatform);
    this.instructionDialogOpen.set(true);
  }

  private detectPreferredInstructionPlatform(): 'android' | 'ios' | 'windows' {
    if (typeof navigator === 'undefined') {
      return 'windows';
    }

    const userAgent = navigator.userAgent.toLowerCase();
    const platform = (navigator.platform || '').toLowerCase();

    if (userAgent.includes('iphone') || userAgent.includes('ipad') || userAgent.includes('ipod')) {
      return 'ios';
    }

    if (userAgent.includes('android')) {
      return 'android';
    }

    if (platform.includes('win')) {
      return 'windows';
    }

    if (platform.includes('iphone') || platform.includes('ipad') || platform.includes('ipod') || userAgent.includes('mac os') && 'ontouchend' in document) {
      return 'ios';
    }

    return 'windows';
  }

  protected closeInstructionDialog(): void {
    this.instructionDialogOpen.set(false);
  }

  protected modalGuides(dashboard: UserDashboard) {
    const order = ['android', 'ios', 'windows'];
    return order
      .map((platform) => dashboard.platformGuides.find((guide) => guide.platform === platform))
      .filter((guide): guide is UserDashboard['platformGuides'][number] => guide !== undefined);
  }

  protected selectedModalGuide(dashboard: UserDashboard) {
    return this.modalGuides(dashboard).find((guide) => guide.platform === this.activeInstructionPlatform())
      ?? this.modalGuides(dashboard)[0]
      ?? null;
  }

  protected platformLabel(platform: string): string {
    switch (platform) {
      case 'android':
        return 'Android';
      case 'ios':
        return 'iPhone';
      case 'windows':
        return 'Windows';
      default:
        return platform;
    }
  }

  protected platformIconText(platform: string): string {
    switch (platform) {
      case 'android':
        return 'A';
      case 'ios':
        return 'i';
      case 'windows':
        return 'W';
      default:
        return platform.slice(0, 1).toUpperCase();
    }
  }

  protected resolvedGuideSteps(guide: UserDashboard['platformGuides'][number], credential: IssuedVpnDeviceCredential): string[] {
    return guide.steps.map((step) => step
      .replaceAll(this.mobileConfigUsernamePlaceholder, credential.vpnUsername)
      .replace('Пароль: VPN-пароль устройства, выданный в портале.', `Пароль: ${credential.vpnPassword}.`)
      .replace('Пароль: пароль устройства, который портал показывает один раз после создания или смены.', `Пароль: ${credential.vpnPassword}.`));
  }

  protected selectInstructionPlatform(platform: string): void {
    if (platform === 'android' || platform === 'ios' || platform === 'windows') {
      this.activeInstructionPlatform.set(platform);
    }
  }

  protected resolvedGuideFields(guide: UserDashboard['platformGuides'][number], actualFields: VpnOnboardingField[]): VpnOnboardingField[] {
    const fieldMap = new Map(actualFields.map((field) => [field.label, field.value]));
    const resolvedFields = guide.fields.map((field) => ({
      ...field,
      value: field.label === 'Имя пользователя'
        ? (fieldMap.get('Имя пользователя') ?? field.value)
        : (fieldMap.get(field.label) ?? field.value)
    }));

    if (fieldMap.has('Имя пользователя') && !resolvedFields.some((field) => field.label === 'Имя пользователя')) {
      resolvedFields.push({ label: 'Имя пользователя', value: fieldMap.get('Имя пользователя') ?? '' });
    }

    return [...resolvedFields, { label: 'Пароль устройства', value: this.issuedCredential()?.vpnPassword ?? '' }];
  }

  protected downloadMobileConfig(credential: IssuedVpnDeviceCredential, guide: UserDashboard['platformGuides'][number]): void {
    const fields = this.resolvedGuideFields(guide, credential.onboarding.fields);
    const server = this.fieldValue(fields, 'Сервер');
    const remoteId = this.fieldValue(fields, 'Remote ID') ?? server;

    if (!server) {
      this.showError('Не удалось подготовить .mobileconfig: отсутствует адрес сервера.');
      return;
    }

    const profile = this.buildMobileConfig({
      deviceName: credential.deviceName,
      server,
      remoteId: remoteId ?? server,
      username: credential.vpnUsername,
      password: credential.vpnPassword
    });
    const blob = new Blob([profile], { type: 'application/x-apple-aspen-config' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `${this.safeFilePart(credential.deviceName)}.mobileconfig`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  protected stepSegments(step: string, fields: VpnOnboardingField[]): Array<{ text: string; copyValue: string | null; label: string }> {
    const copyableFields = fields
      .filter((field) => field.value)
      .slice()
      .sort((left, right) => right.value.length - left.value.length);

    if (!copyableFields.length) {
      return [{ text: step, copyValue: null, label: '' }];
    }

    const segments: Array<{ text: string; copyValue: string | null; label: string }> = [];
    let cursor = 0;

    while (cursor < step.length) {
      let matchedField: VpnOnboardingField | null = null;

      for (const field of copyableFields) {
        if (step.startsWith(field.value, cursor)) {
          matchedField = field;
          break;
        }
      }

      if (matchedField) {
        segments.push({
          text: matchedField.value,
          copyValue: matchedField.value,
          label: matchedField.label
        });
        cursor += matchedField.value.length;
        continue;
      }

      let nextIndex = step.length;
      for (const field of copyableFields) {
        const index = step.indexOf(field.value, cursor);
        if (index !== -1 && index < nextIndex) {
          nextIndex = index;
        }
      }

      segments.push({
        text: step.slice(cursor, nextIndex),
        copyValue: null,
        label: ''
      });
      cursor = nextIndex;
    }

    return segments.filter((segment) => segment.text.length > 0);
  }

  protected activeDeviceCount(devices: TrustedDevice[]): number {
    return devices.filter((device) => device.status !== 'revoked' && device.credentialStatus !== 'revoked').length;
  }

  private buildMobileConfig(input: { deviceName: string; server: string; remoteId: string; username: string; password: string }): string {
    const profileUuid = crypto.randomUUID();
    const payloadUuid = crypto.randomUUID();
    const connectionName = `${input.deviceName} VPN`;

    return `<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>PayloadContent</key>
  <array>
    <dict>
      <key>IKEv2</key>
      <dict>
        <key>AuthenticationMethod</key>
        <string>None</string>
        <key>AuthName</key>
        <string>${this.escapeXml(input.username)}</string>
        <key>AuthPassword</key>
        <string>${this.escapeXml(input.password)}</string>
        <key>ExtendedAuthEnabled</key>
        <integer>1</integer>
        <key>RemoteAddress</key>
        <string>${this.escapeXml(input.server)}</string>
        <key>RemoteIdentifier</key>
        <string>${this.escapeXml(input.remoteId)}</string>
      </dict>
      <key>PayloadDisplayName</key>
      <string>${this.escapeXml(connectionName)}</string>
      <key>PayloadIdentifier</key>
      <string>com.vpnportal.vpn.${payloadUuid}</string>
      <key>PayloadType</key>
      <string>com.apple.vpn.managed</string>
      <key>PayloadUUID</key>
      <string>${payloadUuid}</string>
      <key>PayloadVersion</key>
      <integer>1</integer>
      <key>UserDefinedName</key>
      <string>${this.escapeXml(connectionName)}</string>
      <key>VPNType</key>
      <string>IKEv2</string>
    </dict>
  </array>
  <key>PayloadDisplayName</key>
  <string>${this.escapeXml(connectionName)}</string>
  <key>PayloadIdentifier</key>
  <string>com.vpnportal.profile.${profileUuid}</string>
  <key>PayloadRemovalDisallowed</key>
  <false/>
  <key>PayloadType</key>
  <string>Configuration</string>
  <key>PayloadUUID</key>
  <string>${profileUuid}</string>
  <key>PayloadVersion</key>
  <integer>1</integer>
</dict>
</plist>`;
  }

  private escapeXml(value: string): string {
    return value
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&apos;');
  }

  private safeFilePart(value: string): string {
    return value.trim().replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/-+/g, '-').replace(/^-|-$/g, '') || 'vpn-profile';
  }
}
