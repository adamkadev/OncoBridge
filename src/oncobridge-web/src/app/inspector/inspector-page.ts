import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { Router } from '@angular/router';

import { asNumber } from '../core/api-values';
import { failureOf, valueOf } from '../core/async';
import {
  EntityInstance,
  defaultEntityIdOf,
  entityInstancesOf,
  stagingOf,
} from '../core/entities';
import { contributingSourcesOf, evidenceRecordsOf } from '../core/evidence';
import { findingViewsOf, relatedCheckIdsOf } from '../core/findings';
import { InspectorDataService } from '../core/inspector-data';
import { StageFlow } from '../shared/stage-flow';
import { StandardsNote } from '../shared/standards-note';
import { EntitySelector } from './entity-selector';
import { InspectorHeader } from './inspector-header';
import { NormalizedPane } from './normalized-pane';
import { ProvenancePane } from './provenance-pane';
import { QualityPane } from './quality-pane';
import { SourcePane } from './source-pane';

@Component({
  selector: 'ob-inspector-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    InspectorHeader,
    StandardsNote,
    EntitySelector,
    StageFlow,
    SourcePane,
    NormalizedPane,
    QualityPane,
    ProvenancePane,
  ],
  template: `
    <div class="workbench">
      <header>
        <ob-inspector-header [importBatchId]="importBatchId()" [value]="import()" />
        <ob-standards-note />
      </header>

      <main>
        @if (importFailure(); as failure) {
          <section class="import-failure">
            <p class="badge-row">
              <span class="badge"><span class="swatch" aria-hidden="true"></span>Error</span>
              <span class="ob-mono request"
                >GET /api/v1/imports/{{ importBatchId() }}{{
                  failure.status ? ' · ' + failure.status : ''
                }}</span
              >
            </p>
            <h2>{{ failure.title }}</h2>
            <p class="detail">
              {{
                failure.detail ??
                  'Nothing downstream can be shown, because every other pane is scoped to an import.'
              }}
            </p>
            <p class="actions">
              <button type="button" class="ob-button" (click)="retryImport()">Retry</button>
              <a class="ob-button-quiet" href="/">Back to import</a>
            </p>
          </section>
        } @else {
          @if (patientIds().length > 1) {
            <section class="patients">
              <span class="ob-micro">Patient</span>
              <nav aria-label="Canonical patients in this import">
                <ul>
                  @for (patientId of patientIds(); track patientId) {
                    <li>
                      <button
                        type="button"
                        class="patient"
                        [class.selected]="patientId === selectedPatientId()"
                        [attr.aria-current]="patientId === selectedPatientId() ? 'true' : null"
                        (click)="selectPatient(patientId)"
                      >
                        <span class="ob-mono">{{ patientId }}</span>
                      </button>
                    </li>
                  }
                </ul>
              </nav>
            </section>
          }

          @if (entityInstances().length > 0) {
            <ob-entity-selector
              [instances]="entityInstances()"
              [selectedId]="selectedEntityId()"
              [summary]="entitySummary()"
              (entitySelected)="selectEntity($event)"
            />
          }

          <ob-stage-flow>{{ flowNote() }}</ob-stage-flow>

          <div class="panes">
            <ob-source-pane
              [evidence]="contributingSources()"
              [totalResources]="totalResources()"
              [selectedSourceId]="selectedSourceId()"
              [loadingMessage]="sourceLoading()"
              [failure]="provenanceFailure()"
              [request]="provenanceRequest()"
              (sourceSelected)="selectSource($event)"
              (retry)="retryProvenance()"
            />

            <ob-normalized-pane
              [record]="record()"
              [entity]="selectedEntity()"
              [evidence]="evidence()"
              [relatedCheckIds]="relatedCheckIds()"
              [loadingMessage]="recordLoading()"
              [failure]="recordFailure()"
              [request]="recordRequest()"
              [hasPatient]="patientIds().length > 0"
              (retry)="retryRecord()"
            />

            <ob-quality-pane
              [views]="findingViews()"
              [loadingMessage]="findingsLoading()"
              [failure]="findingsFailure()"
              [request]="'GET /api/v1/imports/' + importBatchId() + '/findings'"
              (retry)="retryFindings()"
            />

            <ob-provenance-pane
              [records]="evidence()"
              [entityLabel]="provenanceLabel()"
              [loadingMessage]="provenanceLoading()"
              [failure]="provenanceFailure()"
              [request]="provenanceRequest()"
              (retry)="retryProvenance()"
            />
          </div>
        }
      </main>
    </div>
  `,
  styles: `
    .workbench {
      background: var(--ob-canvas);
      border: 1px solid var(--ob-border);
      min-height: 100vh;
    }

    .panes {
      display: grid;
      grid-template-columns: minmax(0, 1.05fr) minmax(0, 1fr);
      gap: 12px;
      padding: 12px 16px 16px;
      align-items: start;
    }

    @media (max-width: 1100px) {
      .panes {
        grid-template-columns: minmax(0, 1fr);
      }
    }

    @media (max-width: 640px) {
      .panes {
        padding: 10px 12px 14px;
      }
    }

    .patients {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 16px;
      background: var(--ob-surface);
      border-bottom: 1px solid var(--ob-border);
      flex-wrap: wrap;
    }

    .patients ul {
      margin: 0;
      padding: 0;
      list-style: none;
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    .patient {
      border: 1px solid var(--ob-border-2);
      background: var(--ob-surface-2);
      border-radius: 3px;
      padding: 7px 10px;
      cursor: pointer;
      min-height: 34px;
      font-size: 11.5px;
    }

    .patient.selected {
      border-color: var(--ob-accent);
      background: var(--ob-accent-wash);
    }

    .import-failure {
      margin: 16px;
      padding: 16px;
      background: var(--ob-surface);
      border: 1px solid var(--ob-border);
      border-radius: 3px;
      display: flex;
      flex-direction: column;
      gap: 10px;
      align-items: flex-start;
    }

    .import-failure p,
    .import-failure h2 {
      margin: 0;
    }

    .import-failure h2 {
      font-size: 15px;
      font-weight: 600;
    }

    .badge-row {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
    }

    .badge {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      font-size: 10px;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      font-weight: 700;
      color: var(--ob-error-ink);
      background: var(--ob-error-wash);
      border: 1px solid color-mix(in oklab, var(--ob-error) 35%, transparent);
      padding: 2px 6px;
      border-radius: 2px;
    }

    .swatch {
      width: 7px;
      height: 7px;
      background: var(--ob-error);
      display: inline-block;
    }

    .request {
      font-size: 11px;
      color: var(--ob-muted);
    }

    .detail {
      font-size: 12.5px;
      color: var(--ob-ink-2);
      line-height: 1.55;
      text-wrap: pretty;
    }

    .actions {
      display: flex;
      gap: 9px;
      align-items: center;
      flex-wrap: wrap;
    }

    .actions a {
      border-bottom: 0;
      text-decoration: none;
      display: inline-flex;
      align-items: center;
    }
  `,
})
export class InspectorPage {
  private readonly data = inject(InspectorDataService);
  private readonly router = inject(Router);

