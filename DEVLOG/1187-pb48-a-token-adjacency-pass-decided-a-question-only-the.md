## Entry 1187 — 2026-08-05 23:56 PDT — PB48: a token-adjacency pass decided a question only the binder could answer, and the keyword-omitted arm had been printing the right answer all along

**`FUNCTION LOWER-CASE(ZERO)` was rejected as illegal COBOL. `LOWER-CASE(ZERO)`, the same reference with the
word FUNCTION omitted, returned `"0"`.** Two arms of one dispatch, one right — the sixth time that shape has
been the diagnosis here, and this time it handed over the expected answer before a line of code changed.

**THE SPEC.** §8.3.3.6.4 GR4: "The zero format represents the numeric value '0', one or more of the boolean
character '0', or one or more of the character '0' in the computer's runtime coded character set, **depending on
context**." GR1 picks the character reading and GR3b makes a bare one ONE character. §8.4.3.2.3 SR8 admits a
literal as argument-1 and §8.3.3.6.3 SR1 admits a figurative constant wherever a rule allows a literal — so a
bare `ZERO` argument is legal to every function, and *which* reading applies is decided by that function's own
§15.3 argument type. The converse is SR1a: where the literal is restricted to numeric, "the only figurative
constant permitted is ZERO (ZEROS, ZEROES) **without the ALL phrase**". Every citation `cite.py --check`ed.

**THE MECHANISM.** `ZeroTokenRewriter` converts `ZERO` to the arithmetic `ZERO_ARITH` whenever it is adjacent to
`(` or `)`, reasoning that such a ZERO sits inside a parenthesized arithmetic expression. An argument list is
delimited by exactly those characters. So every bare ZERO argument was decided as class NUMERIC *before any
function was known* — by a pass that structurally cannot know one. §8.4.3.2.3 SR6 says that paren is
categorically not a grouping paren: "the left parenthesis is ALWAYS treated as the left parenthesis of that
function's arguments."

**THE FIX IS A TOKEN TYPE, AND DELIBERATELY NOT A SECOND COPY OF THE PREDICATE.** The lexer already maintained
`_fnParenStack` — the one place that knows which parens those are — so it now publishes it as
`FNARG_LPAREN`/`FNARG_RPAREN`. The rewriter's own rule about ARITHMETIC parens became true as written, with no
change to it. Re-deriving "is this '(' an argument list" inside the rewriter was the obvious smaller diff, and it
is the one thing `feedback_one_rule_one_place` exists to forbid.

**IT WAS FOUR ARMS, NOT ONE, AND THE ENTRY NAMED ONE.** Fixing the token moved nine probe cases green and
immediately exposed the rest:

- **The §15.3 class screen** modelled an operand's class as a SCALAR. A figurative constant has none, so it fell
  out of the screen entirely as "not statically decidable" and fail-open passed it to the emitter — which is why
  `FUNCTION ABS(SPACE)` compiled clean and aborted at run time. It is now `CandidateClasses`, the SET of classes
  the operand can present, and `Violation` INTERSECTS. ZERO passes a class-numeric rule *and* a class-alphanumeric
  one; SPACE passes only the second; `ALL "5"` is deliberately not neutral, which is exactly SR1a's "without the
  ALL phrase". `ClassOf` survives as the singleton view, so no fixed-class operand changes verdict.
- **The MAX/MIN body choice** read `args.All(IsStringOperand)`, counting a class-neutral ZERO as a NON-string
  vote. `FUNCTION MAX(ZERO "A")` returned `"0"` — **a wrong answer, not a crash**: §15.59.4 r1 compares by the
  §8.8.4.2 relation rules and "A" (65) exceeds "0" (48).
- **The result type** read `CategoryOf(args, 0)` literally, so a leading figurative collapsed the list to numeric
  while the body choice had already picked the STRING comparison. ⭐ The two halves of one decision disagreeing
  produced a *new* failure mode — `FUNCTION MAX (no numeric render recipe)` at run time — which is the sharpest
  demonstration I have of why the source comment reading "SEPARATE concern, deliberately not merged" is a
  warning and not a note. §15.59.3 r2 ("All arguments shall be of the same class") is what makes both repairs
  exact: the arguments that HAVE a class are the context GR4 defers to.
