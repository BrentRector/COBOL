## Entry 1189 — 2026-08-06 00:41 PDT — the battery caught 31 NIST regressions PB48 shipped, and every other gate on that tree was green

**`bash scripts/battery.sh` exited 0 and was NOT green.** Its verdict line read
`=== FAILURES: nist=31 audit=1 unit_rc=0 int_rc=1 baselines=0 ===`, and NIST had gone from 353 MATCH / 0
REGRESSION to **322 MATCH / 31 REGRESSION**. Reading the exit code instead of the verdict would have closed the
session on a broken tree — which is why plan §0 says to gate on the verdict line, and why I am glad it does.

**ALL 31 WERE THE `IF` SUITE — INTRINSIC FUNCTIONS — SO IT WAS UNAMBIGUOUSLY MINE.** A single coherent suite
with two exception shapes (`IndexOutOfRangeException`, `InvalidOperationException: Sequence contains no
elements`), every one a clean compile that threw at RUN TIME. §0 warns that a high-JOBS parallel NIST leg can
false-red; 31 failures confined to exactly the suite my change touched is not that, and I did not treat it as
that.

**THE CAUSE, AND IT IS THE SHAPE I HAD JUST WRITTEN AN ENTRY ABOUT.** PB48 split one lexeme into two token
types — the argument-list `(` of §8.4.3.2.3 SR6 versus an arithmetic grouping `(`. I swept the GRAMMAR consumers
(`functionCall`, `refModPart`) and did not sweep the CODE consumers. The legacy oracle's `MapFunctionArgTokens`
down-converts a nested call's argument tokens to their SUBSCRIPT-mode twins and maps `LPAREN → SUB_LPAREN`; it
had no arm for `FNARG_LPAREN`, so `FUNCTION MAX(FUNCTION ABS(X) 3)` fed the segment binder an unknown token and
the emitted program threw. ⭐ **The FNARG_SEPARATOR twin was already handled three lines below, by the same
author, for the same nested-call reason.** The sibling was sitting right there.

⛔ **A TYPE SPLIT IS INVISIBLE TO THE COMPILER.** Every `t.Type is LPAREN` kept compiling and silently stopped
matching half the parens it used to. That is why nothing before the battery could see it: the wave-local gate,
the FULL greenfield Conformance suite (4212/4212), the unit suite (3702/3702), characterization and the GnuCOBOL
differential (0 per-case flips) were ALL GREEN on the broken tree. The differential's documented blind spot is
exactly this — a defect that leaves compilability unaltered is invisible to it, and this one changed only
RUNTIME output.

**SO I SWEPT ALL FIVE CONSUMERS PROPERLY, AND THE SWEEP FOUND A SECOND HOLE.** `IsBoolOperandTerm` — the
predicate confirming a binary B-operator has a genuine LEFT operand — listed `RPAREN` and `SUB_RPAREN` but not
the FNARG twin, so `FUNCTION f(x) B-AND y` would have taken the comparison path instead of the boolean one. Two
more sites are safe and I verified rather than assumed it: `RenderSegment`'s `default:` arm routes any
unrenderable token to D18 (PB42's rule making the omission safe rather than lucky), and `IsFunctionBearing`
tests the KEYWORD-OMITTED form, whose `(` by definition has no FUNCTION token before it and is never retyped.

**THE GUARD IS A SOURCE-FORM TEST, AND ITS RULE IS "DECIDE", NOT "INCLUDE".** A blanket "always handle both"
helper would be WRONG: `ZeroTokenRewriter` must see grouping parens ONLY — including the twin there is precisely
the defect PB48 fixed — while `MapFunctionArgTokens` must see both. So `ParenTokenTwinDriftTests` asserts that
every site naming the plain paren type either names the twin or writes `GROUPING-PAREN-ONLY` in a nearby
comment. The author's answer stays the author's; what is no longer possible is not noticing there was a question.
It carries a second test asserting it still reaches all four known sites, because a file-per-row theory that
matched nothing would pass forever.

⚙ **AND ITS FIRST RUN CAUGHT SOMETHING I HAD NOT DESIGNED IT FOR — three code comments still quoting the
pre-PB48 grammar.** `IntrinsicBinder`'s class doc, `ReferenceResolver`'s ref-mod-carrier note and the legacy
binder's header all recited `functionCall : FUNCTION functionName (LPAREN functionArgList? RPAREN)?` or the old
`refModPart`. CLAUDE.md rule 6 says every doc describes the CURRENT compiler and a code comment is a doc; a
grammar quotation that has silently gone false is the most believable kind of wrong documentation, because it
reads like it was checked.

⭐ **THE RULE I WANT FROM THIS: SPLITTING A TOKEN TYPE IS A REFACTOR THE TYPE SYSTEM CANNOT HELP WITH, so the
sweep is not optional and the grammar is only half of it.** I swept the grammar because that is where the token
appears in a RULE; the consumers that appear in a `switch` are equally callers, and they fail silently rather
than loudly. Next time the sweep list is "every rule AND every type test", and the guard goes in with the split
rather than after it.

