import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { Router } from '@angular/router';

import { failureOf, valueOf } from '../core/async';
import { InspectorDataService } from '../core/inspector-data';
import { anchorChainOf, anchoredEventsOf } from '../core/timeline';
import { InspectorHeader } from '../inspector/inspector-header';
import { PaneEmpty, PaneError, PaneLoading } from '../shared/pane-state';
import { PrecisionRail } from '../shared/precision-rail';
import { StageFlow } from '../shared/stage-flow';
import { TimelineGroup } from './timeline-group';
import { UnsequencedSection } from './unsequenced-section';

const PRECISION_EXAMPLES: readonly { readonly name: string; readonly example: string }[] = [
  { name: 'Year', example: '2019' },
  { name: 'Month', example: '2019-03' },
  { name: 'Day', example: '2019-04-02' },
  { name: 'Instant', example: '2019-03-14T10:00:00+02:00' },
];

@Component({
  selector: 'ob-timeline-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    InspectorHeader,
    StageFlow,
    TimelineGroup,
    UnsequencedSection,
    PrecisionRail,
    PaneLoading,
    PaneError,
    PaneEmpty,
  ],
  template: `
    <div class="workbench">
      <header>
        <ob-inspector-header [importBatchId]="importBatchId()" [value]="import()" />
      </header>

      <ob-stage-flow
        [inspectorCommands]="inspectorCommands()"
        [queryParams]="inspectorQueryParams()"
      >
        {{ trackNote() }}
      </ob-stage-flow>

      <p class="policy">
        <span class="description">{{
          policy()?.description ?? 'Events are sequenced by the projection the API returns.'
        }}</span>
        <span class="static-copy">No date arithmetic happens in this view.</span>
        @if (policy(); as stated) {
          <span class="version ob-mono">projection policy {{ stated.version }}</span>
        }
      </p>

      <main>
        @if (importFailure(); as failure) {
          <section class="panel">
            <ob-pane-error
              [failure]="failure"
              [request]="'GET /api/v1/imports/' + importBatchId()"
              consequence="No timeline can be projected, because the canonical patients of this import are unknown."
              retryLabel="Retry import"
              (retry)="retryImport()"
            />
          </section>
        } @else {
          @if (patientIds().length > 1) {
            <section class="patients">
              <span class="ob-micro">Patient</span>
              <nav aria-label="Canonical patients in this import">
                <ul>
                  @for (candidate of patientIds(); track candidate) {
                    <li>
                      <button
                        type="button"
                        class="patient"
                        [class.selected]="candidate === selectedPatientId()"
                        [attr.aria-current]="candidate === selectedPatientId() ? 'true' : null"
                        (click)="selectPatient(candidate)"
                      >
                        <span class="ob-mono">{{ candidate }}</span>
                      </button>
                    </li>
                  }
                </ul>
              </nav>
            </section>
          }

          <div class="columns">
            <section class="panel">
              <div class="panel-head">
                <h2 class="ob-pane-label">Canonical chronology</h2>
                <span class="ob-meta">{{ summary() }}</span>
              </div>

              @if (patientIds().length === 0 && import()) {
                <ob-pane-empty
                  title="No canonical patient was produced"
                  caveat="Normalization is derived and re-runnable — a later mapper version may produce one."
                >
                  This import stored its source resources, and normalization derived no Patient from
                  them. There is no patient to project a timeline for.
                </ob-pane-empty>
              } @else if (loadingMessage(); as message) {
                <ob-pane-loading [message]="message" />
              } @else if (timelineFailure(); as failure) {
                <ob-pane-error
                  [failure]="failure"
                  [request]="timelineRequest()"
                  consequence="The timeline could not be projected. This is a failed request, not an empty timeline."
                  retryLabel="Retry timeline"
                  (retry)="retryTimeline()"
                />
              } @else if (timeline(); as projected) {
                @if (isEmpty()) {
                  <ob-pane-empty title="No timeline events">
                    No timeline events were produced from the canonical concepts exposed in
                    OncoBridge V1.
                  </ob-pane-empty>
                } @else {
                  <ol class="track">
                    @for (group of projected.groups; track group.sequence) {
                      <li
                        ob-timeline-group
                        [group]="group"
                        [importBatchId]="importBatchId()"
                        [patientId]="selectedPatientId() ?? ''"
                        [first]="$first"
                        [last]="$last"
                      ></li>
                    }
                  </ol>

                  @if (projected.unsequencedEvents.length > 0) {
                    <ob-unsequenced-section
                      [events]="projected.unsequencedEvents"
                      [importBatchId]="importBatchId()"
                      [patientId]="selectedPatientId() ?? ''"
                    />
                  }
                }
              }
            </section>

            <aside>
              <section class="panel">
                <div class="panel-head">
                  <h2 class="ob-pane-label">What this view asserts</h2>
                </div>
                <dl class="asserts">
                  <dt class="ob-micro">Anchors</dt>
                  <dd>
                    @if (anchorChain().length > 0) {
                      @for (phrase of anchorChain(); track $index) {
                        @if (!$first) {
                          <span class="arrow" aria-hidden="true">→</span>
                        }
                        <span>{{ phrase }}</span>
                      }
                    } @else {
                      <span class="ob-absent-text">no anchor established</span>
                    }
                  </dd>

                  <dt class="ob-micro">Not asserted</dt>
                  <dd>
                    how a whole period relates to the other events, and any order inside a group
                  </dd>

                  <dt class="ob-micro">Precision</dt>
                  <dd>shown exactly as stated · never widened, never inferred</dd>
                </dl>
              </section>

              <section class="panel">
                <div class="panel-head">
                  <h2 class="ob-pane-label">Precision</h2>
                  <span class="ob-meta">the four stated values</span>
                </div>
                <ul class="legend">
                  @for (step of precisionExamples; track step.name) {
                    <li>
                      <ob-precision-rail [precision]="step.name" />
                      <span class="ob-mono example">{{ step.example }}</span>
                    </li>
                  }
                </ul>
                <p class="legend-note">One cell of four marks the value’s own category.</p>
              </section>
            </aside>
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

    .policy {
      margin: 0;
      padding: 6px 16px;
      background: var(--ob-surface-2);
      border-bottom: 1px solid var(--ob-rule);
      font-size: 11px;
      color: var(--ob-muted);
      line-height: 1.5;
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
      align-items: baseline;
      text-wrap: pretty;
    }

    .static-copy {
      color: var(--ob-faint);
    }

    .version {
      margin-left: auto;
      font-size: 10px;
      color: var(--ob-faint);
      white-space: nowrap;
    }

    main {
      display: block;
      padding: 16px;
    }

    .columns {
      display: grid;
      grid-template-columns: minmax(0, 1.1fr) minmax(0, 1fr);
      gap: 16px;
      align-items: start;
    }

    .panel {
      background: var(--ob-surface);
      border: 1px solid var(--ob-border);
      border-radius: 3px;
      min-width: 0;
      display: flex;
      flex-direction: column;
    }

    .panel-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 8px 12px;
      border-bottom: 1px solid var(--ob-rule);
      background: var(--ob-surface-2);
      flex-wrap: wrap;
    }

    .panel-head h2 {
      margin: 0;
    }

    .track {
      margin: 0;
      padding: 0 14px;
      list-style: none;
      display: flex;
      flex-direction: column;
      min-width: 0;
    }

    ob-unsequenced-section {
      padding: 0 14px 14px;
    }

    .patients {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 0 0 12px;
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

    aside {
      display: flex;
      flex-direction: column;
      gap: 16px;
      min-width: 0;
    }

    .asserts {
      margin: 0;
      padding: 12px;
      display: grid;
      grid-template-columns: 116px minmax(0, 1fr);
      gap: 7px 10px;
      font-size: 12px;
    }

    .asserts dt {
      padding-top: 2px;
    }

    .asserts dd {
      margin: 0;
      color: var(--ob-ink-2);
      line-height: 1.5;
      display: flex;
      align-items: baseline;
      gap: 6px;
      flex-wrap: wrap;
      min-width: 0;
    }

    .arrow {
      color: var(--ob-faint);
    }

    .legend {
      margin: 0;
      padding: 12px;
      list-style: none;
      display: flex;
      flex-direction: column;
      gap: 7px;
    }

    .legend li {
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
    }

    .example {
      font-size: 11px;
      color: var(--ob-faint);
      overflow-wrap: anywhere;
    }

    .legend-note {
      margin: 0;
      padding: 0 12px 12px;
      font-size: 11.5px;
      color: var(--ob-muted);
      border-top: 1px solid var(--ob-rule-soft);
      padding-top: 9px;
    }

    @media (max-width: 1100px) {
      .columns {
        grid-template-columns: minmax(0, 1fr);
      }
    }

    @media (max-width: 640px) {
      main {
        padding: 12px;
      }

      .columns {
        gap: 12px;
      }

      .track {
        padding: 0 10px;
      }

      ob-unsequenced-section {
        padding: 0 10px 12px;
      }

      .asserts {
        grid-template-columns: minmax(0, 1fr);
        gap: 4px 0;
      }

      .asserts dd {
        padding-bottom: 6px;
      }
    }
  `,
})
export class TimelinePage {
  private readonly data = inject(InspectorDataService);
  private readonly router = inject(Router);

