import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { ApiFailure } from '../core/async';
import { asNumber } from '../core/api-values';
import { FindingView } from '../core/findings';
import { EvidenceMarker } from '../shared/evidence-marker';
import { Pane } from '../shared/pane';
import { PaneEmpty, PaneError, PaneLoading } from '../shared/pane-state';
import { SeverityBadge } from '../shared/severity-badge';

@Component({
  selector: 'ob-quality-pane',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Pane, PaneLoading, PaneError, PaneEmpty, SeverityBadge, EvidenceMarker],
  template: `
    <ob-pane number="03" label="Quality">
      <span pane-meta>{{ meta() }}</span>

      @if (loadingMessage()) {
        <ob-pane-loading [message]="loadingMessage()!" />
      } @else if (failure()) {
        <ob-pane-error
          [failure]="failure()!"
          [request]="request()"
          consequence="The quality findings are unavailable for this import. This is not the same as an import with no findings — nothing can be concluded about quality from this state."
          retryLabel="Retry findings"
          (retry)="retry.emit()"
        />
      } @else if (views().length === 0) {
        <ob-pane-empty
          title="No findings for this import"
          caveat="This is not full mCODE profile validation, and no statement is made about parts of the specification OncoBridge does not check."
        >
          The OncoBridge conformance checks — a subset of mCODE STU4 — raised nothing on this batch.
        </ob-pane-empty>
      } @else {
        <ul class="findings">
          @for (view of views(); track view.finding.checkId + view.finding.target.id) {
            <li class="finding" [class.related]="view.relatedToSelection">
              <span class="bar" [attr.data-severity]="view.finding.severity" aria-hidden="true"></span>
              <div class="body">
                <div class="head">
                  <span class="ob-mono check">{{ view.finding.checkId }}</span>
                  <ob-severity-badge [severity]="view.finding.severity" />
                  <span class="ob-chip-outline">{{ categoryLabel(view.finding.category) }}</span>
                  @if (view.relatedToSelection) {
                    <span class="related-tag">
                      @if (view.marker) {
                        <ob-evidence-marker [marker]="view.marker" [active]="true" />
                      }
                      Targets this selection
                    </span>
                  }
                </div>

                <p class="message">{{ view.finding.message }}</p>

                <dl class="evidence">
                  <dt>Target</dt>
                  <dd class="ob-mono">{{ targetLabel(view) }}</dd>

                  @if (view.finding.expected) {
                    <dt>Expected</dt>
                    <dd class="ob-mono break">{{ view.finding.expected }}</dd>
                  }

                  @if (view.finding.actual) {
                    <dt>Actual</dt>
                    <dd class="ob-mono break">{{ view.finding.actual }}</dd>
                  }

                  <dt>Citation</dt>
                  <dd>
                    @if (isExternal(view.finding.citation)) {
                      <a
                        class="ob-mono citation"
                        [href]="view.finding.citation"
                        target="_blank"
                        rel="noopener noreferrer"
                        >{{ citationLabel(view.finding.citation) }}</a
                      >
                    } @else {
                      <span class="ob-mono">{{ view.finding.citation }}</span>
                    }
                  </dd>
                </dl>
              </div>
            </li>
          }
        </ul>
      }

      <p pane-footnote class="ob-footnote">
        All findings for the import are listed; selection is an annotation, never a filter.
      </p>
    </ob-pane>
  `,
  styles: `
    .findings {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .finding {
      display: flex;
      border-bottom: 1px solid var(--ob-rule-soft);
    }

    .finding.related {
      background: var(--ob-accent-wash-2);
    }

    .bar {
      width: 3px;
      flex: none;
      background: var(--ob-error);
    }

    .bar[data-severity='Warning'] {
      background: var(--ob-warning);
    }

    .bar[data-severity='Information'] {
      background: var(--ob-info);
    }

    .body {
      flex: 1;
      min-width: 0;
      padding: 10px 12px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .head {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
    }

    .check {
      font-size: 12px;
      font-weight: 650;
    }

    .related-tag {
      margin-left: auto;
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-size: 10px;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: var(--ob-accent-ink);
      white-space: nowrap;
    }

    .message {
      margin: 0;
      font-size: 12.5px;
      line-height: 1.5;
      text-wrap: pretty;
    }

    .evidence {
      margin: 0;
      display: grid;
      grid-template-columns: 66px 1fr;
      gap: 3px 10px;
      font-size: 11px;
      line-height: 1.5;
    }

    dt {
      color: var(--ob-faint);
      text-transform: uppercase;
      font-size: 9.5px;
      letter-spacing: 0.07em;
      padding-top: 2px;
    }

    dd {
      margin: 0;
      color: var(--ob-ink-2);
      min-width: 0;
      overflow-wrap: anywhere;
    }

    dd.break {
      word-break: break-word;
    }

    .citation {
      font-size: 11px;
    }

    @media (max-width: 900px) {
      .evidence {
        grid-template-columns: 1fr;
        gap: 1px 0;
      }

      .related-tag {
        margin-left: 0;
      }
    }
  `,
})
export class QualityPane {
  readonly views = input.required<readonly FindingView[]>();
  readonly loadingMessage = input<string | null>(null);
  readonly failure = input<ApiFailure | null>(null);
  readonly request = input('');

  readonly retry = output<void>();

  protected readonly meta = computed(() => {
    const views = this.views();
    const count = (severity: string) =>
      views.filter((view) => view.finding.severity === severity).length;

    return `${views.length} ${views.length === 1 ? 'finding' : 'findings'} for this import · ${count('Error')} Error · ${count('Warning')} Warning · ${count('Information')} Information`;
  });

  protected categoryLabel(category: string): string {
    return category.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
  }

  protected targetLabel(view: FindingView): string {
    const target = view.finding.target;
    const source = view.targetSource;

    if (!source) {
      return `${target.kind}${target.domainEntityType ? ' · ' + target.domainEntityType : ''} · ${target.id}`;
    }

    const parts = [
      target.kind,
      source.resourceType ?? 'Unknown type',
      source.sourceLogicalId ?? target.id,
      `entry ${asNumber(source.entryIndex)}`,
    ];

    return parts.join(' · ');
  }

  protected isExternal(citation: string): boolean {
    return citation.startsWith('http://') || citation.startsWith('https://');
  }

  protected citationLabel(citation: string): string {
    return citation.replace(/^https?:\/\//, '');
  }
}
