## Entry 665 — 2026-07-07 16:45 PDT — Rearchitecture & 100%-ISO master roadmap (multi-agent review) — DOCS ONLY

Owner-directed: a full architectural code review of the greenfield compiler + a clean-slate rearchitecture &
reimplementation plan (permission to revisit everything grammar→codegen, rename/reorg files/folders, incorporate ALL
100% ISO pending work), delivered as a COMPLETE, execution-grade, RESUMABLE document set that future sessions execute
phase-by-phase until "clean architecture + 100% ISO". **NO production code changed.**

**How it was produced.** A 5-phase Workflow (wf_7720e6f8-fd6, 46 agents, 7.3M tok): Survey (15 subsystem + 2
cross-cutting readers) → Critique (6 dimensions incl. an exhaustive ISO-pending inventory) → Design (8 dimension owners,
each writing a `DESIGN-*.md`) → Roadmap skeleton (1 xhigh agent → the ordered phase decomposition) → Phase authoring
(1 agent per phase → a step-by-step `PHASE-NN.md`). 6 units hit the StructuredOutput schema-retry cap (the largest:
the CSharpEmitter / data-model-coherence / frontend-plumbing / runtime-IO / runtime-value surveys + the encapsulation
critique) and were BACKFILLED as plain-text agents, each writing its artifact + a ROADMAP GAP CHECK. Owner mid-course
correction: the dual-backend goal ([[project_dual_backend_goal]] — selectable Roslyn↔CIL codegen behind
`ICodeGenBackend` over ONE backend-neutral bound tree) was under-weighted → a focused agent ELEVATED it
(`DESIGN-backend-abstraction.md` + `PHASE-16` + the IR-neutrality additions PHASE-05/06/07 must enforce; **Mono.Cecil**
chosen for portable-PDB / sequence-point support — Reflection.Emit lacks it).

**Deliverable (~15,000 lines under `docs/`):** `docs/COBOLNET_REARCHITECTURE_PLAN.md` (the master — resume protocol,
north-star 5-assembly architecture, 11 principles, the dual-backend §3 mandate, a 17-phase dependency-ordered index
with status checkboxes, an owner-decisions table [~12 forks, 2 resolved], §7 the six backfill refinements) + `docs/
rearchitecture/`: 9 `DESIGN-*.md`, 6 `SURVEY`/`CRITIQUE-*.md` (the highest-risk units, each with a gap check), 17
`PHASE-00..16-*.md`.

**The 17-phase roadmap** (F/R/I/C tracks, each independently landable + battery-green at its boundary): 00 safety net →
01 rename + dead-grammar/JSON-XML delete → 02 `Cobol.Net.Editions` leaf + first-class diagnostic registry → 03
version-gating → 04 frontend `Cst` façade → 05 unified data model (`StorageForm`, killing the 7-site mutable
`StoreAsImage`) → 06 real binder pass-DAG + `SymbolTable` + immutable `BoundCompilation` → 07 exhaustive visitor +
god-class decomposition (+ the backend seam-proof at Step 13) → 08 runtime `RunUnit`/`FileConnector` reorg → 09 M2 OO →
10 M2 residual → 11 intrinsics-to-zero + Tier-C codec → 12 M3 (2014) → 13 M4 (2023) + EC remnants → 14 matrix closure →
15 G8 legacy retirement → 16 CIL/Cecil backend + equivalence harness.

**Validated findings:** the numeric model IS coherent (one `CobolNum.Store` funnel; COMP-3 == DISPLAY at rest, usage in
`NumProfile.Truncation` — typed-native confirmed); the headline invariant breach is the emitter mutating the binder's
`DataItem` (`MarkStoreAsImage`); the bound tree is C#-shaped today (`Place` + several statement nodes carry C# strings)
so neutralization must reach `BoundTree.cs` + the program skeleton, not just `Place`. JSON/XML re-confirmed NON-ISO
(deleted in Phase 01). Baseline unchanged: 2028 conformance · 213 unit GREEN. **NEXT:** owner reviews the plan + the
~12 §6 decisions; execution begins at Phase 00. The TYPEDEF/ISO feature work (DEVLOG ≤664) and this rearchitecture are
two tracks the owner will sequence.