  readonly importBatchId = input.required<string>();
  readonly patientId = input<string | undefined>(undefined);

  protected readonly precisionExamples = PRECISION_EXAMPLES;

  protected readonly import = computed(() => valueOf(this.data.import()));
  protected readonly timeline = computed(() => valueOf(this.data.timeline()));

  protected readonly importFailure = computed(() => failureOf(this.data.import()));
  protected readonly timelineFailure = computed(() => failureOf(this.data.timeline()));

  protected readonly loadingMessage = computed(() => {
    if (this.data.import().kind === 'loading') {
      return 'Loading import…';
    }

    return this.data.timeline().kind === 'loading' ? 'Loading patient timeline…' : null;
  });

  protected readonly patientIds = computed<readonly string[]>(
    () => this.import()?.patientIds ?? [],
  );

  protected readonly selectedPatientId = computed(() => {
    const ids = this.patientIds();
    const requested = this.patientId();

    if (requested && ids.includes(requested)) {
      return requested;
    }

    return ids.length > 0 ? ids[0] : null;
  });

  protected readonly policy = computed(() => this.timeline()?.projectionPolicy ?? null);

  protected readonly isEmpty = computed(() => {
    const projected = this.timeline();

    return !!projected && projected.groups.length === 0 && projected.unsequencedEvents.length === 0;
  });

