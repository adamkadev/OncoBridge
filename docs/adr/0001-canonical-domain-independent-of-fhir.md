# ADR-0001 — The canonical domain is independent of FHIR

**Status:** Accepted · **Phase:** 1

## Context

OncoBridge ingests FHIR R4 and measures it against mCODE STU4. The cheapest implementation would
make the internal model a thin wrapper over FHIR resources, or adopt mCODE profiles as the domain
model directly.

That choice would destroy the project's thesis. If the internal model is FHIR with different
names, then OncoBridge renders resources rather than normalizing them, and every claim it makes
about interoperability boundaries is unearned. The interesting engineering — a graph of four
sibling `Observation` resources becoming one staging aggregate — only exists if the two models are
genuinely different shapes.

## Decision

`OncoBridge.Domain` is written in OncoBridge's own vocabulary, with its own invariants and its own
cardinalities, and has **zero dependencies** — no NuGet packages and no project references.

Concretely:

- No domain type exposes, wraps, or is generated from a FHIR type.
- Domain names diverge from FHIR names where the concepts diverge (`PrimaryCancerDiagnosis`, not
  `Condition`), so the boundary is visible in the code rather than only in documentation.
- Provenance identity crosses into the domain as a plain identifier (`SourceResourceId`), never as
  a FHIR reference.
- `OncoBridge.Interop.Fhir` is the only production project permitted to reference `Hl7.Fhir.*`.

The constraint is enforced by `DomainBoundaryTests`, which reads the real project files and the
loaded assembly's reference graph. A boundary defended only by discipline is one that has already
been crossed.

## Consequences

- Mapping code must be written by hand rather than generated. This is accepted: the mapping *is*
  the product, and hand-written mappers are what make golden-file tests meaningful.
- The domain is deliberately lossy. Anything V1 does not model is recorded as a `CoverageNote`
  rather than silently carried.
- The domain can be unit-tested with no FHIR, database, or web dependency, which is why Phase 1
  could deliver 125 domain tests before any infrastructure exists.
- Adding a package reference to `OncoBridge.Domain` fails the build, not a review.
