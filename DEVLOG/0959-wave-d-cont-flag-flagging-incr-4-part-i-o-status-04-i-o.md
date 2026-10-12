## Entry 959 — 2026-07-21 19:05 PDT — Wave D (cont.): FLAG flagging Incr 4 (part) — I-O-STATUS-04 / I-O-STATUS-07 (FILE-STATUS reference tagging)

**Detectors 14 + 15 of 19.** FLAG-14 **e I-O-STATUS-04** + **f I-O-STATUS-07** (§7.3.15.4 GR4 e/f): a reference to a
FILE STATUS data item that tests for '04' / '07'. First Incr-4 (new-analysis) options — they introduce FILE-STATUS
reference tagging. One shared detector (`VisitComparisonExpression`) serves both (only the literal differs).

**Two reference forms, both spec-faithful:**
1. **Relation** — `comparisonExpression` (the relation-condition leaf, in `Core/CobolExpressions.g4`) with 2
   `comparisonOperand`s + a `comparisonOperator`, one side a FILE-STATUS item and the other the nonnumeric literal
   '04'/'07' (either operand order, any relational operator).
2. **Level-88 condition-name** — a bare reference to an 88 whose singleton VALUE is '04'/'07', defined ON the
   FILE-STATUS item (a reference to that condition-name is likewise "a reference … that tests for '04'/'07'").

**Machinery.** `Run` builds three name-role sets from `unit.Data.Files[]`: the FILE-STATUS item names
(`FileModel.FileStatusItem.CobolName`) + the 88-names on such an item whose singleton `Condition88.Values` strip to
'04' / '07' (the m/f global-name-set idiom — a role fact, not a scope-sensitive identity like d/e). Operand
navigation uses the **canonical helpers** (tooling-first, scout-recommended): `comparisonOperand.valueOperand()` →
`arithmeticExpression()` unwrapped by `ConditionBinder.SoleDataRef` (a lone reference, not an expression) for the
data-name side, and `.nonNumericLiteral().STRINGLIT()` for the literal side. New override
`VisitComparisonExpression`; the class/sign-condition and >1-operand arms are excluded so only genuine relations /
condition-names are examined.

**CLI-probed all cases**: ✅ `IF FS = "04"` → 04 · ✅ `IF "07" = FS` (reverse order) → 07 · ✅ `IF FS-AVAIL`
(88 VALUE '04') → 04 · ✅ `IF FS-DUP-KEY` (88 VALUE '07') → 07 · ✅ `IF FS = "05"` NOT · ✅ `IF WS = "04"`
(non-FILE-STATUS item) NOT · ✅ per-option gating (only 04 ON, `FS = "07"`) NOT · ✅ directive OFF NOT. 7 unit tests
added (`FlagDirectiveTests` 54→61). Wave-local gate green: fresh build + FLAG 61/61 + characterization **33/33
byte-exact**. Detectors done **15 of 19**; Incr-4 remainder = I-O-DECLARATIVE + EC-PROGRAM-EXCEPTIONS.

**A transient during probing:** the first run of a fresh 88-form test file showed no warnings; a clean re-run
flagged correctly (a stale `cobol.exe` caught mid-rebuild — the `feedback_fresh_build_before_no_build_test` hazard in
CLI form). Every verdict here is from a post-build run.

