# Architecture Decision Records

Each record states **context**, **decision**, **consequences**. Decisions are recorded when they
are settled and needed, not in advance.

**These records are the only place architectural rationale lives.** Source code carries no
explanatory comments and no XML documentation: names, types, invariants and tests are expected to
carry meaning on their own, and anything they cannot express belongs here rather than in a comment
that drifts out of date beside the code it describes.

| ADR | Decision | Status |
|-----|----------|--------|
| [0001](0001-canonical-domain-independent-of-fhir.md) | The canonical domain is independent of FHIR | Accepted |
| [0003](0003-immutable-source-derived-normalization.md) | Source payloads are immutable; normalization is derived and re-runnable | Accepted |
| [0004](0004-finding-categories-and-attachment.md) | Four finding categories, and where each attaches | Accepted |
| [0005](0005-variable-precision-temporal-model.md) | Variable-precision temporal model with an explicit indeterminate outcome | Accepted |
| [0006](0006-postgresql-and-raw-payload-storage.md) | PostgreSQL, with raw payloads stored as bytes and `jsonb` used only for queryable copies | Accepted |
| [0007](0007-modular-monolith-enforced-boundary.md) | Modular monolith with an executably enforced dependency boundary | Accepted |
| [0009](0009-no-terminology-server-in-v1.md) | No terminology server in V1 | Accepted |
| [0010](0010-staged-fhir-bundle-extraction.md) | Staged FHIR bundle extraction, nullable source metadata, ingestion limits | Accepted |
| [0011](0011-timeline-temporal-projection-policy.md) | Timeline temporal projection policy: anchors, conservative components, verified group order | Accepted |

## Numbered in Phase 0, never written up

Two numbers were reserved in the Phase 0 analysis and never became records of their own. The
numbering gap is kept so Phase 0's numbering still resolves:

- **ADR-0002** — mCODE as a conformance and export boundary, never the ingestion contract. The
  conformance half is implemented: P3 normalizes without ever treating an mCODE profile as the
  ingestion contract, and the P4 checks cite mCODE STU4 rather than validating against it. The
  export half is P7B, a stretch goal V1 does not include.
- **ADR-0008** — hand-written cited conformance checks rather than a general profile validator.
  Implemented in P4: each of the six V1 checks names the specification statement it was derived
  from, and every finding carries that citation.

## Superseded wording

ADR-0006 replaces an earlier Phase 0 draft that treated a `jsonb` column as the byte-preserving
audit record. That was wrong, and the reasoning is recorded in ADR-0006 rather than deleted, since
the mistake is instructive.
