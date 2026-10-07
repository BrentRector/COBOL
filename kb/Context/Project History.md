---
title: Project History
area: context
status: draft
last_updated: 2026-07-23
related_files:
  - DEVLOG.md
  - PROMPT.md
  - CLAUDE.md
  - docs/COBOLNET_DESIGN.md
  - README.md
tags:
  - cobolsharp
  - context
---

# Project History

The narrative record is [[DEVLOG]] (2.6 MB, **DESCENDING** — newest entry first; the sole how-we-got-here history,
since every other doc reads as current state). See [[kb/Context/Doctrine & Anti-Patterns]].

## The arc
The project began as **CobolSharp**, a COBOL→.NET compiler built around a byte `ProgramState` substrate with a phased
pipeline (grammar → semantics → bound tree → IR lowering → CIL emission → runtime), validated against the NIST CCVS
COBOL-85 suite. That era's scaffolding survives in the `create-*.ps1` scripts (phases 0–21, IR-lowering agents,
~75% grammar coverage, ~75% NIST passing). See [[kb/Modernization/Audit Artifacts]].

## The 2026-06-08 owner-directed PIVOT
The owner reframed the effort as a blank-slate, spec-first rewrite → **COBOL.NET** (compiler `cobol.exe`,
`src/Cobol.Net.*`): COBOL translated to **idiomatic, typed-native C#** compiled by Roslyn — a COBOL record IS a .NET
`record struct`, an elementary item IS a native field, numerics are native scaled integers. **There is NO byte
substrate; the legacy engine is deleted from `main` (P15 Cut 2).** The
PROMPT.md non-negotiable rules were "repeatedly corrected 2026-06-08." See [[kb/Architecture/High-Level Design]].

## The G0–G8 greenfield build order
Drives the rearchitecture (design SSOT [[docs/COBOLNET_DESIGN]]): bound-tree pipeline with no lowered IR, `Place`
lvalue, PC-dispatcher control flow, then REDEFINES, files, OO, EC, intrinsics — culminating in G8, the legacy
retirement/cut-over (P15). See [[kb/Modernization/Tasks]].

## Current state — branch `phase-14`, the spec-first conformance campaign
Recent DEVLOG entries (1013–1019, 2026-07-23) are the INDEPENDENT-MINORS batch closing 8/8: CA17 (indexed-REWRITE
prime-key collating), CA8+V56 (float sign/relation), CA3 (figurative DISPLAY under a PCS), CA19+CA20 (UNSTRING
category screens), CA18 (line-seq REWRITE in place), CA26 (the alphanumeric repertoire is Unicode/UTF-16), plus DA1
(a hex literal in an ALPHABET clause now decodes) discovered and fixed mid-batch. Each entry cites the exact ISO § and
records blast radius + gate results.

> DEVLOG preamble: *"Ordering: DESCENDING — newest entry FIRST … Add a new entry by inserting it directly under this
> note."*

## Key concepts
- CobolSharp (the legacy byte-array engine, NIST-oracle era) → **2026-06-08 pivot** → COBOL.NET (typed-native, spec-first).
- DEVLOG.md is the ONLY historical doc; descending order; real date+time header stamps per commit.
- Legacy compiler survives only as a differential oracle until G8/P15 cut-over.
- Doctrine sources: [[PROMPT]], [[CLAUDE]], [[CONSTRAINTS]].

## See also
- [[kb/Context/Goals]] — the North Star & process rules.
- [[kb/Context/Doctrine & Anti-Patterns]] — the engineering doctrine.
- [[kb/Modernization/Tasks]] — the phase roadmap.
- [[kb/Architecture/High-Level Design]] — the post-pivot design.

## Backlinks
- [[kb/Context/MOC]] · [[kb/Index]] — link here.
