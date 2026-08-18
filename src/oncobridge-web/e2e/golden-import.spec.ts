import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import { expect, test } from '@playwright/test';

const repoRoot = resolve(process.cwd(), '..', '..');
const fixture = resolve(repoRoot, 'test-data/synthetic/phase4/bundle-acceptance-defects.json');

test('imports the acceptance bundle and inspects its evidence ledger', async ({ page }) => {
  const consoleErrors: string[] = [];
  const failedRequests: string[] = [];

  page.on('console', (message) => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('requestfailed', (request) => {
    failedRequests.push(`${request.method()} ${request.url()}`);
  });

  await page.goto('/');

  await expect(page.getByRole('heading', { level: 1, name: 'OncoBridge' })).toBeVisible();

  await page.setInputFiles('#bundle-file', fixture);

  await expect(page.getByText('bundle-acceptance-defects.json')).toBeVisible();

  await page.getByRole('button', { name: 'Import FHIR Bundle' }).click();

  await page.waitForURL(
    /\/imports\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/,
  );

  const header = page.locator('ob-inspector-header');

  await expect(header).toContainText('Normalized');
  await expect(header).toContainText('7 · collection');

  await header.getByRole('button', { name: 'show' }).click();

  const expectedHash = createHash('sha256').update(readFileSync(fixture)).digest('hex');

  await expect(page.getByTestId('payload-hash')).toHaveText(expectedHash);

  const selected = page.locator('ob-entity-selector .choice.selected');

  await expect(selected).toHaveCount(1);
  await expect(selected).toContainText('Cancer staging');
  await expect(selected).toContainText('Stage IIA');

  const normalized = page.locator('ob-normalized-pane');

  await expect(normalized).toContainText('Stage IIA');
  await expect(normalized).toContainText('T2');
  await expect(normalized).toContainText('N1');
  await expect(normalized).toContainText('M0');
  await expect(normalized).toContainText('2019-04-02');
  await expect(normalized).toContainText('Method');
  await expect(normalized).toContainText('Not stated');

  const sourceRows = page.locator('ob-source-pane .row');

  await expect(sourceRows).toHaveCount(4);

  for (const [index, marker] of ['A', 'B', 'C', 'D'].entries()) {
    await expect(sourceRows.nth(index).locator('ob-evidence-marker')).toHaveText(
      `Evidence ${marker}`,
    );
  }

  await expect(sourceRows.nth(0)).toContainText('staging-group-001');
  await expect(sourceRows.nth(1)).toContainText('staging-t-001');
  await expect(sourceRows.nth(2)).toContainText('staging-n-001');
  await expect(sourceRows.nth(3)).toContainText('staging-m-001');

  const findings = page.locator('ob-quality-pane .finding');

  await expect(findings).toHaveCount(3);
  await expect(findings.locator('.check')).toHaveText([
    'OB-CONF-001',
    'OB-CONF-002',
    'OB-REF-001',
  ]);
  await expect(findings.locator('ob-severity-badge')).toHaveText(['Error', 'Error', 'Error']);

  const quality = page.locator('ob-quality-pane');

  for (const absent of ['OB-STR-001', 'OB-REF-002', 'OB-DOM-001']) {
    await expect(quality).not.toContainText(absent);
  }

  const lineage = page.locator('ob-provenance-pane tbody tr');

  await expect(lineage).toHaveCount(4);
  await expect(lineage.locator('.scope')).toHaveText([
    'Whole entity',
    'Category T',
    'Category N',
    'Category M',
  ]);

  for (let row = 0; row < 4; row++) {
    await expect(lineage.nth(row)).toContainText('FhirCancerStagingNormalization');
    await expect(lineage.nth(row)).toContainText('1.0.0');
  }

  expect(consoleErrors).toEqual([]);
  expect(failedRequests).toEqual([]);
});
