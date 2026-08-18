# ADR-0007 — Modular monolith with an executably enforced dependency boundary

**Status:** Accepted · **Phase:** 1

## Context

The project needs enough structure to demonstrate real architectural boundaries and little enough
that a reviewer can hold it in their head. Two failure modes are equally bad: one project per
concept, which is architecture astronautics; and one project total, which proves nothing about
boundaries.

Separately, the boundary that matters most (ADR-0001) is worthless if it is only a convention.

## Decision

**Five production projects, four test projects, one Angular app later.** Sub-structure lives in
folders and namespaces; a new project is created only when a *reference-direction* problem demands
one.

```
OncoBridge.Domain          zero dependencies
OncoBridge.Application     -> Domain
OncoBridge.Interop.Fhir    -> Domain, Application  (sole owner of Hl7.Fhir.*, since P2)
OncoBridge.Infrastructure  -> Domain, Application  (sole owner of EF Core / Npgsql)
OncoBridge.Api             -> Application, Infrastructure, Interop.Fhir  (host, since P5)
```

**Why both adapters point at `Application` (revised in P3D).** Through P3C, `Application` was empty and
`Interop.Fhir` referenced only `Domain`. That was correct *at the time* and is recorded here rather
than quietly rewritten: Phase 2 had no use case, so there was nothing for a port to abstract, and
`Infrastructure.Tests` was the only place the two adapters met (see **Test projects** below).

Phase 3D introduced the first real use case — `NormalizeImportBatch`, which loads a batch's immutable
source resources, normalizes them, and atomically replaces that batch's derived canonical tier. That
use case needs two abstractions, and both belong to it rather than to either adapter:

| Port | Owned by | Implemented by |
|---|---|---|
| `ICanonicalNormalizer` | `Application` | `Interop.Fhir.FhirNormalizer` |
| `INormalizationStore` | `Application` | `Infrastructure.NormalizationStore` |

`NormalizationResult` moved from `Interop.Fhir` to `Application` in the same phase. It only ever held
`Domain` types, and once it became the contract a use case returns, having the FHIR adapter own it
would have forced `Infrastructure` to reference `Interop.Fhir` to persist a normalization result —
exactly the edge this ADR exists to forbid.

So `Interop.Fhir -> Application` is a **dependency-inversion arrow, not drift**: the adapter depends
on the abstraction the application layer defines, and nothing in `Application` knows that FHIR or EF
Core exist. The forbidden directions are unchanged and still asserted:
`Application -> Interop.Fhir`, `Application -> Infrastructure`, `Interop.Fhir -> Infrastructure`, and
`Infrastructure -> Interop.Fhir`.

**Why the API points at both adapters (revised in P5).** Through P4 `OncoBridge.Api` was an empty class
library referencing `Application` and `Infrastructure`. P5 turns it into the real executable host, and a
composition root has one job the layers below cannot do for it: name the concrete adapters that satisfy
the application's ports. Registering `FhirBundleIngestor`, `FhirNormalizer` and
`FhirSourceQualityEvaluator` therefore requires `Api -> Interop.Fhir`, exactly as registering
`ImportBatchStore`, `NormalizationStore`, `QualityStore` and `OncoBridgeReadStore` requires
`Api -> Infrastructure`.

This is the one legitimate place where the two adapters meet. It replaces the P2-era arrangement in
which `OncoBridge.Infrastructure.Tests` was the only such place, and it does not weaken any forbidden
direction: the adapters still cannot see each other, and `Application` still cannot see either.

P5 adds four ports, owned by the use cases that need them:

| Port | Owned by | Implemented by |
|---|---|---|
| `IImportPayloadIngestor` | `Application` | `Interop.Fhir.FhirBundleIngestor` |
| `IImportBatchWriter` | `Application` | `Infrastructure.ImportBatchStore` |
| `IOncoBridgeReadStore` | `Application` | `Infrastructure.OncoBridgeReadStore` |
| `ISourceQualityEvaluator` | `Application` | `Interop.Fhir.FhirSourceQualityEvaluator` |

`IngestedBundle` moved from `Interop.Fhir` to `Application` as `IngestedPayload` for the same reason
`NormalizationResult` moved in P3D: once a use case returns it, the FHIR adapter must not own it. It
held only `Domain` types before the move and still does.

The risk this arrow carries is not a reference cycle but a temptation: an executable host that knows
every concrete type is the easiest place to start writing business logic. `OncoBridge.Api` therefore
holds a composition root, endpoint mapping, its own DTOs and the mapping between those DTOs and
`Application`/`Domain` types — and nothing else. Every decision it appears to make about oncology data
is made in `Application` or `Domain` and merely wired here.

**Projects deliberately not created:**

