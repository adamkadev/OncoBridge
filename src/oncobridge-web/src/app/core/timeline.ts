import { TimelineEventResponse, TimelineGroupResponse } from '../api';
import { asNumber } from './api-values';

export interface PrecisionCell {
  readonly letter: string;
  readonly name: string;
  readonly marked: boolean;
}

const PRECISIONS: readonly { readonly letter: string; readonly name: string }[] = [
  { letter: 'Y', name: 'Year' },
  { letter: 'M', name: 'Month' },
  { letter: 'D', name: 'Day' },
  { letter: 'I', name: 'Instant' },
];

export function precisionCellsOf(precision: string): readonly PrecisionCell[] {
  return PRECISIONS.map((step) => ({ ...step, marked: step.name === precision }));
}

export interface GroupState {
  readonly label: string;
  readonly explanation: string;
}

const GROUP_STATES: Readonly<Record<string, GroupState>> = {
  SharedTemporalAnchor: {
    label: 'Shared temporal anchor',
    explanation:
      'These events have the same stated temporal anchor. No before/after sequence is asserted ' +
      'within this group.',
  },
  OrderNotEstablished: {
    label: 'Order not established',
    explanation:
      'The stated precision admits no definite ordering, so no claim is made about the order of ' +
      'these events.',
  },
};

export function groupStateOf(kind: string): GroupState | null {
  return GROUP_STATES[kind] ?? null;
}

const ENTITY_KIND_LABELS: Readonly<Record<string, string>> = {
  PrimaryCancerDiagnosis: 'Primary cancer diagnosis',
  CancerStaging: 'Cancer staging',
  CancerSurgicalProcedure: 'Cancer surgical procedure',
};

export function entityKindLabelOf(entityKind: string): string {
  return ENTITY_KIND_LABELS[entityKind] ?? entityKind;
}

const OCCURRENCE_ROLES: Readonly<Record<string, string>> = {
  PrimaryCancerDiagnosis: 'onset',
  CancerStaging: 'effective',
  CancerSurgicalProcedure: 'performed',
};

export function occurrenceRoleOf(entityKind: string): string {
  return OCCURRENCE_ROLES[entityKind] ?? 'occurrence';
}

const UNSEQUENCED_NOTES: Readonly<Record<string, string>> = {
  NoAnchorBound: 'No start bound is stated, so this occurrence has no timeline anchor.',
};

export function unsequencedNoteOf(reason: string): string | null {
  return UNSEQUENCED_NOTES[reason] ?? null;
}

const DATE_ANCHOR = 'Date';
const PERIOD_START_ANCHOR = 'PeriodStart';

export function anchorsDate(anchorSource: string | null | undefined): boolean {
  return anchorSource === DATE_ANCHOR;
}

export function anchorsPeriodStart(anchorSource: string | null | undefined): boolean {
  return anchorSource === PERIOD_START_ANCHOR;
}

export function sequenceLabelOf(group: TimelineGroupResponse): string {
  return `${asNumber(group.sequence)}`.padStart(2, '0');
}

export function anchoredEventsOf(groups: readonly TimelineGroupResponse[]): number {
  return groups.reduce((total, group) => total + group.events.length, 0);
}

const SHORT_KINDS: Readonly<Record<string, string>> = {
  PrimaryCancerDiagnosis: 'diagnosis',
  CancerStaging: 'staging',
  CancerSurgicalProcedure: 'procedure',
};

export function anchorPhraseOf(event: TimelineEventResponse): string {
  const kind = SHORT_KINDS[event.entityKind] ?? event.entityKind;

  return anchorsPeriodStart(event.anchorSource)
    ? `${kind} start`
    : `${kind} ${occurrenceRoleOf(event.entityKind)}`;
}

export function anchorChainOf(groups: readonly TimelineGroupResponse[]): readonly string[] {
  return groups.map((group) => {
    const state = groupStateOf(group.kind);

    if (state) {
      return `${state.label.toLowerCase()} (${group.events.length} events)`;
    }

    return group.events.map(anchorPhraseOf).join(' + ');
  });
}

export function tnmOf(event: TimelineEventResponse): readonly string[] {
  return (event.staging?.categories ?? []).map((category) => category.code.code);
}
