# DESIGN — User documentation, publication and distribution

> **Status:** design, owner decisions recorded in `kb/Work/R50.md` (2026-09-26). The work that implements this doc
> is tracked ONLY in `kb/Work/` (notes PB1610–PB1614, filed from §7) — this doc never carries a worklist.

## §1 The obligation

ISO/IEC 1989:2023 §4.2.16 (`cite.py --check 4.2.16` OK): *"An implementation shall satisfy the user documentation
requirements specified in 4.2.3, 4.2.4, 4.2.5, 4.2.6, 4.2.10, 4.2.12, and 4.2.13 by specification in at least one
form of documentation."* Any form qualifies — manuals, on-line documentation, help screens — and a requirement may be
met by reference to other documents.

| Clause | Subject | Current source of truth |
|---|---|---|
| §4.2.3 | Interaction with non-COBOL runtime modules | .NET interop: `COBOLNET_INTERPROGRAM_DESIGN.md`, R48 (STOP RUN under a .NET host) |
| §4.2.4 | Interaction between COBOL implementations | not yet written as user text (migration chapter) |
| §4.2.5 | Implementor-defined language elements | `docs/CONFORMANCE.md` §7 (Annex A.1 register, 182/182) + §3 determinations |
| §4.2.6 | Processor-dependent language elements | `docs/CONFORMANCE.md` §2 (Annex A.3) + §4 documented non-support |
| §4.2.10 | Nonstandard extensions | `docs/CONFORMANCE.md`, `GnuCOBOL extensions.md`, dialect gating |
| §4.2.12 / §4.2.13 | Archaic / obsolete elements | `constructs.json` + `VERSION_CHANGE_REFERENCE.md` (edition matrix) |

The facts exist; what is missing is a USER-facing presentation, versioned with the compiler.

## §2 Decisions (R50)

1. **Docs-as-code, not a wiki.** The §4.2.16 documentation describes ONE compiler version, so it is versioned,
   reviewed and CI-checked with the code (the rustc book / Reference, CPython `Doc/`, GnuCOBOL's Texinfo manual,
   dotnet/docs all do this). A wiki is unversioned, unreviewed and cannot be generated.
2. **Astro Starlight**, hosted on **wiseowlsoftware.com** (the existing Astro site, which carries Demeanor's docs).
3. **The home page bifurcates** into two products; **Demeanor remains primary**.
4. **NuGet distribution under the `WiseOwl.` prefix**, as Demeanor ships (`WiseOwl.Demeanor`, `WiseOwl.Demeanor.MSBuild`).

## §3 Source layout and publication

**What a reader sees:** ordinary web pages at `https://wiseowlsoftware.com/cobol/docs/` — browse, search and link
like any documentation site, nothing to download or install. A version picker switches between releases;
`/cobol/docs/latest/` is the default. Everything below is BUILD plumbing that readers never see.

- **Source:** `docs/manual/` in THIS repo, Starlight-compatible Markdown/MDX (frontmatter `title`, `description`,
  `sidebar`). Hand-written chapters live there; GENERATED chapters are written by generators into
  `docs/manual/reference/` at build time and are never hand-edited (a header comment says so).
- **Bundle (a build input, never a user download):** the release pipeline produces `docs-bundle-<version>.zip` (hand-written + generated pages, plus an
  `index.json` with version and commit) as a release artifact. `main` publishes a rolling `next` bundle.
- **Site:** the wiseowlsoftware.com repo mounts Starlight at `/cobol/docs/` and its BUILD (CI, at deploy time) fetches the bundles into
  `src/content/docs/cobol/<version>/`; `/cobol/docs/latest/` aliases the newest release. The site repo owns theme,
  navigation and the bifurcated home page; this repo owns the content. Neither copies the other's files by hand.
- **Why a bundle, not a submodule or a copy:** the site builds any number of versions without checking out old
  compiler trees, and the conformance statement a user reads is byte-for-byte the one that shipped with their version.

## §4 Content map

**Hand-written** (`docs/manual/`):
- *Install* — `dotnet tool install -g WiseOwl.COBOL`; prerequisites (.NET 10 runtime); verifying the install.
- *Getting started* — hello world; compile, run, the produced assembly.
- *User guide* — the `cobol` command; `--std 85|2002|2014|2023` and `--permissive`; reference format (fixed / free /
  `>>SOURCE`); COPY libraries and search; files and I-O (organizations, the Latin-1 no-CODE-SET rule of R47, status
  codes); run units and STOP RUN under a .NET host (R48); locale and collation (A.4.9); exception handling.
- *.NET interoperability* (§4.2.3) — calling COBOL from C# and C# from COBOL; the runtime package.
- *Migrating* (§4.2.4) — from IBM Enterprise COBOL, Micro Focus and GnuCOBOL: what is standard, what is a dialect
  extension, what CobolSharp rejects and why.
- *Release notes* — per version, derived from the DEVLOG entries between release tags (edited, not dumped).

**Generated** (`docs/manual/reference/`), each by one generator with a drift test:
- *Conformance statement* (§4.2.5, §4.2.6, §4.2.10) — from `docs/CONFORMANCE.md` §2–§5, §7 and the traceability
  inventory: the implementor-defined register, processor-dependent dispositions, optional-module (A.4) claims,
  documented non-support with its warning codes, and the limits.
- *Edition matrix* (§4.2.12, §4.2.13) — from `constructs.json` + `VERSION_CHANGE_REFERENCE.md`: per construct, the
  introducing edition, archaic/obsolete status, and what `--std` does with it.
- *Command-line reference* — from `CliOptions` (one source for `--help` and the page).
- *Diagnostics reference* — from the diagnostic catalog: one entry per `COBOLNET####` code with severity, the rule it
  enforces (§ citation) and a minimal example.
- *Runtime API reference* — from the runtime assembly's XML documentation comments.

## §5 Drift tests (what keeps "generated" true)

One drift test per generator, in the Conformance or Unit assembly: regenerate into a temp directory and compare with
`docs/manual/reference/`; a new diagnostic code, CLI option, `constructs.json` row or CONFORMANCE.md determination
without its page is RED. Every citation a generated page prints passes `cite.py --check` (the generator runs it).

## §6 Distribution (proposal until PB1613 lands)

| Package | Content | Notes |
|---|---|---|
| `WiseOwl.COBOL` | the compiler as a .NET global tool, command `cobol` | `PackAsTool`, `ToolCommandName=cobol` |
| `WiseOwl.COBOL.Runtime` | the runtime library compiled programs reference | versioned in lockstep with the compiler |
| `WiseOwl.COBOL.MSBuild` | optional: compile `.cob` items inside a .csproj | mirrors `WiseOwl.Demeanor.MSBuild`; decide in PB1613 |

License metadata: `PackageLicenseExpression` `BUSL-1.1` (the SPDX id of the Business Source License 1.1) or
`PackageLicenseFile` pointing at `LICENSE` — settled in PB1613 against how Demeanor's packages declare theirs. Package
README = the Install + Getting started pages.

## §7 Work items

Filed as `kb/Work` notes (rule 8 — no list here): **PB1610** manual scaffold + hand-written chapters · **PB1611**
conformance-statement and edition-matrix generators + drift tests · **PB1612** CLI and diagnostics reference
generators + drift tests · **PB1613** NuGet packaging and release pipeline (including the docs bundle) ·
**PB1614** wiseowlsoftware.com: Starlight integration at `/cobol/docs/` and the two-product home page.
