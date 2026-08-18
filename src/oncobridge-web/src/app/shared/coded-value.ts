import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { CodedConceptResponse } from '../api';

@Component({
  selector: 'ob-coded-value',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="primary" [style.font-size.px]="size()">{{ primary() }}</span>
    <span class="secondary">{{ secondary() }}</span>
  `,
  styles: `
    :host {
      display: flex;
      align-items: baseline;
      gap: 10px;
      flex-wrap: wrap;
      min-width: 0;
    }

    .primary {
      font-weight: 600;
    }

    .secondary {
      font-family: var(--ob-mono);
      font-size: 10.5px;
      color: var(--ob-faint);
      word-break: break-all;
    }
  `,
})
export class CodedValue {
  readonly concept = input.required<CodedConceptResponse>();
  readonly size = input(15);

  protected readonly primary = computed(() => {
    const concept = this.concept();

    return concept.display ?? concept.code;
  });

  protected readonly secondary = computed(() => {
    const concept = this.concept();

    return `${shortSystem(concept.system)} | ${concept.code}`;
  });
}

function shortSystem(system: string): string {
  return system.replace(/^https?:\/\//, '').replace(/\/$/, '');
}
