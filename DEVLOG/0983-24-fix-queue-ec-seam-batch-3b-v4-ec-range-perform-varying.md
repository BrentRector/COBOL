## Entry 983 — 2026-07-22 13:22 PDT — §24 fix-queue: EC-seam batch #3b — V4 EC-RANGE-PERFORM-VARYING (fatal) LANDED

Increment B of the V4 EC-RANGE family. §14.9.28.4 GR3 (:29222): a PERFORM VARYING/AFTER that initializes an INDEX-NAME
from a data-item FROM operand whose value is NOT POSITIVE (≤0) raises the FATAL EC-RANGE-PERFORM-VARYING. NARROW — only
an index-name target with a data-item FROM (a literal FROM `BoundNumLiteral` and an index-name FROM `BoundIndexRef` are
out of scope; a data-item induction variable is out of scope — GR3 is index-name-only).

**The design-validation Workflow corrected the routing:** the literal "FatalAmbientGates entry alone (mirror
EC-BOUND-REF-MOD)" is INERT for a PERFORM — `EcWrap.QueryFor` has no perform case, so a PERFORM is never wrapped, the flag
never sets, and a bare targeted throw would escape to the run-unit catch and abort (defeating RESUME). The structural
improvement (a PERFORM is ONE identifiable node, unlike ref-mod's inline-in-expression raise): a **PRECISE QueryFor arm**
(`case BoundInlinePerform/BoundOutOfLinePerform { Control: PerformVarying { CheckIndexRange: true } }` → Query the EC)
produces the `BoundEcChecked` wrapper, which THEN makes the FatalAmbientGates entry + `PerformVaryingChecking` flag live
and routes the throw through `EmitArgOrPlain`'s try/catch → `EcDispatchExpr` → USE-F3 RESUME.

Wiring: `PerformVarying.CheckIndexRange` (bind-time `Turn.Enabled` capture in `ControlFlowBinder.BindVarying`) →
`EcBinder.QueryFor` precise arm → `EcEmitter.FatalAmbientGates` entry → `ExceptionState.PerformVaryingChecking` +
`PerformVaryingIndexError(value, detail)` (mirror `RefModError`; tests the DATA-ITEM value ≤0, GR3, not the post-conversion
index). The FOUR FROM-init sites in `EmitVarying` (268/286/293/302 — never the BY/augment sites 277/285/306, GR3 is
FROM-only) route through ONE new `InitVaryingTarget` helper: for a checked index-name-from-data-item it materializes the
FROM once (`__pv{n}`, a new `NameAllocator.NextVary` counter), calls `PerformVaryingIndexError`, then assigns the index;
else the plain byte-identical store.

Gate: golden `2023/ec_range_perform_varying` (positive control no-raise · index-name FROM 0 AND FROM −1 raise+RESUME via
USE-F3 · literal-FROM control · data-item-target control · AFTER-level index raise — all 6 correct) · characterization
**33/33 byte-identical** (the refactor preserves every non-index-name/directive-free PERFORM VARYING — `char_conditionals`
varies a DATA ITEM `J`, outside the trigger) · PERFORM/varying/ec_range Conformance **99/99** · greenfield Unit **571/571**.
⏭ Increment C = EC-RANGE-INVALID (the THROUGH-range nonfatal; the largest but single-mechanism). Comprehensive gate before
the batch merges.

