# Phase 2 ingestion fixture

**Hand-authored for this repository.** Not generated, not derived from any dataset, and containing
no real or re-identifiable data. Identifiers are `urn:uuid:` literals and an invented
`urn:oncobridge:synthetic:mrn` namespace.

| File | Purpose |
|---|---|
| `bundle-minimal.json` | A four-entry FHIR R4 `collection` Bundle: Patient, Condition, Observation, Procedure. |

## Why the formatting is deliberately inconsistent

`bundle-minimal.json` mixes indentation depths, puts `id` before `resourceType` in the Condition,
writes the Observation entry on a single line, and leaves a blank line between entries.

None of that is accidental. Phase 2 must prove that `ImportBatch.RawPayload` stores the bytes that
arrived rather than a re-serialisation, and that proof is only meaningful if a re-serialisation
would look different. A canonically formatted fixture would pass a byte-comparison test even if the
implementation were silently round-tripping the payload through a parser.

The same property makes the fixture useful for the opposite assertion: after storage in a `jsonb`
column, key order and whitespace are normalised, so the reloaded resource JSON is **semantically**
equivalent but **not** byte-identical. Tests compare it semantically and never textually (ADR-0006).

## Scope

These resources exercise ingestion and storage only. Phase 2 performs no normalisation, so the
clinical content is deliberately thin — enough to be valid FHIR R4 and to cover the resource types
later phases will map, and no more. The Condition deliberately carries
`category = encounter-diagnosis` rather than a problem-list category, which is the shape a later
conformance check will report on; nothing in Phase 2 examines it.
