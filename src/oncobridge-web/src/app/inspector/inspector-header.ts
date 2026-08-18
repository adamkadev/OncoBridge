import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { ImportResponse } from '../api';
import { asNumber } from '../core/api-values';

@Component({
  selector: 'ob-inspector-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="identity">
      <span class="brand">OncoBridge</span>
      <span class="divider" aria-hidden="true"></span>
      <span class="tagline">Oncology interoperability and data-quality workbench</span>
    </div>

    <dl class="meta">
      <div class="cell">
        <dt class="ob-micro">Import</dt>
        <dd class="ob-mono">{{ importBatchId() }}</dd>
      </div>

      @if (value(); as batch) {
        <div class="cell">
          <dt class="ob-micro">Status</dt>
          <dd class="status">
            <span class="dot" aria-hidden="true"></span>
            {{ batch.status }}
          </dd>
        </div>
        <div class="cell">
          <dt class="ob-micro">Source system</dt>
          <dd class="ob-mono">{{ batch.sourceSystemLabel }}</dd>
        </div>
        <div class="cell">
          <dt class="ob-micro">Resources</dt>
          <dd class="ob-mono">
            {{ asNumber(batch.entryCount) }} · {{ batch.bundleType ?? 'unknown' }}
          </dd>
        </div>
        <div class="cell">
          <dt class="ob-micro">Normalizer</dt>
          <dd class="ob-mono">{{ normalizer() }}</dd>
        </div>
        <div class="cell">
          <dt class="ob-micro">Payload SHA-256</dt>
          <dd class="hash">
            <span class="ob-mono" data-testid="payload-hash">{{ hash() }}</span>
            <button type="button" class="reveal" (click)="revealed.set(!revealed())">
              {{ revealed() ? 'hide' : 'show' }}
            </button>
          </dd>
        </div>
      } @else {
        <div class="cell">
          <dt class="ob-micro">Status</dt>
          <dd aria-live="polite" class="waiting">
            <span class="ob-spinner" aria-hidden="true"></span>
            Loading import…
          </dd>
        </div>
      }
    </dl>
  `,
  styles: `
    :host {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 24px;
      flex-wrap: wrap;
      padding: 11px 16px;
      background: var(--ob-surface);
      border-bottom: 1px solid var(--ob-border);
    }

    .identity {
      display: flex;
      align-items: baseline;
      gap: 10px;
      flex-wrap: wrap;
    }

    .brand {
      font-size: 15px;
      font-weight: 650;
      letter-spacing: -0.01em;
    }

    .divider {
      width: 1px;
      height: 13px;
      background: var(--ob-border-2);
      display: inline-block;
    }

    .tagline {
      font-size: 12px;
      color: var(--ob-muted);
    }

    .meta {
      margin: 0;
      display: flex;
      gap: 20px;
      flex-wrap: wrap;
    }

    .cell {
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 0;
    }

    dt {
      margin: 0;
    }

    dd {
      margin: 0;
      font-size: 11.5px;
    }

    .status {
      display: flex;
      align-items: center;
      gap: 5px;
      font-weight: 600;
    }

    .dot {
      width: 6px;
      height: 6px;
      background: var(--ob-accent);
      display: inline-block;
    }

    .waiting {
      display: flex;
      align-items: center;
      gap: 6px;
      color: var(--ob-muted);
    }

    .hash {
      display: flex;
      align-items: center;
      gap: 5px;
      color: var(--ob-muted);
      min-width: 0;
    }

    .hash .ob-mono {
      word-break: break-all;
    }

    .reveal {
      font-family: var(--ob-sans);
      font-size: 9.5px;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      color: var(--ob-ink-2);
      background: var(--ob-surface-2);
      border: 1px solid var(--ob-border-2);
      padding: 1px 4px;
      border-radius: 2px;
      cursor: pointer;
      flex: none;
    }
  `,
})
export class InspectorHeader {
  readonly importBatchId = input.required<string>();
  readonly value = input<ImportResponse | null>(null);

  protected readonly revealed = signal(false);

  protected readonly asNumber = asNumber;

  protected readonly hash = computed(() => {
    const contentHash = this.value()?.contentHash ?? '';

    return this.revealed() || contentHash.length <= 14
      ? contentHash
      : `${contentHash.slice(0, 6)}…${contentHash.slice(-4)}`;
  });

  protected readonly normalizer = computed(() => {
    const batch = this.value();

    if (!batch) {
      return '';
    }

    const version = batch.normalizerVersion ?? 'not normalized';

    return batch.normalizedAt ? `${version} · ${batch.normalizedAt}` : version;
  });
}
