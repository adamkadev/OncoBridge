import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { precisionCellsOf } from '../core/timeline';

@Component({
  selector: 'ob-precision-rail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="rail" aria-hidden="true">
      @for (cell of cells(); track cell.letter) {
        <span class="cell" [class.marked]="cell.marked">{{ cell.letter }}</span>
      }
    </span>
    <span class="name"><span class="ob-sr-only">Precision: </span>{{ precision() }}</span>
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      min-width: 0;
    }

    .rail {
      display: inline-flex;
      gap: 2px;
      flex: none;
    }

    .cell {
      width: 15px;
      height: 15px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      border: 1px solid var(--ob-border-2);
      border-radius: 2px;
      background: var(--ob-surface);
      font-family: var(--ob-mono);
      font-size: 9px;
      color: var(--ob-faint);
      flex: none;
    }

    .cell.marked {
      background: var(--ob-ink-2);
      border-color: var(--ob-ink-2);
      color: #fff;
      font-weight: 700;
    }

    .name {
      font-size: 10px;
      letter-spacing: 0.07em;
      text-transform: uppercase;
      color: var(--ob-ink-2);
    }
  `,
})
export class PrecisionRail {
  readonly precision = input.required<string>();

  protected readonly cells = computed(() => precisionCellsOf(this.precision()));
}
