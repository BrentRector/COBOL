## Entry 1912 — 2026-10-07 04:06 PDT — Train 1029: D10's first half lands (PB2113, PB1968): the SUBSCRIPT lexer mode is gone and every subscript, reference modifier and keyword-omitted argument list is a parse node

**Cluster C — kb/Work PB2113 (with PB1968).** The owner's D10 ruling, unblocked by the legacy deletion (R69): remove
the lexer `SUBSCRIPT` mode, the flat `SUB_*` token stream and the hand-rolled C# re-parsers of it. The implementer
first resolved DESIGN-frontend-grammar §9.4 from the spec: ISO §8.3.5 1) "The COBOL character space is a separator" and
2) (comma and semicolon followed by a space are separators wherever a space is), so space-separated subscript and
argument lists are legal and Option A stands (Option B would reject legal source). The fix shape: the '(' after a
data-name-capable word is the virtual `REF_LPAREN`/`REF_RPAREN` pair, and `_parenRegions` marks it a LIST REGION
(FNARG_SEPARATOR live, sign-adjacent literals retyped) with no lexer mode; `subscriptPart : REF_LPAREN subscriptList?
REF_RPAREN` with `subscriptItem : ALL | functionArgument`; `refModPart` takes `REF_LPAREN` and the COLON keeps the two
alternatives disjoint. The binder reads the tree: `ReferenceResolver.SegmentsOf` applies PB136's declaration-informed
paren cut on the parse, one `ReadRefMod` reads every ref-mod, `IsAllSegment` is shared, and `IntrinsicBinder.ArgumentsOf`
binds the keyword-omitted arguments from the parse (`FunctionArgFragment`, `functionArgListFragment` and the lexer's
`PrimeFunctionArgs` are deleted). Eight sibling readers moved off `SUB_*` (FlagConformancePass, ExpressionFormationPass,
SearchAllFormat2Rules, DataBinder.Reports/Constants, ReferenceResolver.ObjectProperty, the CobolParserCoreBase scans,
SeparatorRule, ZeroTokenRewriter). The re-probe corpus (18 shapes) printed identically on main's build and the branch.
One spec-correct behaviour change fell out: a subscript's arithmetic is now an ordinary `arithmeticExpression` node, so
§8.8.1.2 Table 3's (unary, unary) cell reaches it and `DISPLAY X (- - 2)` (which printed X(2)) is COBOLNET1719 at all
four editions, landing PB1968. Goldens: `85/pb2113_subscript_list_separators`, `2002/pb2113_subscript_expressions_and_arguments`
(Annex D.3.5.3's examples), negatives `pb2113-keyword-omitted-arguments-85` (COBOLNET1639) and `pb1968-subscript-unary-unary`
(COBOLNET1719). The implementer stopped at its turn cap (SPLIT) with three conformance reds and named the fix for
each; the lander applied those two grammar fixes, exactly as the report described. (1) `SUM (1:4)` / `SIGN (1:4)` under
FUNCTION ALL INTRINSIC died as COBOL0001 because `functionCall`'s reserved arm required a `subscriptPart` and `(1:4)`
now parses as a `refModPart`; the arm is now `reservedIntrinsicArgFn (subscriptPart | refModPart) refModPart*`, so
IntrinsicBinder's existing "no subscriptPart, a refModPart" branch reports §8.4.3.2.3 SR6 (COBOLNET1543) again. (2)
`negative/pb1030-refmod-string-bound` (`X ("A" : 1)`) died as COBOL0001 instead of COBOLNET2363 because `refModSpec`
took `arithmeticExpression` only; each position is now the superset `functionArgument` (the operand a subscript item
already takes), so a non-arithmetic position reaches `ReadRefMod`'s segment renderer and the D18 materializer's named
§8.4.3.3.3 SR4 refusal. DESIGN-frontend-grammar §9 D10.2 now says so. Rows: the implementer's batch
(`batch-pb2113-codelocations.json`, 12 rows, 8 code-locations added and 13 retired) was re-applied on the merged tree
with `record_verdicts`, never merged as a JSON hunk; the witness-loss audit reports the 13 as retired with excuse.
PB2113 and PB1968 flip to landed with `closes_rows: []` and reasons. The emitted-C# oracle comparison
(`scripts/arch/compare_oracle.py`) was not on the implementer's base and was not run; the corpus and the full gate are
the behaviour-neutrality proof here.

**New lead filed.** kb/Work PB2151 — D10's second half: subscript and ref-mod positions still reach codegen as a
bind-time C# string (`ReferenceResolver.RenderSegment`) and a D18 text re-parse (`SubscriptExpressionFragment`);
moving them onto `BoundExpr` deletes both.

**The train.** One cluster (SPLIT, landed whole after the two named fixes). First gate (`build-local.ps1 -Mode lander`,
run 20261007T105800Z-331a15, on db070acb5): GREEN, Conformance 10,724/10,724 · Unit 31,654/31,654 · Characterization
35/35, with the three former reds passing by name; Linux gate GREEN. Pipelined behind train 1028 (still in CI), the
cluster was then rebased onto train 1028's head fdf1ef1dc: five conflicts, all comment or doc prose about the D10 carrier
(ReferenceResolver.cs and StatementBinder.SubscriptSegments.cs remarks, DATA_MODEL, DESIGN-binder-bound-tree, whose G8
row keeps train 1028's rewording, and DESIGN-frontend-grammar §9 status), resolved hunk by hunk, with PB2113's
grammar auto-merging beside PB2114's call-argument rules. The inventory batch re-checked as already applied. Because two
C# files conflicted, the whole population was re-gated on the rebased tree (run 20261007T111324Z-861e83):
`=== BUILD-LOCAL GATE: GREEN — Conformance 10,724/10,724 · Unit 31,661/31,661 · Characterization 35/35 cases ran
(skipped 0) in 1 of 1 leg(s)`, and the Linux gate is GREEN on it too (all four legs plus the hook self-tests, HEAD 61c1c04dd).
Semgrep unchanged (BigInteger 46, decimal 2, rendered-text 3, raw-code literal 273). CI audits run locally (code and doc
citations, evidence supersession, witness loss, the drift-rule index, `work.py check`): all green. GAP 173 → 173. No
diagnostic codes claimed. Review over `git diff origin/main...HEAD`: no new correctness finding; the two reported reds
were fixed in the cluster's own commit. No cluster dropped.

