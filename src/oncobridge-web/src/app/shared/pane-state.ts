import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ApiFailure } from '../core/async';

@Component({
  selector: 'ob-pane-loading',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="line" aria-live="polite">
      <span class="ob-spinner" aria-hidden="true"></span>
      {{ message() }}
    </p>
    <span class="bar" style="width: 62%"></span>
    <span class="bar" style="width: 48%"></span>
    <span class="bar" style="width: 54%"></span>
    <span class="bar" style="width: 40%"></span>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      gap: 9px;
      padding: 12px;
    }

    .line {
      margin: 0;
      display: flex;
      align-items: center;
      gap: 7px;
      font-size: 11.5px;
      color: var(--ob-muted);
    }

    .bar {
      height: 14px;
      background: var(--ob-rule-soft);
      border-radius: 2px;
      display: inline-block;
    }
  `,
})
export class PaneLoading {
  readonly message = input.required<string>();
}

@Component({
  selector: 'ob-pane-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="badge-row">
      <span class="badge">
        <span class="swatch" aria-hidden="true"></span>
        Error
      </span>
      <span class="ob-mono request">{{ request() }}{{ failure().status ? ' · ' + failure().status : '' }}</span>
    </p>
    <p class="title">{{ failure().title }}</p>
    <p class="detail">{{ detail() }}</p>
    <button type="button" class="ob-button-quiet" (click)="retry.emit()">{{ retryLabel() }}</button>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      gap: 10px;
      padding: 14px;
      align-items: flex-start;
    }

    p {
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

    .title {
      font-size: 14px;
      font-weight: 600;
    }

    .detail {
      font-size: 12.5px;
      line-height: 1.55;
      color: var(--ob-ink-2);
      text-wrap: pretty;
    }
  `,
})
export class PaneError {
  readonly failure = input.required<ApiFailure>();
  readonly request = input('');
  readonly consequence = input('');
  readonly retryLabel = input('Retry');

  readonly retry = output<void>();

  protected detail(): string {
    return this.failure().detail ?? this.consequence();
  }
}

@Component({
  selector: 'ob-pane-empty',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="title">{{ title() }}</p>
    <p class="body"><ng-content /></p>
    @if (caveat()) {
      <p class="caveat">{{ caveat() }}</p>
    }
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 16px 14px;
    }

    p {
      margin: 0;
    }

    .title {
      font-size: 13.5px;
      font-weight: 600;
    }

    .body {
      font-size: 12px;
      line-height: 1.55;
      color: var(--ob-ink-2);
      text-wrap: pretty;
    }

    .caveat {
      font-size: 11px;
      color: var(--ob-muted);
      line-height: 1.5;
      border-top: 1px solid var(--ob-rule-soft);
      padding-top: 8px;
    }
  `,
})
export class PaneEmpty {
  readonly title = input.required<string>();
  readonly caveat = input('');
}
