import {
  PartialDateResponse,
  PatientTimelineResponse,
  TemporalOccurrenceResponse,
  TimelineEventResponse,
  TimelineGroupResponse,
  UnsequencedTimelineEventResponse,
} from '../api';
import { entityIds, sourceIds } from './fixtures';

export const projectionPolicy = {
  version: '1.0.0',
  description:
    'Events are sequenced by their temporal anchor, projected on stated bounds only. A period is ' +
    'anchored by its stated start bound.',
} as const;

export function partialDate(value: string, precision: string): PartialDateResponse {
  return { value, precision };
}

export function dateOccurrence(date: PartialDateResponse): TemporalOccurrenceResponse {
  return { kind: 'Date', date, period: null };
}

export function periodOccurrence(
  start: PartialDateResponse | null,
  end: PartialDateResponse | null,
): TemporalOccurrenceResponse {
  return { kind: 'Period', date: null, period: { start, end } };
}

function anchorSourceOf(
  anchor: PartialDateResponse | null,
  occurrence: TemporalOccurrenceResponse | null,
): string | null {
  if (!anchor) {
    return null;
  }

  return occurrence?.kind === 'Period' ? 'PeriodStart' : 'Date';
}

export function diagnosisEvent(
  anchor: PartialDateResponse | null,
  occurrence: TemporalOccurrenceResponse | null,
  recordedDate: PartialDateResponse | null = null,
): TimelineEventResponse {
  return {
    entityId: entityIds.diagnosis,
    entityKind: 'PrimaryCancerDiagnosis',
    label: 'Malignant neoplasm of breast (disorder)',
    anchor,
    anchorSource: anchorSourceOf(anchor, occurrence),
    occurrence,
    diagnosis: {
      code: {
        system: 'http://snomed.info/sct',
        code: '254837009',
        display: 'Malignant neoplasm of breast (disorder)',
      },
      recordedDate,
    },
    staging: null,
    procedure: null,
  };
}

export function stagingEvent(
  anchor: PartialDateResponse | null,
  occurrence: TemporalOccurrenceResponse | null,
): TimelineEventResponse {
  return {
    entityId: entityIds.staging,
    entityKind: 'CancerStaging',
    label: 'Stage IIA',
    anchor,
    anchorSource: anchorSourceOf(anchor, occurrence),
    occurrence,
    diagnosis: null,
    staging: {
      stageGroup: { system: 'http://cancerstaging.org', code: 'IIA', display: 'Stage IIA' },
      categories: [
        {
          axis: 'T',
          code: { system: 'http://cancerstaging.org', code: 'T2', display: null },
          sourceResourceId: sourceIds.primaryTumour,
        },
        {
          axis: 'N',
          code: { system: 'http://cancerstaging.org', code: 'N1', display: null },
          sourceResourceId: sourceIds.regionalNodes,
        },
        {
          axis: 'M',
          code: { system: 'http://cancerstaging.org', code: 'M0', display: null },
          sourceResourceId: sourceIds.distantMetastases,
        },
      ],
    },
    procedure: null,
  };
}

export function procedureEvent(
  anchor: PartialDateResponse | null,
  occurrence: TemporalOccurrenceResponse | null,
  entityId = entityIds.procedure,
  label = 'Lumpectomy of breast (procedure)',
): TimelineEventResponse {
  return {
    entityId,
    entityKind: 'CancerSurgicalProcedure',
    label,
    anchor,
    anchorSource: anchorSourceOf(anchor, occurrence),
    occurrence,
    diagnosis: null,
    staging: null,
    procedure: {
      code: { system: 'http://snomed.info/sct', code: '392021009', display: label },
    },
  };
}

export function group(
  sequence: number,
  kind: string,
  events: TimelineEventResponse[],
): TimelineGroupResponse {
  return { sequence, kind, events };
}

