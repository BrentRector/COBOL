## Entry 1104 - 2026-07-29 11:59 PDT - DA2: the fold was observable, a green test held the gap open, and the review found three blockers in the fix

`DISPLAY FUNCTION ORD(C)` threw at run time. The queue entry called it an accept-display gap. It was several other
things, and all of them are worth more than the ticket.

**It was not about DISPLAY.** `OperandText.AsString` is the ONE renderer for every string-context operand, so
`MOVE FUNCTION ORD(C) TO` a `PIC X` item failed identically, with the same message. Testing sideways from the
repro found that, and it is why the fix is a single arm at one entry point rather than a patch in the DISPLAY
emitter. 8.4.3.1.2 Format 1 of an IDENTIFIER is `function-identifier-1`, so every "identifier-N" position admits
a function unless a syntax rule excludes it, and 14.9.11.3 SR1 excludes only message-tag, object and pointer.
This was conforming source being refused.

**The real finding: the compile-time FOLD was OBSERVABLE.** `FUNCTION LENGTH(G)` over a fixed item folds to a
numeric literal, and a literal renders as its own text - so it printed `5`. `FUNCTION ORD(C)` has a runtime
argument, does not fold, and threw. Same construct, same statement, behaviour decided purely by whether an
optimization fired. So the fix could not just be "stop throwing"; it had to pin ONE rendering rule both paths
reach, or the two would merely have disagreed more quietly.

**Choosing the rule.** 15.4 puts a returned value in a "temporary elementary data item" and 15.4.1 makes its
characteristics IMPLEMENTOR-DEFINED under native arithmetic; 14.9.11.4 GR1 independently makes the DISPLAY
conversion implementor-defined. The standard does not choose - but it obliges us to document the choice. Surveying
first: GnuCOBOL prints `000000108` for a computed `ORD("k")` and `8` for `MAX(3 -14 0 8 -3)` - zero-padded when
computed, minimal when folded. That is our own inconsistency, showing up in their output instead of as a crash.
We deliberately did not match it. COBOL.NET's determination is the LITERAL form: significant digits, no zero
padding, leading minus when negative, exactly the result's scale in fraction digits. Chosen because the fold
renders literals, so it is the only rule under which fold and compute are indistinguishable. Documented in
`docs/CONFORMANCE.md` as an Annex A.1 determination, including the deliberate divergence and why.

**THE DANGEROUS PART: A GREEN TEST WAS HOLDING THE GAP OPEN.** The gate came back 4116 passed, 1 failed - and the
red was `NumericIntrinsicInStringContext_FailsLoud`, asserting that this construct MUST fail loud, with a
rationale ("hazard H3, a named staged-out channel"). Not a stale test: a deliberate characterization of the gap as
intended behaviour. This is the most dangerous shape a defect can take. A RED test is a bug report; a GREEN test
pinning the wrong behaviour reads as a decision, and nothing in the battery will ever question it. The fact is
inverted now and says so.

**AND THEN THE REVIEW FOUND THREE BLOCKERS IN THE FIX ITSELF.** A 25-agent adversarial pass - the owner's four
review dimensions plus a spec-derived audit of every loud-staging site - returned defects I had just introduced,
each with a runnable repro. I reproduced every one independently before believing it, and all held.

1. **The intercept dropped `deSign`, violating 14.9.25.4 GR6a.** Putting the numeric arm at `AsString`'s ENTRY put
it AHEAD of `Visitor(deSign, floatCheck)`, and the helper never took the flag. GR6a is unconditional - "If the
sending operand is described as being signed numeric, the operational sign is not moved" - and the 15.4.1 /
14.9.11.4 GR1 latitude covers the FORM of the text, never whether the sign travels. `MOVE SGN TO A` gave `014`
while `MOVE FUNCTION MIN(3 -14 0) TO B` gave `-14`: a signed item and a function returning the same value behaved
differently in identical statements, and a text comparison disagreed with itself. Before my change that path was
LOUD; I turned it into a silent wrong answer, which is strictly worse. The structural lesson is about
interception-at-the-entry - that entry now carries three special cases, each re-implementing by hand the flags the
visitor owns. Mine forgot one.

2. **A function's printed text depended on a PRECEDING statement.** `NumericIntrinsicText` called `RenderNum`
directly, and an intrinsic's working scale is `max(Receiver.Scale, 9)` read from the renderer's ambient receiver.
`DISPLAY FUNCTION SQRT(2)` printed `1.414213562`, then `1.414213562373` after an unrelated `ADD 1 TO R`. The root
cause was not mine: `Render` and `AsNum` save and restore the ambient receiver in a `finally`, but `Fold` and
`Combine` merely ASSIGNED it, so every ADD/SUBTRACT left its receiver latched on the per-unit renderer. The class
was invisible because a leak only shows when a LATER render has no receiver of its own - which nothing did until a
numeric function could reach a text context. Fixed at the source (both entries restore now), not only at my call
site, which additionally passes `ReceiverContext.None` - the convention `IntrinsicRenderer.ArgNum` already
documented and I had bypassed. `ReceiverContextRestoreDriftTests` pins the discipline for every future public
entry, and was made to fail once by re-breaking `Combine`.

3. **Two implementations of the one rule, already drifted.** The whole point was that fold and compute render
identically - and the FOLD path had its own renderer, `IntrinsicBinder.Decimalize`, which early-returned `"0"` for
a zero magnitude and DROPPED the scale. `FUNCTION LOWEST-ALGEBRAIC` of an unsigned `9V99` item folded to `"0"`
where the runtime rule gives `"0.00"` - a literal at the wrong scale, feeding whatever arithmetic used it. I had
written the anti-pattern I claimed to be removing. `Decimalize` now DELEGATES to `CobolNum.FormatFunctionText`
(keeping a BigInteger fallback beyond `Int128`), and the bare `"0"` on the unsigned-LOWEST path routes through it.

**The golden was weak, and the review said so.** Its MOVE case used only the positive `FUNCTION ORD`, so it passed
with or without the GR6a bug. It now carries the negative-signed MOVE, the DISPLAY that must KEEP the sign, the
text relation on both sides, two SQRT renders that must agree across an intervening ADD, and the four ALGEBRAIC
folds that must carry scale. A golden that cannot distinguish the fix from the bug is decoration.

**Discovered while testing sideways - DA4, queued.** `STRING FUNCTION ORD(C) DELIMITED BY SIZE INTO A` does not
parse: `no viable alternative at input 'FUNCTION'`. 14.9.43.2 gives the sending operand as `identifier-1`,
8.4.3.1.2 makes a function-identifier an identifier, and SR8 explicitly contemplates a NUMERIC identifier-1.
DA2's fix cannot reach it - the rejection is a stage earlier, in the grammar. Filed rather than folded in.

**A self-inflicted scare worth logging.** Writing this entry, I truncated DEVLOG.md to zero bytes:
`open(path,'wb').write(add + anchor)` evaluates `open(...,'wb')` - which TRUNCATES - before evaluating an argument
that then raised a TypeError (str + bytes). The committed history was safe and `git checkout --` restored it; only
this uncommitted entry was lost and had to be rewritten. For a 3 MB append-only file that is the whole log, the
lesson is to build the bytes FIRST and open for writing only once they exist.

**Gates.** Greenfield Unit 940/940, zero skipped. FULL Conformance 4117/4117, zero skipped, nothing red.

