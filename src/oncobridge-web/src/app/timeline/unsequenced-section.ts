import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { UnsequencedTimelineEventResponse } from '../api';
import { TimelineEventCard } from './timeline-event-card';

@Component({
  selector: 'ob-unsequenced-section',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TimelineEventCard],
  template: `
    <div class="head">
      <h2 class="label">Unsequenced</h2>
      <span class="meta">no timeline anchor · not sequenced</span>
    </div>

    <ul>
      @for (unsequenced of events(); track unsequenced.event.entityId) {
        <li>
          <ob-timeline-event
            [event]="unsequenced.event"
            [importBatchId]="importBatchId()"
            [patientId]="patientId()"
            [reason]="unsequenced.reason"
            [unsequenced]="true"
          />
        </li>
      }
    </ul>

    <p class="note">
      These events are part of the canonical record and remain inspectable. They carry no sequence
      number, because placing them before the first group or after the last would assert a position
      the record does not state.
    </p>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      gap: 10px;
      border-top: 1px solid var(--ob-rule);
      margin-top: 4px;
      padding-top: 12px;
      min-width: 0;
    }

    .head {
      display: flex;
      align-items: baseline;
      gap: 12px;
      flex-wrap: wrap;
    }

    .label {
      margin: 0;
      font-size: 10.5px;
      letter-spacing: 0.12em;
      text-transform: uppercase;
      font-weight: 650;
    }

    .meta {
      font-size: 10.5px;
      color: var(--ob-muted);
    }

    ul {
      margin: 0;
      padding: 0;
      list-style: none;
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-width: 0;
    }

    .note {
      margin: 0;
      font-size: 11.5px;
      color: var(--ob-muted);
      line-height: 1.5;
      text-wrap: pretty;
    }
  `,
})
export class UnsequencedSection {
  readonly events = input.required<readonly UnsequencedTimelineEventResponse[]>();
  readonly importBatchId = input.required<string>();
  readonly patientId = input.required<string>();
}
