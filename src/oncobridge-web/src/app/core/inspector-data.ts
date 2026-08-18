import { Injectable, inject, signal } from '@angular/core';

import {
  Api,
  FindingResponse,
  ImportResponse,
  LineageResponse,
  PatientRecordResponse,
  PatientTimelineResponse,
  getDomainProvenance,
  getImport,
  getImportFindings,
  getPatientRecord,
  getPatientTimeline,
} from '../api';
import { Async, failed, idle, loaded, loading, toApiFailure } from './async';

@Injectable({ providedIn: 'root' })
export class InspectorDataService {
  private readonly api = inject(Api);

  private readonly importState = signal<Async<ImportResponse>>(idle);
  private readonly findingsState = signal<Async<readonly FindingResponse[]>>(idle);
  private readonly recordState = signal<Async<PatientRecordResponse>>(idle);
  private readonly provenanceState = signal<Async<readonly LineageResponse[]>>(idle);
  private readonly timelineState = signal<Async<PatientTimelineResponse>>(idle);

  private importKey: string | null = null;
  private findingsKey: string | null = null;
  private recordKey: string | null = null;
  private provenanceKey: string | null = null;
  private timelineKey: string | null = null;

  readonly import = this.importState.asReadonly();
  readonly findings = this.findingsState.asReadonly();
  readonly record = this.recordState.asReadonly();
  readonly provenance = this.provenanceState.asReadonly();
  readonly timeline = this.timelineState.asReadonly();

  loadImport(importBatchId: string, force = false): void {
    if (!force && this.importKey === importBatchId) {
      return;
    }

    this.importKey = importBatchId;
    this.importState.set(loading);

    this.api
      .invoke(getImport, { id: importBatchId })
      .then((value) => this.settle(this.importState, this.importKey, importBatchId, loaded(value)))
      .catch((error: unknown) =>
        this.settle(
          this.importState,
          this.importKey,
          importBatchId,
          failed(toApiFailure(error, 'The import could not be loaded')),
        ),
      );
  }

  loadFindings(importBatchId: string, force = false): void {
    if (!force && this.findingsKey === importBatchId) {
      return;
    }

    this.findingsKey = importBatchId;
    this.findingsState.set(loading);

    this.api
      .invoke(getImportFindings, { id: importBatchId })
      .then((value) =>
        this.settle(this.findingsState, this.findingsKey, importBatchId, loaded(value)),
      )
      .catch((error: unknown) =>
        this.settle(
          this.findingsState,
          this.findingsKey,
          importBatchId,
          failed(toApiFailure(error, 'Findings could not be loaded')),
        ),
      );
  }

  loadRecord(patientId: string, force = false): void {
    if (!force && this.recordKey === patientId) {
      return;
    }

    this.recordKey = patientId;
    this.recordState.set(loading);

    this.api
      .invoke(getPatientRecord, { patientId })
      .then((value) => this.settle(this.recordState, this.recordKey, patientId, loaded(value)))
      .catch((error: unknown) =>
        this.settle(
          this.recordState,
          this.recordKey,
          patientId,
          failed(toApiFailure(error, 'The canonical patient record could not be loaded')),
        ),
      );
  }

  clearRecord(): void {
    this.recordKey = null;
    this.recordState.set(idle);
  }

  loadTimeline(patientId: string, force = false): void {
    if (!force && this.timelineKey === patientId) {
      return;
    }

    this.timelineKey = patientId;
    this.timelineState.set(loading);

    this.api
      .invoke(getPatientTimeline, { patientId })
      .then((value) => this.settle(this.timelineState, this.timelineKey, patientId, loaded(value)))
      .catch((error: unknown) =>
        this.settle(
          this.timelineState,
          this.timelineKey,
          patientId,
          failed(toApiFailure(error, 'The patient timeline could not be loaded')),
        ),
      );
  }

  clearTimeline(): void {
    this.timelineKey = null;
    this.timelineState.set(idle);
  }

  loadProvenance(domainEntityId: string, force = false): void {
    if (!force && this.provenanceKey === domainEntityId) {
      return;
    }

    this.provenanceKey = domainEntityId;
    this.provenanceState.set(loading);

    this.api
      .invoke(getDomainProvenance, { domainEntityId })
      .then((value) =>
        this.settle(
          this.provenanceState,
          this.provenanceKey,
          domainEntityId,
          loaded(value.records),
        ),
      )
      .catch((error: unknown) =>
        this.settle(
          this.provenanceState,
          this.provenanceKey,
          domainEntityId,
          failed(toApiFailure(error, 'Lineage could not be loaded for this entity')),
        ),
      );
  }

  clearProvenance(): void {
    this.provenanceKey = null;
    this.provenanceState.set(idle);
  }

  private settle<T>(
    state: { set(value: Async<T>): void },
    currentKey: string | null,
    requestedKey: string,
    next: Async<T>,
  ): void {
    if (currentKey === requestedKey) {
      state.set(next);
    }
  }
}
