## Entry 1182 — 2026-08-05 20:41 PDT — the parenthesis was a PARSER decision, not a lexer one: an unlicensed grammar repetition let EVALUATE peel a function's argument list into a second selection object

**PB45's open third, closed — and both of my recorded diagnoses were wrong, in opposite directions.** The entry
first said "a BIND-level misclassification, not a grammar gap"; I refuted that myself and replaced it with "the
discriminator is the ARGUMENT PARENTHESIS … that points at the SUBSCRIPT lexer mode". The handoff block in plan §0
carried the lexer hypothesis into this session as its one non-derivable carry-over. **It was also wrong.**

**The token dump settled it in one measurement.** `EVALUATE TRUE / WHEN FUNCTION SQRT(W-Z) > 1` (throws at run
time) and `IF FUNCTION SQRT(W-Z) > 1` (has always worked) lex to **byte-identical** streams, entirely in DEFAULT
mode:

    FUNCTION IDENTIFIER(SQRT) LPAREN IDENTIFIER(W-Z) RPAREN GT INTEGERLIT(1)

Not one `SUB_*` token in either. This is the **third** time an entry has named a lexer-mode root cause that a dump
refuted — PB8 is the precedent §0 carries, and PB45 has now done it twice. ⭐ **The rule this earns, stated so it
generalizes: a parenthesis is also a PARSER decision, so "the discriminator is a parenthesis" does not implicate
the lexer.** I had treated `SQRT(` as a subscript trigger without checking whether the trigger actually fired.

**What the parse tree showed.** `evaluateWhenGroup` was `NOT? evaluateWhenItem+`, and that `+` was splitting ONE
selection object into TWO items: `FUNCTION SQRT` (a bare zero-argument function-identifier, bound as a
*valueOperand*) and `(W-Z) > 1` (a relation over a *parenthesised arithmetic expression*). The argument
parenthesis was **peeled off the function and re-read as a separate object**. Item 1 is then a VALUE object under
an `EVALUATE TRUE` subject — which is exactly `EvaluateBinder`'s `BoundConditionError("EVALUATE TRUE/FALSE paired
with a value WHEN object")`. The run-time throw was reporting the tree it was handed, faithfully.

**Why the parenthesis discriminated, precisely.** Where both readings are viable ANTLR takes the greedy one —
which is why `DISPLAY`, `STRING`, `INSPECT`, nested function arguments and multi-operand `ADD` never peeled (all
measured). Here the greedy reading *dies*: once the item ends there is no way to consume the trailing `> 1`. So
the only surviving `valueOperand` path was the peel, and `valueOperand` precedes `condition`, so ANTLR preferred
it. `FUNCTION PI > 1` worked because with no parenthesis there is nothing to peel. **The `+` supplied the
viability; the alternative order merely picked the winner** — which is why reordering was neither necessary nor
sufficient, exactly as the note warned for its own (different, also correct) reason.

