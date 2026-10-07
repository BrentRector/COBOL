---
title: Modernization — Tasks & Roadmap
area: modernization
status: draft
last_updated: 2026-07-23
related_files:
  - docs/COBOLNET_REARCHITECTURE_PLAN.md
  - docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md
  - docs/COBOLNET_DESIGN.md
  - docs/DOC_INDEX.md
tags:
  - cobolsharp
  - modernization
---

# Modernization — Tasks & Roadmap

The rearchitecture roadmap lives in [[docs/COBOLNET_REARCHITECTURE_PLAN]] — **THE ONE PLANNING DOCUMENT** (owner
directive 2026-07-19; it absorbed `resume-prompt.md`, all 17 per-phase docs, and the P13 audit). Its **§0 LIVE RESUME
STATE** is the single-write live-state SSOT; every session reads §0 first and updates it before ending.

## 17 phases (00–16), dependency-ordered across four tracks
(F = foundation, R = rearchitecture, I = feature/ISO, C = cleanup)

- **Done (00–12):** 00 migration safety net → 01 namespace rename + dead-grammar removal → 02 `Cobol.Net.Editions`
  leaf assembly → 03 version-conformance pipeline (superset parse + one two-arm gating pass) → 04 frontend
  consolidation → 05 unified data model (`StorageForm`/`RecordLayout`) → 06 real binder (`SymbolTable`, immutable
  `BoundCompilation`) → 07 god-class decomposition + source-generated exhaustive visitor → 08 runtime reorg → 09 OO
  rearchitecture → 10 M2 residual catalog → 11 intrinsics-to-zero → 12 M3 (COBOL-2014) deltas.
- **In progress — P13/P14:** P13 (M4/COBOL-2023 deltas) core MERGED to `main` 2026-07-22 ("M4-beta"); its residue
  proceeds on branch `phase-14`. **P14 Step 0 = the FOUR-EDITION SPEC-TRACEABILITY INVENTORY** — the **D13 "definition
  of DONE"** instrument: every clause/statement/function/directive/format × edition mapped LIVE/STAGED/DISPOSED/GAP,
  driven to **zero-GAP**. A parallel lane runs the D17 OO mandatory-surface opening wave.
- **Future:** **P15 = v1.0** — G8 legacy retirement (three cuts + "CUT 2.5" D10 SUBSCRIPT-mode removal) + §4.2.16
  conformance docs + runtime namespace flip. **P16 = v2** — the CIL/Cecil backend, explicitly off the conformance
  critical path.

## The spec-first CONFORMANCE-FIX-QUEUE campaign
[[docs/rearchitecture/CONFORMANCE-FIX-QUEUE]] is the current work — audit-surfaced defects, independently re-verified
against the spec, each carrying a decision-complete fix and a **spec-derived golden** (expected value computed from the
ISO §, never copied from a regression net). Tally as of **2026-07-23: 46 total (44 confirmed + 2 owner-decided),
30 LANDED / 16 REMAIN.** The 16 remaining are the coordinated EC-infra + OO super-batch (shares
`EcBinder`/`EcEmitter`/`ExceptionState`, kept serial under one design pass) plus owner-decided CA14/V59. See
[[kb/Modernization/Audit Artifacts]].

## Dual-backend mandate (§2.1, D4/D5)
Selectable code-gen backends — Roslyn C# (primary) and direct-CIL/Cecil (P16) — behind `ICodeGenBackend` over ONE
bound tree that carries no C#; a structural `Place` lvalue keeps the tree backend-neutral. An interim
`BackendNeutrality` gate guards it until P16. See [[kb/Architecture/High-Level Design]].

## Key concepts
- §0 is the only live-state SSOT (single-write rule; duplication breeds stale banners).
- P14 Step-0 zero-GAP inventory = D13 "100% conforming × 4 editions" definition of done.
- Tiered testing: wave-local ~2 min gate per commit, full Conformance per accumulated batch pre-merge.
- Each fix ships with its spec-derived golden in the same commit; legacy differential is opt-in.
- D16 rescheduled the bulk of the §24 fix queue into P14's GAP-closing work.

## See also
- [[kb/Modernization/Audit Artifacts]] — the audits feeding the fix queue.
- [[kb/Context/Project History]] — how the project reached phase-14.
- [[kb/Context/Goals]] — the D13 conformance target.
- [[kb/Spec/Version Targeting]] — the version matrix validating the mission.

## Backlinks
- [[kb/Modernization/MOC]] · [[kb/Index]] — link here.
