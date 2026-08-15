# ADR-0007 — Modular monolith with an executably enforced dependency boundary

**Status:** Accepted · **Phase:** 1

## Context

The project needs enough structure to demonstrate real architectural boundaries and little enough
that a reviewer can hold it in their head. Two failure modes are equally bad: one project per
concept, which is architecture astronautics; and one project total, which proves nothing about
boundaries.

Separately, the boundary that matters most (ADR-0001) is worthless if it is only a convention.

## Decision

**Five production projects, three test projects, one Angular app later.** Sub-structure lives in
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

**The boundary is a test, not a convention.** `DomainBoundaryTests` asserts four things, using two
complementary techniques because either alone has a hole:

1. `OncoBridge.Domain.csproj` declares zero `PackageReference` items.
2. `OncoBridge.Domain.csproj` declares zero `ProjectReference` items.
3. The loaded domain assembly's reference graph contains no `Hl7.Fhir`,
   `Microsoft.EntityFrameworkCore`, `Npgsql` or `Microsoft.AspNetCore`.
4. Only `OncoBridge.Interop.Fhir` may declare an `Hl7.Fhir.*` package reference.

Reading the project files catches a forbidden reference the moment it is *declared* — necessary
because the compiler omits unused references from assembly metadata, so a declared-but-unused
package would be invisible to reflection alone. Reading the assembly graph catches anything
arriving by a route the project file does not spell out. Checks 1–4 keep working once P2 and P3
actually add these packages.

**Solution format:** `.slnx`. The .NET 10 SDK supports it in `dotnet build` and `dotnet test`, and
it drops the GUID bookkeeping that makes classic `.sln` files merge badly. The cost is that older
tooling cannot open it, which is acceptable for a project already pinned to .NET 10.

**SDK pinning:** `global.json` pins `10.0.400` with `rollForward: disable`. This is the strictest
option and will fail loudly on a machine with a different SDK patch, which is the intended
behaviour — silent roll-forward to a newer major is precisely what must not happen.

## Consequences

- `OncoBridge.Api` is a plain class library in Phase 1, not `Microsoft.NET.Sdk.Web`. The Web SDK
  would implicitly framework-reference ASP.NET Core, contradicting the Phase 1 gate. It converts in
  P5.
- Two test projects exist with only scope-assertion tests. Their reference direction is fixed from
  the start so it is never retrofitted.
- Phase-scope tests (`Phase1ScopeTests`, `Phase1_has_no_persistence_or_FHIR_packages_anywhere`) are
  written to be **deleted** by the phase that invalidates them. Their failure is the signal that a
  phase has begun, and removing one is a deliberate recorded act.
- Central package management (`Directory.Packages.props`) keeps versions consistent across the test
  projects and makes "the production projects reference no packages at all" visible in one file.
