import { spawn, spawnSync } from 'node:child_process';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { createServer } from 'node:net';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = join(webRoot, '..', '..');

const container = 'oncobridge-e2e';
const postgresImage = 'postgres:18.6';

const children = [];
const workspace = mkdtempSync(join(tmpdir(), 'oncobridge-e2e-'));
let containerStarted = false;

function run(command, args, options = {}) {
  const result = spawnSync(command, args, { stdio: 'inherit', ...options });

  if (result.status !== 0) {
    throw new Error(`${command} ${args.join(' ')} exited with ${result.status}`);
  }
}

function capture(command, args) {
  const result = spawnSync(command, args, { encoding: 'utf8' });

  if (result.status !== 0) {
    throw new Error(`${command} ${args.join(' ')} exited with ${result.status}`);
  }

  return result.stdout.trim();
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

function freePort() {
  return new Promise((resolve, reject) => {
    const probe = createServer();

    probe.unref();
    probe.on('error', reject);
    probe.listen(0, '127.0.0.1', () => {
      const { port } = probe.address();

      probe.close(() => resolve(port));
    });
  });
}

function publishedPort(containerPort) {
  const mapping = capture('docker', ['port', container, String(containerPort)]).split('\n')[0];
  const port = mapping.match(/:(\d+)$/)?.[1];

  if (!port) {
    throw new Error(`docker published no host port for ${containerPort}, reported '${mapping}'`);
  }

  return Number(port);
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

  rmSync(workspace, { recursive: true, force: true });
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
    '127.0.0.1::5432',
    postgresImage,
  ]);
  containerStarted = true;

  const postgresPort = publishedPort(5432);
  const apiPort = await freePort();
  const webPort = await freePort();

  const apiOrigin = `http://127.0.0.1:${apiPort}`;
  const webOrigin = `http://127.0.0.1:${webPort}`;
  const connectionString = `Host=127.0.0.1;Port=${postgresPort};Database=oncobridge;Username=oncobridge;Password=oncobridge`;

  console.log(`[harness] postgres ${postgresPort} · api ${apiPort} · web ${webPort}`);

  const proxyConfig = join(workspace, 'proxy.conf.json');

  writeFileSync(
    proxyConfig,
    JSON.stringify({ '/api': { target: apiOrigin, secure: false, changeOrigin: false } }),
  );

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
  start(
    'web',
    'npx',
    [
      'ng',
      'serve',
      '--host',
      '127.0.0.1',
      '--port',
      String(webPort),
      '--proxy-config',
      proxyConfig,
    ],
    { cwd: webRoot },
  );

  await waitFor('Angular', () => reachable(webOrigin));

  console.log('[harness] running Playwright');
  run('npx', ['playwright', 'test'], {
    cwd: webRoot,
    env: { ...process.env, ONCOBRIDGE_WEB_ORIGIN: webOrigin },
  });
} catch (error) {
  console.error(`[harness] ${error instanceof Error ? error.message : error}`);
  exitCode = 1;
} finally {
  cleanUp();
}

process.exit(exitCode);
