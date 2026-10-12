## Entry 1192 — 2026-08-06 16:35 PDT — PB46's boolean operand was the small half: the conformance screen underneath it made FIVE picture categories impossible as method parameters

**A category-boolean formal parameter could not cross an INVOKE in any passing mode, and neither could a
national, numeric-edited, pointer or program-pointer one.** PB46's remaining half was written up as "the boolean
value channel threaded through `BoundInvokeArg` and `OoEmitter`" — four named layers, all of them above the
actual blocker. The first probe went straight past the operand:

```
INVOKE O "TAKEB" USING BY REFERENCE B1   ⛔ formal category Boolean       is not yet carried across INVOKE
INVOKE O "TAKEB" USING B1                ⛔ (same — bare, BY REFERENCE implied)
INVOKE O "TAKEN" USING N1                ⛔ formal category National      is not yet carried across INVOKE
INVOKE O "TAKEE" USING E1                ⛔ formal category NumericEdited is not yet carried across INVOKE
INVOKE O "TAKEP" USING PTR               ⛔ formal category Pointer       is not yet carried across INVOKE
```

`OoConformance.DescriptionMismatch` — THE one §14.8.2.3.2 identical-description check, on the path of every
INVOKE argument and every override signature — switched on the formal's category with arms for object-reference,
numeric and alphanumeric, and a `default:` that answered *"formal category {c} is not yet carried across
INVOKE"*. Nothing was actually missing. All three string-imaged categories are `OoClassTable.StringCarried`, so
the marshaling arms had carried them the whole time; only the screen said no. Deleting the default arm and
writing the rule made all five work on the first rebuild, with correct values.

⭐ **THE REASON IT SURVIVED EVERY GATE IS THE SENTENCE IT PRINTED.** "…is not yet carried across INVOKE" READS
like a deliberate staging decision, so nobody re-derived it against the clause — the exact shape
`feedback_green_test_can_hold_a_gap_open` names, in a diagnostic rather than in a test. §14.8.2.3.2 states ONE
rule for every category ("the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE
clauses"), and its own lettered exceptions **b** and **c** pair a BIT GROUP with an elementary bit item and a
NATIONAL GROUP with an elementary national item — the standard names exactly the categories the default refused.
`OoConformanceCategoryDriftTests` now asserts that no `PicCategory` may fall into a rejecting default, and
asserts the failing direction too so the identity test cannot pass vacuously.

**THE OPERAND ITSELF, once the formals could exist.** §14.9.23.2's BY CONTENT branch admits
`arithmetic-expression-1 | boolean-expression-1 | identifier-5 | literal-2`; the arithmetic operand landed
earlier today, this is the boolean one — its own value channel (D-B1: a '0'/'1' bit string whose length §8.8.2
rule 10 fixes), never a second spelling of the numeric one. A grammar alternative under the proven
`{boolExprAhead()}?` gate, `ContentBool` + `ContentBoolWidth` on `BoundInvokeArg`, a bind arm applying
§14.8.2.3.3 rule 2d through §14.9.25.3 Table 16's BOOLEAN row, and an emit arm storing by the receiving
category's MOVE discipline. A boolean **literal** rides the same channel: it had none at all, and fell to
"argument form … not yet carried" although §14.9.23.3 SR17 bars only a ZERO-LENGTH literal-2.

⛔ **THE PREDICATE OVER-REACHES, AND THE DESIGN IS THAT IT DOES NOT MATTER.** `boolExprAhead()` is the SHARED
condition predicate and its scan runs to the statement's period, so in
`USING BY CONTENT N + 1 BY CONTENT B1 B-AND B2` the FIRST argument's decision already sees the SECOND argument's
`B-AND` and takes the boolean alternative. Narrowing the scan would tighten a shared condition predicate for a
local reason — the DEVLOG-621 lesson. Instead the binder REDUCES a B-operator-free `booleanExpression` back to
its bare `valueOperand` (`ConditionBinder.UnwrapBareBool`, the same reduction `BindPrimaryBoolean` uses), so the
predicate decides which NODE an operand parses into and never what it MEANS. Four regression controls in the
golden prove the arithmetic, identifier, numeric-literal and alphanumeric-literal channels bind identically —
and `InvokeContentOperandChannelTests` asserts the over-reach REALLY HAPPENS, because otherwise those four
controls would pass while testing nothing (`feedback_green_gates_arent_evidence`).

⛔ **AND THE EDITION GATE WAS A TWO-ARM DISPATCH I WAS ABOUT TO MAKE A THREE-ARM ONE.** `VersionConformancePass`
gated the boolean operators at the `primaryCondition` and `computeStatement` altitudes with two copies of the
same four lines. Adding a third copy is how the next site ships ungated: `BY CONTENT B1 B-SHIFT-L 2` is a
COBOL-2023 construct inside a COBOL-2002 statement and would have compiled clean under `--std 2002`. The gate is
now written ONCE (`GateBooleanOperators`) with a call per site, and `BooleanExpressionGateSiteDriftTests` DERIVES
the site list from the `.g4` files rather than remembering it, so a fourth site fails a test instead of passing
silently; each exemption carries its reason. Verified in the failing direction by deleting the call and watching
it go red, then restored.

**WHAT I DID NOT DO, and why it is filed rather than folded in.** `OoContentMismatch` still judges a
boolean/national/numeric-edited formal under BY CONTENT by §14.8.2.3.2 STRICT IDENTITY, where §14.8.2.3.3 rule
2d asks for the MOVE rules — so Table-16-legal pairings (boolean→national, alphanumeric→boolean,
national→boolean) are refused. That is **PB53**, and it needs the emitter to acquire the receiving category's
MOVE store discipline, not four more binder arms. The new boolean-expression arm refuses exactly the same
pairings ON PURPOSE: two arms of one rule disagreeing is worse than one named residue
(`feedback_two_arm_dispatch`), and a reader comparing them could not have told which one was right.

⚠ **PROCESS, honestly: the four commits before this one landed with NO DEVLOG ENTRY** — PB51, PB46's arithmetic
half, PB33 and PB49 (`1838a304`, `dd45ddfd`, `bd778375`, `4808178a`). Entry 1191 covers PB40 and then the log
stops while four items shipped. Their commit messages are forensic and their `kb/Work/` notes are current, so
nothing is lost — but the NARRATIVE for those four is missing, and this log is the only place it belongs. I have
not written them: reconstructing four sessions' entries from their own commit messages after the fact would put
invented recollection where lived narrative goes. Flagged in plan §0 for an owner call.

**GATE.** Wave-local, filtered on what the change TOUCHES rather than where the goldens live
(`~Corpus|~Negative|~Oo|~Invoke|~Boolean|~VersionMatrix`): Conformance **2704/2704, zero skipped** · Unit
**4021/4021, zero skipped** (3986 + 35 new). Golden `pb46_invoke_by_content_boolean` — 20 assertions: the four
binary/unary operators, both COBOL-2023 shift forms, the §8.8.2 rule-10 width case, a boolean literal, the
identifier and bare/write-back paths, the alphanumeric receiver, all three formerly-blocked sibling categories
plus a pointer, and the four over-reach controls. Negative
`pb46-invoke-by-content-boolean-numeric-formal` — Table 16's BOOLEAN row, NUMERIC column: "No".
⛔ CI is still disabled. GitHub's Actions incident was updated at 23:13 UTC reporting throughput being
*gradually* restored with webhook-triggered workflows still ramping, so a dispatched run stays unattributable
and re-enabling waits. `bash scripts/battery.sh` remains the only gate, and it is Windows/DEBUG only.

