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
OncoBridge.Interop.Fhir    -> Domain            (sole owner of Hl7.Fhir.*, from P3)
OncoBridge.Infrastructure  -> Domain, Application
OncoBridge.Api             -> Application, Infrastructure
```

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
| `OncoBridge.Application` | `Hl7.Fhir`, EF Core, Npgsql |
| `OncoBridge.Interop.Fhir` | EF Core, Npgsql, and `OncoBridge.Infrastructure` |
| `OncoBridge.Infrastructure` | `Hl7.Fhir` |
| `OncoBridge.Api` | ASP.NET Core (until P5) |

Plus two exclusivity rules: only `Interop.Fhir` may reference `Hl7.Fhir.*`, and only
`Infrastructure` may reference EF Core or Npgsql.

Reading the project files catches a forbidden reference the moment it is *declared* — necessary
because the compiler omits unused references from assembly metadata, so a declared-but-unused
package would be invisible to reflection alone. Reading the assembly graph catches anything
arriving by a route the project file does not spell out.

**Solution format:** `.slnx`. The .NET 10 SDK supports it in `dotnet build` and `dotnet test`, and
it drops the GUID bookkeeping that makes classic `.sln` files merge badly. The cost is that older
tooling cannot open it, which is acceptable for a project already pinned to .NET 10.

**SDK pinning:** `global.json` pins `10.0.400` with `rollForward: disable`. This is the strictest
option and will fail loudly on a machine with a different SDK patch, which is the intended
behaviour — silent roll-forward to a newer major is precisely what must not happen.

## Consequences

- `OncoBridge.Api` is a plain class library, not `Microsoft.NET.Sdk.Web`. The Web SDK would
  implicitly framework-reference ASP.NET Core, contradicting the phase gate. It converts in P5.
- Phase-scope tests are written to be **deleted** by the phase that invalidates them; their failure
  is the signal that a phase has begun, and removing one is a deliberate recorded act. P2 retired
  `OncoBridge.Interop.Fhir.Tests/Phase1ScopeTests` and the
  `Phase1_has_no_persistence_or_FHIR_packages_anywhere` assertion, exactly as designed.
  `OncoBridge.Api.Tests/Phase1ScopeTests` remains and retires in P5.
- EF Core is pinned explicitly in `Infrastructure` rather than taken transitively. The Npgsql
  provider depends on an older EF Core patch and `Microsoft.EntityFrameworkCore.Design` is
  `PrivateAssets=all`, so without an explicit pin the API project resolved a different EF Core than
  Infrastructure was compiled against, producing assembly-conflict warnings.
- Generated EF migrations are marked `generated_code = true` in `.editorconfig`. Hand-written style
  rules should not apply to scaffolder output, and this keeps `dotnet ef migrations add` producing
  code that builds unmodified under `TreatWarningsAsErrors`.
- Central package management (`Directory.Packages.props`) keeps versions consistent across the test
  projects and makes "the production projects reference no packages at all" visible in one file.