  protected readonly anchorChain = computed(() => anchorChainOf(this.timeline()?.groups ?? []));

  protected readonly summary = computed(() => {
    const projected = this.timeline();

    if (!projected) {
      return '';
    }

    const anchored = anchoredEventsOf(projected.groups);
    const unsequenced = projected.unsequencedEvents.length;
    const total = anchored + unsequenced;

    return (
      `${total} ${total === 1 ? 'event' : 'events'} · ` +
      `${anchored} ${anchored === 1 ? 'anchor' : 'anchors'} established · ` +
      `${unsequenced === 0 ? 'none unsequenced' : `${unsequenced} unsequenced`}`
    );
  });

  protected readonly trackNote = computed(() => {
    const projected = this.timeline();

    if (!projected) {
      return '';
    }

    const groups = projected.groups.length;

    return `${groups} projected ${groups === 1 ? 'group' : 'groups'} · order owned by the projection`;
  });

  protected readonly timelineRequest = computed(
    () => `GET /api/v1/patients/${this.selectedPatientId() ?? ''}/timeline`,
  );

  protected readonly inspectorCommands = computed(() => ['/imports', this.importBatchId()]);

  protected readonly inspectorQueryParams = computed(() => {
    const patientId = this.selectedPatientId();

    return patientId ? { patientId } : {};
  });

  constructor() {
    effect(() => this.data.loadImport(this.importBatchId()));

    effect(() => {
      const requested = this.patientId();
      const selected = this.selectedPatientId();

      if (requested && selected && requested !== selected) {
        void this.router.navigate([], {
          queryParams: { patientId: selected },
          queryParamsHandling: 'merge',
          replaceUrl: true,
        });
      }
    });

    effect(() => {
      const patientId = this.selectedPatientId();

      if (patientId) {
        this.data.loadTimeline(patientId);
      } else if (this.data.import().kind === 'loaded') {
        this.data.clearTimeline();
      }
    });
  }

  protected selectPatient(patientId: string): void {
    void this.router.navigate([], {
      queryParams: { patientId },
      queryParamsHandling: 'merge',
    });
  }

  protected retryImport(): void {
    this.data.loadImport(this.importBatchId(), true);
  }

  protected retryTimeline(): void {
    const patientId = this.selectedPatientId();

    if (patientId) {
      this.data.loadTimeline(patientId, true);
    }
  }
}
