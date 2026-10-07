---
title: Goals & Process Rules
area: context
status: draft
last_updated: 2026-07-23
related_files:
  - PROMPT.md
  - CLAUDE.md
  - CONSTRAINTS.md
  - specs/ISO_COBOL.md
  - docs/COBOLNET_DESIGN.md
  - docs/VERSION_TEST_MATRIX_DESIGN.md
tags:
  - cobolsharp
  - context
---

# Goals & Process Rules

## North Star (owner decision D13)
A **commercial-quality, decades-sustainable COBOL compiler, 100% CONFORMING to ISO/IEC 1989:2023 per §4.2.1, with
correct support for all prior editions (1985 / 2002 / 2014).** "100% conforming" = the mandatory core of each edition
complete + every required implementor-documentation item; optional modules/processor-dependent elements may remain
*documented non-support* (the `CONFORMANCE.md` dispositions are part of the deliverable, not a waiver). "Implement
every optional module" is explicitly NOT the target. **DONE is defined mechanically:** the P14 Step-0 four-edition
traceability inventory reaching zero-GAP. Validated by the VERSION TEST MATRIX; default `--std` is COBOL-2023. Release
milestones (D14): **v1.0 = P15 exit**; **v2 = P16 CIL backend**. See [[kb/Spec/Overview]] and
[[kb/Modernization/Tasks]].

## The four NON-NEGOTIABLE process rules (PROMPT.md / CLAUDE.md)
Owner-emphasized, repeatedly corrected:

1. **The ISO spec is authority for EVERY case** — read [[specs/ISO_COBOL]] and cite the § (in code + DEVLOG) for any
   semantics/syntax/output question. Never guess; never infer behavior from the NIST goldens or the GnuCOBOL differential (regression nets with
   known holes, NOT authority).
2. **Implement each feature FROM its subsystem deep-dive design doc** ([[docs/COBOLNET_DESIGN]] §0.5 indexes them) +
   the spec — follow the doc, do not improvise.
3. **Implement the COMPLETE feature to the spec + design — NEVER scope to what a test references.** Tests VERIFY; they
   do not bound what to build.
4. **Keep the docs CURRENT** — a change that supersedes a deep-dive updates that deep-dive in the same change set;
   every doc except DEVLOG reads as current state (strip how-we-got-here history).

> PROMPT.md rule 1: *"The ISO/IEC 1989:2023 spec defines the correct behavior for EVERY case … READ the spec and CITE
> the § … Never guess; never infer behavior from a regression net."*

## Supporting standing rules
- **Typed-native, no byte substrate** — the greenfield `src/Cobol.Net.*` never routes through a byte `ProgramState`;
  native scaled numerics; every NEW subsystem is typed-native.
- **Singular pattern** — one mechanism per job (the best one); two coexisting mechanisms is the anti-pattern; resolve
  the structural singular-pattern at design time.
- **Never default to deferral** — the complete correct-per-spec implementation regardless of effort; a
  GAP/reject-legal-source is debt, allowed only by explicit owner decision.
- **Root-cause fixes only** — never work around a compiler bug by editing valid source; fix the compiler.
- **Fully autonomous** — commit AND push every checkpoint; a conformance test + a DEVLOG entry per feature commit;
  guard-green every commit.
- **Spec-first is the ONLY going priority** — convert each verified fix into a spec-derived golden.

See [[kb/Context/Doctrine & Anti-Patterns]] for the anti-pattern catalog these rules defend against.

## Key concepts
- D13 conformance target; DONE = P14 Step-0 zero-GAP; four editions via the version matrix.
- Spec-is-authority, implement-from-design-doc, complete-to-spec, keep-docs-current.
- No byte substrate; singular pattern; no-deferral-default; root-cause-only.

## See also
- [[kb/Spec/Overview]] · [[kb/Spec/Version Targeting]]
- [[kb/Context/Project History]] · [[kb/Context/Doctrine & Anti-Patterns]]
- [[kb/Modernization/Tasks]]

## Backlinks
- [[kb/Index]] — "Start here."
- [[kb/Context/MOC]] — indexes this note.
