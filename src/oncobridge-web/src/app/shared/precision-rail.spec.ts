import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { PrecisionRail } from './precision-rail';

describe('PrecisionRail', () => {
  let fixture: ComponentFixture<PrecisionRail>;

  beforeEach(() => {
    fixture = TestBed.createComponent(PrecisionRail);
  });

  async function render(precision: string): Promise<void> {
    fixture.componentRef.setInput('precision', precision);
    await fixture.whenStable();
  }

  function cells(): HTMLElement[] {
    return [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLElement>('.cell')];
  }

  function marked(): string[] {
    return cells()
      .filter((cell) => cell.classList.contains('marked'))
      .map((cell) => cell.textContent?.trim() ?? '');
  }

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it('always renders the same four categorical cells', async () => {
    for (const precision of ['Year', 'Month', 'Day', 'Instant']) {
      await render(precision);

      expect(cells().map((cell) => cell.textContent?.trim())).toEqual(['Y', 'M', 'D', 'I']);
    }
  });

  it('marks the year cell for a year', async () => {
    await render('Year');

    expect(marked()).toEqual(['Y']);
  });

  it('marks the month cell for a month', async () => {
    await render('Month');

    expect(marked()).toEqual(['M']);
  });

  it('marks the day cell for a day', async () => {
    await render('Day');

    expect(marked()).toEqual(['D']);
  });

  it('marks the instant cell for an instant', async () => {
    await render('Instant');

    expect(marked()).toEqual(['I']);
  });

  it('marks exactly one cell, never a cumulative fill', async () => {
    await render('Day');

    expect(marked()).toHaveLength(1);
  });

  it('marks no cell for a precision it does not know', async () => {
    await render('Fortnight');

    expect(marked()).toEqual([]);
    expect(cells()).toHaveLength(4);
  });

  it('writes the precision name beside the rail', async () => {
    await render('Instant');

    expect(text()).toContain('Instant');
  });

  it('names the precision for assistive technology and hides the rail from it', async () => {
    await render('Month');

    const rail = (fixture.nativeElement as HTMLElement).querySelector('.rail');

    expect(rail?.getAttribute('aria-hidden')).toBe('true');
    expect(text()).toContain('Precision: ');
  });

  it('keeps the rail geometry independent of any date value', async () => {
    await render('Year');
    const forYear = cells().map((cell) => cell.className);

    await render('Instant');
    const forInstant = cells().map((cell) => cell.className);

    expect(forYear).toHaveLength(forInstant.length);
    expect(forYear.filter((name) => name.includes('marked'))).toHaveLength(1);
    expect(forInstant.filter((name) => name.includes('marked'))).toHaveLength(1);
  });
});
