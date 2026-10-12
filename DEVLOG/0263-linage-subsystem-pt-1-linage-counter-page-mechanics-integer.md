## Entry 263 — LINAGE subsystem pt.1: LINAGE-COUNTER + page mechanics (integer LINAGE) → SQ201M/SQ209M

The user asked to "tackle the LINAGE-COUNTER parse form"; investigation showed it is not a parse fix at all —
the 4 SQ20xM tests need the whole LINAGE page-handling subsystem (ISO §13.18.34 / §14.9.51 / §8.4.3.14).
Built it for the integer-LINAGE case this entry (data-name LINAGE phrases are pt.2):

- **LINAGE-COUNTER special register (§8.4.3.14)** — a read-only numeric *value* (not storage). Grammar:
  `dataReference : LINAGE_COUNTER ((OF|IN) cobolWord)? | …`. New `BoundLinageCounterExpression(file)`
  (binder resolves the optional file qualifier, else the single LINAGE file via `SemanticModel.FindLinageFile`),
  lowered to a new `IrLinageCounter` numeric expression → `FileRuntime.GetLinageCounter` (returns decimal,
  like any numeric operand). Wired into the two value-consumption paths: MOVE source (via `IrComputeStore`)
  and comparison/UNTIL operands (via `ComparisonOperand.FromArithmeticExpression`).
- **Counter maintenance (§13.18.34 GR7)** — `SequentialFileHandler.AdvanceLinageCounter` rewritten:
  ADVANCING n → +n; plain WRITE → +1 (GR7c3, wired in `WriteRecord`); ADVANCING PAGE → reset to 1 (GR7c1);
  page overflow (counter > body) → reset to 1 + end-of-page (GR26a); footing-area (counter ≥ footing start)
  → end-of-page (GR26b). OPEN OUTPUT already set the counter to 1. Wired into `WriteAdvancing`/`WriteRecord`.
- **AT END-OF-PAGE / NOT AT END-OF-PAGE phrases (§14.9.51 GR26–28)** — the grammar already parsed
  `writeAtEndOfPage` but `BindWrite` dropped it. Bound it onto `BoundWriteStatement` (new AtEndOfPage /
  NotAtEndOfPage lists); `LowerWrite` now returns a continuation block and, when EOP phrases are present,
  emits `IrCheckEndOfPage` (→ `FileRuntime.WasEndOfPage`) and branches via `LowerConditionalBranch` (the
  same shape as READ … AT END). New `IrCheckEndOfPage` IR + `EmitCheckEndOfPage`.

**SQ201M** (LINAGE 50 FOOTING 45 TOP 10 BOTTOM 6): 12 auto-checks (LINAGE-COUNTER after OPEN=1, after
ADVANCING PAGE=1, after WRITE/ADVANCING sequences, and the four END-OF-PAGE phrase combinations) all PASS,
0 FAIL* (11 remaining are CCVS visual-INSPECTION items). **SQ209M** (LINAGE 40, no footing): 0 FAIL*.
Both baselined. Guard ALL GREEN: 1000 unit / 347 integration / **221 NIST** (94 NC + 42 IF + 12 SM + 4 IC +
48 SQ + 19 RL + 2 IX), 0 regressions — the WRITE-path changes (high blast radius: every report write) are
gated on `LinageBody > 0` / presence of EOP phrases, so non-LINAGE writes are unaffected.

**SQ208M/SQ210M use data-name LINAGE phrases** (`LINAGE LINAGE-CTR FOOTING FOOT-CTR …`), whose values are
read at OPEN OUTPUT (§13.18.34 GR6b); with the page params unset the counter never advances and
`PERFORM … UNTIL LINAGE-COUNTER EQUAL 66` hangs. Data-name LINAGE is pt.2.

