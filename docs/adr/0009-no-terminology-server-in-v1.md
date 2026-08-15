# ADR-0009 — No terminology server in V1

**Status:** Accepted · **Phase:** 1

## Context

Terminology is unbounded, and it is where portfolio projects go to die. Value-set expansion,
subsumption testing and concept mapping each require infrastructure that adds no visible quality to
the result.

There is also a licensing constraint that is easy to breach by accident. SNOMED International
charges nothing for Affiliate Licensing within Member countries, but a licence is still required and
licences are per-country; LOINC carries its own terms.

## Decision

**V1 does:**

- Store codes as `CodedConcept(System, Code, Display)`, carried through **unchanged** — no trimming,
  no case folding, no canonicalization. Presence is validated; content is never altered.
- Recognize code systems by **URI only** — `http://snomed.info/sct`, `http://loinc.org`,
  `http://hl7.org/fhir/sid/icd-10-cm`, `http://unitsofmeasure.org`.
- Permit one small, versioned, cited in-repo lookup table for a single purpose: identifying staging
  observations by LOINC code (`21908-9`, `21902-2`, `21914-7` for the stage group, plus the T/N/M
  category codes). It is data, reviewable in a diff.

**V1 does not:** run or embed a terminology server; expand value sets or test membership; perform
subsumption, hierarchy or concept mapping; translate between code systems.

**The licensing rule that shapes the code:** the repository may contain code *references*, never
terminology *content*. Storing a code that appeared in a synthetic bundle is fine. Checking in
SNOMED description files, hierarchy extracts, or a display-name lookup table is redistribution and
must not happen.

This has a direct design consequence: **`CodedConcept.Display` is populated only from what the
source supplied.** OncoBridge never enriches a display name from a table of its own.

## Consequences

- A display name may be absent or unhelpful when the source omitted it. That is correct behaviour,
  not a gap to fill.
- The planned `OB-CONF-003` check can only test *code-system membership*, not value-set membership.
  Its message must state that limitation plainly rather than implying value-set validation.
- Two codes for the same concept in different systems are unrelated as far as V1 is concerned.
- No check may depend on the *meaning* of a code — only on its system, presence, or structural role.
