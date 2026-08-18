import {
  FindingResponse,
  ImportResponse,
  LineageResponse,
  PatientRecordResponse,
  SourceResourceResponse,
} from '../api';

export const sourceIds = {
  patient: '00000000-0000-4000-8000-000000000000',
  condition: '00000000-0000-4000-8000-000000000001',
  stageGroup: '00000000-0000-4000-8000-000000000002',
  primaryTumour: '00000000-0000-4000-8000-000000000003',
  regionalNodes: '00000000-0000-4000-8000-000000000004',
  distantMetastases: '00000000-0000-4000-8000-000000000005',
  procedure: '00000000-0000-4000-8000-000000000006',
} as const;

export const entityIds = {
  patient: sourceIds.patient,
  diagnosis: 'dddddddd-0000-4000-8000-00000000000d',
  staging: sourceIds.stageGroup,
  procedure: 'pppppppp-0000-4000-8000-00000000000p'.replace(/p/g, 'b'),
} as const;

export const importBatchId = '9d3f2c18-4b7a-4c51-9f2e-8a1d6b0e5c73';

export const contentHash = '4f9c1d2b8e5a06c7f0b3a9d1e2c48576a90b1c2d3e4f5061728394a5b6c7a37b';

const stageGroupJson = {
  resourceType: 'Observation',
  id: 'staging-group-001',
  status: 'final',
  code: { coding: [{ system: 'http://loinc.org', code: '21908-9' }] },
  effectiveDateTime: '2019-04-02',
};

function source(
  id: string,
  entryIndex: number,
  resourceType: string,
  sourceLogicalId: string,
  resourceJson: unknown = { resourceType, id: sourceLogicalId },
): SourceResourceResponse {
  return {
    id,
    entryIndex,
    resourceType,
    sourceLogicalId,
    fullUrl: `urn:uuid:${id}`,
    contentHash: `${entryIndex}`.repeat(8).padEnd(64, 'abcdef'),
    resourceJson,
  };
}

export function importResponse(overrides: Partial<ImportResponse> = {}): ImportResponse {
  return {
    importBatchId,
    sourceSystemLabel: 'api',
    receivedAt: '2026-08-18T14:22:05.000+00:00',
    fileName: 'bundle-acceptance-defects.json',
    contentHash,
    bundleType: 'collection',
    entryCount: 7,
    status: 'Normalized',
    normalizerVersion: '1.0.0',
    normalizedAt: '2026-08-18T14:22:07.000+00:00',
    patientIds: [entityIds.patient],
    sourceResources: [
      source(sourceIds.patient, 0, 'Patient', 'patient-001'),
      source(sourceIds.condition, 1, 'Condition', 'condition-001'),
      source(sourceIds.stageGroup, 2, 'Observation', 'staging-group-001', stageGroupJson),
      source(sourceIds.primaryTumour, 3, 'Observation', 'staging-t-001'),
      source(sourceIds.regionalNodes, 4, 'Observation', 'staging-n-001'),
      source(sourceIds.distantMetastases, 5, 'Observation', 'staging-m-001'),
      source(sourceIds.procedure, 6, 'Procedure', 'procedure-001'),
    ],
    ...overrides,
  };
}

