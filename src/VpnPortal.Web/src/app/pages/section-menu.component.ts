import { Component, input, output } from '@angular/core';

export interface SectionMenuItem {
  id: string;
  label: string;
  description?: string;
  count?: string | null;
  accent?: boolean;
}

@Component({
  selector: 'app-section-menu',
  standalone: true,
  template: `
    <section class="panel data-panel section-subtle section-menu-shell" [class.section-menu-shell-compact]="compact()">
      @if (!compact()) {
        <div class="content-section-header content-section-header-single">
          <div>
            <p class="eyebrow">{{ eyebrow() }}</p>
            <h2>{{ title() }}</h2>
            <p>{{ description() }}</p>
          </div>
        </div>
      }

      <nav class="section-nav section-menu" [class.section-menu-compact]="compact()" [attr.aria-label]="ariaLabel()">
        @for (section of sections(); track section.id) {
          <button
            type="button"
            class="section-menu-button"
            [class.active]="activeSectionId() === section.id"
            [class.section-menu-accent]="section.accent"
            [class.section-menu-button-compact]="compact()"
            (click)="sectionChange.emit(section.id)">
            <div class="section-menu-copy">
              <strong>{{ section.label }}</strong>
              @if (section.description && !compact()) {
                <span>{{ section.description }}</span>
              }
            </div>
            @if (section.count && !compact()) {
              <span class="section-menu-count">{{ section.count }}</span>
            }
          </button>
        }
      </nav>
    </section>
  `
})
export class SectionMenuComponent {
  readonly eyebrow = input('Разделы');
  readonly title = input('Быстрые переходы');
  readonly description = input('Выберите нужный рабочий раздел.');
  readonly ariaLabel = input('Навигация по разделам');
  readonly compact = input(false);
  readonly activeSectionId = input<string | null>(null);
  readonly sections = input<SectionMenuItem[]>([]);
  readonly sectionChange = output<string>();
}
