import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'ob-severity-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [attr.data-severity]="severity()">
    <span
      class="swatch"
      [class.hollow]="hollow()"
      [class.round]="round()"
      aria-hidden="true"
    ></span>
    {{ severity() }}
  </span>`,
  styles: `
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      font-size: 10px;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      font-weight: 700;
      padding: 2px 6px;
      border-radius: 2px;
      white-space: nowrap;
      color: var(--ob-error-ink);
      background: var(--ob-error-wash);
      border: 1px solid color-mix(in oklab, var(--ob-error) 35%, transparent);
    }

    .badge[data-severity='Warning'] {
      color: var(--ob-warning-ink);
      background: var(--ob-warning-wash);
      border-color: color-mix(in oklab, var(--ob-warning) 35%, transparent);
    }

    .badge[data-severity='Information'] {
      color: var(--ob-info-ink);
      background: var(--ob-info-wash);
      border-color: color-mix(in oklab, var(--ob-info) 35%, transparent);
    }

    .swatch {
      width: 7px;
      height: 7px;
      background: var(--ob-error);
      display: inline-block;
      flex: none;
    }

    .badge[data-severity='Warning'] .swatch {
      background: transparent;
      border: 1.5px solid var(--ob-warning);
    }

    .badge[data-severity='Information'] .swatch {
      background: var(--ob-info);
      border-radius: 50%;
    }
  `,
})
export class SeverityBadge {
  readonly severity = input.required<string>();

  protected readonly hollow = computed(() => this.severity() === 'Warning');
  protected readonly round = computed(() => this.severity() === 'Information');
}
