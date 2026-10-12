## Entry 387 — M2-PROC-6: `GOBACK RETURNING` (ISO §14.9.16, COBOL-2002); EXIT variants verified; CONTINUE AFTER deferred

Implemented `GOBACK RETURNING identifier` (and the `GIVING` synonym) — the operand supplies the value returned
to the activating element. Semantics: it is equivalent to moving the operand into the PROCEDURE DIVISION
RETURNING item, then returning — so it reuses the existing CALL … RETURNING wiring (DEVLOG 365). Tiny, clean
change:
- Grammar `gobackStatement : GOBACK ({is2002()}? (RETURNING | GIVING) dataReference)?` — the phrase is
  **dialect-gated to 2002+** (verified: rejected under `--standard cobol85`).
- `BoundGoBackStatement` gains an optional `Returning` expression (bound in `BoundTreeBuilder` via
  `BindDataReferenceWithSubscripts`).
- The Binder lowers `GOBACK RETURNING x`, when the program has a `ProcedureReturningItem`, as a synthetic
  `BoundMoveStatement(x → returning-item)` through `_ctx.DataMovement.LowerMove`, then `IrGoBack`. With no
  RETURNING item the move is safely skipped (verified: compiles + runs, value harmlessly ignored — no crash).

Verified end-to-end: a subprogram `… RETURNING LK-R` doing `GOBACK RETURNING WS-SUM`, called by
`CALL … RETURNING R`, yields `R=0042` / `0013`. Conformance `tests/conformance/2002/goback_returning`.

**EXIT variants:** the grammar already covers EXIT PROGRAM/PERFORM[ CYCLE]/SECTION/PARAGRAPH/METHOD/FUNCTION,
and EXIT PROGRAM/PERFORM are exercised green across the NIST suite — no gap found. **CONTINUE AFTER** (the 2002
timed form `CONTINUE AFTER expr SECONDS`) is **deferred**: it is a non-deterministic runtime delay with no
stable output, a poor fit for the conformance corpus, and low value. So M2-PROC-6 is substantially complete
(GOBACK RETURNING + EXIT verified); only CONTINUE AFTER remains, noted in the plan.

Build hiccup worth noting: the first build after the grammar edit produced a `Generated/CobolParserCore.cs`
peppered with NUL bytes (a transient ANTLR regeneration/copy glitch, `CS1056 unexpected character '\0'`); a
clean `rm -rf Generated` + rebuild fixed it. Not a source defect.

Guard ALL GREEN (1047 unit / 477 integration / 364 NIST), 0 regressions.

