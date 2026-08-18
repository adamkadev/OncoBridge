import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { TemporalOccurrenceResponse } from '../api';
import { AbsentValue } from './absent-value';
import { PartialDate } from './partial-date';

@Component({
  selector: 'ob-temporal-occurrence',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PartialDate, AbsentValue],
  template: `
    @let value = occurrence();
    @if (value.kind === 'Period') {
      <span class="ob-chip-outline">Period</span>
      <span class="bounds">
        <span class="bound">
          <span class="bound-label">Start</span>
          @if (value.period?.start) {
            <ob-partial-date [date]="value.period!.start!" />
          } @else {
            <span class="ob-absent-text">Open</span>
          }
        </span>
        <span class="bound">
          <span class="bound-label">End</span>
          @if (value.period?.end) {
            <ob-partial-date [date]="value.period!.end!" />
          } @else {
            <span class="ob-absent-text">Open</span>
          }
        </span>
      </span>
    } @else if (value.date) {
      <ob-partial-date [date]="value.date" />
    } @else {
      <ob-absent-value />
    }
  `,
  styles: `
    :host {
      display: flex;
      align-items: baseline;
      gap: 10px;
      flex-wrap: wrap;
    }

    .bounds {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .bound {
      display: flex;
      align-items: baseline;
      gap: 8px;
    }

    .bound-label {
      font-size: 11.5px;
      color: var(--ob-muted);
      min-width: 34px;
    }
  `,
})
export class TemporalOccurrence {
  readonly occurrence = input.required<TemporalOccurrenceResponse>();
}
