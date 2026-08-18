import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { TimelineEventResponse } from '../api';
import { entityKindLabelOf, occurrenceRoleOf, tnmOf, unsequencedNoteOf } from '../core/timeline';
import { TimelineOccurrence } from './timeline-occurrence';

const PERIOD_NOTE =
  'Sequenced on its start anchor. The relation of the whole period to the other events is not ' +
  'asserted; both bounds are shown as stated.';

const OPEN_BOUND_NOTE =
  'No end bound was stated. This is a fact about the record, not a statement that the procedure ' +
  'is ongoing.';

@Component({
  selector: 'ob-timeline-event',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TimelineOccurrence],
  host: { '[class.unsequenced]': 'unsequenced()' },
  template: `
    @let value = event();

    <div class="head">
      <span class="kind-row">
        <span class="kind">{{ kindLabel() }}</span>
        @if (isPeriod()) {
          <span class="ob-chip-outline">Period</span>
        }
      </span>
      <a
        class="inspect"
        [routerLink]="['/imports', importBatchId()]"
        [queryParams]="{ patientId: patientId(), entityId: value.entityId }"
        [attr.aria-label]="'Inspect ' + value.label"
      >
        Inspect <span aria-hidden="true">→</span>
      </a>
    </div>

    <div class="name-row">
      <span class="name">{{ value.label }}</span>
      @if (tnm().length > 0) {
        <span class="tnm">
          @for (category of tnm(); track category) {
            <span class="category">{{ category }}</span>
          }
        </span>
      }
    </div>

    @if (value.occurrence; as occurrence) {
      <ob-timeline-occurrence
        [occurrence]="occurrence"
        [anchorSource]="value.anchorSource ?? null"
        [role]="role()"
      />
    } @else {
      <span class="ob-absent-text not-stated">Date not stated</span>
    }

    @if (note(); as text) {
      <span class="note">{{ text }}</span>
    }

    @if (value.diagnosis?.recordedDate; as recordedDate) {
      <span class="note">
        Recorded <span class="ob-mono">{{ recordedDate.value }}</span> · metadata, not a second
        event
      </span>
    }
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      gap: 8px;
      border: 1px solid var(--ob-border);
      border-radius: 3px;
      background: var(--ob-surface);
      padding: 10px 12px;
      min-width: 0;
    }

    :host(.unsequenced) {
      border-style: dashed;
    }

    .head {
      display: flex;
      align-items: baseline;
      gap: 10px;
      flex-wrap: wrap;
    }

    .kind-row {
      display: inline-flex;
      align-items: baseline;
      gap: 8px;
      flex-wrap: wrap;
      min-width: 0;
    }

    .kind {
      font-size: 9.5px;
      letter-spacing: 0.09em;
      text-transform: uppercase;
      color: var(--ob-faint);
    }

    .inspect {
      margin-left: auto;
      font-size: 10.5px;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: var(--ob-accent-ink);
      border: 1px solid color-mix(in oklab, var(--ob-accent) 40%, transparent);
      background: var(--ob-surface);
      padding: 3px 8px;
      border-radius: 2px;
      white-space: nowrap;
      text-decoration: none;
    }

    .inspect:hover {
      border-color: var(--ob-accent);
      background: var(--ob-accent-wash);
    }

    .name-row {
      display: flex;
      align-items: baseline;
      gap: 10px;
      flex-wrap: wrap;
      min-width: 0;
    }

    .name {
      font-size: 15px;
      font-weight: 600;
      overflow-wrap: anywhere;
    }

    .tnm {
      display: inline-flex;
      align-items: baseline;
      gap: 8px;
      font-family: var(--ob-mono);
      font-size: 11.5px;
      color: var(--ob-faint);
    }

    .not-stated {
      font-size: 13px;
    }

    .note {
      font-size: 11.5px;
      color: var(--ob-muted);
      line-height: 1.5;
      text-wrap: pretty;
    }

    @media (max-width: 640px) {
      :host {
        padding: 10px;
      }

      .name {
        font-size: 13.5px;
      }

      .head {
        display: contents;
      }

      .inspect {
        margin-left: 0;
        min-height: 44px;
        display: inline-flex;
        align-items: center;
        align-self: flex-start;
        order: 9;
      }
    }
  `,
})
export class TimelineEventCard {
  readonly event = input.required<TimelineEventResponse>();
  readonly importBatchId = input.required<string>();
  readonly patientId = input.required<string>();
  readonly unsequenced = input(false);
  readonly reason = input<string | null>(null);

  protected readonly kindLabel = computed(() => entityKindLabelOf(this.event().entityKind));

  protected readonly role = computed(() => occurrenceRoleOf(this.event().entityKind));

  protected readonly tnm = computed(() => tnmOf(this.event()));

  protected readonly isPeriod = computed(() => this.event().occurrence?.kind === 'Period');

  protected readonly note = computed(() => {
    const reason = this.reason();

    if (reason) {
      return unsequencedNoteOf(reason);
    }

    if (!this.isPeriod()) {
      return null;
    }

    return this.event().occurrence?.period?.end ? PERIOD_NOTE : OPEN_BOUND_NOTE;
  });
}
