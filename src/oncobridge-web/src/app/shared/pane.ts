import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ob-pane',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="head">
      <h2 class="ob-pane-label">
        <span class="ob-pane-number">{{ number() }}</span>
        {{ label() }}
      </h2>
      <p class="ob-meta"><ng-content select="[pane-meta]" /></p>
    </div>
    <div class="body">
      <ng-content />
    </div>
    <ng-content select="[pane-footnote]" />
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      min-width: 0;
      border: 1px solid var(--ob-border);
      border-radius: 3px;
      background: var(--ob-surface);
    }

    .head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 8px 12px;
      border-bottom: 1px solid var(--ob-rule);
      background: var(--ob-surface-2);
      min-width: 0;
      flex-wrap: wrap;
    }

    h2 {
      margin: 0;
      display: flex;
      align-items: baseline;
      gap: 8px;
    }

    p {
      margin: 0;
      text-align: right;
      min-width: 0;
      overflow-wrap: anywhere;
    }

    .body {
      display: flex;
      flex-direction: column;
      min-width: 0;
      flex: 1;
    }
  `,
})
export class Pane {
  readonly number = input.required<string>();
  readonly label = input.required<string>();
}
