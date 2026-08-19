import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { MockInstance, beforeEach, describe, expect, it, vi } from 'vitest';

import { ImportResponse, PatientTimelineResponse } from '../api';
import { entityIds, importBatchId, importResponse, sourceIds } from '../testing/fixtures';
import {
  emptyTimeline,
  nonLexicalOrderTimeline,
  openEndPeriodTimeline,
  orderNotEstablishedTimeline,
  sharedAnchorTimeline,
  timelineResponse,
  unsequencedTimeline,
  zeroLengthPeriodTimeline,
} from '../testing/timeline-fixtures';
import { TimelinePage } from './timeline-page';

interface Scenario {
  readonly import?: ImportResponse;
  readonly timeline?: PatientTimelineResponse | 'skip';
  readonly patientId?: string;
}

const secondPatientId = '11111111-2222-4333-8444-555555555555';

describe('TimelinePage', () => {
  let fixture: ComponentFixture<TimelinePage>;
  let httpMock: HttpTestingController;
  let navigate: MockInstance;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    httpMock = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(TimelinePage);
  });

  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    await fixture.whenStable();
  }

  async function start(scenario: Scenario = {}): Promise<void> {
    fixture.componentRef.setInput('importBatchId', importBatchId);

    if (scenario.patientId) {
      fixture.componentRef.setInput('patientId', scenario.patientId);
    }

    await settle();
  }

  async function flushImport(scenario: Scenario = {}): Promise<void> {
    request(`/api/v1/imports/${importBatchId}`).flush(scenario.import ?? importResponse());
    await settle();
  }

  async function flushTimeline(scenario: Scenario = {}): Promise<void> {
    if (scenario.timeline === 'skip') {
      return;
    }

    const timeline = scenario.timeline ?? timelineResponse();
    const patientId = scenario.patientId ?? timeline.patientId;

    request(`/api/v1/patients/${patientId}/timeline`).flush(timeline);
    await settle();
  }

  async function golden(scenario: Scenario = {}): Promise<void> {
    await start(scenario);
    await flushImport(scenario);
    await flushTimeline(scenario);
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

  function cards(): HTMLElement[] {
    return all('ob-timeline-event');
  }

  function railOf(card: HTMLElement, index = 0): string[] {
    const rails = [...card.querySelectorAll<HTMLElement>('ob-precision-rail')];

    return [...(rails[index]?.querySelectorAll<HTMLElement>('.cell.marked') ?? [])].map(
      (cell) => cell.textContent?.trim() ?? '',
    );
  }

  describe('data load', () => {
    it('loads the import so the canonical patient ids stay authoritative', async () => {
      await start();

      request(`/api/v1/imports/${importBatchId}`).flush(importResponse());
      await settle();

      request(`/api/v1/patients/${entityIds.patient}/timeline`).flush(timelineResponse());
    });

    it('loads the only canonical patient without being asked', async () => {
      await golden();

      expect(text('.panel-head')).toContain('3 events');
    });

    it('reports that it is loading the import before anything arrives', async () => {
      await start();

      expect(text()).toContain('Loading import…');
    });

    it('reports that it is loading the timeline once the import has arrived', async () => {
      await start();
      await flushImport();

      expect(text()).toContain('Loading patient timeline…');
    });

    it('announces loading politely', async () => {
      await start();
      await flushImport();

      expect(all('[aria-live="polite"]').length).toBeGreaterThan(0);
    });

    it('never derives a patient id from a source resource', async () => {
      await start();
      await flushImport({ import: importResponse({ patientIds: [] }) });

      httpMock.verify();
    });
  });

  describe('patient selection', () => {
    it('offers no patient chooser when the import holds one patient', async () => {
      await golden();

      expect(all('.patient')).toHaveLength(0);
    });

    it('offers a compact chooser when the import holds several patients', async () => {
      await start();
      await flushImport({
        import: importResponse({ patientIds: [entityIds.patient, secondPatientId] }),
      });
      await flushTimeline();

      expect(all('.patient')).toHaveLength(2);
    });

    it('loads the timeline of the patient named in the query state', async () => {
      await start({ patientId: secondPatientId });
      await flushImport({
        import: importResponse({ patientIds: [entityIds.patient, secondPatientId] }),
      });

      request(`/api/v1/patients/${secondPatientId}/timeline`).flush(
        timelineResponse({ patientId: secondPatientId }),
      );
      await settle();

      expect(text('.panel-head')).toContain('3 events');
    });

    it('puts the chosen patient into query state rather than local state', async () => {
      await start();
      await flushImport({
        import: importResponse({ patientIds: [entityIds.patient, secondPatientId] }),
      });
      await flushTimeline();

      all('.patient')[1].click();

      expect(navigate).toHaveBeenCalledWith([], {
        queryParams: { patientId: secondPatientId },
        queryParamsHandling: 'merge',
      });
    });

    it('keeps a valid requested patient and corrects nothing', async () => {
      await start({ patientId: secondPatientId });
      await flushImport({
        import: importResponse({ patientIds: [entityIds.patient, secondPatientId] }),
      });
      await flushTimeline({ patientId: secondPatientId });

      expect(navigate).not.toHaveBeenCalled();
    });

    it('rewrites an invalid requested patient to the patient it actually shows', async () => {
      await start({ patientId: 'not-a-canonical-patient' });
      await flushImport();
      await flushTimeline();

      expect(navigate).toHaveBeenCalledExactlyOnceWith([], {
        queryParams: { patientId: entityIds.patient },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    });

    it('replaces the invalid URL rather than pushing a second history entry', async () => {
      await start({ patientId: 'not-a-canonical-patient' });
      await flushImport();
      await flushTimeline();

      expect(navigate.mock.calls[0][1]).toMatchObject({ replaceUrl: true });
    });

    it('settles after one correction when the corrected patient arrives as input', async () => {
      await start({ patientId: 'not-a-canonical-patient' });
      await flushImport();
      await flushTimeline();

      fixture.componentRef.setInput('patientId', entityIds.patient);
      await settle();

      expect(navigate).toHaveBeenCalledTimes(1);
    });

    it('corrects nothing when no patient is requested', async () => {
      await golden();

      expect(navigate).not.toHaveBeenCalled();
    });

    it('corrects nothing when the import produced no canonical patient', async () => {
      await start({ patientId: 'not-a-canonical-patient' });
      await flushImport({ import: importResponse({ patientIds: [] }) });

      expect(navigate).not.toHaveBeenCalled();
    });

    it('states that no canonical patient was produced rather than inventing one', async () => {
      await start();
      await flushImport({ import: importResponse({ patientIds: [] }) });

      expect(text()).toContain('No canonical patient was produced');
      expect(all('ob-timeline-event')).toHaveLength(0);
    });
  });

  describe('failure and emptiness', () => {
    it('names the failing import request and offers a targeted retry', async () => {
      await start();

      request(`/api/v1/imports/${importBatchId}`).flush(
        { title: 'The import could not be loaded', status: 404 },
        { status: 404, statusText: 'Not Found' },
      );
      await settle();

      expect(text()).toContain('The import could not be loaded');
      expect(text()).toContain(`GET /api/v1/imports/${importBatchId}`);
      expect(text()).toContain('Retry import');
    });

    it('distinguishes a failed timeline request from an empty timeline', async () => {
      await start();
      await flushImport();

      request(`/api/v1/patients/${entityIds.patient}/timeline`).flush(
        { title: 'The patient timeline could not be loaded', status: 500 },
        { status: 500, statusText: 'Server Error' },
      );
      await settle();

      expect(text()).toContain('The patient timeline could not be loaded');
      expect(text()).toContain('This is a failed request, not an empty timeline');
      expect(text()).toContain('Retry timeline');
      expect(text()).not.toContain('No timeline events were produced');
    });

    it('retries only the timeline when the timeline failed', async () => {
      await start();
      await flushImport();

      request(`/api/v1/patients/${entityIds.patient}/timeline`).flush(
        { title: 'The patient timeline could not be loaded' },
        { status: 500, statusText: 'Server Error' },
      );
      await settle();

      all('ob-pane-error button')[0].click();
      await settle();

      request(`/api/v1/patients/${entityIds.patient}/timeline`).flush(timelineResponse());
      await settle();

      expect(cards()).toHaveLength(3);
    });

    it('states truthfully that OncoBridge V1 produced no timeline events', async () => {
      await golden({ timeline: emptyTimeline() });

      expect(text()).toContain(
        'No timeline events were produced from the canonical concepts exposed in OncoBridge V1',
      );
      expect(text()).not.toContain('No clinical history');
    });
  });

  describe('the golden projection', () => {
    it('renders one group per projected group', async () => {
      await golden();

      expect(all('li[ob-timeline-group]')).toHaveLength(3);
    });

    it('renders the badges in the sequence the API supplied', async () => {
      await golden();

      expect(all('.badge').map((badge) => badge.textContent?.trim())).toEqual(['01', '02', '03']);
    });

    it('summarises the projection as anchors established, not orders proven', async () => {
      await golden();

      expect(text('.panel-head')).toContain('3 anchors established');
      expect(text()).not.toContain('every relative order proven');
    });

    it('counts nothing as unsequenced for the golden fixture', async () => {
      await golden();

      expect(text('.panel-head')).toContain('none unsequenced');
      expect(all('ob-unsequenced-section')).toHaveLength(0);
    });

    it('renders the diagnosis card from canonical fields', async () => {
      await golden();

      const card = cards()[0];

      expect(card.textContent).toContain('Primary cancer diagnosis');
      expect(card.textContent).toContain('Malignant neoplasm of breast');
      expect(card.textContent).toContain('onset');
      expect(card.textContent).toContain('2019-03');
      expect(card.textContent).toContain('Month');
      expect(railOf(card)).toEqual(['M']);
    });

    it('keeps the recorded date as metadata rather than a second event', async () => {
      await golden();

      expect(cards()[0].textContent).toContain('metadata, not a second event');
      expect(cards()).toHaveLength(3);
    });

    it('renders the staging card with its axis-ordered T N M summary', async () => {
      await golden();

      const card = cards()[1];

      expect(card.textContent).toContain('Cancer staging');
      expect(card.textContent).toContain('Stage IIA');
      expect(card.textContent).toContain('effective');
      expect(card.textContent).toContain('2019-04-02');
      expect(railOf(card)).toEqual(['D']);
      expect([...card.querySelectorAll('.category')].map((c) => c.textContent?.trim())).toEqual([
        'T2',
        'N1',
        'M0',
      ]);
    });

    it('renders the procedure card with both period bounds at their own precision', async () => {
      await golden();

      const card = cards()[2];

      expect(card.textContent).toContain('Cancer surgical procedure');
      expect(card.textContent).toContain('Period');
      expect(card.textContent).toContain('Lumpectomy of breast');
      expect(card.textContent).toContain('2019-05');
      expect(card.textContent).toContain('2019-06-12');
      expect(railOf(card, 0)).toEqual(['M']);
      expect(railOf(card, 1)).toEqual(['D']);
    });

    it('renders the server-owned projection policy verbatim', async () => {
      await golden();

      expect(text('.policy')).toContain(
        'Events are sequenced by their temporal anchor, projected on stated bounds only. A period ' +
          'is anchored by its stated start bound.',
      );
      expect(text('.policy')).toContain('projection policy 1.0.0');
    });

    it('keeps the no-date-arithmetic sentence as presentation copy beside the API policy', async () => {
      await golden();

      expect(text('.static-copy').trim()).toBe('No date arithmetic happens in this view.');
      expect(text('.description').trim()).toBe(
        'Events are sequenced by their temporal anchor, projected on stated bounds only. A period ' +
          'is anchored by its stated start bound.',
      );
    });

    it('states what it does not assert', async () => {
      await golden();

      expect(text('.asserts')).toContain('how a whole period relates to the other events');
      expect(text('.asserts')).toContain('diagnosis onset');
      expect(text('.asserts')).toContain('procedure start');
    });

    it('shows no finding, severity or quality treatment', async () => {
      await golden();

      for (const banned of ['Finding', 'Severity', 'Error', 'Warning', 'OB-']) {
        expect(text('main')).not.toContain(banned);
      }
    });
  });

  describe('order comes from the API', () => {
    it('renders a later anchor first when the API sequenced it first', async () => {
      await golden({ timeline: nonLexicalOrderTimeline() });

      expect(all('.badge').map((badge) => badge.textContent?.trim())).toEqual(['01', '02']);
      expect(cards()[0].textContent).toContain('2020');
      expect(cards()[1].textContent).toContain('2019');
    });

    it('keeps the technical event order inside a group that has no temporal order', async () => {
      await golden({ timeline: orderNotEstablishedTimeline() });

      expect(cards()[0].textContent).toContain('Stage IIA');
      expect(cards()[0].textContent).toContain('2019-03-15');
      expect(cards()[1].textContent).toContain('Malignant neoplasm of breast');
      expect(cards()[1].textContent).toContain('2019-03');
    });
  });

  describe('shared temporal anchor', () => {
    it('names the state and explains it in the frozen words', async () => {
      await golden({ timeline: sharedAnchorTimeline() });

      expect(text('.grouped-label')).toContain('Shared temporal anchor');
      expect(text('.grouped-explanation')).toBe(
        'These events have the same stated temporal anchor. No before/after sequence is asserted ' +
          'within this group.',
      );
    });

    it('leaves the group badge unfilled because it holds no internal sequence', async () => {
      await golden({ timeline: sharedAnchorTimeline() });

      expect(all('.badge')[0].classList).toContain('quiet');
    });

    it('keeps each stated instant representation, including its stated offset', async () => {
      await golden({ timeline: sharedAnchorTimeline() });

      expect(all('.stated').map((value) => value.textContent?.trim())).toEqual([
        '2019-03-14T10:00:00+02:00',
        '2019-03-14T08:00:00+00:00',
      ]);
    });

    it('marks both events as instants without normalising them to one value', async () => {
      await golden({ timeline: sharedAnchorTimeline() });

      expect(railOf(cards()[0])).toEqual(['I']);
      expect(railOf(cards()[1])).toEqual(['I']);
    });

    it('gives no event inside the group a sequence of its own', async () => {
      await golden({ timeline: sharedAnchorTimeline() });

      for (const card of cards()) {
        expect(card.querySelectorAll('.badge')).toHaveLength(0);
      }
    });
  });

  describe('order not established', () => {
    it('names the state and explains it in the frozen words', async () => {
      await golden({ timeline: orderNotEstablishedTimeline() });

      expect(text('.grouped-label')).toContain('Order not established');
      expect(text('.grouped-explanation')).toBe(
        'The stated precision admits no definite ordering, so no claim is made about the order of ' +
          'these events.',
      );
    });

    it('leaves the group badge unfilled', async () => {
      await golden({ timeline: orderNotEstablishedTimeline() });

      expect(all('.badge')[0].classList).toContain('quiet');
    });

    it('uses no error or warning wording', async () => {
      await golden({ timeline: orderNotEstablishedTimeline() });

      for (const banned of ['Error', 'Warning', 'conflict', 'invalid', 'ambiguous']) {
        expect(text('main')).not.toContain(banned);
      }
    });

    it('is not styled as a severity, badge or finding', async () => {
      await golden({ timeline: orderNotEstablishedTimeline() });

      expect(all('ob-severity-badge')).toHaveLength(0);
      expect(all('.swatch')).toHaveLength(0);
    });
  });

  describe('unsequenced events', () => {
    it('renders them off the numbered spine in their own section', async () => {
      await golden({ timeline: unsequencedTimeline() });

      const section = all('ob-unsequenced-section')[0];

      expect(section).toBeTruthy();
      expect(section.textContent).toContain('Unsequenced');
      expect(section.textContent).toContain('no timeline anchor · not sequenced');
      expect(section.querySelectorAll('.badge')).toHaveLength(0);
    });

    it('counts them apart from the anchored events', async () => {
      await golden({ timeline: unsequencedTimeline() });

      expect(text('.panel-head')).toContain('3 events · 1 anchor established · 2 unsequenced');
    });

    it('states that no date was stated rather than fabricating one', async () => {
      await golden({ timeline: unsequencedTimeline() });

      const card = all('ob-unsequenced-section ob-timeline-event')[0];

      expect(card.textContent).toContain('Mastectomy of breast');
      expect(card.textContent).toContain('Date not stated');
      expect(card.classList).toContain('unsequenced');
    });

    it('keeps the stated end bound of an occurrence that has no anchor', async () => {
      await golden({ timeline: unsequencedTimeline() });

      const card = all('ob-unsequenced-section ob-timeline-event')[1];

      expect(card.textContent).toContain('Sentinel lymph node biopsy');
      expect(card.textContent).toContain('Not stated');
      expect(card.textContent).toContain('2019-06-12');
      expect(railOf(card)).toEqual(['D']);
      expect(card.textContent).toContain(
        'No start bound is stated, so this occurrence has no timeline anchor.',
      );
    });
  });

  describe('periods', () => {
    it('marks the bound the API named as the anchor and no other', async () => {
      await golden();

      const card = cards()[2];
      const anchors = [...card.querySelectorAll<HTMLElement>('.anchor')];

      expect(anchors).toHaveLength(1);
      expect(card.textContent).toContain('start');
      expect(card.textContent).toContain('end');
    });

    it('shows an open end as Open with no precision rail', async () => {
      await golden({ timeline: openEndPeriodTimeline() });

      const card = cards()[0];

      expect(card.textContent).toContain('Open');
      expect(card.textContent).toContain('no bound stated');
      expect(card.querySelectorAll('ob-precision-rail')).toHaveLength(1);
      expect(card.textContent).toContain('not a statement that the procedure is ongoing');
    });

    it('never claims both bounds are stated when only one is', async () => {
      await golden({ timeline: openEndPeriodTimeline() });

      expect(cards()[0].textContent).not.toContain('both bounds are shown as stated');
    });

    it('states that the whole period is not related to the other events', async () => {
      await golden();

      expect(cards()[2].textContent).toContain(
        'The relation of the whole period to the other events is not asserted',
      );
    });

    it('reads the anchored bound from the projection rather than from the stated values', async () => {
      await golden({ timeline: zeroLengthPeriodTimeline() });

      const bounds = all('ob-timeline-bound');

      expect(bounds.map((bound) => bound.querySelector('.stated')?.textContent?.trim())).toEqual([
        '2019-05-12',
        '2019-05-12',
      ]);
      expect(railOf(cards()[0], 0)).toEqual(['D']);
      expect(railOf(cards()[0], 1)).toEqual(['D']);
    });

    it('anchors a zero-length period on its start bound alone', async () => {
      await golden({ timeline: zeroLengthPeriodTimeline() });

      const bounds = all('ob-timeline-bound');

      expect(all('.anchor')).toHaveLength(1);
      expect(bounds[0].querySelector('.role')?.textContent?.trim()).toBe('start');
      expect(bounds[0].querySelectorAll('.anchor')).toHaveLength(1);
      expect(bounds[1].querySelector('.role')?.textContent?.trim()).toBe('end');
      expect(bounds[1].querySelectorAll('.anchor')).toHaveLength(0);
    });
  });

  describe('inspect navigation', () => {
    it('sends every event to the existing inspector with its canonical entity id', async () => {
      await golden();

      const hrefs = all('.inspect').map((link) => link.getAttribute('href'));

      expect(hrefs).toEqual([
        `/imports/${importBatchId}?patientId=${entityIds.patient}&entityId=${entityIds.diagnosis}`,
        `/imports/${importBatchId}?patientId=${entityIds.patient}&entityId=${entityIds.staging}`,
        `/imports/${importBatchId}?patientId=${entityIds.patient}&entityId=${entityIds.procedure}`,
      ]);
    });

    it('offers inspect from an unsequenced event too', async () => {
      await golden({ timeline: unsequencedTimeline() });

      const href = all('ob-unsequenced-section .inspect')[1].getAttribute('href');

      expect(href).toContain(`/imports/${importBatchId}`);
      expect(href).toContain(`entityId=${sourceIds.procedure}`);
    });

    it('opens no timeline detail route of its own', async () => {
      await golden();

      for (const href of all('.inspect').map((link) => link.getAttribute('href'))) {
        expect(href).not.toContain('timeline');
      }
    });

    it('names the inspect target for assistive technology', async () => {
      await golden();

      expect(all('.inspect')[0].getAttribute('aria-label')).toBe(
        'Inspect Malignant neoplasm of breast (disorder)',
      );
    });

    it('keeps the inspect action a real keyboard-reachable anchor', async () => {
      await golden();

      for (const link of all('.inspect')) {
        expect(link.tagName).toBe('A');
        expect(link.getAttribute('href')).toBeTruthy();
      }
    });
  });

  describe('semantics', () => {
    it('renders the groups as an ordered list so reading order carries the sequence', async () => {
      await golden();

      const list = (fixture.nativeElement as HTMLElement).querySelector('ol.track');

      expect(list).toBeTruthy();
      expect([...(list?.children ?? [])].map((child) => child.tagName)).toEqual(['LI', 'LI', 'LI']);
    });

    it('writes each group state as text rather than leaving it to the spine', async () => {
      await golden({ timeline: sharedAnchorTimeline() });

      const label = (fixture.nativeElement as HTMLElement).querySelector('.grouped-label');

      expect(label?.tagName).toBe('H3');
      expect(label?.textContent?.trim()).toBe('Shared temporal anchor');
    });

    it('hides the decorative spine from assistive technology', async () => {
      await golden();

      for (const spine of all('.spine')) {
        expect(spine.getAttribute('aria-hidden')).toBe('true');
      }
    });

    it('offers a timeline-to-inspector navigation that keeps the patient', async () => {
      await golden();

      const link = all('ob-stage-flow a')[0];

      expect(link.getAttribute('href')).toBe(
        `/imports/${importBatchId}?patientId=${entityIds.patient}`,
      );
    });

    it('marks the timeline as the current view in the navigation strip', async () => {
      await golden();

      expect(all('ob-stage-flow [aria-current="page"]')[0].textContent).toContain('Timeline');
    });
  });
});
