# Phase 0 corrections (Rev B)

Four corrections accepted at technical review before implementation began. The Phase 0 analysis
remains the architectural baseline in every other respect; it has been updated in place, and this
file records what changed and where each correction is now enforced.

---

## 1 — Raw source storage is bytes, not `jsonb`

**Wrong in the original draft.** It treated a `jsonb` column as the byte-for-byte immutable record
of what was received. `jsonb` is a *parsed* representation: it may reorder object keys, drop
insignificant whitespace, normalise number formatting and rewrite string escapes. It preserves
meaning, not bytes, so it cannot support an audit claim and cannot reproduce the original digest.

**Corrected to:**

- `ImportBatch.RawPayload` holds the exact uploaded bytes (PostgreSQL `bytea` when persistence lands).
- `ImportBatch.ContentHash` is SHA-256 over exactly those bytes.
- A parsed, queryable `jsonb` copy may be added to `SourceResource` in P2. It is a derived
  convenience and explicitly **not** the audit representation.
- The **P2 gate now has two separate obligations**: (a) exact byte round-trip of `RawPayload`;
  (b) semantic persistence of parsed resources in `jsonb`. Satisfying (b) does not satisfy (a).

**Where enforced now:** [ADR-0006](adr/0006-postgresql-and-raw-payload-storage.md); `ImportBatch`
computes its own digest from the bytes so a mismatched one cannot be supplied;
`ContentHash.ComputeSha256` accepts only `ReadOnlySpan<byte>`, with no string overload;
`SourceResource` deliberately carries no JSON field in Phase 1.
Tested by `ImportBatchTests` — including a case proving that a semantically identical but
differently encoded payload yields a different digest.

*No persistence was implemented in Phase 1.*

---

## 2 — "Three-valued comparison" was the wrong name

**Wrong in the original draft.** It listed four outcomes — `Before`, `After`, `Same`,
`Indeterminate` — while calling the model three-valued.

**Corrected to:** the type is `TemporalComparison`, and the model is described as a
**partial-order temporal comparison with an explicit indeterminate outcome**. Semantics are
unchanged: variable precision is preserved, unprovable relationships return `Indeterminate`, and no
result ever fabricates an ordering.

**Where enforced now:** [ADR-0005](adr/0005-variable-precision-temporal-model.md);
`TemporalComparison`; every occurrence in the Phase 0 document.

---

## 3 — `PartialPeriod` now exists

**Gap in the original draft.** It stated that V1 reads occurrence as either a date or a period, but
only specified `PartialDate`.

**Corrected to:** `PartialPeriod(Start: PartialDate?, End: PartialDate?)` — domain-native, no FHIR
types. It never collapses a period to its start, never fabricates a missing boundary, and each
boundary keeps its own precision.

Invariants, deliberately minimal:

1. At least one boundary must be stated.
2. When both are stated, only a **definite** contradiction is rejected — that is, only
   `Compare(End, Start) == Before`. `Same` is allowed (a zero-length period is meaningful) and
   `Indeterminate` is allowed, because an unprovable ordering is not a contradiction. Rejecting
   ambiguity would be exactly the fabrication this model exists to prevent.

`TemporalOccurrence` was added alongside it to hold the "either a date or a period, never both"
invariant in one place instead of repeating it on every entity that records an occurrence.

Period-to-period and period-to-date comparison are **not** implemented: no V1 concept needs them,
and the semantics should be settled by the rule that first requires them.

**Where enforced now:** [ADR-0005](adr/0005-variable-precision-temporal-model.md); `PartialPeriod`;
`TemporalOccurrence`; `PartialPeriodTests`, `TemporalOccurrenceTests`.

---

## 4 — mCODE export is a stretch goal

**Changed scope.** The old P7 bundled mandatory release work with optional export work, which put
V1 completion at the mercy of the least essential task.

**Corrected to:**

- **P7A (mandatory)** — README, ADRs, Docker Compose one-command run, screenshots, CI, cleanup.
  This is what V1 completion requires.
- **P7B (stretch)** — canonical domain → mCODE-shaped FHIR export, plus a FHIR `Provenance`
  projection. Valuable, but it must not delay a finished vertical slice.

**Where enforced now:** the phase table in the Phase 0 document. Neither was implemented in Phase 1.
