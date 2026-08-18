import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { PartialDateResponse, TemporalOccurrenceResponse } from '../api';
import { anchorsDate, anchorsPeriodStart } from '../core/timeline';
import { PrecisionRail } from '../shared/precision-rail';

@Component({
  selector: 'ob-timeline-bound',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PrecisionRail],
  template: `
    <span class="role">{{ role() }}</span>

    <span class="value">
      @if (date(); as stated) {
        <span class="stated">{{ stated.value }}</span>
        @if (anchored()) {
          <span class="anchor">Anchor</span>
        }
      } @else {
        <span class="ob-absent-text">{{ absentLabel() }}</span>
      }
    </span>

    <span class="role precision-label">precision</span>

    <span class="precision">
      @if (date(); as stated) {
        <ob-precision-rail [precision]="stated.precision" />
      } @else if (noBoundStated()) {
        <span class="ob-chip-outline">no bound stated</span>
      }
    </span>
  `,
  styles: `
    :host {
      display: grid;
      grid-template-columns: 62px 230px minmax(0, 1fr);
      align-items: center;
      gap: 6px 10px;
      min-width: 0;
    }

    .role {
      font-size: 9.5px;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      color: var(--ob-faint);
    }

    .precision-label {
      display: none;
    }

    .value {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
      min-width: 0;
    }

    .stated {
      font-family: var(--ob-mono);
      font-size: 13.5px;
      overflow-wrap: anywhere;
    }

    .anchor {
      font-size: 9.5px;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: var(--ob-muted);
      border: 1px solid var(--ob-border-2);
      padding: 1px 5px;
      border-radius: 2px;
      white-space: nowrap;
      flex: none;
    }

    .precision {
      display: inline-flex;
      align-items: center;
      justify-self: start;
      min-width: 0;
    }

    @media (max-width: 640px) {
      :host {
        grid-template-columns: 52px minmax(0, 1fr);
      }

      .precision-label {
        display: inline;
      }
    }
  `,
})
export class TimelineBound {
  readonly role = input.required<string>();
  readonly date = input<PartialDateResponse | null>(null);
  readonly anchored = input(false);
  readonly absentLabel = input('Not stated');
  readonly noBoundStated = input(false);
}

@Component({
  selector: 'ob-timeline-occurrence',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TimelineBound],
  template: `
    @let value = occurrence();

    @if (value.kind === 'Period') {
      <div class="period">
        <span class="spanbar" aria-hidden="true"></span>
        <div class="bounds">
          <ob-timeline-bound
            role="start"
            [date]="value.period?.start ?? null"
            [anchored]="startAnchored()"
          />
          <ob-timeline-bound
            role="end"
            [date]="value.period?.end ?? null"
            absentLabel="Open"
            [noBoundStated]="true"
          />
        </div>
      </div>
    } @else {
      <ob-timeline-bound [role]="role()" [date]="value.date ?? null" [anchored]="dateAnchored()" />
    }
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }

    .period {
      display: flex;
      gap: 10px;
      min-width: 0;
    }

    .spanbar {
      width: 3px;
      align-self: stretch;
      background: var(--ob-border-2);
      border-radius: 2px;
      flex: none;
    }

    .bounds {
      display: flex;
      flex-direction: column;
      gap: 7px;
      flex: 1;
      min-width: 0;
    }
  `,
})
export class TimelineOccurrence {
  readonly occurrence = input.required<TemporalOccurrenceResponse>();
  readonly anchorSource = input<string | null>(null);
  readonly role = input('occurrence');

  protected readonly dateAnchored = computed(() => anchorsDate(this.anchorSource()));

  protected readonly startAnchored = computed(() => anchorsPeriodStart(this.anchorSource()));
}
