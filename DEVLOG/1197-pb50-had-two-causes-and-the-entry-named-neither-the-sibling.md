## Entry 1197 — 2026-08-07 05:55 PDT — PB50 had TWO causes and the entry named neither; the sibling sweep found a WRONG ANSWER worse than the defect being fixed (PB54)

**`MOVE E(ZERO + 1) TO T` aborted at run time.** §8.8.1.1 admits "the figurative constant ZERO" as an arithmetic
operand, §8.4.2.3.2 makes a subscript an arithmetic expression and §8.4.3.3.3 SR4 does the same for both
reference-modification positions — so it is legal source, and so is
`FUNCTION UPPER-CASE("abcdef") (ZERO + 2:3)`, which failed the same way by a different route.

⛔ **THE NOTE'S HEADLINE NAMED A CAUSE THE CODE HAD NOT HAD SINCE PB42.** It read *"RenderSegment is a
hand-maintained token-type switch with no arm for it"*. PB42 had already widened that switch's `default:` to
route EVERY unrenderable token to the D18 materializer, so no arm was missing. Third time this session that a
queue entry's stated root cause was a claim rather than a fact — and the note's own "WHAT TO CHECK FIRST"
section predicted the REAL second cause correctly while its title asserted something false.

**TWO CAUSES, AND FIXING EITHER ALONE CHANGES NOTHING OBSERVABLE:**

1. **The `SUB_IDENTIFIER` arm returned `null` instead of routing to D18.** SUBSCRIPT lexer mode has no ZERO
   token (`SUB_IDENTIFIER : NAME_BODY`), so the figurative arrives as an ordinary identifier, resolves to no
   data item, and that early return was **the one place in the renderer deciding a segment was unrenderable
   without asking D18** — contradicting the rule stated ten lines below it in the same function.
2. **`SubscriptExpressionFragment` — the D18 re-parse itself — did not apply `ZeroTokenRewriter`**, so even once
   routed there the bare ZERO could not match `arithmeticExpression`.

A half-fix would have read as "no progress" rather than "half done", which is worth knowing before budgeting one.

⭐ **THE SWEEP FOUND SOMETHING WORSE THAN THE ITEM BEING FIXED — PB54.** Cause 2 is not local to subscripts.
THREE fragment re-parsers (`SubscriptExpressionFragment`, `FunctionArgFragment`, `DirectiveExpressionFragment`)
each hand-assembled the same five steps — lex, prime, token stream, parse at an edition, null on syntax error —
each with its own copy of `SyntaxErrorFlag`, and **exactly one applied the rewriter**. In the argument-list one
the consequence is not an abort:

```
FUNCTION MIN(ZERO + 5, 2)  →  2      ✅ MIN(5, 2)
         MIN(ZERO + 5, 2)  →  0      ⛔ MIN(0, 5, 2) — WRONG ANSWER, silent
FUNCTION REM(ZERO + 7, 4)  →  3      ✅
         REM(ZERO + 7, 4)  →  ⛔ "takes 2 argument(s); 3 given"
```

§8.4.3.2 SR2 makes the keyword-omitted form THE SAME REFERENCE, so those pairs are one program written two ways.
All three now share **`FragmentParse`**, where `rewriteZero` is an EXPLICIT argument with no default — a new
fragment must answer the question rather than inherit an answer by omission, which is exactly how both defects
arose.

⚠ **MY FIRST PROBE OF PB54 WAS WORTHLESS AND PASSED.** I used `MAX(ZERO + 5, 2)`, which is **5** under both the
correct reading and the split one, so it agreed with the keyword form while the defect was live
(`feedback_probe_the_shape_the_subject_hides`). MIN is the discriminator — 2 versus 0 — and REM's fixed arity
turns the same cause into a loud error. Both are in the golden, asserted as PAIRS so the two spellings cannot
drift apart again.

⚠ **AND I WROTE A CODE COMMENT THAT WAS FALSE, THEN MEASURED IT.** The new D18 route's comment claimed an
undefined subscript name now "reports the unresolved reference by NAME at bind time — strictly better than the
run-time abort this replaced". It does not: `E(NOSUCHNAME + 1)` aborted at run time before the change and aborts
at run time after it; only the MESSAGE improved, from the whole reference text to the offending name. Verified in
both directions and the comment rewritten. That an undefined subscript name is a run-time abort at all is the
wrong-stage family — recorded in PB50's note as observed residue, not fixed here, and the posture is unchanged
so nothing was made worse.

**GATE.** Conformance `~Corpus|~Negative|~Intrinsic|~Subscript|~RefMod|~VersionMatrix` **2677/2677** zero
skipped · Unit **4028/4028** zero skipped. ⚙ The unit count rose by one with no unit test added, and the reason
was checked rather than waved through: `ParenTokenTwinDriftTests` is a `[Theory]` with one case per source file,
and `FragmentParse.cs` is a new source file. Goldens
`pb50_figurative_zero_position_expressions` (7 assertions — both spellings of the figurative, both ref-mod
positions) and `pb54_keyword_omitted_figurative_zero_argument` (6 — three keyword/keyword-omitted pairs).

