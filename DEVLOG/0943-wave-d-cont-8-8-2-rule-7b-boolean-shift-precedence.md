## Entry 943 — 2026-07-21 10:04 PDT — Wave D (cont.): §8.8.2 rule-7b boolean shift precedence implemented in the runtime binder — the COBOLNET1569 legal-source reject removed (ledger C2)

Wired the runtime COMPUTE-Format-2 boolean binder (`ConditionBinder`) onto the shared
`BooleanExpressionResolver` landed in Entry 942, and **removed the COBOLNET1569 reject** — the debt
the owner's no-deferral push surfaced. The reject refused the legal mixed shift-with-binary form
(`A B-AND B B-SHIFT-L n`) and told the user to parenthesize, because the fixed grammar tiers can't
express §8.8.2 rule 7b (a shift inherits the precedence of the operator before it). The resolver
re-derives the correct grouping, so the form is now ACCEPTED and grouped per the standard.

- `BindBoolExpr` → `BooleanExpressionResolver.Resolve<BoundBoolExpr>(ctx, leaf, not, binary, shift)`;
  the four tier-walk methods + `ShiftMixedWithBinary` + the 1569 emit deleted; the combine callbacks
  (`BindBoolOperandValue` / an ALL-folding B-NOT lambda / `MakeBoolBinary` / a new
  `BindBoolShiftSuffix`) build the SAME bound nodes as before for every non-mixed expression, so
  existing boolean output is byte-identical (characterization 33/0 byte-exact confirms).
- New conformance test `tests/conformance/2023/boolean_shift_mixed.cob`, **verified end-to-end**
  (compiled + ran against a hand-computed rule-7b oracle: A=1010, B=0110 → `A B-AND B B-SHIFT-L 1`
  = 0100, not the shift-first 1000; `A B-OR B B-SHIFT-L 1` = 1100; `A B-SHIFT-L 1 B-AND B` = 0100).

**Friction (honest).** The first legacy guard came back RED — `ConformanceTests.Conformance("2023",
"boolean_shift_mixed")` failed: the conformance corpus runs against BOTH compilers and the frozen
legacy has no boolean support at all (`boolean_ops`/`boolean_shift` are already `GreenfieldOnly`).
I'd omitted the `GreenfieldOnly` exclusion — exactly the `feedback_legacy_suite_on_shared_corpus`
class. Added it in the same change set; re-ran green. (A stale CLI binary also briefly hid the fix
until a full `CobolSharp.sln` rebuild — the fresh-build-before-no-build lesson.) Guards: legacy
`guard.sh` `=== ALL GREEN ===`; greenfield Unit 379/0, Characterization 33/0, Conformance 3784/0.