- **`BindByteLengthFold` was PB25's own defect one method away from where PB25 fixed it.** Its default arm named
  "a numeric/**figurative** literal" invalid on the authority of §15.14.3 — which says the opposite. r1 admits
  "an alphanumeric or national literal", and GR1 makes a figurative in a character context precisely that.
  `FUNCTION BYTE-LENGTH(SPACE)` was legal source aborting at run time *before* PB48 as well.

**WHAT I GOT WRONG, AND WHAT CAUGHT IT.** The first cut broke three PB8 ref-mod cases outright: I had encoded
SR6's *conclusion* without its *precondition*. SR6 hands that paren to the argument list only "if a function's
definition **permits arguments**" — a CATALOG question no lexer can answer — so after a zero-argument name the
same token is a reference modifier, and `FUNCTION CURRENT-DATE (1:8)` (the standard's own D.14.3.6 shape) stopped
parsing. `refModPart` now accepts both flavours, and the rewriter gained the ref-mod `COLON` as arithmetic
context (§8.4.3.3.3 SR4 — both positions are arithmetic expressions). The wave-local gate caught it in 25
seconds; nothing about the design had predicted it.

**MEASURED, NOT REASONED — THE PROBE MATRIX.** 60 cases compiled AND run before the change and after each step,
classified by STAGE (compile-reject / run-time abort / clean output) and diffed mechanically. That is what made
"nothing else moved" a measurement rather than a hope, and it is what turned three later questions into answers:
a token dump refuted my `SIGNED_INTEGERLIT` hypothesis about the ref-mod positions, and `git stash` + rebuild
proved two run-time aborts I had suspected myself of causing were present on the pre-change tree.

**THE GOLDEN ASSERTS BOTH REFERENCE FORMS, VALUE FOR VALUE** — 30 assertions, each derived from the clause and
not from what the compiler printed: `ORD(ZERO)` is 49 because '0' is collating position 48 and §15.70.4 makes the
lowest ordinal 1; `LENGTH(ZERO)` and `BYTE-LENGTH(ZERO)` are 1 by GR3b while `BYTE-LENGTH(ALL "AB")` is 2 by
GR3c; `MIN(SPACE "A")` is a space because 32 < 65. The new drift tests were **proven in the failing direction**
— removing the retype reds seven rows — because a green assertion that never looked at the change is not
evidence.

**AND THE PROBE FOUND THREE DEFECTS THE ENTRY NEVER MENTIONED, ALL FILED.** `COMPUTE WS-N = ZERO` is REJECTED
with "a boolean COMPUTE expression shall not consist solely of an ALL literal" — on a statement §8.8.1.1 makes
legal arithmetic, with a diagnostic naming a construct the source does not contain (PB51). `E(ZERO + 1)` and
`FUNCTION UPPER-CASE("abcdef") (ZERO + 2:3)` abort at run time because `ReferenceResolver.RenderSegment` is a
hand-maintained token-type switch with no arm for the figurative — PB42's lesson recurring one layer down, where
the token LIST *is* the renderer (PB50). And PB52 itemises the residue this fix could not reach. ⭐ **PB50 and
PB51 share a root that PB48 does not fix: `primaryExpression` cannot express the figurative ZERO that §8.8.1.1
names as an arithmetic operand, and a token-adjacency pass has been patching over the gap.** PB48 fixed where
that pass fires WRONGLY; those two are where it does not fire at all. Retiring it for a grammar alternative is
the structural candidate — recorded with the ambiguity survey it demands, because `IF X = ZERO` over a `PIC X(5)`
compares against `"00000"` and ANTLR's first-match would decide that silently.

**GATE:** Conformance 715/715 wave-local, zero skipped · Unit 3702/3702 · characterization 33/33.

