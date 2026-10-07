---
title: Spec Overview — ISO/IEC 1989:2023
area: spec
status: draft
last_updated: 2026-07-23
related_files:
  - specs/ISO_COBOL.md
  - docs/CONFORMANCE.md
  - CLAUDE.md
  - PROMPT.md
  - docs/DOC_INDEX.md
tags:
  - cobolsharp
  - spec
---

# Spec Overview — ISO/IEC 1989:2023

**ISO/IEC 1989:2023** is the third edition (2023-01) of the international COBOL standard — successor to the
1985, 2002, and 2014 editions. The full normative text lives in the repo as a private submodule at
[[specs/ISO_COBOL]]. It is treated as the **single authoritative source**: the #1 non-negotiable process rule
([[CLAUDE]], [[PROMPT]]) is that *the spec defines correct behavior for every case — read it and cite the exact
`§`/General-Rule for any semantics, syntax, or output question.* The NIST goldens and the GnuCOBOL differential are demoted to
regression nets "with known holes," never authority. See [[kb/Context/Goals]].

## How the spec is organized

16 top-level clauses plus annexes (from the in-repo table of contents):

- **1** Scope · **2** Normative references · **3** Terms and definitions
- **4** Conformance — the pivotal clause (see §4.2.16 below)
- **5** Description techniques · **6** Reference format (fixed / free form) · **7** Compiler directing facility (COPY / REPLACE + the `>>` directives)
- **8** Language fundamentals (character sets, alphabets, locales, lexical elements, PICTURE concepts)
- **9** I-O, objects, and user-defined functions · **10** Structured compilation group
- **11** Identification division · **12** Environment division · **13** Data division · **14** Procedure division
- **15** Intrinsic functions · **16** Standard classes
- **Annexes** (heavy for gating): **A** implementor-defined / processor-dependent / optional-element registers (A.1/A.3/A.4); **E** the 2014→2023 substantive change list; **F** archaic (F.1) / obsolete (F.2) designations.

## Conformance record (§4.2.16)

[[docs/CONFORMANCE]] is the implementor's user documentation required by §4.2.16 (and §4.2.6 / §4.2.7 / §4.2.13).
For every processor-dependent element in Annex A.3 and every optional A.4 module it states support as
**Claimed / Partial / Not claimed**, and pins implementor determinations (I-O status codes, rounding, STOP RUN exit
status). Syntactically-detectable unsupported elements emit a compile-time warning in the **COBOLNET1560 band** per
§4.2.6 ¶3. See [[kb/Semantics/Validation Rules]].

## Documented non-support facilities

Five whole facilities are not implemented: **MCS asynchronous messaging** (SEND/RECEIVE), **COMMIT/ROLLBACK**,
**VALIDATE**, **SCREEN handling**, and the **Locale facility**. Four are recognized-and-warned
(SCREEN→COBOLNET1560, MCS→1578, COMMIT/ROLLBACK→1579, VALIDATE→1580); the locale module is rejected at bind with the
**COBOLNET1518 error** (the A.4.1 unclaimed-optional posture) — RETIRED at kb/Work PB64 T6: the locale module is claimed whole and the diagnostic is deleted (its code is never reallocated).

## Four-editions mission

The North Star (owner decision **D13**): a compiler **100% conforming per §4.2.1 across all four editions** —
1985, 2002, 2014, 2023 — selected by `--std 85|2002|2014|2023`, default **COBOL-2023** (or 85 under `--nist`).
"Done" is defined as the **PHASE-14 Step-0 traceability inventory** (every Annex A.1 required documentation item) at
zero GAP. See [[kb/Spec/Version Targeting]] and [[kb/Modernization/Tasks]].

## Key concepts

- Spec = authority; cite `§`/GR before implementing (`feedback_spec_is_the_oracle`).
- 16 clauses; Annexes A / E / F drive conformance and edition gating.
- §4.2.16 user-documentation obligation → [[docs/CONFORMANCE]].
- Annex A.1 lists ~222 implementor-defined elements (199 requiring documentation) — the definition of "done."
- Four non-support facilities (the locale facility left §4 at PB64 T6 — claimed whole); the COBOLNET1560-band warnings.
- Default edition 2023; four editions via `--std`.

## See also

- [[kb/Spec/Language Features]] — the implemented language surface.
- [[kb/Spec/Version Targeting]] — four-compilers-in-one, edition gating.
- [[kb/Spec/Constraints]] — reference format, directives, non-support.
- [[kb/Semantics/Validation Rules]] — the diagnostic bands that enforce §4.2.6.
- [[kb/Modernization/Tasks]] — the P14 traceability inventory (definition of done).

- **Canonical docs (SSOT, now in-graph):** [[docs/CONFORMANCE]] (the §4.2.16 record) · [[docs/DOC_INDEX]] · [[CLAUDE]] · [[PROMPT]].

## Backlinks

- [[kb/Index]] — links here as the Spec entry point.
- [[kb/Spec/MOC]] — indexes this note.
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Validate (conformance) phase maps here.
