import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ob-absent-value',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="ob-absent-text">Not stated</span>
    @for (checkId of relatedCheckIds(); track checkId) {
      <span class="ob-chip-dashed">see {{ checkId }}</span>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: baseline;
      gap: 10px;
      flex-wrap: wrap;
      font-size: 14px;
    }
  `,
})
export class AbsentValue {
  readonly relatedCheckIds = input<readonly string[]>([]);
}
