## Entry 1722 — 2026-09-26 20:37 PDT — Wave-62 train 64: group-formal figurative fills, the predefined NULL as an argument

**PB1617 (w62o) — a figurative constant or ALL literal passed BY CONTENT fills a GROUP formal's record too.** PB1418 had
sized the fill only for an elementary formal, so `CALL … AS NESTED USING BY CONTENT ALL "*"` into
`01 G. 05 A PIC X(2). 05 B PIC X(2).` displayed `*   ` where §14.2.3 GR9 (the allocated record has the formal's
description, at its maximum length for a variable-occurrence formal) and §8.3.3.6.4 GR2 (the figurative is repeated
character by character) give `****`; the INVOKE lane refused every figurative but NULL as "not yet carried", a third
arm the note had missed. The re-probe refuted one premise: an OCCURS DEPENDING formal's extent is not a run-time fact
here, because GR9 allocates the record at the MAXIMUM, and a §8.5.1.12 variable-length group formal is not a fill case
at all but a bind-time non-conformance. The fix is ONE fill for all three lanes, `CallEmitter.FigurativeArgumentImage`
(CALL and function through `ArgText`; INVOKE through a new `BoundInvokeArg.ContentFill` channel), sized by the
formal's text-crossing window `BoundaryImageWidth`. HIGH-/LOW-VALUE now read the national sequence at a national
formal, which the CALL arm had omitted. CallEmitter's private copy of GR2's repetition is gone. The sibling screen
changed too: in `ParameterConformance.ContentConformanceReason` a group formal asks the whole MOVE question
(`MoveTable16.Validity`: SR2 strong type, SR9 variable-length group) where `OoConformance` used to answer every group
"conformant", and a pointer or object-reference formal takes §14.8.2.3.3's SET paragraph. The implementer's own
self-review found that SPACE into a POINTER formal had reached the backend as CS1503. Golden
`2002/pb1617_figurative_group_formal_fill`; three negatives (varlength group COBOLNET1688, strong-group INVOKE
COBOLNET0828, pointer formal COBOLNET1688). No inventory row was claimed; PB1617 landed with `closes_rows: []` and
a reason. No codes: 2485–2487 were returned.

**PB1630 (w62p) — the predefined NULL written as an argument crosses as the FORMAL's null.** NULL is an identifier
(§8.4.3.1.2 Formats 6 and 8) that §8.4.3.10.3 SR1 a) admits as a program-prototype CALL, function-activation or
method-invocation argument, but the CALL and function lanes crossed it as a one-character LOW-VALUE fill. A pointer
formal died at run time with EC-PROGRAM-ARG-MISMATCH; a PIC X or group formal silently received a NUL. BY VALUE NULL
was refused as a non-numeric literal-2, SR23 being asked of an identifier that SR22 admits, and INVOKE refused NULL at
a pointer formal. The sweep found those four arms beyond the note's one. The fix adds one storage-free wire carrier,
`PredefinedNullArgument.Instance`, which crosses in the written mode. The callee's slot adapter answers it with its
own carrier's null (`CobolArgAdapt.PredefinedNull<T>`, now also the omitted-argument seed) and every other adapter
refuses it, so the §12.3.8.4 GR10 c) no-formal crossing is the same crossing. The one §14.8.2.3.3 verdict admits NULL
only at a pointer or object-reference formal (§14.9.25.3 SR1 refuses the rest) for CALL, function and INVOKE alike,
and INVOKE renders the formal's `DefaultInitializer`. Golden `2002/pb1630_null_argument_crossing`, negative
`pb1630-call-content-null-alnum-formal` (COBOLNET1688), Unit `PredefinedNullArgumentTests` (7). No inventory row was
claimed; the NULL rows stay with PB1427, which remains open. No codes: 2488–2490 were returned.

**The train.** Both clusters applied cleanly: the w62o patch reproduced the implementer's own merge commit exactly,
and w62p's delta matched its branch head. The gate was the WHOLE Conformance assembly (`-Filter "~CobolNet.Tests"`,
9,046 selected) plus Unit and Characterization at Normal priority. Conformance passed 9,046 of 9,046, Unit 29,491 of
29,491 and Characterization 33 of 33, all GREEN. The legacy Integration assembly passed 503 with 1 skipped of 504.
semgrep verify: PASS, no count changed. `work.py check`: ✓ 1,562 items. Citations re-run with `cite.py --check`:
§14.2.3 9), §8.3.3.6.4 2), §14.8.2.1, §8.4.3.10.3 1) a), §14.9.4.3 22) and §14.8.2.3.3 2), all OK. The lander's
full-code review of the train diff found nothing (0 findings). GAP 1061 → 1061: no batch in either cluster.
**Cluster w62q (PB1627, ALL literal-1 concatenation operands) was DROPPED.** It arrived after the gate started and was
carried on a re-gate. That re-gate was RED on one Conformance case that none of the implementer's ten filter terms
selected: `ClassClauseForPhraseTests.LiteralOfTheOtherClass_IsSR17_3(PB976C3A)` expects
`COBOLNET1671: CLASS HN FOR NATIONAL: ALL "AB" — each noninteger literal …` and now gets `ALL"AB"`, because the
reworked `DataBinder.Switches.AllLiteralCharacters` names the operand by the parse node's `GetText()`, which drops the
space. The two-cluster tree had already passed that case, so the red belongs to w62q alone. It goes back to its
implementer. Leads filed: PB1631 (INVOKE BY VALUE ZERO is refused as "literal-2 shall be a numeric literal",
`OoBinder.cs:633`; latent) and PB1632 (CALL BY CONTENT of a pointer whose address was taken crashes the compiler,
`CallEmitter.CallPlaceIsManaged`; crashes). The BY REFERENCE NULL diagnostic-quality finding is appended to PB1427.

