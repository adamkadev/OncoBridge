# ADR-0011 — Timeline temporal projection policy

**Status:** Accepted · **Phase:** 6B

## Context

ADR-0005 settled how a single stated date compares to another and deliberately stopped there. Three
of its consequences are the whole reason this record exists:

- `PartialDate.Compare` is a **partial order**, so `PartialDate` cannot implement `IComparable` and
  values cannot be sorted. That ADR says outright that "timeline ordering in a later phase must
  decide its own tie-breaking policy and state it".
- `Indeterminate` is a correct answer, not a failure. Any consumer must handle four outcomes.
- **Period-to-date and period-to-period comparison are not implemented**, because no V1 concept
  needed them and the semantics — containment versus overlap versus ordering — should be settled by
  the rule that first requires them.

Phase 6B is that later phase. A patient timeline is the first feature that must place several
occurrences in a reading order, and two of the three V1 concepts can state a period. Nothing in the
domain answers "where does this period sit relative to that date", so the timeline cannot be
produced without an explicit, named projection policy. Writing that policy into the Angular client
would put temporal reasoning in the least testable layer of the system and outside the boundary
ADR-0007 enforces, so it is decided here and implemented server-side.

The Phase 6B design was frozen before this record was written, and it renders the policy sentence to
the reader rather than hiding it. That is a requirement on the projection, not a UI nicety: the
response carries the policy so the screen cannot silently disagree with the server.

## Decision

The timeline is a **read projection** over the canonical record. `PrimaryCancerDiagnosis`,
`CancerStaging` and `CancerSurgicalProcedure` know nothing about it; no timeline state is persisted,
and no schema changes.

**Policy, stated verbatim in every response.** *"Events are sequenced by their temporal anchor,
projected on stated bounds only. A period is anchored by its stated start bound."* The policy
carries a version alongside the sentence so a later change is visible to clients rather than silent.

**V1 events.** Exactly three concepts become events: a primary cancer diagnosis (occurrence: its
onset), a staging assessment (its effective date), and a cancer-related surgical procedure (its
performed occurrence). `Patient.BirthDate` is not an event. `PrimaryCancerDiagnosis.RecordedDate` is
carried on the diagnosis event as metadata and never becomes a second event — when a record was
written down is not when the thing happened.

**Anchor.** A stated date anchors itself. A stated period is anchored by its **start** bound. A
period with no stated start has **no anchor**: the end bound is never used as a fallback, because
"the procedure ended by June" places nothing — the occurrence could have started at any time before
that. This is the point where the absence of period-to-date comparison becomes visible, and the
projection resolves it by refusing to place the event rather than by inventing a comparison ADR-0005
declined to define.

**The anchor's source is stated, not rediscovered.** An anchored event carries a
`TimelineAnchorSource` beside its anchor: `Date` when the occurrence states a date, `PeriodStart`
when it states a period. An event with no anchor carries no source. A client cannot recover this by
matching the anchor back to a bound, because a zero-length period states the same value at the same
precision on both bounds and both would match — so the projection names the bound it used. There is
no `PeriodEnd`: under this policy the end bound never anchors an event.

Only anchors are ever compared, and only with `PartialDate.Compare`. The projection therefore never
claims a relation between a whole period and anything else. A period event states its position
through its start bound and shows both bounds as stated.

**Unsequenced events.** An event with no usable anchor stays in the response, off the sequence, with
an explicit reason: `NoOccurrenceStated` when the entity states no occurrence at all, and
`NoAnchorBound` when an occurrence exists but its start bound does not. A `NoAnchorBound` event
keeps its stated end bound in the response — it is not undated, and dropping the one bound the
record does state would lose information the source asserted. Unsequenced events carry no sequence
number, because placing them before the first group or after the last would assert a position the
record never stated.

**Conservative grouping by connected components.** Two anchored events are *connected* when their
anchors compare `Same` or `Indeterminate`. The projection computes the **connected components** of
that undirected relation, and each component becomes one group.

The alternative — grouping only directly incomparable pairs — silently invents chronology. Given

```
A Indeterminate B      B Indeterminate C      A Before C
```

pairwise grouping yields separately numbered groups containing `A` and `C`, and a reader who sees
`01 A`, `02 B`, `03 C` reasonably concludes the record establishes that order. It does not: nothing
places `B` relative to either. Keeping `A`, `B` and `C` in one component withholds the provable
`A Before C` fact inside the group and asserts nothing false. V1 chooses **never inventing
chronology** over **displaying every pairwise temporal fact**, and this is the concrete price.

**Group kind.** A one-event component is `Established`. A multi-event component in which *every*
pair compares `Same` is `SharedTemporalAnchor`. Any other multi-event component is
`OrderNotEstablished`.

`SharedTemporalAnchor` means every pair compares `TemporalComparison.Same`, which is *not*
`PartialDate.Equals`. ADR-0005 keeps temporal sameness and representational equality apart on
purpose: `2019-03-14T10:00:00+02:00` and `2019-03-14T08:00:00+00:00` compare `Same` and are not
equal. Each event therefore keeps its own stated value and its own stated UTC offset; the group has
no lexical anchor value of its own, and nothing is normalized into a single displayed form.

`OrderNotEstablished` is a neutral projection state. It is not a finding, a warning or an error, and
it does not mean the record is defective — `OB-DOM-001` fires only on `Before` and never on
`Indeterminate`, and this projection creates no findings at all. It means the component cannot be
exposed as a truthful before/after sequence under this policy.

**Group order, verified rather than assumed.** Between two distinct components no comparison can be
`Same` or `Indeterminate` — such a relation would have merged them. Every cross-component pair is
therefore checked to agree on one strict direction, and the resulting strict total order is verified
to rank the components uniquely before any sequence number is assigned. Sequence numbers start at 1
in that verified order. An inconsistent relation **fails explicitly**; the projection never picks a
direction to get past it, and entity ids are never used as a temporal tie-breaker. Given ADR-0005's
closed-interval semantics the check cannot fire — `A.End < B.Start` and `B.End < C.Start` imply
`A.End < C.Start` — so it stands as an executable statement of the invariant rather than as
recovery.

**Deterministic serialization without temporal meaning.** `SharedTemporalAnchor` and
`OrderNotEstablished` groups have no internal order, but a JSON array is ordered. Events inside a
group, and the unsequenced list, are serialized by entity kind and then entity id. That order is
technical: it carries no temporal claim, it is tested to be stable under reordered inputs, and no
per-event sequence number is ever exposed inside a group.

## Consequences

- The projection is pure over `PatientRecord`, so every rule above is testable without a database,
  an HTTP host or a FHIR payload.
- Grouping is `O(n²)` in anchored events per patient. At V1 volumes — three concepts for one
  patient — that is irrelevant, and the clarity of comparing every pair explicitly is worth more
  than an index that would have to encode an ordering the model does not have.
- A component may withhold provable pairwise facts. That is the deliberate trade recorded above, and
  a later phase may expose intra-component relations only by designing a presentation that cannot be
  read as a sequence.
- Adding a fourth timeline concept means naming its occurrence and its anchor here; the projection
  has no default for a concept it does not know.
- If a rule ever needs to relate a whole period to another occurrence, ADR-0005's deferred
  comparators become necessary and this policy's anchor rule should be revisited with them, not
  worked around in the projection.
- The policy sentence is part of the HTTP contract. Changing the wording changes the response, which
  is the intended pressure: the reader and the server cannot drift apart quietly.
