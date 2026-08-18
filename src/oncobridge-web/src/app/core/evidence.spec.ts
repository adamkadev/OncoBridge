import { describe, expect, it } from 'vitest';

import {
  contributingSourcesOf,
  evidenceRecordsOf,
  markerOfFieldPath,
  markerOfSource,
} from './evidence';
import {
  importResponse,
  patientRecordResponse,
  sourceIds,
  stagingProvenance,
} from '../testing/fixtures';

const staging = patientRecordResponse().cancerStagings[0];
const sources = importResponse().sourceResources;

describe('evidence records', () => {
  it('assigns A to the whole-entity record and then orders by source entry index', () => {
    const records = evidenceRecordsOf(stagingProvenance(), sources, staging);

    expect(records.map((record) => [record.marker, record.source?.sourceLogicalId])).toEqual([
      ['A', 'staging-group-001'],
      ['B', 'staging-t-001'],
      ['C', 'staging-n-001'],
      ['D', 'staging-m-001'],
    ]);
  });

  it('does not simply mirror the order the provenance endpoint returned', () => {
    const provenance = stagingProvenance();

    expect(provenance.map((record) => record.fieldPath)).toEqual([
      null,
      'DistantMetastases',
      'PrimaryTumour',
      'RegionalNodes',
    ]);

    const records = evidenceRecordsOf(provenance, sources, staging);

    expect(records.map((record) => record.lineage.fieldPath)).toEqual([
      null,
      'PrimaryTumour',
      'RegionalNodes',
      'DistantMetastases',
    ]);
  });

  it('derives category roles from the canonical staging axes, not from the source JSON', () => {
    const records = evidenceRecordsOf(stagingProvenance(), sources, staging);

    expect(records.map((record) => record.role)).toEqual([
      'Whole entity',
      'Category T',
      'Category N',
      'Category M',
    ]);
  });

  it('falls back to the field path when no canonical axis names the source', () => {
    const records = evidenceRecordsOf(stagingProvenance(), sources, null);

    expect(records.map((record) => record.role)).toEqual([
      'Whole entity',
      'PrimaryTumour',
      'RegionalNodes',
      'DistantMetastases',
    ]);
  });

  it('is stable when the provenance order changes', () => {
    const reversed = [...stagingProvenance()].reverse();

    const records = evidenceRecordsOf(reversed, sources, staging);

    expect(records.map((record) => [record.marker, record.role])).toEqual([
      ['A', 'Whole entity'],
      ['B', 'Category T'],
      ['C', 'Category N'],
      ['D', 'Category M'],
    ]);
  });

  it('maps markers back to sources and field paths', () => {
    const records = evidenceRecordsOf(stagingProvenance(), sources, staging);

    expect(markerOfSource(records, sourceIds.stageGroup)).toBe('A');
    expect(markerOfSource(records, sourceIds.primaryTumour)).toBe('B');
    expect(markerOfSource(records, sourceIds.condition)).toBeNull();
    expect(markerOfFieldPath(records, null)).toBe('A');
    expect(markerOfFieldPath(records, 'RegionalNodes')).toBe('C');
  });

  it('lists one contributing source per distinct source resource', () => {
    const records = evidenceRecordsOf(stagingProvenance(), sources, staging);

    expect(contributingSourcesOf(records)).toHaveLength(4);
  });

  it('keeps a lineage record whose source is not part of the import', () => {
    const records = evidenceRecordsOf(stagingProvenance(), [], staging);

    expect(records).toHaveLength(4);
    expect(records.every((record) => record.source === null)).toBe(true);
  });
});
