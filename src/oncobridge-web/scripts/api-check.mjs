import { spawnSync } from 'node:child_process';
import { mkdtempSync, readFileSync, readdirSync, rmSync, statSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, posix, relative, sep } from 'node:path';

const committed = 'src/app/api';
const temp = mkdtempSync(join(tmpdir(), 'oncobridge-api-check-'));

function walk(root) {
  const files = new Map();

  const visit = (directory) => {
    for (const entry of readdirSync(directory)) {
      const absolute = join(directory, entry);

      if (statSync(absolute).isDirectory()) {
        visit(absolute);
      } else {
        files.set(
          relative(root, absolute).split(sep).join(posix.sep),
          readFileSync(absolute, 'utf8'),
        );
      }
    }
  };

  visit(root);

  return files;
}

try {
  const generation = spawnSync('npx', ['ng-openapi-gen', '--output', temp], {
    stdio: ['ignore', 'ignore', 'inherit'],
    shell: process.platform === 'win32',
  });

  if (generation.status !== 0) {
    console.error('api:check — the API client could not be regenerated from the OpenAPI snapshot.');
    process.exit(1);
  }

  const expected = walk(temp);
  const actual = walk(committed);

  const missing = [...expected.keys()].filter((file) => !actual.has(file));
  const extra = [...actual.keys()].filter((file) => !expected.has(file));
  const changed = [...expected.keys()].filter(
    (file) => actual.has(file) && actual.get(file) !== expected.get(file),
  );

  if (missing.length === 0 && extra.length === 0 && changed.length === 0) {
    console.log(
      `api:check — ${expected.size} generated files match the committed OpenAPI contract.`,
    );
    process.exit(0);
  }

  console.error('api:check — the committed API client has drifted from the OpenAPI snapshot.');

  for (const file of missing) {
    console.error(`  missing from ${committed}: ${file}`);
  }

  for (const file of extra) {
    console.error(`  not produced by the generator: ${file}`);
  }

  for (const file of changed) {
    console.error(`  differs from the generated output: ${file}`);
  }

  console.error(
    'Run `npm run api:generate` and commit the result; never hand-edit generated files.',
  );
  process.exit(1);
} finally {
  rmSync(temp, { recursive: true, force: true });
}