- **`OncoBridge.Quality`** — the most tempting and the most wrong. Quality checks split by *input
  type*: conformance checks need FHIR types, domain-consistency checks need domain types. A single
  Quality project would either drag FHIR into itself, breaking ADR-0001, or become an empty shell.
  The `Finding` model therefore lives in `Domain`, and each check will live where its input lives.
- **Separate Ingestion / Normalization / Conformance projects** — one cohesive concern with a single
  external dependency. Folders now; split only if a genuine reference-direction problem appears.
- **`OncoBridge.Contracts`** — DTOs will have exactly one consumer. Premature.

**Test projects.** `OncoBridge.Infrastructure.Tests` was added in P2 and is the one place where the
Interop.Fhir and Infrastructure adapters are composed together. That composition lives in a test
project rather than in `OncoBridge.Application` because Phase 2 has no use case that would justify a
port, and inventing one purely to have somewhere to put the wiring would distort the dependency
graph. It is a separate project rather than part of an existing one because its tests require Docker
and are therefore slow: keeping them apart means the unit suites still run on a machine with no
container runtime.

**The boundary is a test, not a convention.** `DomainBoundaryTests` asserts the full permitted
reference matrix, using two complementary techniques because either alone has a hole:

| Project | Must not reference |
|---|---|
| `OncoBridge.Domain` | `Hl7.Fhir`, EF Core, Npgsql, ASP.NET Core — and in fact *no package at all*, and no project |
| `OncoBridge.Application` | `Hl7.Fhir`, EF Core, Npgsql — and any project other than `Domain` |
| `OncoBridge.Interop.Fhir` | EF Core, Npgsql, and `OncoBridge.Infrastructure` |
| `OncoBridge.Infrastructure` | `Hl7.Fhir`, and `OncoBridge.Interop.Fhir` |
| `OncoBridge.Api` | `Hl7.Fhir`, EF Core, Npgsql — the sole permitted web project, and the only one |

`Application_depends_on_the_domain_alone` asserts the second row as an exact set rather than a
blocklist, because that is the row a future phase is most likely to erode one reference at a time.

Plus three exclusivity rules: only `Interop.Fhir` may reference `Hl7.Fhir.*`, only `Infrastructure`
may reference EF Core or Npgsql, and only `OncoBridge.Api` may reference ASP.NET Core or use
`Microsoft.NET.Sdk.Web`. The API consumes EF Core and the FHIR SDK transitively through its project
references, which is what a composition root is for; it declares neither package itself.

Reading the project files catches a forbidden reference the moment it is *declared* — necessary
because the compiler omits unused references from assembly metadata, so a declared-but-unused
package would be invisible to reflection alone. Reading the assembly graph catches anything
arriving by a route the project file does not spell out.

**Solution format:** `.slnx`. The .NET 10 SDK supports it in `dotnet build` and `dotnet test`, and
it drops the GUID bookkeeping that makes classic `.sln` files merge badly. The cost is that older
tooling cannot open it, which is acceptable for a project already pinned to .NET 10.

**SDK pinning:** `global.json` pins `10.0.400` with `rollForward: latestPatch`. Silent roll-forward
to a newer major or minor is what must not happen; tolerating a newer *patch* of the same feature
band keeps the repository buildable on a machine or CI image that ships 10.0.4xx without weakening
that guarantee. `disable` was considered and rejected as needlessly brittle for the benefit gained.

## Consequences

- `OncoBridge.Api` became `Microsoft.NET.Sdk.Web` in P5, as this ADR anticipated. It was a plain class
  library through P4 so that the Phase 1 gate ("no API implementation has started") was executably
  true rather than merely claimed.
- Phase-scope tests are written to be **deleted** by the phase that invalidates them; their failure
  is the signal that a phase has begun, and removing one is a deliberate recorded act. P2 retired
  `OncoBridge.Interop.Fhir.Tests/Phase1ScopeTests` and the
  `Phase1_has_no_persistence_or_FHIR_packages_anywhere` assertion, exactly as designed. P5 retired
  `OncoBridge.Api.Tests/Phase1ScopeTests` and `Api_has_not_started_and_references_no_web_packages`,
  replacing them with the positive assertions that the API is the composition root and the only web
  project.
- EF Core is pinned explicitly in `Infrastructure` rather than taken transitively. The Npgsql
  provider depends on an older EF Core patch and `Microsoft.EntityFrameworkCore.Design` is
  `PrivateAssets=all`, so without an explicit pin the API project resolved a different EF Core than
  Infrastructure was compiled against, producing assembly-conflict warnings.
- Generated EF migrations are marked `generated_code = true` in `.editorconfig`. Hand-written style
  rules should not apply to scaffolder output, and this keeps `dotnet ef migrations add` producing
  code that builds unmodified under `TreatWarningsAsErrors`.
- Central package management (`Directory.Packages.props`) keeps versions consistent across the test
  projects and makes "the production projects reference no packages at all" visible in one file.
