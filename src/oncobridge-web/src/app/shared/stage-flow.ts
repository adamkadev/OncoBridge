import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Params, RouterLink } from '@angular/router';

const STAGES: readonly { readonly number: string; readonly label: string }[] = [
  { number: '01', label: 'Source' },
  { number: '02', label: 'Normalized' },
  { number: '03', label: 'Quality' },
  { number: '04', label: 'Provenance' },
];

@Component({
  selector: 'ob-stage-flow',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <nav aria-label="Import views">
      <ol>
        @for (stage of stages; track stage.number) {
          <li>
            @if (inspectorCommands(); as commands) {
              <a [routerLink]="commands" [queryParams]="queryParams()">
                <span class="ob-pane-number">{{ stage.number }}</span>
                <span class="ob-pane-label">{{ stage.label }}</span>
              </a>
            } @else {
              <span class="here">
                <span class="ob-pane-number">{{ stage.number }}</span>
                <span class="ob-pane-label">{{ stage.label }}</span>
              </span>
            }
          </li>
        }

        <li class="timeline">
          @if (timelineCommands(); as commands) {
            <a [routerLink]="commands" [queryParams]="queryParams()">
              <span class="ob-pane-label">Timeline</span>
            </a>
          } @else {
            <span class="here current" aria-current="page">
              <span class="ob-pane-label">Timeline</span>
            </span>
          }
        </li>
      </ol>
    </nav>

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

    nav {
      min-width: 0;
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
      align-items: center;
    }

    a,
    .here {
      display: flex;
      align-items: baseline;
      gap: 7px;
      padding: 8px 16px 8px 0;
      border-bottom: 0;
      text-decoration: none;
      color: inherit;
    }

    a:hover .ob-pane-label {
      color: var(--ob-accent-ink);
    }

    li + li a,
    li + li .here {
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

    li.timeline {
      border-left: 1px solid var(--ob-border-2);
    }

    li.timeline::before {
      display: none;
    }

    li.timeline a,
    li.timeline .here {
      padding-left: 16px;
      padding-right: 16px;
    }

    .current {
      box-shadow: inset 0 -2px 0 var(--ob-accent);
    }

    .current .ob-pane-label {
      color: var(--ob-accent-ink);
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

    @media (max-width: 640px) {
      a,
      .here {
        min-height: 44px;
        align-items: center;
      }
    }
  `,
})
export class StageFlow {
  readonly inspectorCommands = input<readonly unknown[] | null>(null);
  readonly timelineCommands = input<readonly unknown[] | null>(null);
  readonly queryParams = input<Params | null>(null);

  protected readonly stages = STAGES;
}
