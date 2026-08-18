import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { EntityInstance } from '../core/entities';

@Component({
  selector: 'ob-entity-selector',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="lead">
      <span class="ob-micro">Canonical entity</span>
      <span class="ob-meta">{{ summary() }}</span>
    </div>

    <nav aria-label="Canonical entity instances">
      <ul>
        @for (instance of instances(); track instance.id) {
          <li>
            <button
              type="button"
              class="choice"
              [class.selected]="instance.id === selectedId()"
              [attr.aria-current]="instance.id === selectedId() ? 'true' : null"
              (click)="entitySelected.emit(instance.id)"
            >
              <span class="kind">{{ instance.kindLabel }}</span>
              <span class="label">{{ instance.label }}</span>
            </button>
          </li>
        }
      </ul>
    </nav>
  `,
  styles: `
    :host {
      display: flex;
      align-items: stretch;
      gap: 12px;
      padding: 10px 16px;
      border-bottom: 1px solid var(--ob-border);
      background: var(--ob-surface);
      flex-wrap: wrap;
    }

    .lead {
      display: flex;
      flex-direction: column;
      justify-content: center;
      gap: 2px;
      min-width: 112px;
    }

    nav {
      flex: 1;
      min-width: 0;
    }

    ul {
      margin: 0;
      padding: 0;
      list-style: none;
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    li {
      flex: 1 1 180px;
      min-width: 0;
    }

    .choice {
      width: 100%;
      height: 100%;
      display: flex;
      flex-direction: column;
      gap: 2px;
      align-items: flex-start;
      text-align: left;
      border: 1px solid var(--ob-border-2);
      border-radius: 3px;
      padding: 7px 10px;
      background: var(--ob-surface-2);
      font-family: var(--ob-sans);
      cursor: pointer;
      min-height: 44px;
    }

    .choice:hover {
      border-color: var(--ob-border);
    }

    .choice.selected {
      border-color: var(--ob-accent);
      background: var(--ob-accent-wash);
      box-shadow: inset 0 -2px 0 var(--ob-accent);
    }

    .kind {
      font-size: 9.5px;
      letter-spacing: 0.09em;
      text-transform: uppercase;
      color: var(--ob-faint);
    }

    .choice.selected .kind {
      color: var(--ob-accent-ink);
      font-weight: 650;
    }

    .label {
      font-size: 13px;
      color: var(--ob-ink-2);
    }

    .choice.selected .label {
      color: var(--ob-ink);
      font-weight: 600;
    }
  `,
})
export class EntitySelector {
  readonly instances = input.required<readonly EntityInstance[]>();
  readonly selectedId = input<string | null>(null);
  readonly summary = input('');

  readonly entitySelected = output<string>();
}
