#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

origin="${ONCOBRIDGE_SMOKE_ORIGIN:-http://localhost:8080}"
fixture="test-data/synthetic/phase4/bundle-acceptance-defects.json"
missing_id="00000000-0000-0000-0000-000000000000"
keep_up="${ONCOBRIDGE_SMOKE_KEEP_UP:-0}"

cleanup() {
  if [ "$keep_up" = "1" ]; then
    echo "[smoke] leaving the stack up (ONCOBRIDGE_SMOKE_KEEP_UP=1)"
    return
  fi

  echo "[smoke] tearing the stack down"
  docker compose down -v --remove-orphans >/dev/null 2>&1 || true
}

trap cleanup EXIT

fail() {
  echo "[smoke] FAIL: $*" >&2
  echo "[smoke] --- compose state ---" >&2
  docker compose ps -a >&2 || true
  exit 1
}

poll_until() {
  local label="$1" url="$2" expected="$3" attempts="${4:-90}" seen=""

  for _ in $(seq 1 "$attempts"); do
    seen="$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 "$url" || echo 000)"

    if [ "$seen" = "$expected" ]; then
      echo "[smoke] $label ready"
      return 0
    fi

    sleep 1
  done

  fail "$label never returned $expected (last status $seen) at $url"
}

echo "[smoke] starting the stack"
docker compose down -v --remove-orphans >/dev/null 2>&1 || true
docker compose up --build -d

migrate_container="$(docker compose ps -aq migrate)"
[ -n "$migrate_container" ] || fail "the migrate service produced no container"

migrate_exit="$(docker wait "$migrate_container")"
[ "$migrate_exit" = "0" ] || fail "the migration step exited with $migrate_exit"
echo "[smoke] migrations applied"

poll_until "web root" "$origin/" 200
poll_until "api through the proxy" "$origin/api/v1/imports/$missing_id" 404

python3 - "$origin" "$fixture" <<'PY' || fail "the golden journey did not hold through the proxy"
import hashlib
import json
import sys
import urllib.error
import urllib.request

origin, fixture = sys.argv[1], sys.argv[2]


def call(method, path, body=None, content_type=None):
    request = urllib.request.Request(f'{origin}{path}', data=body, method=method)

    if content_type:
        request.add_header('Content-Type', content_type)

    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return response.status, response.read()
    except urllib.error.HTTPError as error:
        raise SystemExit(f'{method} {path} failed with {error.code}: {error.read()[:400]!r}')


def read_json(path):
    _, payload = call('GET', path)
    return json.loads(payload)


def expect(actual, wanted, what):
    if actual != wanted:
        raise SystemExit(f'{what}: expected {wanted!r}, got {actual!r}')


posted = open(fixture, 'rb').read()
status, payload = call(
    'POST',
    '/api/v1/imports?fileName=bundle-acceptance-defects.json',
    posted,
    'application/fhir+json',
)
expect(status, 201, 'import status')

import_id = json.loads(payload)['importBatchId']
print(f'[smoke] imported batch {import_id}')

batch = read_json(f'/api/v1/imports/{import_id}')

expect(
    batch['contentHash'].lower(),
    hashlib.sha256(posted).hexdigest(),
    'contentHash of the posted bytes (the proxy must not alter the uploaded body)',
)
expect(batch['entryCount'], 7, 'entryCount')
expect(len(batch['sourceResources']), 7, 'stored source resources')
expect(len(batch['patientIds']), 1, 'canonical patients')
print(f"[smoke] {len(posted)} bytes preserved through the proxy · sha256 {batch['contentHash'][:16]}…")

patient_id = batch['patientIds'][0]
record = read_json(f'/api/v1/patients/{patient_id}/record')
staging = record['cancerStagings'][0]

expect(staging['stageGroup']['code'], 'IIA', 'stage group')
expect([c['code']['code'] for c in staging['categories']], ['T2', 'N1', 'M0'], 'stage categories')

findings = read_json(f'/api/v1/imports/{import_id}/findings')
expect(
    sorted(finding['checkId'] for finding in findings),
    ['OB-CONF-001', 'OB-CONF-002', 'OB-REF-001'],
    'quality findings',
)

provenance = read_json(f"/api/v1/domain/{staging['id']}/provenance")
expect(len(provenance['records']), 4, 'lineage records for the staging aggregate')

timeline = read_json(f'/api/v1/patients/{patient_id}/timeline')
expect(len(timeline['groups']), 3, 'timeline groups')
expect(timeline['unsequencedEvents'], [], 'unsequenced events')

status, shell = call('GET', f'/imports/{import_id}?patientId={patient_id}')
expect(status, 200, 'SPA deep link status')

if b'<ob-root>' not in shell:
    raise SystemExit('the SPA deep link did not fall back to index.html')

print('[smoke] staging aggregate, findings, 4 lineage records, timeline and deep link all verified')
PY

echo "[smoke] PASS"
