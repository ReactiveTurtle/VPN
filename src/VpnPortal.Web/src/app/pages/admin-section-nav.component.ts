import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ADMIN_SECTION_LINKS } from './admin-shared';

@Component({
  selector: 'app-admin-section-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <section class="panel data-panel section-subtle">
      <div class="content-section-header content-section-header-single">
        <div>
          <p class="eyebrow">Разделы суперадминки</p>
          <h2>Быстрые переходы</h2>
          <p>Каждый операционный поток вынесен в самостоятельный раздел, чтобы заявки, аудит, сессии и учетные записи не смешивались на одной странице.</p>
        </div>
      </div>

      <div class="section-nav">
        @for (section of sections; track section.route) {
          <a [routerLink]="section.route" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
            <strong>{{ section.label }}</strong>
            <span>{{ section.description }}</span>
          </a>
        }
      </div>
    </section>
  `
})
export class AdminSectionNavComponent {
  protected readonly sections = ADMIN_SECTION_LINKS;
}
