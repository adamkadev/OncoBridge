# Phase 3 normalization fixtures

**Hand-authored for this repository.** Not generated, not derived from any dataset, and containing
no real or re-identifiable data. Identifiers are `urn:uuid:` literals and an invented
`urn:oncobridge:synthetic:mrn` namespace.

| File | Purpose |
|---|---|
| `bundle-primary-cancer.json` | A two-entry FHIR R4 `collection` Bundle: one Patient and one Condition carrying the mCODE `PrimaryCancerCondition` profile. |
| `bundle-tnm-staging.json` | A six-entry FHIR R4 `collection` Bundle: the same Patient and Condition plus a TNM Stage Group Observation and the three category Observations it composes. |
| `bundle-surgical-procedure.json` | A two-entry FHIR R4 `collection` Bundle: the same Patient and a Procedure carrying the mCODE `CancerRelatedSurgicalProcedure` profile. It holds **no** Condition. |
| `bundle-complete-normalization.json` | A seven-entry FHIR R4 `collection` Bundle carrying the whole V1 graph at once: Patient, primary cancer Condition, TNM Stage Group, its three T/N/M member Observations, and a cancer-related surgical Procedure. |

## Why this is separate from `phase2/bundle-minimal.json`

`phase2/bundle-minimal.json` proves ingestion semantics — byte-exact payload storage and `jsonb`
round-tripping — and its deliberately inconsistent formatting is load-bearing for that proof. It is
frozen. Normalization needs different content (a declared profile, a body site, a recorded date),
and editing the Phase 2 fixture to supply them would weaken the assertions that fixture exists to
support.

Formatting here is conventional, because nothing in Phase 3 asserts anything about the bytes.

## Why staging is a separate bundle

`bundle-primary-cancer.json` is the Phase 3A fixture and asserts, among other things, that a bundle
without staging produces no field-level lineage. Adding staging observations to it would destroy
that assertion. The staging bundle carries its own Patient and Condition so each fixture stays
readable on its own.

## `bundle-primary-cancer.json` — what each element is present for

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

## `bundle-tnm-staging.json` — what each element is present for

| Element | Exercises |
|---|---|
| Stage Group `Observation.code` = LOINC `21908-9` | The clinical stage group code, one of the three cited codes ADR-0009 permits. Recognition reads `system` + `code` only. |
| Stage Group `Observation.focus` | Links the assessment to the `PrimaryCancerCondition` being staged, by `fullUrl`. |
| Stage Group `Observation.subject` | Must agree with the patient the focused Condition names. |
| Stage Group `Observation.hasMember` | The **only** thing that composes T/N/M into this assessment. Stated as a mixture of one `fullUrl` and two `Observation/{id}` references so both resolution forms are exercised. |
| Stage Group `Observation.valueCodeableConcept` | Maps to `CancerStaging.StageGroup`, carried through unchanged. |
| Stage Group `Observation.method` | Maps to `CancerStaging.Method`. It is a different concept from `Observation.code` and is never inferred from it. |
| Stage Group `Observation.effectiveDateTime` = `2019-04-02` | Day precision maps into `CancerStaging.Effective`. |
| T/N/M `Observation.code` = LOINC `21905-5`, `21906-3`, `21907-1` | Axis classification from the cited LOINC subset. |
| T/N/M `Observation.valueCodeableConcept` | The actual category result — never fabricated from `Observation.code`. |
| T/N/M `focus` and `subject` | Stated as a mixture of `fullUrl` and relative references, and consistent with the stage group, so the consistency checks pass on the happy path. |

The four Observations make this the first canonical entity assembled from several source resources,
which is what the entity-level plus field-level lineage in ADR-0003 exists to demonstrate.

## `bundle-surgical-procedure.json` — what each element is present for

| Element | Exercises |
|---|---|
| `Procedure.meta.profile` | The only signal Phase 3C accepts for recognising a cancer-related surgical procedure. `Procedure.code` has an extensible binding, so identifying cancer surgery from the code alone would need terminology knowledge ADR-0009 withholds. |
| `Procedure.subject` | `urn:uuid:` reference resolved against the entry's `fullUrl`. |
| `Procedure.code` | System, code and display carried through unchanged (ADR-0009). |
| `Procedure.performedPeriod` = `2019-05` … `2019-06-12` | The point of Phase 3C: `performed[x]` polymorphism through the existing temporal model, with each boundary keeping the precision it was stated at — Month for the start, Day for the end. |
| `Procedure.bodySite` | FHIR states it `0..*`; the domain records one, selected by the same first-usable-coding policy the diagnosis body site uses. |
| `Procedure.reasonCode` | Present so the resource is not knowingly contrary to the source profile, which uses it to establish cancer relation. Phase 3C reads none of it — the canonical entity records `PatientId`, `Code`, `Performed` and `BodySite` only. |

**No Condition.** This is deliberate and load-bearing: a `CancerSurgicalProcedure` references its
Patient, not a diagnosis, so this bundle proves a Procedure can pull its Patient into the canonical
result with no `PrimaryCancerCondition` anywhere in the batch. It also keeps the bundle's lineage at
exactly two entity-level rows, which is what the no-field-level-lineage assertion rests on.

`status` is present only so the resource is credible FHIR R4. Phase 3C reads none of it.

## `bundle-complete-normalization.json` — what it is for

The three bundles above each isolate one mapper. This one exists for the opposite reason: Phase 3D
persists a whole `NormalizationResult` in a single transaction, so it needs one input that produces
every canonical concept at once.

| Element | Exercises |
|---|---|
| All four concepts in one batch | The complete derived tier — 1 Patient, 1 diagnosis, 1 staging, 3 categories, 1 procedure — written, replaced and reloaded as a unit. |
| The reference graph | `Condition.subject` → Patient; stage group `focus` → Condition, `subject` → Patient, `hasMember` → T/N/M; `Procedure.subject` → Patient. Every canonical foreign key in the schema is populated. |
| `Condition.onsetDateTime` = `2019-03` | Month precision survives a PostgreSQL round trip. |
| `Observation.effectiveDateTime` = `2019-04-02` | Day precision survives it. |
| `Patient.birthDate` = `1968` | Year precision survives it — no month or day is fabricated by the database. |
| `Procedure.performedPeriod` = `2019-05` … `2019-06-12` | The strongest fidelity case: a period whose two bounds sit at **different** precisions, proving each is stored independently rather than collapsed to one column or to the start. |
| 7 lineage rows | 1 Patient + 1 diagnosis + 1 procedure + 1 staging entity + 3 staging field rows — the count re-normalization must reproduce exactly rather than accumulate. |

Instant precision and open-ended periods are proved by small inline bundles in
`OncoBridge.Infrastructure.Tests` rather than here, because a fixture cannot hold two different
`onset` values at once and those cases exist only to pin down column fidelity.

## Scope

Phase 3 normalizes Patient, primary cancer diagnosis, TNM staging and cancer-related surgical
procedures. Nothing here carries medications, radiotherapy or treatment plans: adding resources that
nothing yet reads would suggest a coverage this phase does not have.
