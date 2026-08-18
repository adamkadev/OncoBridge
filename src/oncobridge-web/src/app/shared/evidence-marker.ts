import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ob-evidence-marker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="marker" [class.active]="active()" [attr.data-marker]="marker()"
    ><span class="visually-hidden">Evidence </span>{{ marker() }}</span
  >`,
  styles: `
    .marker {
      font-family: var(--ob-mono);
      font-size: 11px;
      font-weight: 700;
      color: var(--ob-ink-2);
      background: var(--ob-marker-resting);
      border: 1px solid var(--ob-border-2);
      width: 18px;
      height: 18px;
      flex: none;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      border-radius: 2px;
    }

    .marker.active {
      color: #fff;
      background: var(--ob-accent);
      border-color: var(--ob-accent);
    }

    .visually-hidden {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
      clip-path: inset(50%);
      white-space: nowrap;
    }
  `,
})
export class EvidenceMarker {
  readonly marker = input.required<string>();
  readonly active = input(false);
}
