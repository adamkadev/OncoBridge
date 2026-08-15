# ADR-0004 — Four finding categories, and where each attaches

**Status:** Accepted · **Phase:** 1 (model) / P4 (checks)

## Context

"Data quality" is meaningless as a single bucket. A payload that will not parse, a payload that
parses but omits a profile-mandatory element, a reference that does not resolve, and a normalized
result that is internally incoherent are four different problems, detectable at four different
points, with different consequences.

A second question is easy to get wrong: what does a finding attach to?

## Decision

**Four categories**, ordered by the pipeline stage at which each becomes detectable. A category can
only be evaluated once every category above it has passed.

| Category | Detected at | Blocks normalization? |
|---|---|---|
| `Structural` | Parse | Yes — the resource never enters the domain |
| `Conformance` | Post-parse | No |
| `ReferentialIntegrity` | Reference resolution | Partially — may leave an aggregate incomplete |
| `DomainConsistency` | Post-normalization | No |

**Severity is derived, not chosen.** Assigning it by intuition is how a quality tool becomes
untrustworthy, so the rule is mechanical:

- `Error` — the specification states the element is mandatory (min cardinality ≥ 1), or the check is
  a pure structural or graph fact involving no interpretation.
- `Warning` — the element is must-support rather than mandatory, or the binding is extensible rather
  than required, or the finding is a domain-consistency observation.
- `Information` — context only.

**Where findings attach**, which follows from what each is a statement *about*:

- **Conformance, structural and referential findings attach to the `SourceResource`.** "This
  resource lacks its mandatory method" is a claim about the input. It stays true forever, whatever
  normalization later does.
- **Domain-consistency findings attach to the domain entity.** "This staging assessment predates its
  diagnosis" is a claim about *our normalized result*, and could legitimately change when the mapper
  changes.

The practical payoff: re-running normalization must invalidate domain-consistency findings and must
leave conformance findings untouched. That falls out cleanly from this split and is a mess without
it.

**`CoverageNote` is a separate type from `Finding`, with no severity and no shared base type.** A
resource type outside V1 scope, or an occurrence stated in a form V1 does not read, is not a quality
problem — OncoBridge simply did not look at it. Conflating "not examined" with "wrong" is the most
common failure mode of data-quality tooling and it undermines trust in every other number on screen.
Keeping them as unrelated types makes the conflation impossible rather than merely discouraged: a
coverage note cannot be counted among findings because it will not compile as one.

**`Citation` is required on every finding.** It makes each finding auditable in seconds, and it is
the evidence that these rules were derived from published specifications rather than from anywhere
else. A check that cannot cite a source is not a check OncoBridge should run.

**Vocabulary is a control, not a style preference.** The type is `Finding`, never `Error`, `Alert`
or `Violation`; identifiers are opaque (`OB-CONF-002`) rather than descriptive
(`StagingIncompleteWarning`), because the latter reads as a clinical judgement about a patient's
record. No public identifier in the codebase may contain *diagnose*, *assess*, *recommend*, *risk*
or *clinical*.

## Consequences

- A missing staging method is **not** a `CancerStaging` invariant. If the aggregate refused to exist
  without one, the check reporting its absence could never run, and the defect the system exists to
  surface would be invisible. `CancerStaging.Method` is nullable for precisely this reason.
- Finding messages must be deterministic — same input, same string — so runs are comparable.
- Persistence must key findings by target kind, since the two kinds have different invalidation
  rules.
