import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'ob-standards-note',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `OncoBridge conformance checks — a subset of mCODE STU4. Not full mCODE profile
    validation.`,
  styles: `
    :host {
      display: block;
      padding: 6px 16px;
      background: var(--ob-surface-2);
      border-bottom: 1px solid var(--ob-rule);
      font-size: 11px;
      color: var(--ob-muted);
    }
  `,
})
export class StandardsNote {}
