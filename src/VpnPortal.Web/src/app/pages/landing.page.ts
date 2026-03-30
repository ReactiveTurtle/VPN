import { NgClass, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PortalApiService } from '../core/portal-api.service';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [ReactiveFormsModule, NgClass, NgIf],
  template: `
    <section class="hero hero-single">
      <div class="hero-main hero-main-centered">
        <div class="hero-copy-block">
          <h1>Простое подключение VPN с контролем устройств и входов</h1>
          <p class="lead">Подключайте новые устройства, управляйте доступом и меняйте привязанный IP через один понятный личный кабинет.</p>
        </div>

        <div class="landing-notes-grid section-block">
          <article class="landing-note-card">
            <span class="metric-label">Устройства</span>
            <strong>Отдельные данные для каждого устройства</strong>
            <p class="detail-copy">Ноутбук и телефон подключаются со своими данными, без одного общего пароля на все устройства.</p>
          </article>
          <article class="landing-note-card">
            <span class="metric-label">Безопасность</span>
            <strong>IP закрепляется за устройством</strong>
            <p class="detail-copy">Первое успешное подключение привязывает текущий IP к устройству, а сменить его можно через личный кабинет.</p>
          </article>
        </div>
      </div>
    </section>

    <section class="panel request-panel request-panel-accent" id="request-access">
      <div class="content-section-header content-section-header-single">
        <div>
          <h2>Подать заявку на получение доступа к VPN</h2>
        </div>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" class="request-form request-form-stack">
        <label>
          <span>Email</span>
          <input type="email" formControlName="email" placeholder="ivan@company.com" [ngClass]="{ 'field-invalid': isInvalid('email') }" />
          @if (isInvalid('email')) {
            <small class="field-error">Укажите корректный email.</small>
          }
        </label>

        <label>
          <span>Имя <span class="field-note">необязательно</span></span>
          <input type="text" formControlName="name" placeholder="Иван Иванов" />
        </label>

        <button type="submit" class="button primary" [disabled]="submitting()">
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

  protected readonly submitting = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
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

  protected isInvalid(controlName: 'email' | 'name'): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }
}
