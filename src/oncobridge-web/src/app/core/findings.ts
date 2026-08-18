import { FindingResponse, SourceResourceResponse } from '../api';
import { EvidenceRecord, evidenceSourceIdsOf, markerOfSource } from './evidence';

export interface FindingView {
  readonly finding: FindingResponse;
  readonly targetSource: SourceResourceResponse | null;
  readonly marker: string | null;
  readonly relatedToSelection: boolean;
}

export function findingViewsOf(
  findings: readonly FindingResponse[],
  sourceResources: readonly SourceResourceResponse[],
  evidence: readonly EvidenceRecord[],
  selectedEntityId: string | null,
): readonly FindingView[] {
  const sources = new Map(sourceResources.map((source) => [source.id, source]));
  const evidenceSourceIds = evidenceSourceIdsOf(evidence);

  return findings.map((finding) => ({
    finding,
    targetSource: sources.get(finding.target.id) ?? null,
    marker: markerOfSource(evidence, finding.target.id),
    relatedToSelection:
      finding.target.id === selectedEntityId || evidenceSourceIds.has(finding.target.id),
  }));
}

export function relatedCheckIdsOf(views: readonly FindingView[]): readonly string[] {
  return [
    ...new Set(
      views.filter((view) => view.relatedToSelection).map((view) => view.finding.checkId),
    ),
  ];
}
