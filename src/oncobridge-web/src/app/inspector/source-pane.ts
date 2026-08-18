import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { ApiFailure } from '../core/async';
import { asNumber } from '../core/api-values';
import { EvidenceRecord } from '../core/evidence';
import { EvidenceMarker } from '../shared/evidence-marker';
import { Pane } from '../shared/pane';
import { PaneEmpty, PaneError, PaneLoading } from '../shared/pane-state';

@Component({
  selector: 'ob-source-pane',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Pane, PaneLoading, PaneError, PaneEmpty, EvidenceMarker],
  template: `
    <ob-pane number="01" label="Source">
      <span pane-meta>{{ meta() }}</span>

      @if (loadingMessage()) {
        <ob-pane-loading [message]="loadingMessage()!" />
      } @else if (failure()) {
        <ob-pane-error
          [failure]="failure()!"
          [request]="request()"
          consequence="Source evidence for this entity is unavailable."
          retryLabel="Retry source"
          (retry)="retry.emit()"
        />
      } @else if (evidence().length === 0) {
        <ob-pane-empty title="No contributing source resources">
          No lineage record names a source resource for the selected canonical entity.
        </ob-pane-empty>
      } @else {
        <ul class="rows">
          @for (record of evidence(); track record.lineage.sourceResourceId) {
            <li>
              <button
                type="button"
                class="row"
                [class.selected]="record.lineage.sourceResourceId === selectedSourceId()"
                [attr.aria-current]="record.lineage.sourceResourceId === selectedSourceId()"
                (click)="sourceSelected.emit(record.lineage.sourceResourceId)"
              >
                <ob-evidence-marker
                  [marker]="record.marker"
                  [active]="record.lineage.sourceResourceId === selectedSourceId()"
                />
                <span class="type">{{ record.source?.resourceType ?? 'Unknown type' }}</span>
                <span class="role" [class.whole]="record.role === 'Whole entity'">{{
                  record.role
                }}</span>
                <span class="ob-mono logical">{{
                  record.source?.sourceLogicalId ?? record.lineage.sourceResourceId
                }}</span>
                <span class="ob-mono entry">{{ entryLabel(record) }}</span>
              </button>
            </li>
          }
        </ul>

        @if (selected(); as current) {
          <div class="json-head">
            <span class="json-title">
              <ob-evidence-marker [marker]="current.marker" [active]="true" />
              <span class="ob-micro">Stored resource JSON</span>
            </span>
            <span class="ob-id">{{
              current.source?.fullUrl ?? current.lineage.sourceResourceId
            }}</span>
          </div>

          @if (json(); as text) {
            <div class="json">
              <pre>{{ text }}</pre>
            </div>
          } @else {
            <div class="json-absent">
              <p class="absent-title">No parsed JSON stored for this resource</p>
              <p class="absent-body">
                The stored resource JSON is a derived, queryable representation and may be absent.
                The received bytes remain intact in the import batch payload and its SHA-256 is
                unchanged.
              </p>
            </div>
          }
        }
      }

      <p pane-footnote class="ob-footnote">
        The stored resource JSON is the queryable representation of this entry. The byte-exact
        evidence is the import batch raw payload, digested as the payload SHA-256 in the header.
      </p>
    </ob-pane>
  `,
  styles: `
    .rows {
      margin: 0;
      padding: 0;
      list-style: none;
      min-width: 0;
    }

    .row {
      display: flex;
      align-items: center;
      gap: 10px;
      width: 100%;
      padding: 7px 12px;
      border: 0;
      border-bottom: 1px solid var(--ob-rule-soft);
      border-left: 3px solid transparent;
      background: transparent;
      font-family: var(--ob-sans);
      font-size: 12.5px;
      color: var(--ob-ink);
      text-align: left;
      cursor: pointer;
      min-height: 34px;
      min-width: 0;
    }

    .row:hover {
      background: var(--ob-surface-2);
    }

    .row.selected {
      background: var(--ob-accent-wash);
      border-left-color: var(--ob-accent);
      font-weight: 600;
    }

    .type {
      min-width: 82px;
    }

    .role {
      font-size: 10.5px;
      letter-spacing: 0.05em;
      text-transform: uppercase;
      color: var(--ob-muted);
      border: 1px solid var(--ob-border-2);
      padding: 1px 5px;
      border-radius: 2px;
      min-width: 92px;
      text-align: center;
      font-weight: 400;
    }

    .role.whole {
      color: var(--ob-accent-ink);
      border-color: color-mix(in oklab, var(--ob-accent) 40%, transparent);
      background: var(--ob-surface);
    }

    .logical {
      font-size: 11.5px;
      color: var(--ob-ink-2);
      font-weight: 400;
      min-width: 0;
      overflow-wrap: anywhere;
    }

    .entry {
      margin-left: auto;
      font-size: 11px;
      color: var(--ob-faint);
      font-weight: 400;
      white-space: nowrap;
    }

    .json-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      flex-wrap: wrap;
      min-width: 0;
      padding: 6px 12px;
      background: var(--ob-surface-2);
      border-bottom: 1px solid var(--ob-rule);
      border-top: 1px solid var(--ob-rule);
    }

    .json-title {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .json-title .ob-micro {
      color: var(--ob-ink-2);
      letter-spacing: 0.06em;
      font-size: 11px;
    }

    .json {
      overflow: auto;
      min-width: 0;
      max-height: 300px;
      padding: 10px 12px;
      background: var(--ob-code-surface);
    }

    pre {
      font-family: var(--ob-mono);
      font-size: 11.5px;
      line-height: 1.6;
      color: var(--ob-code-ink);
      white-space: pre;
    }

    .json-absent {
      margin: 12px;
      border: 1px dashed var(--ob-border);
      background: #fbfcfc;
      border-radius: 2px;
      padding: 16px 14px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .json-absent p {
      margin: 0;
    }

    .absent-title {
      font-size: 12.5px;
      font-weight: 600;
    }

    .absent-body {
      font-size: 11.5px;
      color: var(--ob-muted);
      line-height: 1.5;
    }

    .json-head .ob-id {
      min-width: 0;
      overflow-wrap: anywhere;
    }

    @media (max-width: 900px) {
      .row {
        flex-wrap: wrap;
        min-height: 44px;
      }

      .role,
      .type {
        min-width: 0;
      }

      .entry {
        margin-left: 0;
        white-space: normal;
      }
    }
  `,
})
export class SourcePane {
  readonly evidence = input.required<readonly EvidenceRecord[]>();
  readonly totalResources = input<number | null>(null);
  readonly selectedSourceId = input<string | null>(null);
  readonly loadingMessage = input<string | null>(null);
  readonly failure = input<ApiFailure | null>(null);
  readonly request = input('');

  readonly sourceSelected = output<string>();
  readonly retry = output<void>();

  protected readonly selected = computed(
    () =>
      this.evidence().find(
        (record) => record.lineage.sourceResourceId === this.selectedSourceId(),
      ) ?? null,
  );

  protected readonly json = computed(() => {
    const value = this.selected()?.source?.resourceJson;

    return value === null || value === undefined ? null : JSON.stringify(value, null, 2);
  });

  protected readonly meta = computed(() => {
    const total = this.totalResources();
    const named = this.evidence().length;

    return total === null
      ? `${named} resources named by this entity's lineage`
      : `${named} of ${total} resources named by this entity's lineage`;
  });

  protected entryLabel(record: EvidenceRecord): string {
    const source = record.source;

    if (!source) {
      return 'not in this import';
    }

    const hash = source.contentHash ? ` · ${source.contentHash.slice(0, 6)}…` : '';

    return `entry ${asNumber(source.entryIndex)}${hash}`;
  }
}