**The fix is the ARITY, and the printed page says so.** §14.9.13.2's general format, rendered from the PDF at
folio 618 and matching the transcription exactly, is
`{ { WHEN selection-object [ ALSO selection-object ] … } … imperative-statement-1 } …` — **selection objects
repeat ONLY through ALSO**, never by juxtaposition; §14.9.13.3 SR2 fixes the count against the subjects. The `+`
was a superset the standard never licensed, and the binder had given it an invented, uncited meaning ("additional
items AND in (a faithful reading of consecutive operands)") — a semantics that could only ever fire on a misparse.
`evaluateWhenGroup : NOT? evaluateWhenItem`, and the fold is gone from both binders.

**⛔ THE SPEC-DERIVED GOLDEN FOUND A SECOND, INDEPENDENT DEFECT THE REPORT NEVER MENTIONED.** Writing the golden
from §14.9.13's general format rather than from the reported symptom (`feedback_spec_scopes_not_tests`) surfaced
that **`EVALUATE TRUE / WHEN VALID-CODE` — a bare level-88 object, one of the commonest idioms in COBOL — threw
the SAME run-time error.** Pre-existing, not a regression: a bare condition-name has no parenthesis to peel, so it
already lost to `valueOperand`. **Table 15** (§14.9.13.3 SR10) admits ONLY *Condition* / *TRUE-FALSE* / *ANY*
against a TRUE/FALSE subject, and §8.8.4.1.2 makes a bare condition-name a condition — but it arrives through the
`valueOperand` arm because **a bare word is equally a condition-name and an `arithmeticExpression`, and only the
RESOLVED SYMBOL can tell them apart.** That is the concrete reason no alternative ORDER can express this rule. The
classification now lives in `EvaluateBinder.BindWhenItem`, asking through
`ConditionBinder.BareOperandAsCondition` — **extracted** from the abbreviated-relation path, not copied, so
level-88, switch-status (§8.8.4.6) and simple-boolean (§8.8.4.3) objects all resolve through the one rule.
⚠ The FROZEN legacy oracle already handled this case and greenfield did not, and the GnuCOBOL differential is
structurally blind to it: the program compiles either way.

**The sweep, measured before and after.** A generic detector — a `functionCall` that took NO argument list while a
`(` still stands to its right — over 17 statement shapes: **2 peeled sites before (both EVALUATE), 0 after**;
`IF` / `PERFORM UNTIL` / `DISPLAY` / `STRING` / `INSPECT` / `MOVE` / `COMPUTE` / nested function arguments /
`ADD` with one operand and with two were clean throughout. So no general `functionCall` restructure was warranted
— that conclusion is measured, not assumed.

**⚠ AND ONE FINDING I NEARLY FILED AS A DEFECT THAT IS NOT ONE.** The sweep showed
`CALL "SUB" USING FUNCTION SQRT(X)` failing with `no viable alternative`. The obvious filing — "same family as the
arithmetic verbs" — is **false**: §14.9.4.3 SR3 confines Format 1's identifier-2 to a data item defined in the
file/working-storage/local-storage/linkage sections and SR5 makes it a RECEIVING operand, so a function result (a
temporary, §15.4) is genuinely inadmissible there. Checking what the construct can syntactically BE, before
trusting the citation, is what caught it (`feedback_validate_the_premise_not_only_the_rule`). The real gap that
probe exposed is narrower and is now **PB46**: CALL Format 2 and INVOKE both write BY CONTENT as
`identifier | literal`, omitting the `arithmetic-expression-1` and `boolean-expression-1` their general formats
print — while the BY VALUE arm one line away gets it right. **PB47** files the remaining wrong-stage half of the
EVALUATE site: a Table 15 INVALID combination compiles clean and throws at run time where SR10 makes it a syntax
rule violation.

**Guards, and proving them in the failing direction.** `EvaluateSelectionObjectArityDriftTests` pins four things:
the single-object arity; that ALSO — the repetition the standard DOES license — survives, so the guard cannot be
satisfied by deleting multi-object EVALUATE; that `valueOperand` still precedes `condition`, with the level-88
reasoning in the failure message; and, at the parse level, that a function's argument parenthesis belongs to the
function. Restoring the `+` was measured to break **both** binders at compile time, and the peel detector had
already been observed reporting `PEELED ×2 → 0` across the fix — the guard chain has been seen failing, not just
passing (`feedback_green_gates_arent_evidence`).

**Gate.** One `bash scripts/battery.sh` printing `=== BATTERY: ALL GREEN ===`: Conformance **4205/4205** zero
skipped · Unit **3658/3658** · characterization **33/33** · `guard-fast` ALL GREEN with NIST **353 MATCH / 0
REGRESSION** and audit CLEAN · GnuCOBOL differential **0 PER-CASE FLIPS**. ⭐ That an arity change in reach of
every EVALUATE in the corpus, in NIST and in the differential moved **nothing** is the evidence that the
repetition was dead for legal source — a stronger statement than "the new goldens pass".

**Golden** `tests/conformance/2023/pb45_function_in_when_object` — 12 assertions, every value derived from the
spec BEFORE the run, and **both branches of every condition** so a WHEN object that wrongly matches everything
cannot pass: the defect (`T1`), its false twin (`T2`), the alphanumeric shape that had been a raw PARSE ERROR
(`T3` — one cause, two symptoms), `FUNCTION PI` as the control (`T4`), ALSO (`T5`), GR4a6 value-subject equality
(`T6`), the level-88 object (`T7`), ranges including a function-identifier bound (`T8`/`T9`), NOT, ANY, and an
AND-chain.

