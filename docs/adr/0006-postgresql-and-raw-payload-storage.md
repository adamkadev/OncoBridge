# ADR-0006 — PostgreSQL, with raw payloads stored as bytes and `jsonb` used only for queryable copies

**Status:** Accepted · **Phase:** 1 (decision) / **P2 (implemented)**

> **Correction to the Phase 0 draft.** The original analysis argued for PostgreSQL partly on the
> grounds that `jsonb` could serve as the byte-for-byte immutable record of what was received.
> **That was incorrect and is retracted here.** The reasoning is kept rather than deleted because
> the mistake is instructive: `jsonb` is a *parsed* representation.

## Context

OncoBridge's provenance claim is that it can show exactly what it received. That claim only holds
if what is stored is what arrived.

PostgreSQL's `jsonb` decomposes JSON into a binary form. In doing so it may reorder object keys,
drop insignificant whitespace, normalize number formatting, and rewrite string escapes. The
document that comes back is semantically equivalent but **not byte-identical**, so:

- a digest computed over it will not match a digest computed over the received bytes, and
- it cannot support an audit statement about the original payload.

`jsonb` preserves *meaning*. Audit requires *bytes*. These are different obligations and the Phase 0
draft conflated them.

## Decision

**Database:** PostgreSQL, via Docker only, with EF Core and Npgsql. No local installation.

**Two distinct representations, never confused:**

| | Purpose | Column | Guarantee |
|---|---|---|---|
| `ImportBatch.RawPayload` | Audit record | `import_batch.raw_payload` `bytea` | Byte-for-byte identical to what was received |
| `ImportBatch.ContentHash` | Integrity proof | `import_batch.content_hash` | SHA-256 over exactly those bytes |
| `SourceResource.ContentHash` | Entry-level integrity | `source_resource.content_hash` | SHA-256 over the exact byte **slice** of that entry's `resource` value inside `RawPayload` |
| `SourceResource.ResourceJson` | Semantic querying | `source_resource.resource_json` `jsonb` | Equivalent meaning; **explicitly not** byte-preserving |

The digest is computed inside `ImportBatch.Create` from the bytes themselves, so a caller cannot
supply one that does not match. `ContentHash.ComputeSha256` accepts only `ReadOnlySpan<byte>` —
there is no string or object overload, so the wrong thing cannot accidentally be hashed.

**Naming.** The domain property is `SourceResource.ResourceJson` — it owns *a JSON representation of
the source resource*, and nothing more. Whether that representation is queryable, and how, is
Infrastructure's decision: it maps the property to a `jsonb` column. An earlier name, `QueryableJson`,
leaked that storage concern into the domain vocabulary and was corrected before the schema shipped.

**Entry-level hash semantics.** The hashed bytes are the verbatim contiguous range that entry's
`resource` value occupied in the received payload, obtained from the JSON reader's raw-text span and
never from a re-serialisation. This is asserted directly: each extracted fragment must be findable
as a literal byte subsequence of `RawPayload`. Entries carrying no `resource` at all have no
fragment and therefore no digest, so the column is nullable rather than filled with a fabricated
value.

**The P2 gate therefore had two separate obligations**, and satisfying the second does not satisfy
the first. Both are now met and tested:

- **(a)** exact byte round-trip of `RawPayload` — store, reload, compare against the fixture bytes
- **(b)** semantic persistence of parsed resources in `jsonb` for querying

The `jsonb` round trip is only ever compared **semantically**. A test asserting textual equality on
that column would be asserting something PostgreSQL never promised, and one test exists specifically
to demonstrate the gap: the reloaded Condition is semantically equivalent to the input, is *not*
textually equal to it, and hashes differently.

## Why PostgreSQL rather than SQL Server

The `jsonb` argument is weaker than the Phase 0 draft claimed, since `jsonb` is no longer doing
audit work — but it still holds for obligation (b), which is real. The decisive practical arguments
are unchanged: native arm64 container images so a reviewer on Apple Silicon can run
`docker compose up` unmodified, no edition matrix to explain in a public README, and a clean
`timestamptz` ↔ `DateTimeOffset` mapping.

**Stated honestly:** for a Microsoft-stack audience this is a genuine trade. The mitigation is
architectural — all raw-payload and JSON access stays behind a repository interface in
`OncoBridge.Infrastructure`, so no `jsonb` operator ever appears in `Application` or `Domain`. With
that discipline a provider swap is roughly a day's work; without it, a rewrite.

## Database constraints

Constraints enforce OncoBridge's own invariants and nothing the interchange format merely expects.
Defective FHIR must remain storable, because reporting on it is the point of the system.

Enforced: primary keys; `source_resource.batch_id` → `import_batch.id` (cascade); `lineage.source_resource_id`
→ `source_resource.id` (cascade); unique `(batch_id, entry_index)`; `raw_payload`, `content_hash`,
`source_system_label` and `received_at` not null.

Deliberately **not** enforced: uniqueness or presence of `full_url`, `source_logical_id` or
`resource_type`. `content_hash` is indexed but **not** unique — the same payload may legitimately be
imported twice as two separate batches, and deduplicating on digest would silently discard the second.

## Mapping details worth recording

- **`Lineage` has a shadow primary key.** It is a record, not an aggregate root, and carries no
  domain identity. Giving it one purely so the store has something to key on would be persistence
  leaking into the domain, so the key exists only in the EF model.
- **The design-time `DbContext` factory uses a placeholder connection string.** `dotnet ef` never
  opens a connection while scaffolding a migration, so this keeps migrations generatable without a
  live database. It is not configuration and is never used at run time.
- **Integration tests create a fresh database per test and apply the committed migration to it.**
  `EnsureCreated()` is never used anywhere: it builds a schema from the model and would bypass the
  very migration under test, so a broken migration would still pass.

## Consequences

- `RawPayload` is held in the domain as `ReadOnlyMemory<byte>` and copied defensively on
  construction. An audit record a caller can mutate afterwards is not an audit record.
- `ImportBatch.ReceivedAt` is normalised to UTC on write, because Npgsql rejects a non-zero offset
  for `timestamptz`. This is system time, where ADR-0005 treats the instant as the meaningful value,
  so nothing that matters is lost. Clinical time is never stored this way.
- Batches are not streamed in V1; a whole payload is held in memory. Acceptable for single-patient
  bundles, and a constraint to revisit if cohort-scale import is ever in scope.
- Any future export or re-serialization path must never overwrite `RawPayload`.
