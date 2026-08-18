import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { ApiFailure, toApiFailure } from '../core/async';
import { RawImportClient, fhirJsonMediaType } from '../core/raw-import-client';
import { StandardsNote } from '../shared/standards-note';

@Component({
  selector: 'ob-import-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [StandardsNote],
  template: `
    <div class="page">
      <header>
        <h1>OncoBridge</h1>
        <p class="tagline">Oncology interoperability and data-quality workbench</p>
        <p class="lede">
          Import a FHIR R4 Bundle. The received bytes are preserved as import evidence, a
          deliberately small oncology model is normalized from them, and deterministic findings and
          field-level provenance are recorded.
        </p>
        <ob-standards-note />
      </header>

      <main>
        <form (submit)="submit($event)">
          <div class="field">
            <label class="ob-field-label" for="bundle-file">Bundle file</label>

            @if (file(); as selected) {
              <div class="file selected">
                <span class="file-name">
                  <span class="ob-mono name">{{ selected.name }}</span>
                  <span class="ob-hint">{{ size(selected) }} · posted as {{ mediaType }}</span>
                </span>
                <button type="button" class="ob-button-quiet" (click)="clear()" [disabled]="busy()">
                  Clear
                </button>
              </div>
            } @else {
              <div class="file empty">
                <span class="ob-hint">No file selected</span>
              </div>
            }

            <input
              id="bundle-file"
              type="file"
              accept="application/fhir+json,application/json,.json"
              [disabled]="busy()"
              (change)="choose($event)"
            />
            <p class="ob-hint">
              The file is posted verbatim as {{ mediaType }}; nothing is rewritten in the browser.
            </p>
          </div>

          <div class="field">
            <label class="ob-field-label" for="source-system-label">
              Source system label <span class="optional">· optional</span>
            </label>
            <input
              id="source-system-label"
              class="ob-input"
              type="text"
              placeholder="api"
              [value]="sourceSystemLabel()"
              [disabled]="busy()"
              (input)="setLabel($event)"
            />
            <p class="ob-hint">
              Recorded on the import batch. Omitted labels are recorded as <code>api</code>.
            </p>
          </div>

          @if (failure(); as problem) {
            <div class="failure" role="alert">
              <span class="bar" aria-hidden="true"></span>
              <div class="failure-body">
                <p class="badge-row">
                  <span class="badge"><span class="swatch" aria-hidden="true"></span>Error</span>
                  <span class="ob-mono request"
                    >POST /api/v1/imports{{ problem.status ? ' · ' + problem.status : '' }}</span
                  >
                </p>
                <p class="failure-title">{{ problem.title }}</p>
                @if (problem.detail) {
                  <p class="failure-detail">{{ problem.detail }}</p>
                }
                <p class="ob-hint">
                  The message is the API's problem detail. No stack traces are shown.
                </p>
              </div>
            </div>
          }

          <div class="actions">
            <button type="submit" class="ob-button" [disabled]="!file() || busy()">
              @if (busy()) {
                <span class="ob-spinner light" aria-hidden="true"></span>
                Importing…
              } @else {
                Import FHIR Bundle
              }
            </button>
            <p class="ob-hint" aria-live="polite">
              @if (busy()) {
                Preserving bytes, normalizing, assessing quality. No progress estimate is available
                — the API returns when the batch is queryable.
              } @else if (file()) {
                Normalization and quality assessment run within this request; the inspector opens
                when it returns.
              } @else {
                Select a file to enable import.
              }
            </p>
          </div>
        </form>

        <footer>
          <p>
            Synthetic and public data only. Not diagnostic software, treatment software, clinical
            decision support, a medical device, or clinically validated.
          </p>
          <p>
            Already imported? Open an inspector by import id:
            <code>/imports/&lt;importBatchId&gt;</code>
          </p>
        </footer>
      </main>
    </div>
  `,
  styles: `
    .page {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 22px;
      padding: 64px 16px 72px;
    }

    header,
    main {
      width: 100%;
      max-width: 640px;
      min-width: 0;
    }

    header {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    main {
      display: flex;
      flex-direction: column;
      gap: 22px;
    }

    @media (max-width: 640px) {
      .page {
        padding: 32px 12px 48px;
      }
    }

    h1 {
      margin: 0;
      font-size: 24px;
      font-weight: 650;
      letter-spacing: -0.015em;
    }

    .tagline {
      margin: 0;
      font-size: 14px;
      color: var(--ob-ink-2);
    }

    .lede {
      margin: 0;
      font-size: 12.5px;
      color: var(--ob-muted);
      line-height: 1.55;
      text-wrap: pretty;
    }

    ob-standards-note {
      border: 1px solid var(--ob-rule);
      border-radius: 3px;
      margin-top: 4px;
    }

    form {
      background: var(--ob-surface);
      border: 1px solid var(--ob-border);
      border-radius: 3px;
      padding: 18px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: 7px;
    }

    .optional {
      text-transform: none;
      letter-spacing: 0;
      font-weight: 400;
      color: var(--ob-absent);
    }

    .file {
      border-radius: 3px;
      padding: 12px 14px;
      display: flex;
      align-items: center;
      gap: 12px;
      min-height: 56px;
    }

    .file.selected {
      border: 1px solid color-mix(in oklab, var(--ob-accent) 50%, transparent);
      background: var(--ob-accent-wash);
    }

    .file.empty {
      border: 1px dashed var(--ob-border);
      background: var(--ob-surface-2);
    }

    .file-name {
      display: flex;
      flex-direction: column;
      gap: 3px;
      flex: 1;
      min-width: 0;
    }

    .name {
      font-size: 13px;
      font-weight: 600;
      word-break: break-all;
    }

    input[type='file'] {
      font-family: var(--ob-sans);
      font-size: 12.5px;
      color: var(--ob-ink-2);
    }

    code {
      font-family: var(--ob-mono);
      font-size: 11.5px;
      border: 1px solid var(--ob-border-2);
      background: var(--ob-surface-2);
      padding: 1px 5px;
      border-radius: 2px;
    }

    .failure {
      display: flex;
      border: 1px solid color-mix(in oklab, var(--ob-error) 35%, transparent);
      background: var(--ob-error-wash);
      border-radius: 3px;
      overflow: hidden;
    }

    .bar {
      width: 3px;
      flex: none;
      background: var(--ob-error);
    }

    .failure-body {
      padding: 11px 12px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .failure-body p {
      margin: 0;
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

    .failure-title {
      font-size: 13px;
      font-weight: 600;
    }

    .failure-detail {
      font-size: 12px;
      line-height: 1.5;
      color: var(--ob-ink-2);
      text-wrap: pretty;
    }

    .actions {
      display: flex;
      align-items: center;
      gap: 14px;
      border-top: 1px solid var(--ob-rule-soft);
      padding-top: 14px;
      flex-wrap: wrap;
    }

    .actions button {
      display: inline-flex;
      align-items: center;
      gap: 9px;
    }

    .actions .ob-hint {
      flex: 1 1 220px;
      margin: 0;
    }

    .ob-spinner.light {
      border-color: #fff;
      border-right-color: transparent;
    }

    footer {
      display: flex;
      flex-direction: column;
      gap: 6px;
      font-size: 11.5px;
      color: var(--ob-muted);
      line-height: 1.6;
    }

    footer p {
      margin: 0;
    }
  `,
})
export class ImportPage {
  private readonly client = inject(RawImportClient);
  private readonly router = inject(Router);

  protected readonly mediaType = fhirJsonMediaType;

  protected readonly file = signal<File | null>(null);
  protected readonly sourceSystemLabel = signal('');
  protected readonly busy = signal(false);
  protected readonly failure = signal<ApiFailure | null>(null);

  protected readonly canSubmit = computed(() => this.file() !== null && !this.busy());

  protected choose(event: Event): void {
    const input = event.target as HTMLInputElement;

    this.file.set(input.files?.[0] ?? null);
    this.failure.set(null);
  }

  protected clear(): void {
    this.file.set(null);
    this.failure.set(null);
  }

  protected setLabel(event: Event): void {
    this.sourceSystemLabel.set((event.target as HTMLInputElement).value);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();

    const file = this.file();

    if (!file || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.failure.set(null);

    try {
      const created = await this.client.import(file, this.sourceSystemLabel());

      await this.router.navigate(['/imports', created.importBatchId]);
    } catch (error: unknown) {
      this.failure.set(toApiFailure(error, 'FHIR Bundle import failed'));
    } finally {
      this.busy.set(false);
    }
  }

  protected size(file: File): string {
    return `${(file.size / 1024).toFixed(1)} KB`;
  }
}
