import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  isDevMode,
} from '@angular/core';

import { TimelineGroupResponse } from '../api';
import { groupStateOf, sequenceLabelOf } from '../core/timeline';
import { TimelineEventCard } from './timeline-event-card';

@Component({
  selector: 'li[ob-timeline-group]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TimelineEventCard],
  template: `
    <span
      class="spine"
      [class.first]="first()"
      [class.last]="last()"
      [class.quiet]="!!state()"
      aria-hidden="true"
    >
      <span class="badge" [class.quiet]="!!state()">{{ sequenceLabel() }}</span>
    </span>

    <div class="content">
      <span class="ob-sr-only">Group {{ sequenceLabel() }}</span>

      @if (state(); as grouped) {
        <div class="grouped">
          <div class="grouped-head">
            <h3 class="grouped-label">{{ grouped.label }}</h3>
            <span class="grouped-count">{{ eventCount() }}</span>
          </div>
          <p class="grouped-explanation">{{ grouped.explanation }}</p>
          <div class="grouped-events">
            @for (event of group().events; track event.entityId) {
              <ob-timeline-event
                [event]="event"
                [importBatchId]="importBatchId()"
                [patientId]="patientId()"
              />
            }
          </div>
        </div>
      } @else {
        @if (contractProblem(); as problem) {
          <p class="contract-problem">{{ problem }}</p>
        }
        @for (event of group().events; track event.entityId) {
          <ob-timeline-event
            [event]="event"
            [importBatchId]="importBatchId()"
            [patientId]="patientId()"
          />
        }
      }
    </div>
  `,
  styles: `
    :host {
      display: grid;
      grid-template-columns: 64px minmax(0, 1fr);
      min-width: 0;
    }

    .spine {
      position: relative;
      display: flex;
      justify-content: center;
      padding-top: 12px;
    }

    .spine::before {
      content: '';
      position: absolute;
      top: 0;
      bottom: 0;
      left: 50%;
      width: 1px;
      background: var(--ob-border-2);
    }

    .spine.first::before {
      top: 12px;
    }

    .spine.last::before {
      bottom: auto;
      height: 12px;
    }

    .badge {
      position: relative;
      width: 26px;
      height: 22px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--ob-accent);
      border: 1px solid var(--ob-accent);
      border-radius: 2px;
      font-family: var(--ob-mono);
      font-size: 11px;
      color: #fff;
      font-weight: 700;
      flex: none;
    }

    .badge.quiet {
      background: var(--ob-surface);
      border-color: var(--ob-border-2);
      color: var(--ob-ink-2);
      font-weight: 400;
    }

    .content {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 12px 0;
      min-width: 0;
    }

    .grouped {
      background: var(--ob-surface-3);
      border: 1px solid var(--ob-border-2);
      border-radius: 2px;
      padding: 12px;
      display: flex;
      flex-direction: column;
      gap: 10px;
      min-width: 0;
    }

    .grouped-head {
      display: flex;
      align-items: baseline;
      gap: 10px;
      flex-wrap: wrap;
    }

    .grouped-label {
      margin: 0;
      font-size: 10.5px;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: var(--ob-ink-2);
      font-weight: 650;
    }

    .grouped-count {
      font-family: var(--ob-mono);
      font-size: 10.5px;
      color: var(--ob-muted);
    }

    .grouped-explanation {
      margin: 0;
      font-size: 12px;
      color: var(--ob-ink-2);
      line-height: 1.5;
      text-wrap: pretty;
    }

    .grouped-events {
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-width: 0;
    }

    .contract-problem {
      margin: 0;
      font-size: 11.5px;
      color: var(--ob-ink-2);
      background: var(--ob-surface-3);
      border: 1px solid var(--ob-border-2);
      border-radius: 2px;
      padding: 8px 10px;
      line-height: 1.5;
    }

    @media (max-width: 640px) {
      :host {
        grid-template-columns: 40px minmax(0, 1fr);
      }

      .badge {
        width: 24px;
        height: 20px;
        font-size: 10.5px;
      }

      .grouped {
        padding: 10px;
      }
    }
  `,
})
export class TimelineGroup {
  readonly group = input.required<TimelineGroupResponse>();
  readonly importBatchId = input.required<string>();
  readonly patientId = input.required<string>();
  readonly first = input(false);
  readonly last = input(false);

  protected readonly state = computed(() => groupStateOf(this.group().kind));

  protected readonly sequenceLabel = computed(() => sequenceLabelOf(this.group()));

  protected readonly eventCount = computed(() => {
    const count = this.group().events.length;

    return `${count} ${count === 1 ? 'event' : 'events'}`;
  });

  protected readonly contractProblem = computed(() => {
    const group = this.group();

    if (group.kind !== 'Established' || group.events.length <= 1 || !isDevMode()) {
      return null;
    }

    return (
      `Contract problem: group ${sequenceLabelOf(group)} is Established but carries ` +
      `${group.events.length} events. Every event is shown, in the order the API returned, and no ` +
      `order between them is asserted here.`
    );
  });

  constructor() {
    effect(() => {
      const problem = this.contractProblem();

      if (problem) {
        console.error(problem);
      }
    });
  }
}
