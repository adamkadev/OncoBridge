# Phase 3 normalization fixture

**Hand-authored for this repository.** Not generated, not derived from any dataset, and containing
no real or re-identifiable data. Identifiers are `urn:uuid:` literals and an invented
`urn:oncobridge:synthetic:mrn` namespace.

| File | Purpose |
|---|---|
| `bundle-primary-cancer.json` | A two-entry FHIR R4 `collection` Bundle: one Patient and one Condition carrying the mCODE `PrimaryCancerCondition` profile. |

## Why this is separate from `phase2/bundle-minimal.json`

`phase2/bundle-minimal.json` proves ingestion semantics — byte-exact payload storage and `jsonb`
round-tripping — and its deliberately inconsistent formatting is load-bearing for that proof. It is
frozen. Normalization needs different content (a declared profile, a body site, a recorded date),
and editing the Phase 2 fixture to supply them would weaken the assertions that fixture exists to
support.

Formatting here is conventional, because nothing in Phase 3 asserts anything about the bytes.

## What each element is present for

| Element | Exercises |
|---|---|
| `Patient.birthDate` = `1968` | Year precision survives normalization; no month or day is fabricated (ADR-0005). |
| `Patient.identifier[0].value` | First-usable-identifier selection into `Patient.SourceIdentifier`. |
| `Patient.gender` = `female` | Proves administrative gender is **not** mapped into `SexAtBirthAsRecorded`. FHIR `Patient.gender` represents administrative gender; `SexAtBirthAsRecorded` represents a different semantic concept. The domain property stays `null` until an explicitly supported sex-at-birth source is implemented. |
| `Condition.meta.profile` | The only signal Phase 3A accepts for recognising a primary cancer condition. |
| `Condition.code` | System, code and display carried through unchanged (ADR-0009). |
| `Condition.bodySite` | Optional coded concept mapping. |
| `Condition.subject` | `urn:uuid:` reference resolved against the entry's `fullUrl`. |
| `Condition.onsetDateTime` = `2019-03` | Month precision survives into `TemporalOccurrence`. |
| `Condition.recordedDate` = `2019-04-02` | Day precision. |

`clinicalStatus`, `verificationStatus` and `category` are present only so the resource is credible
FHIR R4. Phase 3A reads none of them.

## Scope

Phase 3A normalizes Patient and primary cancer diagnosis only. The bundle deliberately carries no
staging observations and no procedures: adding resources that nothing yet reads would suggest a
coverage this slice does not have.
