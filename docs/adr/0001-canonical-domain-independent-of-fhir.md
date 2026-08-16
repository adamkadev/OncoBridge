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

## Identifier strategy

An identifier is strongly typed when it is **passed as a parameter or stored as a foreign
reference**, because that is where a bare `Guid` would let two unrelated identities be swapped
silently. Everything else stays a plain `Guid`: wrapping an identifier nothing else refers to would
add a type without preventing anything.

That rule currently yields `PatientId`, `ImportBatchId`, `SourceResourceId` and
`PrimaryCancerDiagnosisId`. The last one joined in Phase 3B, when `CancerStaging` began recording
which cancer it stages: mCODE's TNM Stage Group `Observation.focus` names the primary cancer
Condition, and dropping that to `PatientId` alone would make a patient with two primary cancers
ambiguous. `PrimaryCancerDiagnosis` is the anchor other entities hang from, so its identifier is a
foreign reference and is typed accordingly.

`CancerStaging.Id` remains a plain `Guid` under the same rule — nothing references it yet. The
asymmetry is the rule working, not an oversight.

## Consequences

- Mapping code must be written by hand rather than generated. This is accepted: the mapping *is*
  the product, and hand-written mappers are what make golden-file tests meaningful.
- The domain is deliberately lossy. Anything V1 does not model is recorded as a `CoverageNote`
  rather than silently carried.
- The domain can be unit-tested with no FHIR, database, or web dependency, which is why Phase 1
  could deliver 125 domain tests before any infrastructure exists.
- Adding a package reference to `OncoBridge.Domain` fails the build, not a review.
