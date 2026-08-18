import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { ApiFailure } from '../core/async';
import { asNumber } from '../core/api-values';
import { EvidenceRecord } from '../core/evidence';
import { EvidenceMarker } from '../shared/evidence-marker';
import { Pane } from '../shared/pane';
import { PaneEmpty, PaneError, PaneLoading } from '../shared/pane-state';

@Component({
  selector: 'ob-provenance-pane',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Pane, PaneLoading, PaneError, PaneEmpty, EvidenceMarker],
  template: `
    <ob-pane number="04" label="Provenance">
      <span pane-meta>{{ meta() }}</span>

      @if (loadingMessage()) {
        <ob-pane-loading [message]="loadingMessage()!" />
      } @else if (failure()) {
        <ob-pane-error
          [failure]="failure()!"
          [request]="request()"
          consequence="No lineage records name this canonical entity. Because provenance is rebuilt with normalization, the evidence markers cannot be assigned in this state."
          retryLabel="Retry provenance"
          (retry)="retry.emit()"
        />
      } @else if (records().length === 0) {
        <ob-pane-empty title="No canonical entity selected">
          Provenance is scoped to a canonical entity. Choose one above to read its lineage.
        </ob-pane-empty>
      } @else {
        <table>
          <caption class="visually-hidden">
            Lineage records for the selected canonical entity
          </caption>
          <thead>
            <tr>
              <th scope="col"><span class="visually-hidden">Evidence marker</span></th>
              <th scope="col">Scope · field path</th>
              <th scope="col">Source resource</th>
              <th scope="col">Transformation</th>
            </tr>
          </thead>
          <tbody>
            @for (record of records(); track record.lineage.fieldPath ?? '') {
              <tr [class.whole]="!record.lineage.fieldPath">
                <td class="marker">
                  <ob-evidence-marker
                    [marker]="record.marker"
                    [active]="!record.lineage.fieldPath"
                  />
                </td>
                <td>
                  <span class="scope">{{ record.role }}</span>
                  <span class="ob-id">fieldPath: {{ record.lineage.fieldPath ?? 'null' }}</span>
                </td>
                <td>
                  <span class="source"
                    >{{ record.source?.resourceType ?? 'Unknown type' }} ·
                    <span class="ob-mono">{{
                      record.source?.sourceLogicalId ?? record.lineage.sourceResourceId
                    }}</span></span
                  >
                  <span class="ob-id">{{ sourceMeta(record) }}</span>
                </td>
                <td>
                  <span class="ob-mono transformation">{{
                    record.lineage.transformationName
                  }}</span>
                  <span class="ob-id">{{ record.lineage.transformationVersion }}</span>
                </td>
              </tr>
            }
          </tbody>
        </table>

        <p class="transformations ob-mono">{{ transformationSummary() }}</p>

        <p class="explain">
          {{ explanation() }}
        </p>
      }

      <p pane-footnote class="ob-footnote">
        Whole-entity record first, then one record per derived field. Markers are assigned by matching
        each lineage sourceResourceId to the import's source resources — the same rows listed in pane
        01.
      </p>
    </ob-pane>
  `,
  styles: `
    table {
      width: 100%;
      table-layout: fixed;
      border-collapse: collapse;
      font-size: 12px;
    }

    caption {
      text-align: left;
    }

    th {
      padding: 6px 10px;
      background: var(--ob-canvas);
      border-bottom: 1px solid var(--ob-rule);
      font-size: 9.5px;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: var(--ob-faint);
      font-weight: 400;
      text-align: left;
    }

    td {
      padding: 9px 10px;
      border-bottom: 1px solid var(--ob-rule-soft);
      vertical-align: top;
    }

    td.marker {
      width: 26px;
      padding-right: 0;
    }

    tr.whole {
      background: var(--ob-accent-wash);
    }

    tr.whole .scope {
      font-weight: 600;
    }

    .scope,
    .source,
    .transformation {
      display: block;
      font-size: 12.5px;
    }

    .transformation {
      font-size: 10.5px;
    }

    .ob-id {
      display: block;
      color: var(--ob-absent);
      overflow-wrap: anywhere;
    }

    .scope,
    .source,
    .transformation {
      overflow-wrap: anywhere;
    }

    .transformations {
      display: none;
      margin: 0;
      padding: 7px 12px;
      border-bottom: 1px solid var(--ob-rule-soft);
      font-size: 10.5px;
      color: var(--ob-muted);
      overflow-wrap: anywhere;
    }

    .explain {
      margin: 0;
      padding: 10px 12px;
      font-size: 11.5px;
      color: var(--ob-ink-2);
      line-height: 1.5;
      border-bottom: 1px solid var(--ob-rule);
    }

    .visually-hidden {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
      clip-path: inset(50%);
      white-space: nowrap;
    }

    @media (max-width: 900px) {
      th:nth-child(4),
      td:nth-child(4) {
        display: none;
      }

      .transformations {
        display: block;
      }
    }
  `,
})
export class ProvenancePane {
  readonly records = input.required<readonly EvidenceRecord[]>();
  readonly entityLabel = input('');
  readonly loadingMessage = input<string | null>(null);
  readonly failure = input<ApiFailure | null>(null);
  readonly request = input('');

  readonly retry = output<void>();

  protected readonly meta = computed(() => {
    const count = this.records().length;

    return `${count} lineage ${count === 1 ? 'record' : 'records'}${this.entityLabel() ? ' · ' + this.entityLabel() : ''}`;
  });

  protected readonly transformationSummary = computed(() => [
    ...new Set(
      this.records().map(
        (record) =>
          `${record.lineage.transformationName} ${record.lineage.transformationVersion}`,
      ),
    ),
  ].join(' · '));

  protected readonly explanation = computed(() => {
    const records = this.records();
    const sources = new Set(records.map((record) => record.lineage.sourceResourceId)).size;

    return `${sources} source ${sources === 1 ? 'resource' : 'resources'}, ${records.length} lineage ${records.length === 1 ? 'record' : 'records'}, one canonical entity.`;
  });

  protected sourceMeta(record: EvidenceRecord): string {
    const source = record.source;

    if (!source) {
      return 'not in this import';
    }

    const hash = source.contentHash ? `${source.contentHash.slice(0, 4)}…${source.contentHash.slice(-4)} · ` : '';

    return `${hash}entry ${asNumber(source.entryIndex)}`;
  }
}
