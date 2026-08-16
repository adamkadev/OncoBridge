# Phase 4 source quality fixtures

**Hand-authored for this repository.** Not generated, not derived from any dataset, and containing
no real or re-identifiable data. Identifiers are `urn:uuid:` literals and an invented
`urn:oncobridge:synthetic:mrn` namespace.

These bundles differ in intent from the Phase 2 and Phase 3 fixtures. Those carry *correct* input and
prove that ingestion and normalization read it faithfully. These carry input that is **deliberately
defective in exactly one way**, so a check that stops firing is a failing test rather than a silent
regression. Phase 2 and Phase 3 fixtures are untouched.

| File | Defect it carries | Check it exercises |
|---|---|---|
| `bundle-structural-malformed.json` | An entry whose `resourceType` is unknown to FHIR R4, beside a valid `MedicationRequest` and a valid Condition | `OB-STR-001`, and the coverage note for a resource type outside V1 |
| `bundle-primary-cancer-missing-category.json` | A profiled `PrimaryCancerCondition` whose only category is `encounter-diagnosis` | `OB-CONF-001` |
| `bundle-stage-group-missing-method.json` | A recognisable TNM stage group with no `Observation.method` | `OB-CONF-002` |
| `bundle-dangling-reference.json` | `Condition.subject` naming a `urn:uuid:` that is not in the bundle | `OB-REF-001` |
| `bundle-staging-subject-mismatch.json` | Two Patients; the stage group names one and its T member names the other | `OB-REF-002` |
| `bundle-staging-precedes-diagnosis.json` | Staging effective `2019-05-01` against a diagnosis onset of `2019-06` — source-clean, defective only once normalized | `OB-DOM-001` |
| `bundle-clean-source.json` | None — every covered reference resolves and both conformance rules are met | The control: proves the evaluator reports **nothing** on good input |
| `bundle-acceptance-defects.json` | Three defects at once (see below) | The combined V1 scenario |

## Why the malformed bundle also carries a `MedicationRequest`

It is the check that the structural rule is not simply "OncoBridge does not understand this".
`MedicationRequest` parses perfectly as FHIR R4 and OncoBridge does not normalize it; it must
therefore produce a `CoverageNote` and **no** finding. Conflating "not examined" with "wrong" is the
failure mode ADR-0004 exists to prevent, and this fixture makes that conflation a test failure.

## `bundle-acceptance-defects.json` — the combined scenario

Seven resources: Patient, primary cancer Condition, TNM stage group, its three T/N/M members, and a
cancer-related surgical Procedure. It carries **exactly three** deliberate defects:

| Defect | Check |
|---|---|
| The Condition states no `category` at all | `OB-CONF-001` |
| The stage group states no `method` | `OB-CONF-002` |
| `Procedure.reasonReference` names a Condition that is not in the bundle | `OB-REF-001` |

The load-bearing property is that **normalization is unaffected**: the bundle still yields 1 Patient,
1 `PrimaryCancerDiagnosis`, 1 `CancerStaging` composed of all three T/N/M categories, and
1 `CancerSurgicalProcedure`. The dangling reference sits on `reasonReference`, which V1 normalization
deliberately does not read (Phase 3C), so the defect is visible to the quality engine and invisible
to the mapper.

That separation is the point of the whole phase: source quality is a statement about the input, not
about whether OncoBridge managed to normalize it.

## `bundle-staging-precedes-diagnosis.json` — the one defect no source check can see

Every element of this bundle is conformant: the Condition carries `problem-list-item`, the stage
group carries a `method`, and every covered reference resolves. All five source checks report
nothing. The defect only exists once the bundle has been **normalized**, because it is a statement
about the relationship between two canonical entities — a staging effective on `2019-05-01` against a
diagnosis whose onset is `2019-06`.

That is why `OB-DOM-001` is the only check that lives in FHIR-independent Domain code and the only
one whose findings a re-normalization invalidates.

The dates are chosen so `PartialDate.Compare` returns `Before` rather than `Indeterminate`: the
staging's Day-precision interval closes before the onset's Month-precision interval opens. Widening
either value would change the answer to `Indeterminate` and, correctly, suppress the finding —
which is what the Indeterminate unit tests in `OncoBridge.Domain.Tests` pin down.
