import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

@Component({
  selector: 'ob-id',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="id">{{ shown() }}</span>
    @if (truncated()) {
      <button type="button" class="reveal" (click)="revealed.set(!revealed())">
        {{ revealed() ? 'hide' : 'show' }}
      </button>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      min-width: 0;
    }

    .id {
      font-family: var(--ob-mono);
      font-size: 11.5px;
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
export class IdDisplay {
  readonly value = input.required<string>();
  readonly head = input(6);
  readonly tail = input(4);

  protected readonly revealed = signal(false);

  protected readonly truncated = computed(
    () => this.value().length > this.head() + this.tail() + 1,
  );

  protected readonly shown = computed(() => {
    const value = this.value();

    if (this.revealed() || !this.truncated()) {
      return value;
    }

    return `${value.slice(0, this.head())}…${value.slice(-this.tail())}`;
  });
}
