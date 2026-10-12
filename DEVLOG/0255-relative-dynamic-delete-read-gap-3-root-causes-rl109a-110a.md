## Entry 255 — Relative DYNAMIC delete/read gap: 3 root causes → RL109A/110A/117A/118A + IX107A

Took on the relative runtime gap behind the RL update/verify chains (RL109A reads a file RANDOM,
REWRITEs every 5th record by COMP relative key; RL110A verifies sequentially). The headline symptom
("DELETE AT END PATH TAKEN", wrong post-update counts) turned out to be three distinct bugs, peeled
one at a time by tracing each FAIL* COMPUTED-vs-CORRECT to the responsible layer.

**(1) `XXXXX###` data-file ASSIGN never shared across run units.** The NIST preprocessor mapped the
produce/consume placeholders `XXXXP###`/`XXXXD###` → a shared `"TF###"` literal, but NOT the permanent
variant `XXXXX###`. So RL108A's `ASSIGN … XXXXX061` (the creator) and RL109A/RL110A's `XXXXX061`
(consumers) each fell to the program-id-qualified non-literal host path (DEVLOG 244) → three different
files. RL108A created its file in memory and never shared it; the consumers read nothing → every keyed
read INVALID KEY (masked as the file being empty). A test header confirmed the intent:
`X-61 - "LITERAL" IN "ASSIGN TO" CLAUSE FOR … DATA FILE`. Fix: in `NistPreprocessor`, map the
remaining `XXXXX###` ASSIGN operands to the same `"TF###"` literal as the P/D variants — so the file
persists (literal name) and is shared across run units (same name). **Misstep, then refined:** my first
cut was a blanket ASSIGN-anchored regex over all `XXXXX###`. The full guard caught that it regressed
**SQ130A** — DEVLOG 244 deliberately program-id-qualifies non-literal ASSIGN paths so SEQUENTIAL
absent-file status tests stay isolated (SQ130A's `XXXXX014`/`062`), and the blanket map re-shared them.
The discriminator is the file's **organization**: RELATIVE/INDEXED data files are shared across run
units (the X-card's whole point), SEQUENTIAL ones need isolation. So the final form is anchored to the
SELECT entry — `SELECT…\.` — and only rewrites `XXXXX###` when that entry contains `RELATIVE` or
`INDEXED`. SQ130A keeps its isolation; the relative chains share. (Lesson re-logged: lean on the full
guard before trusting a "no baseline uses this" grep — mine was wrong.)

**(2) Leniency L2 — `RELATIVE data-name` without KEY — wasn't binding the key.** With the file shared,
RANDOM REWRITE still returned status 23. `RelativeFileHandler.Rewrite` (random) uses `_pendingKey`, set
by an `IrSetRelativeKey` the lowerer emits before a random REWRITE — but only when
`ResolveRelativeKeyLocation` finds the file's `RelativeKey`, and it was null: RL109A's SELECT writes
`RELATIVE RL-FR1-KEY` (no KEY), which `relativeKeyClause : RELATIVE KEY IS? dataReference` (KEY required)
did not match, so the key data-name was never captured (it parsed harmlessly as the bare-RELATIVE
organization + a swallowed generic clause). Implemented L2 per the dialect model: grammar now
`RELATIVE KEY? IS? dataReference`, and — crucially — `relativeKeyClause` is ordered **before**
`organizationClause` in `fileControlClauses` so `RELATIVE <data-name>` binds as the key clause while a
lone `RELATIVE` (no following data-name) falls through to the organization. The binder already captured
`dataReference().GetText()`, so the key now resolves and `_pendingKey` is set. Dialect-gated:
`DiagnosticDescriptors.CBL3613`/`CBL3614` (error/warning), checked in `SemanticBuilder` (plumbed
`CompilationOptions` in via `BuildSemanticModel`) — accepted under `--nist`/Default, rejected under
`--standard cobol2023`.

**(3) REWRITE's INVALID KEY / NOT INVALID KEY phrases were never lowered — a general codegen bug.**
With the key bound, the rewrites persisted (RL110A verified 100 updates), yet RL109A still reported 100
"read invalid" via a fall-through: `LowerRewrite` emitted the rewrite + file status and returned,
without lowering `rw.InvalidKey`/`rw.NotInvalidKey`. So after a successful REWRITE the source's
`NOT INVALID KEY GO TO …` never branched and control fell through into the next paragraph (here, the
read-invalid counter). READ and DELETE lower these phrases; REWRITE didn't — affecting every file
organization, not just relative. Fix: `LowerRewrite` now takes `IrMethod`, returns `IrBasicBlock`, and
emits `IrCheckFileInvalidKey` + `LowerConditionalBranch(rw.InvalidKey, rw.NotInvalidKey, …)` exactly as
`LowerDelete` does; caller updated to `return`.

Result: the whole RL108A→RL109A→RL110A TF061 chain is CLEAN, and the L1/L2 unblocks made **RL117A,
RL118A** (self-contained relative tests) and **IX107A** (an `INVALID`-no-KEY READ form, L1) CLEAN and
deterministic. Baselined RL109A/RL110A (chain after RL108A, kept consecutive), RL117A/RL118A (self-
contained, listed first), and IX107A. RL206A/207A/208A and RL210A/211A remain — they are the relative
**variable-length record** subsystem (`RECORD VARYING`), a separate gap (RL206A's 22 FAIL* are all
"WRONG LENGTH RECORD" on create). **NIST baselines 203 → 208** (94 NC + 42 IF + 12 SM + 4 IC + 39 SQ +
**15 RL** + **2 IX**). Full guard ALL GREEN: 1000 unit / 348 integration / 208 NIST, 0 regressions.