export function patientRecordResponse(
  overrides: Partial<PatientRecordResponse> = {},
): PatientRecordResponse {
  return {
    patient: {
      id: entityIds.patient,
      sourceIdentifier: 'SYN-0001',
      birthDate: { value: '1968', precision: 'Year' },
      sexAtBirthAsRecorded: null,
    },
    primaryCancerDiagnoses: [
      {
        id: entityIds.diagnosis,
        patientId: entityIds.patient,
        code: {
          system: 'http://snomed.info/sct',
          code: '254837009',
          display: 'Malignant neoplasm of breast (disorder)',
        },
        onset: {
          kind: 'Date',
          date: { value: '2019-03', precision: 'Month' },
          period: null,
        },
        bodySite: {
          system: 'http://snomed.info/sct',
          code: '76752008',
          display: 'Breast structure (body structure)',
        },
        recordedDate: { value: '2019-04-02', precision: 'Day' },
      },
    ],
    cancerStagings: [
      {
        id: entityIds.staging,
        patientId: entityIds.patient,
        primaryCancerDiagnosisId: entityIds.diagnosis,
        stageGroup: {
          system: 'http://cancerstaging.org',
          code: 'IIA',
          display: 'Stage IIA',
        },
        method: null,
        effective: { value: '2019-04-02', precision: 'Day' },
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
    ],
    cancerSurgicalProcedures: [
      {
        id: entityIds.procedure,
        patientId: entityIds.patient,
        code: {
          system: 'http://snomed.info/sct',
          code: '392021009',
          display: 'Lumpectomy of breast (procedure)',
        },
        performed: {
          kind: 'Period',
          date: null,
          period: {
            start: { value: '2019-05', precision: 'Month' },
            end: { value: '2019-06-12', precision: 'Day' },
          },
        },
        bodySite: {
          system: 'http://snomed.info/sct',
          code: '76752008',
          display: 'Breast structure (body structure)',
        },
      },
    ],
    ...overrides,
  };
}

export function findingsResponse(): FindingResponse[] {
  return [
    {
      checkId: 'OB-CONF-001',
      category: 'Conformance',
      severity: 'Error',
      message:
        'The primary cancer condition does not state the mandatory problem-list-item or health-concern category.',
      target: { kind: 'SourceResource', id: sourceIds.condition, domainEntityType: null },
      citation:
        'https://hl7.org/fhir/us/mcode/STU4/StructureDefinition-mcode-primary-cancer-condition-definitions.html',
      expected:
        'a Condition.category coding of http://terminology.hl7.org/CodeSystem/condition-category|problem-list-item or http://hl7.org/fhir/us/core/CodeSystem/condition-category|health-concern',
      actual: 'no Condition.category coding is stated',
    },
    {
      checkId: 'OB-CONF-002',
      category: 'Conformance',
      severity: 'Error',
      message: 'The TNM stage group does not state a staging method.',
      target: { kind: 'SourceResource', id: sourceIds.stageGroup, domainEntityType: null },
      citation:
        'https://hl7.org/fhir/us/mcode/STU4/StructureDefinition-mcode-tnm-stage-group.html',
      expected: 'Observation.method to be present, which mCODE STU4 states as cardinality 1..1',
      actual: 'Observation.method is absent',
    },
    {
      checkId: 'OB-REF-001',
      category: 'ReferentialIntegrity',
      severity: 'Error',
      message:
        'The reference at Procedure.reasonReference[0] does not resolve within this import batch.',
      target: { kind: 'SourceResource', id: sourceIds.procedure, domainEntityType: null },
      citation: 'https://hl7.org/fhir/R4/bundle.html',
      expected: 'a reference resolving to exactly one resource in the same import batch',
      actual:
        "Procedure.reasonReference[0] = 'urn:uuid:12345678-8888-4888-8888-121212121212'",
    },
  ];
}

function lineage(fieldPath: string | null, sourceResourceId: string): LineageResponse {
  return {
    domainEntityType: 'CancerStaging',
    domainEntityId: entityIds.staging,
    fieldPath,
    sourceResourceId,
    transformationName: 'FhirCancerStagingNormalization',
    transformationVersion: '1.0.0',
  };
}

export function stagingProvenance(): LineageResponse[] {
  return [
    lineage(null, sourceIds.stageGroup),
    lineage('DistantMetastases', sourceIds.distantMetastases),
    lineage('PrimaryTumour', sourceIds.primaryTumour),
    lineage('RegionalNodes', sourceIds.regionalNodes),
  ];
}

export function patientProvenance(): LineageResponse[] {
  return [
    {
      domainEntityType: 'Patient',
      domainEntityId: entityIds.patient,
      fieldPath: null,
      sourceResourceId: sourceIds.patient,
      transformationName: 'FhirPatientNormalization',
      transformationVersion: '1.0.0',
    },
  ];
}
