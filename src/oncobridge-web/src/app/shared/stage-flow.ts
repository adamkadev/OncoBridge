import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'ob-stage-flow',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ol>
      <li><span class="ob-pane-number">01</span><span class="ob-pane-label">Source</span></li>
      <li><span class="ob-pane-number">02</span><span class="ob-pane-label">Normalized</span></li>
      <li><span class="ob-pane-number">03</span><span class="ob-pane-label">Quality</span></li>
      <li><span class="ob-pane-number">04</span><span class="ob-pane-label">Provenance</span></li>
    </ol>
    <p class="note"><ng-content /></p>
  `,
  styles: `
    :host {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-wrap: wrap;
      padding: 0 16px;
      background: var(--ob-surface-3);
      border-bottom: 1px solid var(--ob-border);
    }

    ol {
      margin: 0;
      padding: 0;
      list-style: none;
      display: flex;
      align-items: center;
      flex-wrap: wrap;
    }

    li {
      display: flex;
      align-items: baseline;
      gap: 7px;
      padding: 8px 16px 8px 0;
    }

    li + li {
      padding-left: 16px;
    }

    li + li::before {
      content: '';
      width: 22px;
      height: 1px;
      background: #c3ccd1;
      margin-right: 16px;
      align-self: center;
    }

    .note {
      margin: 0 0 0 auto;
      font-size: 11px;
      color: var(--ob-muted);
    }

    @media (max-width: 900px) {
      .note {
        display: none;
      }
    }
  `,
})
export class StageFlow {}