  readonly importBatchId = input.required<string>();
  readonly entityId = input<string | undefined>(undefined);
  readonly sourceResourceId = input<string | undefined>(undefined);
  readonly patientId = input<string | undefined>(undefined);

  protected readonly import = computed(() => valueOf(this.data.import()));
  protected readonly record = computed(() => valueOf(this.data.record()));

  protected readonly importFailure = computed(() => failureOf(this.data.import()));
  protected readonly recordFailure = computed(() => failureOf(this.data.record()));
  protected readonly findingsFailure = computed(() => failureOf(this.data.findings()));
  protected readonly provenanceFailure = computed(() => failureOf(this.data.provenance()));

  protected readonly patientIds = computed<readonly string[]>(() => this.import()?.patientIds ?? []);

  protected readonly selectedPatientId = computed(() => {
    const ids = this.patientIds();
    const requested = this.patientId();

    if (requested && ids.includes(requested)) {
      return requested;
    }

    return ids.length > 0 ? ids[0] : null;
  });

  protected readonly entityInstances = computed<readonly EntityInstance[]>(() => {
    const record = this.record();

    return record ? entityInstancesOf(record) : [];
  });

  protected readonly selectedEntityId = computed(() => {
    const instances = this.entityInstances();
    const requested = this.entityId();

    if (requested && instances.some((instance) => instance.id === requested)) {
      return requested;
    }

    return defaultEntityIdOf(instances);
  });

  protected readonly selectedEntity = computed<EntityInstance | null>(
    () =>
      this.entityInstances().find((instance) => instance.id === this.selectedEntityId()) ?? null,
  );

  protected readonly evidence = computed(() =>
    evidenceRecordsOf(
      valueOf(this.data.provenance()) ?? [],
      this.import()?.sourceResources ?? [],
      stagingOf(this.record(), this.selectedEntity()),
    ),
  );

