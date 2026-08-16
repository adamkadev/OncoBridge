# ADR-0003 — Source payloads are immutable; normalization is derived and re-runnable

**Status:** Accepted · **Phase:** 1 (model) / P2 (persistence)

## Context

OncoBridge's mappers will change — that is the nature of the work. If normalization were destructive,
every mapper change would require re-ingesting data that may no longer be available, and no finding
could be traced back to what actually arrived.

## Context boundary

This ADR covers *lifecycle*. How the bytes are physically stored, and why `jsonb` is not the audit
representation, is ADR-0006.

## Decision

Three tiers with distinct lifetimes:

| Tier | Lifetime | Rule |
|---|---|---|
| Source evidence on `ImportBatch` + `SourceResource` | Permanent | Written once at ingestion, never edited, never migrated, never "cleaned" |
| Normalization lifecycle metadata on `ImportBatch` | Mutable, rewritten by each run | Records *that* and *when* the batch was normalized, and by which pipeline version |
| Canonical domain entities | Derived | Safe to discard and rebuild from the source tier at any time |
| `Lineage` | Derived, rebuilt with normalization | Links a domain entity to the sources and named transformation that produced it |

**Evidence and lifecycle are different things, and P3D forced the distinction into the open.** An
earlier wording of this ADR called `ImportBatch` and `SourceResource` immutable without qualification.
That was too broad: re-normalization has to record that it happened, and the only sensible place for
that is the batch. The rule is therefore stated per-column rather than per-table.

| Immutable source evidence — never rewritten | Mutable lifecycle metadata — rewritten by each run |
|---|---|
| `ImportBatch.RawPayload` | `ImportBatch.Status` |
| `ImportBatch.ContentHash` | `ImportBatch.NormalizerVersion` |
| `ImportBatch.ReceivedAt`, `.SourceSystemLabel`, `.FileName`, `.BundleType`, `.EntryCount` | `ImportBatch.NormalizedAt` |
| Every `SourceResource` — its id, `EntryIndex`, `ResourceType`, `SourceLogicalId`, `FullUrl`, `ContentHash`, `ResourceJson` — and the count and order of them | |

The evidence rule is **not** weakened by this: re-running normalization must leave every cell in the
left column byte-identical, and `VerifyPayloadIntegrity()` must still hold afterwards. The transition
is a single explicit domain operation, `ImportBatch.MarkNormalized(normalizerVersion, normalizedAt)`,
with no public setters; a normalization instant earlier than `ReceivedAt` is rejected. It is
deliberately idempotent-friendly — re-normalizing an already-`Normalized` batch is legal and simply
overwrites the version and instant.

`NormalizerVersion` is the version of the **whole pipeline** and is distinct from the
`TransformationVersion` recorded on each `Lineage` row, which versions the single transformation that
produced that one entity. The two move independently and both are needed to interpret old output.

`Lineage` records `TransformationName` and `TransformationVersion` together, so lineage written by an
older mapper stays interpretable after the mapper changes.

**Lineage granularity is deliberate.** The default is entity-level: `FieldPath` is `null`, meaning
"this entity, wholly, from this source". Field-level records are emitted **only** where an entity
genuinely draws from more than one source resource. In V1 that is `CancerStaging` alone — stage group
from one resource, each axis category from another.

Recording a field-level row for every property would multiply storage, couple every mapper to a
lineage API, and produce a display nobody reads. Recording it only where several sources converge
means every field-level row present actually carries information — and the staging inspector showing
four lineage rows naming four distinct sources is the screen that demonstrates the whole design.

**FHIR `Provenance` is not the internal model.** It is a reasonable *export* projection later —
`entity.role` includes `derivation`, and the specification explicitly contemplates "an automaton
that transforms input", which is what a normalizer is. Adopting it internally would repeat exactly
the mistake ADR-0001 exists to prevent.

## Consequences

- Re-running normalization must delete and rewrite domain entities and their lineage, and must
  leave the source evidence untouched.
- Replacement must be atomic. Delete-then-insert across five canonical tables plus lineage plus the
  batch lifecycle update runs in one transaction, so a failure can never leave the derived tier
  half-rebuilt. Canonical rows carry the owning batch in persistence only (a shadow `batch_id`),
  so every delete is scoped to one batch and one batch's rerun cannot touch another's rows.
- Derived identity is source-derived, never random, which is what makes replacement idempotent:
  the same source tier normalized twice yields the same canonical ids.
- Combined with ADR-0004, re-normalization invalidates domain-consistency findings and must not
  touch conformance findings.
- Storage grows with every import, since nothing is ever pruned. Acceptable at V1 scale, and a
  constraint to revisit only if cohort-scale import is ever in scope.
