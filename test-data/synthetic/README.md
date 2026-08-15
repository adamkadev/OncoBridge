# Synthetic test data

**This directory contains synthetic and public data only. Real patient data must never be added
here, or anywhere else in this repository.**

Empty in Phase 1 — the domain foundation needs no fixtures, because it has no FHIR ingestion yet.
Fixtures arrive with P2/P3.

## Rules

1. **Synthetic or public only.** No real patient data, no anonymised hospital extracts, no employer
   or institutional data, no proprietary schemas.
2. **Every fixture is attributable.** Each subdirectory carries a README naming the generator, its
   version, and its licence.
3. **Code references, never terminology content.** Codes that appear inside a synthetic bundle are
   fine. Checked-in SNOMED CT or LOINC description files, hierarchy extracts or display-name tables
   are redistribution and are not permitted. See [ADR-0009](../../docs/adr/0009-no-terminology-server-in-v1.md).

## Planned sources

| Source | Licence | Use |
|---|---|---|
| [Synthea](https://github.com/synthetichealth/synthea) breast cancer module | Apache-2.0 | Primary corpus. Notably **not** mCODE-conformant out of the box, which is what makes the conformance checks report real findings rather than contrived ones. |
| Hand-authored minimal bundles | This repository | One fixture per check id, each triggering exactly that check and nothing else. |
| [HL7 mCODE test data](https://confluence.hl7.org/display/COD/mCODE+Test+Data) | Published free of cost, privacy and security restrictions | Cross-check against conformant input. |