  protected readonly contributingSources = computed(() => contributingSourcesOf(this.evidence()));

  protected readonly selectedSourceId = computed(() => {
    const sources = this.contributingSources();
    const requested = this.sourceResourceId();

    if (requested && sources.some((record) => record.lineage.sourceResourceId === requested)) {
      return requested;
    }

    return sources.length > 0 ? sources[0].lineage.sourceResourceId : null;
  });

  protected readonly findingViews = computed(() =>
    findingViewsOf(
      valueOf(this.data.findings()) ?? [],
      this.import()?.sourceResources ?? [],
      this.evidence(),
      this.selectedEntityId(),
    ),
  );

  protected readonly relatedCheckIds = computed(() => relatedCheckIdsOf(this.findingViews()));

  protected readonly totalResources = computed(() => {
    const batch = this.import();

    return batch ? asNumber(batch.entryCount) : null;
  });

  protected readonly recordLoading = computed(() =>
    this.data.record().kind === 'loading' ? 'Loading normalized record…' : null,
  );

  protected readonly findingsLoading = computed(() =>
    this.data.findings().kind === 'loading' ? 'Loading findings…' : null,
  );

  protected readonly provenanceLoading = computed(() =>
    this.data.provenance().kind === 'loading' ? 'Loading provenance…' : null,
  );

  protected readonly sourceLoading = computed(() =>
    this.data.import().kind === 'loading' || this.data.provenance().kind === 'loading'
      ? 'Loading source…'
      : null,
  );

  protected readonly provenanceRequest = computed(
    () => `GET /api/v1/domain/${this.selectedEntityId() ?? ''}/provenance`,
  );

  protected readonly recordRequest = computed(
    () => `GET /api/v1/patients/${this.selectedPatientId() ?? ''}/record`,
  );

  protected readonly entitySummary = computed(() => {
    const instances = this.entityInstances();
    const patients = this.patientIds().length;

    return `${instances.length} ${instances.length === 1 ? 'instance' : 'instances'} · ${patients} ${patients === 1 ? 'patient' : 'patients'}`;
  });

  protected readonly provenanceLabel = computed(() => {
    const entity = this.selectedEntity();

    return entity ? `${entity.kind} ${entity.id.slice(0, 8)}…` : '';
  });

  protected readonly flowNote = computed(() => {
    const sources = this.contributingSources().length;
    const records = this.evidence().length;

    if (records === 0) {
      return 'Evidence markers are assigned from the selected entity’s lineage.';
    }

    return `Evidence A–${this.contributingSources()[sources - 1]?.marker ?? 'A'} — ${sources} source ${sources === 1 ? 'resource' : 'resources'}, ${records} lineage ${records === 1 ? 'record' : 'records'}, one canonical entity`;
  });

  constructor() {
    effect(() => {
      const importBatchId = this.importBatchId();

      this.data.loadImport(importBatchId);
      this.data.loadFindings(importBatchId);
    });

    effect(() => {
      const patientId = this.selectedPatientId();

      if (patientId) {
        this.data.loadRecord(patientId);
      } else if (this.data.import().kind === 'loaded') {
        this.data.clearRecord();
      }
    });

    effect(() => {
      const entityId = this.selectedEntityId();

      if (entityId) {
        this.data.loadProvenance(entityId);
      } else if (this.data.record().kind !== 'loading') {
        this.data.clearProvenance();
      }
    });
  }

  protected selectEntity(entityId: string): void {
    void this.router.navigate([], {
      queryParams: { entityId, sourceResourceId: null },
      queryParamsHandling: 'merge',
    });
  }

  protected selectSource(sourceResourceId: string): void {
    void this.router.navigate([], {
      queryParams: { sourceResourceId },
      queryParamsHandling: 'merge',
    });
  }

  protected selectPatient(patientId: string): void {
    void this.router.navigate([], {
      queryParams: { patientId, entityId: null, sourceResourceId: null },
      queryParamsHandling: 'merge',
    });
  }

  protected retryImport(): void {
    this.data.loadImport(this.importBatchId(), true);
    this.data.loadFindings(this.importBatchId(), true);
  }

  protected retryFindings(): void {
    this.data.loadFindings(this.importBatchId(), true);
  }

  protected retryRecord(): void {
    const patientId = this.selectedPatientId();

    if (patientId) {
      this.data.loadRecord(patientId, true);
    }
  }

  protected retryProvenance(): void {
    const entityId = this.selectedEntityId();

    if (entityId) {
      this.data.loadProvenance(entityId, true);
    }
  }
}
