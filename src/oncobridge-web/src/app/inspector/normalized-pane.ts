import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  CancerStagingResponse,
  CancerSurgicalProcedureResponse,
  PatientRecordResponse,
  PatientResponse,
  PrimaryCancerDiagnosisResponse,
} from '../api';
import { ApiFailure } from '../core/async';
import { EntityInstance } from '../core/entities';
import { EvidenceRecord, markerOfFieldPath } from '../core/evidence';
import { AbsentValue } from '../shared/absent-value';
import { CodedValue } from '../shared/coded-value';
import { EvidenceMarker } from '../shared/evidence-marker';
import { IdDisplay } from '../shared/id-display';
import { Pane } from '../shared/pane';
import { PaneEmpty, PaneError, PaneLoading } from '../shared/pane-state';
import { PartialDate } from '../shared/partial-date';
import { TemporalOccurrence } from '../shared/temporal-occurrence';

@Component({
  selector: 'ob-normalized-pane',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Pane,
    PaneLoading,
    PaneError,
    PaneEmpty,
    EvidenceMarker,
    CodedValue,
    PartialDate,
    TemporalOccurrence,
    AbsentValue,
    IdDisplay,
  ],
  template: `
    <ob-pane number="02" label="Normalized">
      <span pane-meta>{{ meta() }}</span>

      @if (loadingMessage()) {
        <ob-pane-loading [message]="loadingMessage()!" />
      } @else if (failure()) {
        <ob-pane-error
          [failure]="failure()!"
          [request]="request()"
          consequence="Source evidence and findings for this import are still shown; only the canonical view is missing."
          retryLabel="Retry normalized record"
          (retry)="retry.emit()"
        />
      } @else if (!hasPatient()) {
        <ob-pane-empty
          title="No canonical patient was produced"
          caveat="Normalization is derived and re-runnable — a later mapper version may produce one."
        >
          This import stored its source resources, and normalization derived no Patient from them.
          Source evidence and findings are still available; there is no canonical record to inspect.
        </ob-pane-empty>
      } @else if (entity(); as selected) {
        <div class="title">
          <h3>{{ selected.kindLabel }}</h3>
          <ob-id [value]="selected.id" [head]="8" [tail]="4" />
        </div>

        <dl class="fields">
          @if (staging(); as value) {
            <div class="field">
              <span class="marker-cell">
                @if (markerOf(null); as marker) {
                  <ob-evidence-marker [marker]="marker" [active]="true" />
                }
              </span>
              <dt>Stage group</dt>
              <dd>
                @if (value.stageGroup) {
                  <ob-coded-value [concept]="value.stageGroup" />
                } @else {
                  <ob-absent-value [relatedCheckIds]="relatedCheckIds()" />
                }
              </dd>
              <span class="from">{{ fromLabel(markerOf(null)) }}</span>
            </div>

            <div class="field">
              <span class="marker-cell"></span>
              <dt>Method</dt>
              <dd>
                @if (value.method) {
                  <ob-coded-value [concept]="value.method" />
                } @else {
                  <ob-absent-value [relatedCheckIds]="relatedCheckIds()" />
                }
              </dd>
              <span class="from">{{ value.method ? '' : 'absent' }}</span>
            </div>

            <div class="field">
              <span class="marker-cell"></span>
              <dt>Effective</dt>
              <dd>
                @if (value.effective) {
                  <ob-partial-date [date]="value.effective" />
                } @else {
                  <ob-absent-value />
                }
              </dd>
              <span class="from">{{ value.effective ? fromLabel(markerOf(null)) : '' }}</span>
            </div>

            @for (category of value.categories; track category.axis) {
              <div class="field">
                <span class="marker-cell">
                  @if (markerOfSourceId(category.sourceResourceId); as marker) {
                    <ob-evidence-marker [marker]="marker" />
                  }
                </span>
                <dt>Category {{ category.axis }}</dt>
                <dd><ob-coded-value [concept]="category.code" /></dd>
                <span class="from">{{
                  fromLabel(markerOfSourceId(category.sourceResourceId))
                }}</span>
              </div>
            }

            <div class="field related">
              <span class="marker-cell"></span>
              <dt>Associated diagnosis</dt>
              <dd class="stacked">
                <span>{{ diagnosisLabel(value.primaryCancerDiagnosisId) }}</span>
                <span class="ob-id"
                  >PrimaryCancerDiagnosis · {{ value.primaryCancerDiagnosisId }}</span
                >
              </dd>
              <span class="from other">other entity</span>
            </div>

            <div class="field related">
              <span class="marker-cell"></span>
              <dt>Patient</dt>
              <dd class="stacked">
                <span>{{ patientLabel() }}</span>
                <span class="ob-id">Patient · {{ value.patientId }}</span>
              </dd>
              <span class="from other">other entity</span>
            </div>
          } @else if (patient(); as value) {
            <div class="field">
              <span class="marker-cell">
                @if (markerOf(null); as marker) {
                  <ob-evidence-marker [marker]="marker" [active]="true" />
                }
              </span>
              <dt>Source identifier</dt>
              <dd>
                @if (value.sourceIdentifier) {
                  <span class="strong">{{ value.sourceIdentifier }}</span>
                } @else {
                  <ob-absent-value />
                }
              </dd>
              <span class="from">{{ fromLabel(markerOf(null)) }}</span>
            </div>

            <div class="field">
              <span class="marker-cell"></span>
              <dt>Birth date</dt>
              <dd>
                @if (value.birthDate) {
                  <ob-partial-date [date]="value.birthDate" />
                } @else {
                  <ob-absent-value />
                }
              </dd>
              <span class="from">{{ value.birthDate ? fromLabel(markerOf(null)) : '' }}</span>
            </div>

            <div class="field">
              <span class="marker-cell"></span>
              <dt>Sex at birth as recorded</dt>
              <dd>
                @if (value.sexAtBirthAsRecorded) {
                  <ob-coded-value [concept]="value.sexAtBirthAsRecorded" />
                } @else {
                  <ob-absent-value />
                }
              </dd>
              <span class="from"></span>
            </div>
          } @else if (diagnosis(); as value) {
            <div class="field">
              <span class="marker-cell">
                @if (markerOf(null); as marker) {
                  <ob-evidence-marker [marker]="marker" [active]="true" />
                }
              </span>
              <dt>Code</dt>
              <dd><ob-coded-value [concept]="value.code" /></dd>
              <span class="from">{{ fromLabel(markerOf(null)) }}</span>
            </div>

            <div class="field">
              <span class="marker-cell"></span>
              <dt>Onset</dt>
              <dd>
                @if (value.onset) {
                  <ob-temporal-occurrence [occurrence]="value.onset" />
                } @else {
                  <ob-absent-value />
                }
              </dd>
              <span class="from">{{ value.onset ? fromLabel(markerOf(null)) : '' }}</span>
            </div>

            <div class="field">
              <span class="marker-cell"></span>
              <dt>Body site</dt>
              <dd>
                @if (value.bodySite) {
                  <ob-coded-value [concept]="value.bodySite" />
                } @else {
                  <ob-absent-value />
                }
              </dd>
              <span class="from"></span>
            </div>

            <div class="field">
              <span class="marker-cell"></span>
              <dt>Recorded date</dt>
              <dd>
                @if (value.recordedDate) {
                  <ob-partial-date [date]="value.recordedDate" />
                } @else {
                  <ob-absent-value />
                }
              </dd>
              <span class="from"></span>
            </div>

            <div class="field related">
              <span class="marker-cell"></span>
              <dt>Patient</dt>
              <dd class="stacked">
                <span>{{ patientLabel() }}</span>
                <span class="ob-id">Patient · {{ value.patientId }}</span>
              </dd>
              <span class="from other">other entity</span>
            </div>
          } @else if (procedure(); as value) {
            <div class="field">
              <span class="marker-cell">
                @if (markerOf(null); as marker) {
                  <ob-evidence-marker [marker]="marker" [active]="true" />
                }
              </span>
              <dt>Code</dt>
              <dd><ob-coded-value [concept]="value.code" /></dd>
              <span class="from">{{ fromLabel(markerOf(null)) }}</span>
            </div>

            <div class="field">
              <span class="marker-cell"></span>
              <dt>Performed</dt>
              <dd>
                @if (value.performed) {
                  <ob-temporal-occurrence [occurrence]="value.performed" />
                } @else {
                  <ob-absent-value />
                }
              </dd>
              <span class="from">{{ value.performed ? fromLabel(markerOf(null)) : '' }}</span>
            </div>

            <div class="field">
              <span class="marker-cell"></span>
              <dt>Body site</dt>
              <dd>
                @if (value.bodySite) {
                  <ob-coded-value [concept]="value.bodySite" />
                } @else {
                  <ob-absent-value />
                }
              </dd>
              <span class="from"></span>
            </div>

            <div class="field related">
              <span class="marker-cell"></span>
              <dt>Patient</dt>
              <dd class="stacked">
                <span>{{ patientLabel() }}</span>
                <span class="ob-id">Patient · {{ value.patientId }}</span>
              </dd>
              <span class="from other">other entity</span>
            </div>
          }
        </dl>
      } @else {
        <ob-pane-empty title="No canonical entity selected">
          Choose a canonical entity above to inspect the values normalization derived for it.
        </ob-pane-empty>
      }

      <p pane-footnote class="ob-footnote">
        Evidence markers annotate only this entity's own lineage. Related canonical entities are
        shown for context and never carry an evidence marker.
      </p>
    </ob-pane>
  `,
  styles: `
    .title {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: 12px;
      padding: 12px 12px 8px;
    }

    h3 {
      margin: 0;
      font-size: 16px;
      font-weight: 600;
    }

    .fields {
      margin: 0;
      padding: 0 12px 12px;
      display: flex;
      flex-direction: column;
      min-width: 0;
    }

    .field {
      display: grid;
      grid-template-columns: 24px 132px 1fr auto;
      align-items: baseline;
      gap: 0 8px;
      padding: 8px 0;
      border-top: 1px solid var(--ob-rule-soft);
    }

    .field.related {
      border-top-color: var(--ob-rule);
    }

    .marker-cell {
      display: flex;
      align-items: center;
      min-height: 18px;
    }

    dt {
      font-size: 11.5px;
      color: var(--ob-muted);
    }

    dd {
      margin: 0;
      min-width: 0;
      font-size: 13px;
      overflow-wrap: anywhere;
    }

    dd.stacked {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .strong {
      font-size: 15px;
      font-weight: 600;
    }

    .from {
      font-size: 10.5px;
      color: var(--ob-muted);
      text-align: right;
      white-space: nowrap;
    }

    .from.other {
      color: var(--ob-faint);
    }

    @media (max-width: 900px) {
      .field {
        grid-template-columns: 24px 1fr auto;
      }

      dt {
        grid-column: 2;
      }

      dd {
        grid-column: 2 / span 2;
      }
    }
  `,
})
export class NormalizedPane {
  readonly record = input<PatientRecordResponse | null>(null);
  readonly entity = input<EntityInstance | null>(null);
  readonly evidence = input<readonly EvidenceRecord[]>([]);
  readonly relatedCheckIds = input<readonly string[]>([]);
  readonly loadingMessage = input<string | null>(null);
  readonly failure = input<ApiFailure | null>(null);
  readonly request = input('');
  readonly hasPatient = input(true);

