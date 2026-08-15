# ADR-0006 — PostgreSQL, with raw payloads stored as bytes and `jsonb` used only for queryable copies

**Status:** Accepted · **Phase:** 1 (decision) / P2 (implementation)

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

| | Purpose | Storage | Guarantee |
|---|---|---|---|
| `ImportBatch.RawPayload` | Audit record | `bytea` | Byte-for-byte identical to what was received |
| `ImportBatch.ContentHash` | Integrity proof | `text` | SHA-256 over exactly those bytes |
| Parsed resource JSON (P2) | Semantic querying | `jsonb` | Equivalent meaning; **explicitly not** byte-preserving |

The digest is computed inside `ImportBatch.Create` from the bytes themselves, so a caller cannot
supply one that does not match. `ContentHash.ComputeSha256` accepts only `ReadOnlySpan<byte>` —
there is no string or object overload, so the wrong thing cannot accidentally be hashed.

**The P2 gate therefore has two separate obligations**, and satisfying the second does not satisfy
the first:

- **(a)** exact byte round-trip of `RawPayload` — store, reload, recompute the digest, compare
- **(b)** semantic persistence of parsed resources in `jsonb` for querying

`SourceResource` deliberately carries **no** JSON field in Phase 1. Adding one before the
distinction is implemented would invite exactly the confusion this correction removes.

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

## Consequences

- `RawPayload` is held in the domain as `ReadOnlyMemory<byte>` and copied defensively on
  construction. An audit record a caller can mutate afterwards is not an audit record.
- Batches are not streamed in V1; a whole payload is held in memory. Acceptable for single-patient
  bundles, and a constraint to revisit if cohort-scale import is ever in scope.
- Any future export or re-serialization path must never overwrite `RawPayload`.
