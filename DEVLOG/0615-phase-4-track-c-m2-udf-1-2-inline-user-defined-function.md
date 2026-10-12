## Entry 615 — 2026-07-05 07:27 PDT — Phase 4 track (c) M2-UDF-1+2: inline user-defined function invocation

`FUNCTION user-name(args)` is LIVE for the in-group whole-source form — all three udf_* goldens
(`udf_invocation` C=0042/M=0042, `udf_inline_expression` EXPR=0043, `udf_value_args` LIT=0010/ARI=0010)
compiled and ran **byte-exact on the first attempt** after the bind landed. Implemented FROM the
decision-complete design in `docs/PHASE4_RECONCILIATION.md` §"M2-UDF-1 — DECISION-COMPLETE DESIGN"; the
as-built subsection there records every deviation. The shape is pure reuse: a caller-side result temp cloned
from the callee's RETURNING description (§8.4.3.2.4 GR1 :6967), a `BoundCallProgram` = CALL "name" USING
«args» RETURNING «temp» hoisted before the carrying statement through a `BoundSequence` (the property-ref
pattern; always a PRE-op — a function-identifier is never a receiving operand, §8.4.3.2.3 SR1), and the
expression/MOVE reading the temp. Zero new emit surface; no grammar change.

**The design's "KEY ENABLER" claim was wrong — and the fix was the real work.** The design asserted the
run-unit bind was already two-phase (all DATA before any PROCEDURE). It wasn't: `CallBindUnit` bound each
unit's DATA *and* PROCEDURE per-unit sequentially, so a caller could never see a following FUNCTION-ID
unit's signatures. Split it into `CallBindUnitData` / `CallBindUnitProcedure` with the
`UserFunctionSignature` table built between the loops (the `OoClassTable` D1 discipline applied to program
units). The restructure touches EVERY program's bind path — INV-1-STRONG (349/349 byte-exact) + the full
conformance battery prove it behavior-neutral.

Spec-driven decisions (derived + cited BEFORE coding, per the #1 process rule):
- **§12.3.8.2 GR12 (:14885)**: a REPOSITORY-declared function name refers to the USER function "and not to
  an intrinsic function of the same name" (the spec's own factorial-override example :43651) → the
  user-function dispatch PRECEDES `IntrinsicCatalog.TryGet` (the design had it on the miss branch);
  UdfInvocationTests proves a user SQRT(2 args) beats the 1-arg intrinsic.
- **§9.4 (:12529)**: a UDF "always possesses the recursive attribute" → FUNCTION-ID units get
  `Recursive = true` structurally; and UDF arguments "may not use the word ALL as a subscript" → the arg
  binder skips the intrinsics' table(ALL) expansion (an ALL token fails loud).
- **§8.4.3.2.4 GR5a/b (:6977)**: identifier-permitted-as-receiving-operand ⇒ BY REFERENCE over the caller's
  place; literal/arith-expression ⇒ private-copy cell, conformed to the formal by the existing
  `CobolArgAdapt` profile adaptation (the §14.2.3 GR9 copy-in, observably).
- **§14.2 (:23666)**: "The RETURNING phrase shall be specified in a function definition" → COBOLNET1507
  once per unit, even uncalled.
- **§14.9.28/§14.9.37 re-evaluation guard the design missed**: a once-hoisted activation cannot honor a
  PERFORM UNTIL/VARYING or SEARCH WHEN condition (re-evaluated per iteration) → COBOLNET1509 loud, never a
  stale-temp loop. Nested/argument evaluation order (§8.4.3.2.4 GR2 left-to-right) falls out of
  registration order for free.

New band: 1505 repository-declared-but-undefined (the M2-UDF-3 prototype gap), 1506 arity, 1507 no-RETURNING,
1508 duplicate FUNCTION-ID, 1509 per-evaluation guard, 1510 staged RETURNING categories; 1501 gained the GR12
hint when the group defines the name. Registry+matrix row `user-function-invocation-2002` (0900 below 2002 —
the {is2002()}? PD-header RETURNING parse hint fires there too). `OoCreatePropertyTemp` refactored onto the
ONE `CreateCompilerTemp` (feedback_singular_pattern). One test-authoring lesson: `FUNCTION F()` with empty
parens is a §8.4.3.2 PARSE error (the argument list requires an argument) — the zero-arg spelling is the bare
name.

**THE ADVERSARIAL REVIEW WAVE (same change set): 4 find-lenses × 2-skeptic verify (wf_e38982d1-0d2) — 28 raw
→ 24 CONFIRMED / 4 refuted; every confirmed finding fixed, staged loud, or documented-with-citation** (the
full disposition is the reconciliation's "ADVERSARIAL REVIEW WAVE" subsection). The heavy hitters, all
review-caught: ① the **StoreAsImage clone desync** — the flag is still mutable while procedure bodies bind,
so a caller's temp cloned before the callee's own body flipped its RETURNING item desynced the two
activation-boundary carriers and `StoreReturn(long, string)` silently dropped the value; cured structurally
by the (temp, model) re-sync pass after ALL procedure binds (property temps ride it too). ② **EcWrap had no
BoundSequence transparency** — a checked COMPUTE carrying a hoisted activation silently LOST its >>TURN
families (a hole the property-op sequence had had since DEVLOG 607); the family query now recurses into
steps. ③ **§8.8.4.13 r1/r2 short-circuit** — functions evaluate "if and when the conditions containing them
are evaluated", so a hoist out of a non-first AND/OR operand or an EVALUATE selection over-evaluates:
guarded loud (1509) at BindFlatSequence + the EVALUATE shape. ④ **§12.3.4 GR1** configuration inheritance —
contained programs (which cannot own a CONFIGURATION SECTION, §12.3.3 SR1) now inherit the containers'
repository function specifiers. ⑤ **§8.4.6.6 self-name** — self-recursion needs NO repository entry; proven
end-to-end by the new udf_recursion golden (5! = 120, five nested activations). ⑥ **non-numeric RETURNING**
would have compared alphanumeric results NUMERICALLY (and a group RETURNING produced an undeclarable
Pic-less temp) — staged loud 1510, numeric-elementary only. ⑦ ContainsNextSentence gained the BoundSequence
arm. Plus honest 1506 messages for staged argument shapes and the §8.4.3.2.6→§8.4.3.2.4 citation sweep (the
GR1 temp rule lives under §8.4.3.2.4 General rules — my slip, not the design's). Two deviations DOCUMENTED
with citations rather than fixed: the §12.3.8 SR10 forward-definition leniency (the conforming spelling
needs IS PROTOTYPE = M2-UDF-3; the ordering diagnostic lands with it) and the §14.6.2.3.2/.3
static-WS-per-activation deviation of the pre-existing D3/D4 recursive-instance model (predates UDFs —
any PROGRAM-ID … RECURSIVE unit has it; named follow-up on the interprogram track).

Battery after the review wave: conformance 1645/1645 (+35 over the DEVLOG-614 baseline) · unit 123/123 ·
CorpusRunnerTests 2002 now 37 enabled (32+5 udf: invocation, inline_expression, value_args, recursion,
nested_args — recursion + nested_args byte-exact first run, expectations hand-derived from §8.4.3.2.4
GR2/GR5a before execution) · matrix 76 rows · `UdfInvocationTests` ×26 · zero regressions. M2-UDF-2 folded
in as designed. Unblocked: EXIT FUNCTION (M2-PROC-6 leg), UDF-3 prototypes, UDF-4 residual legs.

