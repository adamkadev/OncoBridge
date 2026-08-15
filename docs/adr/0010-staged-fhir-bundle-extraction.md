# ADR-0010 — Staged FHIR bundle extraction

**Status:** Accepted · **Phase:** 2

## Context

Ingestion has to satisfy three obligations that pull against each other:

1. The entry-level digest must cover **exactly** the bytes received, so it has to be computed over a
   verbatim slice of the payload rather than anything re-serialised.
2. A later phase must be able to report *which entry* failed to parse as FHIR R4. Deserialising the
   whole bundle in one call would abort the entire payload on the first bad entry and destroy access
   to the rest, making an entry-level structural check impossible to add without rewriting ingestion.
3. FHIR R4 interpretation must remain Firely's job. Hand-rolling a FHIR parser is out of the question.

## Decision

Extraction runs in stages, and each stage owns exactly one concern:

```
raw bytes
  ↓  System.Text.Json — envelope only: resourceType == "Bundle", type, entry[]
  ↓  per entry: the resource's verbatim byte slice is retained, plus fullUrl
  ↓  Firely FhirJsonDeserializer — interprets each slice INDIVIDUALLY
```

System.Text.Json is used only to read the envelope and to obtain each entry's raw slice. It performs
no FHIR interpretation. Firely performs all of it, one entry at a time.

The slice comes from the JSON reader's raw-text span for the entry's `resource` value, so it is a
contiguous range of the received payload. This is asserted directly rather than assumed: every
extracted fragment must be findable as a literal byte subsequence of `ImportBatch.RawPayload`.

**Per-entry interpretation is the load-bearing part.** An entry that cannot be read as FHIR R4 is
recorded as uninterpretable, keeps its raw bytes, and does not affect its neighbours. Phase 2 records
that outcome and raises nothing; the structural check that consumes it arrives later.

## Nullable source metadata

`SourceResource.ResourceType`, `.ContentHash` and `.ResourceJson` are all nullable, and this is not
laxity:

- A malformed or typeless entry has no determinable resource type. Requiring one would make such an
  entry impossible to store, and OncoBridge exists to ingest defective input and report on it. A
  non-nullable type would make the planned structural check unimplementable.
- A transaction entry carrying only a request and no resource is legitimately typeless, and has no
  resource bytes to hash and no resource JSON to keep. Fabricating values for it would be inventing
  data.

The database mirrors this: nothing the interchange format merely *expects* is enforced as a
constraint (ADR-0006).

## Input handling

Payload size and entry count are bounded by `BundleIngestionOptions`. Ingestion input is untrusted
even though V1 exposes no upload endpoint.

**Failure messages describe the shape of a failure only and never interpolate payload content**,
because exception text reaches logs. A message may state a size or a count; it may not quote what was
received. This looks like an over-restriction until the day a payload ends up in a log file, so it is
recorded here rather than left as a convention.

FHIR XML is not accepted. JSON only, which removes the XXE class outright rather than defending
against it.

## Consequences

- Extraction returns primitives only. No FHIR POCO appears in any public signature of
  `OncoBridge.Interop.Fhir`, so nothing can leak toward the domain (ADR-0001).
- Two representations of every entry exist and must not be confused: the verbatim slice, which the
  digest covers, and the parsed JSON, which is queryable but normalised (ADR-0006).
- `OncoBridge.Interop.Fhir` produces domain records and never persists them. Persisting is
  Infrastructure's job, and a reference in that direction would let FHIR parsing reach the database.
