## Entry 1174 — 2026-08-04 15:05 PDT — PB17 re-measured: the defect is exactly as filed, one citation was wrong, and the stated fix was the wrong move

**Three entries in a row have had their claims move under measurement. PB17's did not — both shapes reproduce
verbatim** at `b21b81d4`: `MOVE W-E(FUNCTION INTEGER(3)) TO W-R` and `MOVE W-A(FUNCTION INTEGER(3):2) TO W-R`
each compile clean and throw `NotImplementedCobolFeatureException` at run time. The PB7/DA7 wrong-stage family.
Worth recording that a re-measurement CONFIRMED an entry, since the recent habit has been the opposite.

**BUT ONE OF ITS THREE CITATIONS WAS WRONG — the third inherited-citation failure this session.** The entry
credited **§8.4.2.3.3** with admitting an arithmetic expression as a subscript. `cite.py --check` rejected it:
that clause exists (Subscripts / Syntax rules) and its eight rules are about qualification, OCCURS, subscript
COUNT and ALL — not expressions. The authority is **§8.4.2.3.2**'s general format (`arithmetic-expression-1`)
and, better because it is a numbered rule rather than a diagram, **§8.4.2.3.4 GR1b**. After §13.18.27-for-
GROUP-USAGE earlier today, the pattern is unmistakable: the number travels with the quoted text and nobody
re-derives it.

⚖ **AND THE PREMISE THAT COULD HAVE KILLED THE FINDING WAS CHECKED FOR ONCE, RATHER THAN ASSUMED.**
§8.4.3.2.3 SR11 bars a numeric function "where an integer operand is required" and SR12 bars an integer function
"where an unsigned integer is required". If a subscript were such a position, the compiler's refusal would be
CORRECT and PB17 would evaporate. It is not: **§8.4.2.3.4 GR1b sets EC-BOUND-SUBSCRIPT when the expression does
not evaluate to an integer** — a runtime exception condition that would be pointless if the position required an
integer syntactically. SR14 confirms it from the other side, having to impose "only numeric literals or
arithmetic expressions whose result is a positive integer" as a SPECIAL restriction for a BY REFERENCE bit item.
Both shapes are legal source.

**THE ENTRY'S STATED FIX — "the durable fix is the segment renderer" — IS THE WRONG MOVE, AND SO WAS MY FIRST
REPLACEMENT FOR IT.** `RenderSegment`'s `default:` arm does literally list `FUNCTION` among the token types it
cannot render, so the root cause is right. But `RenderSegment` is a hand-rolled expression compiler over flat
SUBSCRIPT-mode tokens emitting C# text at BIND time; adding a FUNCTION arm means hand-writing intrinsic rendering
into a StringBuilder — a third expression compiler beside `ExpressionBinder` and `IntrinsicRenderer`.

⚠ **My instinct was then to migrate the carrier to `BoundExpr`, and the design had already forbidden it —
for a reason I would not have guessed.** `RefModPlace.Start`/`Length` are documented as "the D10 TRANSITIONAL
carrier … deliberately the SAME one `RefModSpec` uses so PHASE 15 migrates both in one move rather than leaving a
second, differently-shaped ref-mod behind", and **D10 is an owner ruling relocated to PHASE 15 §"CUT 2.5"**,
blocked because the frozen legacy compiler still shares `SUB_*`/`SubscriptEntryContext` until Cut 2 deletes it.
So the string carrier is deliberate sequencing, not decay — **checking that before escalating a "scope" question
to the owner is what stopped this becoming a fourth wrong claim.**

⭐ **THE DESIGN, AND THE STANDARD HANDED IT OVER.** §15.4: "The evaluation of a function produces a returned value
in a **temporary elementary data item**." So materialize exactly that. Bind the function through the ONE
intrinsic pipeline; synthesize the temp through `DataBinder.CreateCompilerTemp`, already documented as "THE ONE
synthesized-compiler-temp constructor"; register a pending op drained into a `BoundSequence` by `StatementBinder`
— **the pattern `ReferenceResolver.OoTryBindPropertyReference` already uses** for §8.4.3.9 object-property
references, so `ReferenceResolver` is an existing client of the mechanism rather than a new one. The subscript
segment then renders as an ordinary data-name, which `ResolveSubscriptName` already handles.

No new carrier, no second ref-mod shape, no grammar change, and nothing that collides with D10 — when D10 lands
the temp path is deleted along with the rest of the string carrier. It also makes the NEXT case automatic: the
same route serves any segment form `RenderSegment` cannot render.

⚠ **The sweep it opens is recorded rather than done blindly.** That `default:` arm also rejects `SUB_STRINGLIT`,
`SUB_DECIMALLIT` and `SUB_ALL`, and each needs its own adjudication (ALL is legal only in the §8.4.2.3.3 r6
positions; a decimal-literal subscript is legal source that GR1b then faults at run time). Routing them
wholesale would be PB1's mistake — enforcing an unaudited table — repeated in the other direction.

