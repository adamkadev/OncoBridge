import { mkdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import { chromium, devices } from '@playwright/test';

const webRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = join(webRoot, '..', '..');
const fixture = resolve(repoRoot, 'test-data/synthetic/phase4/bundle-acceptance-defects.json');
const shots = resolve(repoRoot, 'docs/screenshots');

const origin = process.env.ONCOBRIDGE_SCREENSHOT_ORIGIN ?? 'http://localhost:8080';
const desktop = { width: 1440, height: 960 };

mkdirSync(shots, { recursive: true });

const browser = await chromium.launch();
const problems = [];

function watch(page) {
  page.on('console', (message) => {
    if (message.type() === 'error') {
      problems.push(`console: ${message.text()}`);
    }
  });
  page.on('requestfailed', (request) => {
    problems.push(`request: ${request.method()} ${request.url()}`);
  });
}

async function importBundle(page) {
  await page.goto(`${origin}/`);
  await page.setInputFiles('#bundle-file', fixture);
  await page.getByRole('button', { name: 'Import FHIR Bundle' }).click();
  await page.waitForURL(/\/imports\/[0-9a-f-]{36}$/);

  return new URL(page.url()).pathname.split('/').pop();
}

async function settle(page) {
  await page.waitForFunction(() => !document.body.textContent.includes('Loading'));
  await page.evaluate(
    () => document.activeElement instanceof HTMLElement && document.activeElement.blur(),
  );
  await page.waitForTimeout(250);
}

async function clipTo(locator, width) {
  const box = await locator.boundingBox();

  return { x: 0, y: 0, width, height: Math.ceil(box.y + box.height + 12) };
}

const context = await browser.newContext({ viewport: desktop, deviceScaleFactor: 2 });
const page = await context.newPage();

watch(page);

const importBatchId = await importBundle(page);

await page.locator('ob-entity-selector .choice.selected').waitFor();
await page.locator('ob-inspector-header').getByRole('button', { name: 'show' }).click();
await page.getByTestId('payload-hash').waitFor();
await settle(page);

await page.screenshot({
  path: join(shots, 'inspector-evidence-ledger.png'),
  fullPage: true,
});

const patientId = new URL(page.url()).searchParams.get('patientId') ?? '';

await page.getByRole('link', { name: 'Timeline' }).click();
await page.waitForURL(/\/timeline/);
await settle(page);

await page.screenshot({
  path: join(shots, 'timeline.png'),
  fullPage: true,
  clip: await clipTo(page.locator('main'), desktop.width),
});

const mobileContext = await browser.newContext({
  ...devices['iPhone 14'],
  deviceScaleFactor: 2,
});
const mobile = await mobileContext.newPage();

watch(mobile);

await mobile.goto(`${origin}/imports/${importBatchId}/timeline?patientId=${patientId}`);
await settle(mobile);

await mobile.screenshot({
  path: join(shots, 'timeline-mobile.png'),
  fullPage: true,
  clip: await clipTo(mobile.locator('ob-timeline-event').nth(1), mobile.viewportSize().width),
});

const overflow = await page.evaluate(
  () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
);

await browser.close();

if (overflow) {
  problems.push('the desktop timeline overflows horizontally');
}

if (problems.length > 0) {
  console.error(`screenshots — refusing to publish, the application reported:`);
  for (const problem of problems) {
    console.error(`  ${problem}`);
  }
  process.exit(1);
}

console.log(`screenshots — wrote 3 images to docs/screenshots from ${origin}`);
