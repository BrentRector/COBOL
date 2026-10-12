## Entry 329 — Owner decision: high-subset profile (option A); Class-B obsolete-flagging pinned to 3 warnings

The owner chose **option A** from Entry 328: CobolSharp declares the **high/full COBOL-85 subset**, so the
Class-A subset-level `…M` flagging modules (IF401M/402M/403M, IX301M/401M, RL301M/401M, SQ401M, SM301M/401M, …)
are **documented N/A** — no `--subset` mode. Locked in plan §3.

Pinned the Class-B implementation precisely. Under `--standard cobol85`, NC303M **already** emits 3/4 flags —
CBL3602 (ALTER) + CBL3606 (bare GO TO ×2), the obsolete-warning paths already exist in `ControlFlowBinder`. The
entire remaining Class-B gap is **3 missing obsolete-element warnings**, sites identified:
- **DATE-COMPILED** paragraph (NC303M's 4th flag) — currently commented out by the Stage-0
  `ReferenceFormatProcessor` (Entry 322); warn at that point or re-parse it.
- **MULTIPLE FILE TAPE** clause (SQ303M) — `SemanticBuilder.VisitIoControlClause`; `IoControlClause` has no
  direct `multipleFileClause()` accessor, so its parse path needs tracing.
- **OPEN … REVERSED** phrase (SQ303M) — `FileIoBinder.BindOpen`, via `OpenFileSpecContext.REVERSED()` (confirmed).

Next increment: add a generic obsolete-element descriptor (CBL3607) + emit at the three sites (gated like
CBL3602/3606, i.e. `< cobol2002`), then a flagging harness asserting NC303M=4 and SQ303M=2 obsolete flags under
cobol85. Analysis tick — no code yet.

