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
| `ImportBatch` + `SourceResource` | Permanent | Written once at ingestion, never edited, never migrated, never "cleaned" |
| Canonical domain entities | Derived | Safe to discard and rebuild from the source tier at any time |
| `Lineage` | Derived, rebuilt with normalization | Links a domain entity to the sources and named transformation that produced it |

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
  leave the source tier untouched.
- Combined with ADR-0004, re-normalization invalidates domain-consistency findings and must not
  touch conformance findings.
- Storage grows with every import, since nothing is ever pruned. Acceptable at V1 scale, and a
  constraint to revisit only if cohort-scale import is ever in scope.