export function timelineResponse(
  overrides: Partial<PatientTimelineResponse> = {},
): PatientTimelineResponse {
  return {
    patientId: entityIds.patient,
    projectionPolicy: { ...projectionPolicy },
    groups: [
      group(1, 'Established', [
        diagnosisEvent(
          partialDate('2019-03', 'Month'),
          dateOccurrence(partialDate('2019-03', 'Month')),
          partialDate('2019-04-02', 'Day'),
        ),
      ]),
      group(2, 'Established', [
        stagingEvent(
          partialDate('2019-04-02', 'Day'),
          dateOccurrence(partialDate('2019-04-02', 'Day')),
        ),
      ]),
      group(3, 'Established', [
        procedureEvent(
          partialDate('2019-05', 'Month'),
          periodOccurrence(partialDate('2019-05', 'Month'), partialDate('2019-06-12', 'Day')),
        ),
      ]),
    ],
    unsequencedEvents: [],
    ...overrides,
  };
}

export function sharedAnchorTimeline(): PatientTimelineResponse {
  return timelineResponse({
    groups: [
      group(1, 'SharedTemporalAnchor', [
        diagnosisEvent(
          partialDate('2019-03-14T10:00:00+02:00', 'Instant'),
          dateOccurrence(partialDate('2019-03-14T10:00:00+02:00', 'Instant')),
        ),
        stagingEvent(
          partialDate('2019-03-14T08:00:00+00:00', 'Instant'),
          dateOccurrence(partialDate('2019-03-14T08:00:00+00:00', 'Instant')),
        ),
      ]),
    ],
  });
}

export function orderNotEstablishedTimeline(): PatientTimelineResponse {
  return timelineResponse({
    groups: [
      group(1, 'OrderNotEstablished', [
        stagingEvent(
          partialDate('2019-03-15', 'Day'),
          dateOccurrence(partialDate('2019-03-15', 'Day')),
        ),
        diagnosisEvent(
          partialDate('2019-03', 'Month'),
          dateOccurrence(partialDate('2019-03', 'Month')),
        ),
      ]),
    ],
  });
}

export function unsequencedTimeline(): PatientTimelineResponse {
  const noOccurrence: UnsequencedTimelineEventResponse = {
    reason: 'NoOccurrenceStated',
    event: procedureEvent(null, null, entityIds.procedure, 'Mastectomy of breast'),
  };

  const noAnchorBound: UnsequencedTimelineEventResponse = {
    reason: 'NoAnchorBound',
    event: procedureEvent(
      null,
      periodOccurrence(null, partialDate('2019-06-12', 'Day')),
      sourceIds.procedure,
      'Sentinel lymph node biopsy',
    ),
  };

  return timelineResponse({
    groups: [
      group(1, 'Established', [
        diagnosisEvent(
          partialDate('2019-03', 'Month'),
          dateOccurrence(partialDate('2019-03', 'Month')),
        ),
      ]),
    ],
    unsequencedEvents: [noOccurrence, noAnchorBound],
  });
}

export function openEndPeriodTimeline(): PatientTimelineResponse {
  return timelineResponse({
    groups: [
      group(1, 'Established', [
        procedureEvent(
          partialDate('2019-08', 'Month'),
          periodOccurrence(partialDate('2019-08', 'Month'), null),
          entityIds.procedure,
          'Axillary lymph node dissection',
        ),
      ]),
    ],
  });
}

export function zeroLengthPeriodTimeline(): PatientTimelineResponse {
  return timelineResponse({
    groups: [
      group(1, 'Established', [
        procedureEvent(
          partialDate('2019-05-12', 'Day'),
          periodOccurrence(partialDate('2019-05-12', 'Day'), partialDate('2019-05-12', 'Day')),
        ),
      ]),
    ],
  });
}

export function nonLexicalOrderTimeline(): PatientTimelineResponse {
  return timelineResponse({
    groups: [
      group(1, 'Established', [
        diagnosisEvent(partialDate('2020', 'Year'), dateOccurrence(partialDate('2020', 'Year'))),
      ]),
      group(2, 'Established', [
        stagingEvent(partialDate('2019', 'Year'), dateOccurrence(partialDate('2019', 'Year'))),
      ]),
    ],
  });
}

export function emptyTimeline(): PatientTimelineResponse {
  return timelineResponse({ groups: [], unsequencedEvents: [] });
}
