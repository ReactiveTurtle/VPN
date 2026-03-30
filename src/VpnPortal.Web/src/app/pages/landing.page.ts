import { AsyncPipe, NgClass, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, of, startWith } from 'rxjs';
import { PortalApiService } from '../core/portal-api.service';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [ReactiveFormsModule, AsyncPipe, NgClass, NgIf, RouterLink],
  template: `
    <section class="hero">
      <div class="hero-main">
        <p class="eyebrow">Безопасный удаленный доступ</p>
        <h1>Управляйте доступом, привязкой устройств и активностью VPN из единого портала.</h1>
        <p class="lead">
          NorthGate объединяет прием заявок, активацию учетных записей, VPN-учетные данные для конкретных устройств, проверку доверенных IP и операционную видимость в одном интерфейсе.
        </p>
        <div class="hero-actions">
          <a routerLink="/login" class="button primary">Вход для пользователя</a>
          <a routerLink="/admin/login" class="button secondary">Админ-панель</a>
        </div>

        <div class="summary-grid section-block">
          <article class="summary-card">
            <span class="metric-label">Модель доступа</span>
            <strong>Отдельные VPN-данные для каждого устройства</strong>
            <p class="detail-copy">Вход в портал отделен от VPN-паролей, которые выдаются каждому зарегистрированному устройству.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Сигналы безопасности</span>
            <strong>Контроль новых IP-адресов</strong>
            <p class="detail-copy">Неожиданные IP-адреса источника требуют подтверждения вместо скрытого пропуска.</p>
          </article>
          <article class="summary-card">
            <span class="metric-label">Операции</span>
            <strong>Модерация с учетом сессий</strong>
            <p class="detail-copy">Администраторы видят заявки, лимиты пользователей, события аудита и активные VPN-сессии в одном месте.</p>
          </article>
        </div>
      </div>

      <aside class="hero-side" *ngIf="status$ | async as status">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Состояние платформы</p>
            <h2>Снимок готовности развертывания</h2>
          </div>
        </div>

        <div class="status-grid">
          <article class="stat-card">
            <span class="metric-label">API</span>
            <strong>{{ status.name }}</strong>
            <span class="meta-line">HTTP-хост отвечает.</span>
          </article>
          <article class="stat-card">
            <span class="metric-label">Версия</span>
            <strong>{{ status.version }}</strong>
            <span class="meta-line">Текущая метка сборки backend.</span>
          </article>
          <article class="stat-card">
            <span class="metric-label">База данных</span>
            <strong [ngClass]="status.databaseConfigured ? 'ok' : 'warn'">{{ status.databaseConfigured ? 'Настроена' : 'Не настроена' }}</strong>
            <span class="meta-line">Состояние строки подключения по данным API.</span>
          </article>
        </div>

        <div class="feature-list pending-block">
          <div>
            <strong>Публичная подача заявок</strong>
            <p class="detail-copy">Форма запроса уже подключена к очереди модерации на backend.</p>
          </div>
          <div>
            <strong>Cookie-аутентификация</strong>
            <p class="detail-copy">Сессии пользователей и суперадминистраторов изолированы через ролевой вход в портал.</p>
          </div>
        </div>
      </aside>
    </section>

    <section class="panel request-panel" id="request-access">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">Запрос доступа</p>
          <h2>Подать заявку на VPN-доступ</h2>
        </div>
        <p>Укажите контактный email и, при желании, отображаемое имя. Заявка сразу попадет в очередь администратора.</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" class="request-form">
        <label>
          <span>Имя</span>
          <input type="text" formControlName="name" placeholder="Иван Иванов" />
        </label>

        <label>
          <span>Email</span>
          <input type="email" formControlName="email" placeholder="ivan@company.com" />
        </label>

        <button type="submit" class="button primary" [disabled]="form.invalid || submitting()">
          {{ submitting() ? 'Отправка...' : 'Отправить заявку' }}
        </button>
      </form>

      <div *ngIf="message() as message" class="feedback success">{{ message }}</div>
      <div *ngIf="error() as error" class="feedback error">{{ error }}</div>
    </section>
  `
})
export class LandingPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PortalApiService);

  protected readonly form = this.fb.nonNullable.group({
    name: [''],
    email: ['', [Validators.required, Validators.email]]
  });

  protected readonly status$ = this.api.getStatus().pipe(
    catchError(() => of({ name: 'VpnPortal.Api', version: 'недоступно', databaseConfigured: false, developmentMode: true })),
    startWith(null)
  );

  protected readonly submitting = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.submitting.set(true);
    this.message.set(null);
    this.error.set(null);

    const payload = this.form.getRawValue();
    this.api.submitRequest(payload).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.message.set(`Заявка #${result.id} создана со статусом «${this.requestStatusLabel(result.status)}». Очередь модерации обновлена.`);
        this.form.reset({ name: '', email: '' });
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Не удалось отправить заявку. Запустите API и попробуйте снова.');
      }
    });
  }

  private requestStatusLabel(status: string): string {
    switch (status) {
      case 'pending':
        return 'ожидает рассмотрения';
      case 'approved':
        return 'одобрена';
      case 'rejected':
        return 'отклонена';
      default:
        return status;
    }
  }
}
