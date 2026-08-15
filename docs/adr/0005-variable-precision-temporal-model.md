# ADR-0005 — Variable-precision temporal model with an explicit indeterminate outcome

**Status:** Accepted · **Phase:** 1

> **Terminology correction.** An earlier Phase 0 draft called this a "three-valued comparison"
> while listing four outcomes. That was simply wrong arithmetic in the naming. The model is a
> **partial-order temporal comparison with an explicit indeterminate outcome**, and the type is
> named `TemporalComparison`. The semantics were never in question; only the label was.

> **Offset-range correction.** This ADR previously gave the legal UTC offset range as
> −12:00 … +14:00. The lower bound was wrong: FHIR inherits the XML Schema range, which is
> −14:00 … +14:00. `PartialDate` widens by ±14:00 and is covered by boundary tests; only this
> document was out of step.

## Context

FHIR `date` and `dateTime` are variable precision: `2019`, `2019-03`, `2019-03-14` and
`2019-03-14T10:00:00+02:00` are all valid, and only the last carries a timezone. Mapping any of
these onto `DateTime` fabricates precision the source never asserted — `2019` silently becomes
`2019-01-01T00:00:00` — after which every comparison is confidently wrong. This is the single most
common defect in naive FHIR integrations.

Phase 0 also specified that V1 reads occurrence stated as either a point or a period, but only
specified the point type. Periods needed a domain-native representation before any mapping work
could begin.

## Decision

**`PartialDate`** records the value together with the `DatePrecision` at which it was stated
(`Year`, `Month`, `Day`, `Instant`). Precision is never inferred and never widened. An instant
retains its stated UTC offset rather than being normalized.

**`TemporalComparison`** has four outcomes — `Before`, `After`, `Same`, `Indeterminate`. Comparison
treats every value as the closed interval it denotes:

- `a.End < b.Start` → `Before`
- `b.End < a.Start` → `After`
- identical intervals → `Same`
- overlapping but not identical → `Indeterminate`

`Indeterminate` is a correct answer, not a failure to compute one. `2019` versus `2019-03` overlaps,
so no ordering exists and none may be invented.

**Mixed floating and instant values.** Year, month and day carry no timezone, so they cannot be
placed on the UTC timeline exactly. When one side is an instant and the other is not, the floating
side is widened by the full range of legal UTC offsets (−14:00 … +14:00) before comparing, so a
definite result is returned only when it holds for *every* offset the value could have had.
Comparing two floating values needs no widening: both sit on the same calendar timeline and the
unknown offset cancels.

**Two notions of sameness, kept apart.** `Equals` is representational — was this written the same
way — so `2019-03-14T10:00:00+02:00` and `2019-03-14T08:00:00+00:00` are *not* equal. `Compare` is
temporal, and reports those same two values as `Same`. Both answer real questions, and conflating
them is the bug this separation prevents.

**`PartialPeriod`** holds `Start` and `End`, each an optional `PartialDate`. Its invariants:

1. At least one boundary must be stated; a period with neither asserts nothing.
2. When both are stated, only a **definitely** contradictory ordering is rejected — that is, only
   `Compare(End, Start) == Before`. `Same` is allowed (a zero-length period is meaningful) and
   `Indeterminate` is allowed, because an unprovable ordering is not a contradiction. Rejecting
   ambiguity here would be exactly the fabrication this model exists to prevent.

A period is never collapsed to its start, and a missing boundary is never filled in.

**`TemporalOccurrence`** holds either a `PartialDate` or a `PartialPeriod`, never both and never
neither. It exists so the exactly-one-of invariant lives in one place rather than being repeated on
every entity that records an occurrence.

## Consequences

- Every rule consuming a comparison must handle four outcomes explicitly. The planned `OB-DOM-001`
  check fires only on `Before` and never on `Indeterminate`.
- Comparison is a partial order, not a total one, so `PartialDate` cannot implement `IComparable`
  and values cannot be naively sorted. Timeline ordering in a later phase must decide its own
  tie-breaking policy and state it.
- Period-to-period and period-to-date comparison are deliberately **not** implemented. No V1
  concept needs them, and the semantics (containment versus overlap versus ordering) should be
  settled by the rule that first requires them.
- Persistence must store precision alongside the value; a bare timestamp column would discard the
  distinction this ADR exists to preserve.
- Interval ends are computed by tick arithmetic rather than by adding a unit and stepping back,
  because adding a year to 9999 overflows `DateTime`. A boundary test caught this during Phase 1.
