---
title: Frequently Asked Questions
area: search
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - search
---

# Frequently Asked Questions

## What is COBOL.NET?
A compiler (`src/Cobol.Net.*`, exe `cobol`) that translates standard COBOL into **idiomatic, typed-native C#** which
Roslyn compiles into a .NET assembly. Built from the ISO/IEC 1989:2023 spec with support for editions 1985/2002/2014.
See [[kb/Architecture/High-Level Design]].

## Why "CobolSharp" *and* "COBOL.NET"?
The repository is `CobolSharp`; the *product* the current code builds is **COBOL.NET**. CobolSharp was the original
byte-engine compiler; a 2026-06-08 owner pivot began the blank-slate typed-native rewrite. The legacy `CobolSharp.*`
projects survive only as a differential oracle until the G8 cut-over. See [[kb/Context/Project History]].

## Is there an intermediate representation (IR)?
Yes and no. There is **one** IR — the typed **bound tree** — and **no separate lowered/branch IR**. C# already has
`if`/`while`/`switch`/`try`, so the design renders the bound tree straight to readable C#. The future CIL backend does
its *own private* lowering, never a shared phase. See [[kb/IR/Node Types]].

## How does COBOL control flow become C#?
Paragraphs/sections become cases in one `switch(pc)` program-counter dispatcher (`__Dispatch`), not separate methods —
so GO TO, ALTER, PERFORM THRU, and fall-through are all expressible. Only STOP RUN / GOBACK use exceptions. See
[[kb/IR/Control Flow]].

## Why no `decimal` for numbers?
Fixed-point numerics are a native integer holding the *unscaled* value with scale as compile-time metadata (`long`
≤18 digits, `Int128` for 19–38); `COMP-1/2` are `float`/`double`. This is faster, exact, and avoids `decimal`/
`BigInteger`. See [[kb/Runtime/Execution Model]].

## How does one compiler target four ISO editions?
`--std 85|2002|2014|2023` (default 2023). The grammar parses a *superset*; the binder is edition-agnostic; a single
`VersionConformancePass` gates each construct against the target edition (accept, or reject/warn with an edition
diagnostic). `--permissive` softens removals for migration. See [[kb/Spec/Version Targeting]] and
[[kb/Semantics/Passes]].

## What does "done" mean for this project?
Owner decision **D13**: 100% conformance per ISO §4.2.1 across all four editions. Mechanically, **done = the P14
Step-0 four-edition traceability inventory at zero-GAP.** Optional modules may remain documented non-support. See
[[kb/Context/Goals]] and [[kb/Modernization/Tasks]].

## Where is the authoritative behavior defined?
The ISO/IEC 1989:2023 spec ([[specs/ISO_COBOL]], a private submodule) — cite the `§` for any behavior question. The
NIST goldens and the GnuCOBOL differential are regression *nets*, not authority. See [[kb/Spec/Overview]].

## What is the CONFORMANCE-FIX-QUEUE?
The current work-list: audit-surfaced defects re-verified against the spec, each with a decision-complete fix and a
spec-derived golden. 46 items, **30 landed / 16 remain** (as of 2026-07-23). See
[[kb/Modernization/Audit Artifacts]].

## How do I build & test it?
`dotnet build` (regenerates ANTLR if stale — needs Java + pwsh), `dotnet test`, and `bash scripts/guard.sh` for the
full NIST regression gate. .NET 10 / C# 14. See [[kb/Compiler/Build System]].

## What are the design SSOTs I should read first?
[[docs/COBOLNET_DESIGN]] (the decision-complete design SSOT) and [[docs/COBOLNET_REARCHITECTURE_PLAN]] §0 (the live
plan/resume state). Then the per-subsystem `docs/COBOLNET_*_DESIGN.md` deep-dives. See
[[kb/Architecture/MOC]].

## See also
- [[kb/Search/Glossary]] · [[kb/Search/Key Concepts]]

## Backlinks
- [[kb/Search/MOC]] · [[kb/Index]] — link here.
