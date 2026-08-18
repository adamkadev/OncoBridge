import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { PartialDateResponse } from '../api';

@Component({
  selector: 'ob-partial-date',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="value">{{ date().value }}</span>
    @if (showPrecision()) {
      <span class="ob-chip">{{ date().precision }}</span>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: baseline;
      gap: 8px;
      flex-wrap: wrap;
    }

    .value {
      font-family: var(--ob-mono);
      font-size: 14px;
    }
  `,
})
export class PartialDate {
  readonly date = input.required<PartialDateResponse>();
  readonly showPrecision = input(true);
}
