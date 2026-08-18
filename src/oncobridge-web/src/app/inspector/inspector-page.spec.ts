import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { MockInstance, beforeEach, describe, expect, it, vi } from 'vitest';

import { FindingResponse, LineageResponse, PatientRecordResponse } from '../api';
import {
  entityIds,
  findingsResponse,
  importBatchId,
  importResponse,
  patientProvenance,
  patientRecordResponse,
  sourceIds,
  stagingProvenance,
} from '../testing/fixtures';
import { InspectorPage } from './inspector-page';

interface Scenario {
  readonly import?: ReturnType<typeof importResponse>;
  readonly findings?: FindingResponse[];
  readonly record?: PatientRecordResponse | 'skip';
  readonly provenance?: LineageResponse[] | 'skip';
  readonly entityId?: string;
  readonly sourceResourceId?: string;
}

describe('InspectorPage', () => {
  let fixture: ComponentFixture<InspectorPage>;
  let httpMock: HttpTestingController;
  let navigate: MockInstance;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    httpMock = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(InspectorPage);
  });

  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    await fixture.whenStable();
  }

  async function start(scenario: Scenario = {}): Promise<void> {
    fixture.componentRef.setInput('importBatchId', importBatchId);

    if (scenario.entityId) {
      fixture.componentRef.setInput('entityId', scenario.entityId);
    }

    if (scenario.sourceResourceId) {
      fixture.componentRef.setInput('sourceResourceId', scenario.sourceResourceId);
    }

    await settle();
  }

  async function flushImport(scenario: Scenario = {}): Promise<void> {
    request(`/api/v1/imports/${importBatchId}`).flush(scenario.import ?? importResponse());
    request(`/api/v1/imports/${importBatchId}/findings`).flush(
      scenario.findings ?? findingsResponse(),
    );
    await settle();
  }

  async function flushRecord(scenario: Scenario = {}): Promise<void> {
    if (scenario.record === 'skip') {
      return;
    }

    const record = scenario.record ?? patientRecordResponse();

    request(`/api/v1/patients/${record.patient.id}/record`).flush(record);
    await settle();
  }

  async function flushProvenance(scenario: Scenario = {}): Promise<void> {
    if (scenario.provenance === 'skip') {
      return;
    }

    const records = scenario.provenance ?? stagingProvenance();
    const domainEntityId = records[0]?.domainEntityId ?? entityIds.staging;

    request(`/api/v1/domain/${domainEntityId}/provenance`).flush({ domainEntityId, records });
    await settle();
  }

  async function golden(scenario: Scenario = {}): Promise<void> {
    await start(scenario);
    await flushImport(scenario);
    await flushRecord(scenario);
    await flushProvenance(scenario);
  }

  function request(url: string) {
    return httpMock.expectOne((candidate) => candidate.url === url);
  }

  function text(selector = ''): string {
    const root = selector
      ? (fixture.nativeElement as HTMLElement).querySelector(selector)
      : (fixture.nativeElement as HTMLElement);

    return root?.textContent ?? '';
  }

  function all(selector: string): HTMLElement[] {
    return [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLElement>(selector)];
  }

  describe('import loading and failure', () => {
    it('reports that it is loading the import before anything arrives', async () => {
      await start();

      expect(text()).toContain('Loading import…');
    });

    it('names the failing request when the import cannot be loaded', async () => {
      await start();

      request(`/api/v1/imports/${importBatchId}`).flush(
        { title: 'The import could not be loaded', status: 404 },
        { status: 404, statusText: 'Not Found' },
      );
      request(`/api/v1/imports/${importBatchId}/findings`).flush([], {
        status: 404,
        statusText: 'Not Found',
      });
      await settle();

      expect(text()).toContain('The import could not be loaded');
      expect(text()).toContain(`GET /api/v1/imports/${importBatchId} · 404`);
      expect(all('ob-source-pane')).toHaveLength(0);
    });

    it('shows the import header metadata once loaded', async () => {
      await golden();

      expect(text('ob-inspector-header')).toContain('Normalized');
      expect(text('ob-inspector-header')).toContain('api');
      expect(text('ob-inspector-header')).toContain('7 · collection');
      expect(text('ob-inspector-header')).toContain('1.0.0');
    });

    it('reveals the full payload hash on request', async () => {
      await golden();

      const hash = all('[data-testid="payload-hash"]')[0];

      expect(hash.textContent).toContain('…');

      all('ob-inspector-header button')[0].click();
      await settle();

      expect(all('[data-testid="payload-hash"]')[0].textContent).toBe(importResponse().contentHash);
    });
  });

  describe('entity selection', () => {
    it('offers one choice per canonical entity instance', async () => {
      await golden();

      expect(all('ob-entity-selector .choice').map((choice) => choice.textContent?.trim())).toEqual(
        [
          expect.stringContaining('Patient'),
          expect.stringContaining('Primary cancer diagnosis'),
          expect.stringContaining('Cancer staging'),
          expect.stringContaining('Cancer surgical procedure'),
        ],
      );
    });

    it('labels instances from canonical data', async () => {
      await golden();

      const labels = text('ob-entity-selector');

      expect(labels).toContain('SYN-0001');
      expect(labels).toContain('Malignant neoplasm of breast (disorder)');
      expect(labels).toContain('Stage IIA');
      expect(labels).toContain('Lumpectomy of breast (procedure)');
    });

    it('defaults to the first cancer staging', async () => {
      await golden();

      const selected = all('ob-entity-selector .choice.selected');

      expect(selected).toHaveLength(1);
      expect(selected[0].textContent).toContain('Cancer staging');
    });

    it('exposes every instance when a patient has several of a kind', async () => {
      const base = patientRecordResponse();
      const record: PatientRecordResponse = {
        ...base,
        cancerStagings: [
          base.cancerStagings[0],
          { ...base.cancerStagings[0], id: 'ffffffff-0000-4000-8000-00000000000f' },
        ],
        cancerSurgicalProcedures: [
          base.cancerSurgicalProcedures[0],
          { ...base.cancerSurgicalProcedures[0], id: 'eeeeeeee-0000-4000-8000-00000000000e' },
        ],
      };

      await start();
      await flushImport();
      await flushRecord({ record });
      await flushProvenance();

      expect(all('ob-entity-selector .choice')).toHaveLength(6);
      expect(text('ob-entity-selector')).toContain('6 instances · 1 patient');
    });

    it('honours the entity id supplied as query state', async () => {
      await golden({ entityId: entityIds.patient, provenance: patientProvenance() });

      const selected = all('ob-entity-selector .choice.selected');

      expect(selected[0].textContent).toContain('Patient');
      expect(text('ob-normalized-pane')).toContain('SYN-0001');
    });

    it('falls back to the default entity when the query state names an unknown id', async () => {
      await golden({ entityId: 'not-a-known-entity' });

      expect(all('ob-entity-selector .choice.selected')[0].textContent).toContain('Cancer staging');
    });

    it('writes the chosen entity into query state', async () => {
      await golden();

      all('ob-entity-selector .choice')[0].click();
      await settle();

      expect(navigate).toHaveBeenCalledWith([], {
        queryParams: { entityId: entityIds.patient, sourceResourceId: null },
        queryParamsHandling: 'merge',
      });
    });
  });

  describe('imports with no canonical patient', () => {
    it('keeps source and quality usable and explains the empty normalized pane', async () => {
      await start();
      await flushImport({ import: importResponse({ patientIds: [] }) });

      expect(text('ob-normalized-pane')).toContain('No canonical patient was produced');
      expect(all('ob-quality-pane .finding')).toHaveLength(3);
      expect(text('ob-provenance-pane')).toContain('No canonical entity selected');
      expect(all('ob-entity-selector')).toHaveLength(0);
    });
  });

  describe('source pane', () => {
    it('lists exactly the four contributing resources with markers A to D', async () => {
      await golden();

      const rows = all('ob-source-pane .row');

      expect(rows).toHaveLength(4);
      expect(
        rows.map((row) => row.querySelector('ob-evidence-marker')?.textContent?.trim()),
      ).toEqual(['Evidence A', 'Evidence B', 'Evidence C', 'Evidence D']);
      expect(rows.map((row) => row.querySelector('.logical')?.textContent)).toEqual([
        'staging-group-001',
        'staging-t-001',
        'staging-n-001',
        'staging-m-001',
      ]);
    });

    it('states the role of each contributing resource', async () => {
      await golden();

      expect(all('ob-source-pane .role').map((role) => role.textContent?.trim())).toEqual([
        'Whole entity',
        'Category T',
        'Category N',
        'Category M',
      ]);
    });

    it('reports how many of the import resources this entity names', async () => {
      await golden();

      expect(text('ob-source-pane')).toContain("4 of 7 resources named by this entity's lineage");
    });

    it('renders the stored resource JSON of the selected row as text', async () => {
      await golden();

      expect(text('ob-source-pane')).toContain('Stored resource JSON');

      const json = all('ob-source-pane pre')[0];

      expect(json.textContent).toContain('"resourceType": "Observation"');
      expect(json.textContent).toContain('"id": "staging-group-001"');
      expect(json.innerHTML).not.toContain('<span');
    });

    it('switches the JSON viewer when another evidence row is chosen', async () => {
      await golden({ sourceResourceId: sourceIds.primaryTumour });

      expect(all('ob-source-pane pre')[0].textContent).toContain('"id": "staging-t-001"');
      expect(all('ob-source-pane .row.selected')[0].textContent).toContain('staging-t-001');
    });

    it('writes the chosen evidence row into query state', async () => {
      await golden();

      all('ob-source-pane .row')[2].click();
      await settle();

      expect(navigate).toHaveBeenCalledWith([], {
        queryParams: { sourceResourceId: sourceIds.regionalNodes },
        queryParamsHandling: 'merge',
      });
    });

    it('explains an absent stored resource JSON instead of inventing one', async () => {
      const batch = importResponse();
      const sources = batch.sourceResources.map((source) =>
        source.id === sourceIds.stageGroup ? { ...source, resourceJson: null } : source,
      );

      await start();
      await flushImport({ import: importResponse({ sourceResources: sources }) });
      await flushRecord();
      await flushProvenance();

      expect(text('ob-source-pane')).toContain('No parsed JSON stored for this resource');
      expect(all('ob-source-pane pre')).toHaveLength(0);
    });

    it('never calls the stored JSON the byte-exact evidence', async () => {
      await golden();

      expect(text('ob-source-pane')).toContain(
        'byte-exact evidence is the import batch raw payload',
      );
      expect(text('ob-source-pane')).not.toContain('byte-exact resource');
    });
  });

  describe('normalized pane · cancer staging', () => {
    it('shows the stage group, categories and effective date', async () => {
      await golden();

      const normalized = text('ob-normalized-pane');

      expect(normalized).toContain('Stage IIA');
      expect(normalized).toContain('T2');
      expect(normalized).toContain('N1');
      expect(normalized).toContain('M0');
      expect(normalized).toContain('2019-04-02');
      expect(normalized).toContain('Day');
    });

    it('attributes each value to the evidence record it came from', async () => {
      await golden();

      const rows = all('ob-normalized-pane .field');
      const shape = rows.map((row) => [
        row.querySelector('dt')?.textContent?.trim(),
        row.querySelector('ob-evidence-marker')?.textContent?.trim() ?? null,
        row.querySelector('.from')?.textContent?.trim(),
      ]);

      expect(shape).toEqual([
        ['Stage group', 'Evidence A', 'from A'],
        ['Method', null, 'absent'],
        ['Effective', null, 'from A'],
        ['Category T', 'Evidence B', 'from B'],
        ['Category N', 'Evidence C', 'from C'],
        ['Category M', 'Evidence D', 'from D'],
        ['Associated diagnosis', null, 'other entity'],
        ['Patient', null, 'other entity'],
      ]);
    });

    it('states an absent method and names the related check the backend reported', async () => {
      await golden();

      const method = all('ob-normalized-pane .field')[1];

      expect(method.textContent).toContain('Not stated');
      expect(method.textContent).toContain('see OB-CONF-002');
    });

    it('does not hard-code the related check id', async () => {
      const findings = findingsResponse().map((finding) =>
        finding.checkId === 'OB-CONF-002' ? { ...finding, checkId: 'OB-CONF-009' } : finding,
      );

      await golden({ findings });

      expect(all('ob-normalized-pane .field')[1].textContent).toContain('see OB-CONF-009');
      expect(text('ob-normalized-pane')).not.toContain('see OB-CONF-002');
    });

    it('never marks a related canonical entity with an evidence marker', async () => {
      await golden();

      const related = all('ob-normalized-pane .field.related');

      expect(related).toHaveLength(2);
      expect(related.map((row) => row.querySelector('dt')?.textContent?.trim())).toEqual([
        'Associated diagnosis',
        'Patient',
      ]);

      for (const row of related) {
        expect(row.querySelector('ob-evidence-marker')).toBeNull();
        expect(row.textContent).toContain('other entity');
      }
    });

    it('shows the related entities without repeating the patient birth date', async () => {
      await golden();

      const normalized = text('ob-normalized-pane');

      expect(normalized).toContain('Malignant neoplasm of breast (disorder)');
      expect(normalized).toContain('SYN-0001');
      expect(normalized).not.toContain('1968');
    });
  });

  describe('normalized pane · other entities', () => {
    it('shows the patient birth date at its stated year precision', async () => {
      await golden({ entityId: entityIds.patient, provenance: patientProvenance() });

      const normalized = text('ob-normalized-pane');

      expect(normalized).toContain('1968');
      expect(normalized).toContain('Year');
      expect(normalized).not.toContain('1968-01-01');
    });

    it('shows a diagnosis onset at month precision without widening it', async () => {
      await golden({
        entityId: entityIds.diagnosis,
        provenance: [
          {
            domainEntityType: 'PrimaryCancerDiagnosis',
            domainEntityId: entityIds.diagnosis,
            fieldPath: null,
            sourceResourceId: sourceIds.condition,
            transformationName: 'FhirPrimaryCancerDiagnosisNormalization',
            transformationVersion: '1.0.0',
          },
        ],
      });

      const normalized = text('ob-normalized-pane');

      expect(normalized).toContain('2019-03');
      expect(normalized).toContain('Month');
      expect(normalized).not.toContain('2019-03-01');
    });

    it('shows a performed period keeping each boundary precision', async () => {
      await golden({
        entityId: entityIds.procedure,
        provenance: [
          {
            domainEntityType: 'CancerSurgicalProcedure',
            domainEntityId: entityIds.procedure,
            fieldPath: null,
            sourceResourceId: sourceIds.procedure,
            transformationName: 'FhirCancerSurgicalProcedureNormalization',
            transformationVersion: '1.0.0',
          },
        ],
      });

      const performed = all('ob-normalized-pane .field')[1];

      expect(performed.textContent).toContain('Period');
      expect(performed.textContent).toContain('2019-05');
      expect(performed.textContent).toContain('Month');
      expect(performed.textContent).toContain('2019-06-12');
      expect(performed.textContent).toContain('Day');
      expect(performed.textContent).not.toContain('2019-05-01');
    });
  });

  describe('quality pane', () => {
    it('shows every finding for the import regardless of the selection', async () => {
      await golden();

      expect(
        all('ob-quality-pane .finding').map((card) => card.querySelector('.check')?.textContent),
      ).toEqual(['OB-CONF-001', 'OB-CONF-002', 'OB-REF-001']);
      expect(text('ob-quality-pane')).toContain('3 findings for this import · 3 Error');
    });

    it('shows the whole evidence shape of a finding', async () => {
      await golden();

      const card = all('ob-quality-pane .finding')[1];

      expect(card.textContent).toContain('OB-CONF-002');
      expect(card.textContent).toContain('Error');
      expect(card.textContent).toContain('Conformance');
      expect(card.textContent).toContain('The TNM stage group does not state a staging method.');
      expect(card.textContent).toContain('Observation.method to be present');
      expect(card.textContent).toContain('Observation.method is absent');

      const citation = card.querySelector<HTMLAnchorElement>('a');

      expect(citation?.getAttribute('href')).toContain('mcode-tnm-stage-group');
      expect(citation?.getAttribute('target')).toBe('_blank');
      expect(citation?.getAttribute('rel')).toBe('noopener noreferrer');
    });

    it('joins the finding target id to the import source resources for display', async () => {
      await golden();

      const target = all('ob-quality-pane dd')[0];

      expect(target.textContent).toContain('SourceResource');
      expect(target.textContent).toContain('Condition');
      expect(target.textContent).toContain('condition-001');
      expect(target.textContent).toContain('entry 1');
    });

    it('shows the raw target id when nothing in the import matches it', async () => {
      const findings: FindingResponse[] = [
        {
          ...findingsResponse()[0],
          target: {
            kind: 'DomainEntity',
            id: 'aaaaaaaa-0000-4000-8000-00000000000a',
            domainEntityType: 'CancerStaging',
          },
        },
      ];

      await golden({ findings });

      const target = all('ob-quality-pane dd')[0];

      expect(target.textContent).toContain('aaaaaaaa-0000-4000-8000-00000000000a');
      expect(target.textContent).toContain('CancerStaging');
    });

    it('annotates only the findings that target this selection', async () => {
      await golden();

      const cards = all('ob-quality-pane .finding');

      expect(cards.map((card) => card.classList.contains('related'))).toEqual([false, true, false]);
      expect(cards[1].textContent).toContain('Targets this selection');
      expect(cards[0].textContent).not.toContain('Targets this selection');
    });

    it('says an import has no findings without implying full profile validation', async () => {
      await golden({ findings: [] });

      const quality = text('ob-quality-pane');

      expect(quality).toContain('No findings for this import');
      expect(quality).toContain('not full mCODE profile validation');
      expect(quality).not.toContain('valid mCODE');
      expect(quality).not.toContain('mCODE compliant');
    });

    it('distinguishes a failed findings request from an import with no findings', async () => {
      await start();

      request(`/api/v1/imports/${importBatchId}`).flush(importResponse());
      request(`/api/v1/imports/${importBatchId}/findings`).flush(
        { title: 'Findings could not be loaded', status: 500 },
        { status: 500, statusText: 'Server Error' },
      );
      await settle();

      const quality = text('ob-quality-pane');

      expect(quality).toContain('Findings could not be loaded');
      expect(quality).toContain('nothing can be concluded about quality from this state');
      expect(quality).not.toContain('No findings for this import');
    });
  });

  describe('provenance pane', () => {
    it('shows exactly the four staging lineage records, whole entity first', async () => {
      await golden();

      const rows = all('ob-provenance-pane tbody tr');

      expect(rows).toHaveLength(4);
      expect(rows.map((row) => row.querySelector('.scope')?.textContent)).toEqual([
        'Whole entity',
        'Category T',
        'Category N',
        'Category M',
      ]);
      expect(rows[0].classList.contains('whole')).toBe(true);
    });

    it('shows the field path, source resource and transformation of each record', async () => {
      await golden();

      const rows = all('ob-provenance-pane tbody tr');

      expect(rows[0].textContent).toContain('fieldPath: null');
      expect(rows[1].textContent).toContain('fieldPath: PrimaryTumour');
      expect(rows[1].textContent).toContain('staging-t-001');

      for (const row of rows) {
        expect(row.textContent).toContain('FhirCancerStagingNormalization');
        expect(row.textContent).toContain('1.0.0');
      }
    });

    it('states the four-to-one shape of the golden case', async () => {
      await golden();

      expect(text('ob-provenance-pane')).toContain(
        '4 source resources, 4 lineage records, one canonical entity',
      );
    });

    it('treats a provenance failure for a canonical entity as an error', async () => {
      await start();
      await flushImport();
      await flushRecord();

      request(`/api/v1/domain/${entityIds.staging}/provenance`).flush(
        { title: 'Lineage could not be loaded for this entity', status: 404 },
        { status: 404, statusText: 'Not Found' },
      );
      await settle();

      expect(text('ob-provenance-pane')).toContain('Lineage could not be loaded for this entity');
      expect(text('ob-source-pane')).toContain('Lineage could not be loaded for this entity');
    });
  });
});
