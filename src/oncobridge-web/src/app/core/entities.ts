import {
  CancerStagingResponse,
  CancerSurgicalProcedureResponse,
  CodedConceptResponse,
  PatientRecordResponse,
  PatientResponse,
  PrimaryCancerDiagnosisResponse,
} from '../api';

export type EntityKind =
  'Patient' | 'PrimaryCancerDiagnosis' | 'CancerStaging' | 'CancerSurgicalProcedure';

export interface EntityInstance {
  readonly id: string;
  readonly kind: EntityKind;
  readonly kindLabel: string;
  readonly label: string;
}

const kindLabels: Readonly<Record<EntityKind, string>> = {
  Patient: 'Patient',
  PrimaryCancerDiagnosis: 'Primary cancer diagnosis',
  CancerStaging: 'Cancer staging',
  CancerSurgicalProcedure: 'Cancer surgical procedure',
};

export function kindLabelOf(kind: EntityKind): string {
  return kindLabels[kind];
}

export function entityInstancesOf(record: PatientRecordResponse): readonly EntityInstance[] {
  return [
    instance('Patient', record.patient.id, patientLabel(record.patient)),
    ...record.primaryCancerDiagnoses.map((diagnosis) =>
      instance('PrimaryCancerDiagnosis', diagnosis.id, diagnosisLabel(diagnosis)),
    ),
    ...record.cancerStagings.map((staging) =>
      instance('CancerStaging', staging.id, stagingLabel(staging)),
    ),
    ...record.cancerSurgicalProcedures.map((procedure) =>
      instance('CancerSurgicalProcedure', procedure.id, procedureLabel(procedure)),
    ),
  ];
}

export function defaultEntityIdOf(instances: readonly EntityInstance[]): string | null {
  const order: readonly EntityKind[] = [
    'CancerStaging',
    'PrimaryCancerDiagnosis',
    'CancerSurgicalProcedure',
    'Patient',
  ];

  for (const kind of order) {
    const match = instances.find((candidate) => candidate.kind === kind);

    if (match) {
      return match.id;
    }
  }

  return null;
}

export function stagingOf(
  record: PatientRecordResponse | null,
  entity: EntityInstance | null,
): CancerStagingResponse | null {
  if (!record || entity?.kind !== 'CancerStaging') {
    return null;
  }

  return record.cancerStagings.find((staging) => staging.id === entity.id) ?? null;
}

export function codedLabel(concept: CodedConceptResponse): string {
  return concept.display ?? concept.code;
}

function instance(kind: EntityKind, id: string, label: string): EntityInstance {
  return { id, kind, kindLabel: kindLabels[kind], label };
}

function patientLabel(patient: PatientResponse): string {
  return patient.sourceIdentifier ?? patient.id;
}

function diagnosisLabel(diagnosis: PrimaryCancerDiagnosisResponse): string {
  return codedLabel(diagnosis.code);
}

function procedureLabel(procedure: CancerSurgicalProcedureResponse): string {
  return codedLabel(procedure.code);
}

function stagingLabel(staging: CancerStagingResponse): string {
  if (staging.stageGroup) {
    return codedLabel(staging.stageGroup);
  }

  const categories = staging.categories.map((category) => codedLabel(category.code));

  return categories.length > 0 ? categories.join(' ') : staging.id;
}
