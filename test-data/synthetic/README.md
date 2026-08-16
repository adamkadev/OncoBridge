# Synthetic test data

**This directory contains synthetic and public data only. Real patient data must never be added
here, or anywhere else in this repository.**

## Contents

| Directory | Phase | Contents |
|---|---|---|
| [`phase2/`](phase2/) | P2 | `bundle-minimal.json` — a four-entry FHIR R4 Bundle used to prove ingestion, byte-exact payload storage and `jsonb` round-tripping. Hand-authored; see [`phase2/README.md`](phase2/README.md). |
| [`phase3/`](phase3/) | P3 | Hand-authored FHIR R4 Bundles used to prove FHIR → canonical normalization of a primary cancer Condition, a TNM staging assessment, a cancer-related surgical Procedure, and all four together; see [`phase3/README.md`](phase3/README.md). |
| [`phase4/`](phase4/) | P4 | Hand-authored FHIR R4 Bundles carrying **deliberate source defects**, one per source quality check, plus a clean control and the combined acceptance bundle; see [`phase4/README.md`](phase4/README.md). |

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
