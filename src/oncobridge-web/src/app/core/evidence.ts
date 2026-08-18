import { CancerStagingResponse, LineageResponse, SourceResourceResponse } from '../api';
import { asNumber, compareOrdinal } from './api-values';

export const wholeEntityRole = 'Whole entity';

export interface EvidenceRecord {
  readonly marker: string;
  readonly role: string;
  readonly lineage: LineageResponse;
  readonly source: SourceResourceResponse | null;
}

export function evidenceRecordsOf(
  provenance: readonly LineageResponse[],
  sourceResources: readonly SourceResourceResponse[],
  staging: CancerStagingResponse | null,
): readonly EvidenceRecord[] {
  const sources = new Map(sourceResources.map((source) => [source.id, source]));
  const ordered = orderForMarkers(provenance, sources);
  const markers = markersFor(ordered);

  return ordered.map((lineage) => ({
    marker: markers.get(lineage.sourceResourceId) ?? '',
    role: roleOf(lineage, staging),
    lineage,
    source: sources.get(lineage.sourceResourceId) ?? null,
  }));
}

export function contributingSourcesOf(
  records: readonly EvidenceRecord[],
): readonly EvidenceRecord[] {
  const seen = new Set<string>();

  return records.filter((record) => {
    if (seen.has(record.lineage.sourceResourceId)) {
      return false;
    }

    seen.add(record.lineage.sourceResourceId);

    return true;
  });
}

export function evidenceSourceIdsOf(records: readonly EvidenceRecord[]): ReadonlySet<string> {
  return new Set(records.map((record) => record.lineage.sourceResourceId));
}

export function markerOfSource(
  records: readonly EvidenceRecord[],
  sourceResourceId: string,
): string | null {
  return (
    records.find((record) => record.lineage.sourceResourceId === sourceResourceId)?.marker ?? null
  );
}

export function markerOfFieldPath(
  records: readonly EvidenceRecord[],
  fieldPath: string | null,
): string | null {
  const match = records.find((record) => (record.lineage.fieldPath ?? null) === fieldPath);

  return match?.marker ?? null;
}

function roleOf(lineage: LineageResponse, staging: CancerStagingResponse | null): string {
  if (!lineage.fieldPath) {
    return wholeEntityRole;
  }

  const axis = staging?.categories.find(
    (category) => category.sourceResourceId === lineage.sourceResourceId,
  )?.axis;

  return axis ? `Category ${axis}` : lineage.fieldPath;
}

function orderForMarkers(
  provenance: readonly LineageResponse[],
  sources: ReadonlyMap<string, SourceResourceResponse>,
): readonly LineageResponse[] {
  const wholeEntity = provenance.filter((lineage) => !lineage.fieldPath);
  const fields = [...provenance.filter((lineage) => !!lineage.fieldPath)].sort((left, right) => {
    const byEntry = entryIndexOf(left, sources) - entryIndexOf(right, sources);

    return byEntry !== 0 ? byEntry : compareOrdinal(left.sourceResourceId, right.sourceResourceId);
  });

  return [...wholeEntity, ...fields];
}

function entryIndexOf(
  lineage: LineageResponse,
  sources: ReadonlyMap<string, SourceResourceResponse>,
): number {
  const source = sources.get(lineage.sourceResourceId);

  return source ? asNumber(source.entryIndex) : Number.MAX_SAFE_INTEGER;
}

function markersFor(ordered: readonly LineageResponse[]): ReadonlyMap<string, string> {
  const markers = new Map<string, string>();

  for (const lineage of ordered) {
    if (!markers.has(lineage.sourceResourceId)) {
      markers.set(lineage.sourceResourceId, markerAt(markers.size));
    }
  }

  return markers;
}

function markerAt(position: number): string {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
  let marker = '';
  let remaining = position;

  do {
    marker = alphabet[remaining % alphabet.length] + marker;
    remaining = Math.floor(remaining / alphabet.length) - 1;
  } while (remaining >= 0);

  return marker;
}
