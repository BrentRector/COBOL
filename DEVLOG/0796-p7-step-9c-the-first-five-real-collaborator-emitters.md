## Entry 796 — 2026-07-11 16:17 PDT — P7 Step 9c: the first FIVE real collaborator emitters (Evaluate · Initialize · Corresponding · AlterSwitch · AcceptDisplay); DISPLAY moves home; two ratchet entries → 0

**What (the phase doc's 9c — low-coupling first).** Five partials become real classes over the per-unit
`EmitContext` (git-mv'd, history preserved): `Verbs/{Evaluate,Initialize,Corresponding,AlterSwitch}Emitter.cs`
NEW-by-move + `Verbs/AcceptDisplayEmitter.cs` converted in place from partial to class — and `EmitDisplay` MOVES
IN from the orchestrator, making the Step-5 filename honest (the census had flagged the DISPLAY half as never
relocated). Wiring per the plan: the host constructs them in `NewUnitEmitters()` at BOTH statement-bearing
per-unit context sites (program classes, OO class units; interface units emit no statements); the 8 `Dispatch.cs`
Visits re-point; the six hub methods the collaborators reach through the host during migration went
`internal` (`EmitStatementList`/`EmitMove`/`EmitArith`/`StoreArith`/`RcvFor`/`Narrow`). `EditCfg()`'s one-line
projection edge was severed (the class reads `ctx.EditCfgArgs` directly).

**Ratchet:** `RuntimeApi` grew its numeric/string/edit members (`NumParseDisplay`, `NumFormatUnsignedDisplay`,
`NumStore`, `NumFormatDisplay`, `StrStore`, `EditFormat`, `EditFormatAlphanumeric` — all `nameof`-anchored,
verified against the live runtime members); AcceptDisplay's 9 bare sites and Corresponding's 1 (a doc-comment
mention — the ratchet reads full text by design) are gone; both baseline entries DELETED. 17 files remain.

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact — the
RuntimeApi-routed fragments render byte-identically) — verdicts read as separate actions.

