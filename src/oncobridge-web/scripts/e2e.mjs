import { spawn, spawnSync } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = join(webRoot, '..', '..');

const container = 'oncobridge-e2e';
const postgresImage = 'postgres:18.6';
const postgresPort = 55433;
const apiOrigin = 'http://127.0.0.1:5080';
const webOrigin = 'http://127.0.0.1:4200';
const connectionString = `Host=127.0.0.1;Port=${postgresPort};Database=oncobridge;Username=oncobridge;Password=oncobridge`;

const children = [];
let containerStarted = false;

function run(command, args, options = {}) {
  const result = spawnSync(command, args, { stdio: 'inherit', ...options });

  if (result.status !== 0) {
    throw new Error(`${command} ${args.join(' ')} exited with ${result.status}`);
  }
}

function start(name, command, args, options = {}) {
  const child = spawn(command, args, { stdio: ['ignore', 'pipe', 'pipe'], ...options });

  child.stdout.on('data', (chunk) => process.stdout.write(`[${name}] ${chunk}`));
  child.stderr.on('data', (chunk) => process.stderr.write(`[${name}] ${chunk}`));
  child.on('exit', (code) => {
    if (code !== 0 && code !== null) {
      console.error(`[${name}] exited with ${code}`);
    }
  });

  children.push(child);

  return child;
}

async function waitFor(label, check, attempts = 90) {
  for (let attempt = 1; attempt <= attempts; attempt++) {
    if (await check()) {
      console.log(`[harness] ${label} ready after ${attempt}s`);

      return;
    }

    await new Promise((resolve) => setTimeout(resolve, 1000));
  }

  throw new Error(`${label} did not become ready`);
}

async function reachable(url) {
  try {
    const response = await fetch(url, { redirect: 'manual' });

    return response.status > 0;
  } catch {
    return false;
  }
}

function postgresReady() {
  return (
    spawnSync('docker', ['exec', container, 'pg_isready', '-U', 'oncobridge'], {
      stdio: 'ignore',
    }).status === 0
  );
}

function cleanUp() {
  for (const child of children) {
    if (child.exitCode === null) {
      child.kill('SIGTERM');
    }
  }

  if (containerStarted) {
    spawnSync('docker', ['rm', '-f', container], { stdio: 'ignore' });
    console.log('[harness] PostgreSQL container removed');
  }
}

let exitCode = 0;

try {
  spawnSync('docker', ['rm', '-f', container], { stdio: 'ignore' });

  console.log('[harness] starting PostgreSQL');
  run('docker', [
    'run',
    '-d',
    '--name',
    container,
    '-e',
    'POSTGRES_DB=oncobridge',
    '-e',
    'POSTGRES_USER=oncobridge',
    '-e',
    'POSTGRES_PASSWORD=oncobridge',
    '-p',
    `${postgresPort}:5432`,
    postgresImage,
  ]);
  containerStarted = true;

  await waitFor('PostgreSQL', async () => postgresReady(), 60);

  console.log('[harness] applying EF migrations');
  run(
    'dotnet',
    [
      'ef',
      'database',
      'update',
      '--project',
      'src/OncoBridge.Infrastructure',
      '--startup-project',
      'src/OncoBridge.Infrastructure',
    ],
    { cwd: repoRoot, env: { ...process.env, ONCOBRIDGE_DESIGN_TIME_CONNECTION: connectionString } },
  );

  console.log('[harness] starting the API');
  start('api', 'dotnet', ['run', '--project', 'src/OncoBridge.Api', '--no-launch-profile'], {
    cwd: repoRoot,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: apiOrigin,
      ConnectionStrings__OncoBridge: connectionString,
    },
  });

  await waitFor('API', () => reachable(`${apiOrigin}/openapi/v1.json`));

  console.log('[harness] starting Angular');
  start('web', 'npx', ['ng', 'serve', '--host', '127.0.0.1', '--port', '4200'], { cwd: webRoot });

  await waitFor('Angular', () => reachable(webOrigin));

  console.log('[harness] running Playwright');
  run('npx', ['playwright', 'test'], { cwd: webRoot });
} catch (error) {
  console.error(`[harness] ${error instanceof Error ? error.message : error}`);
  exitCode = 1;
} finally {
  cleanUp();
}

process.exit(exitCode);