  readonly retry = output<void>();

  protected readonly patient = computed<PatientResponse | null>(() => {
    const record = this.record();
    const entity = this.entity();

    return record && entity?.kind === 'Patient' ? record.patient : null;
  });

  protected readonly diagnosis = computed<PrimaryCancerDiagnosisResponse | null>(() => {
    const entity = this.entity();

    return entity?.kind === 'PrimaryCancerDiagnosis'
      ? (this.record()?.primaryCancerDiagnoses.find((item) => item.id === entity.id) ?? null)
      : null;
  });

  protected readonly staging = computed<CancerStagingResponse | null>(() => {
    const entity = this.entity();

    return entity?.kind === 'CancerStaging'
      ? (this.record()?.cancerStagings.find((item) => item.id === entity.id) ?? null)
      : null;
  });

  protected readonly procedure = computed<CancerSurgicalProcedureResponse | null>(() => {
    const entity = this.entity();

    return entity?.kind === 'CancerSurgicalProcedure'
      ? (this.record()?.cancerSurgicalProcedures.find((item) => item.id === entity.id) ?? null)
      : null;
  });

  protected readonly meta = computed(() => {
    const entity = this.entity();
    const sources = new Set(this.evidence().map((record) => record.lineage.sourceResourceId)).size;

    if (!entity) {
      return 'Derived, safe to rebuild';
    }

    return sources > 0
      ? `${entity.kind} · ${sources} source ${sources === 1 ? 'resource' : 'resources'} → 1 entity`
      : `${entity.kind} · derived, safe to rebuild`;
  });

  protected markerOf(fieldPath: string | null): string | null {
    return markerOfFieldPath(this.evidence(), fieldPath);
  }

  protected markerOfSourceId(sourceResourceId: string): string | null {
    return (
      this.evidence().find(
        (record) =>
          record.lineage.sourceResourceId === sourceResourceId && !!record.lineage.fieldPath,
      )?.marker ?? null
    );
  }

  protected fromLabel(marker: string | null): string {
    return marker ? `from ${marker}` : '';
  }

  protected patientLabel(): string {
    const patient = this.record()?.patient;

    return patient ? (patient.sourceIdentifier ?? patient.id) : '';
  }

  protected diagnosisLabel(diagnosisId: string): string {
    const diagnosis = this.record()?.primaryCancerDiagnoses.find((item) => item.id === diagnosisId);

    if (!diagnosis) {
      return diagnosisId;
    }

    return diagnosis.code.display ?? diagnosis.code.code;
  }
}
