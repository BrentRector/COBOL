## Entry 1410 — 2026-08-31 03:24 PDT — D-F, D-G and four registrations: two notes whose NEXT ACTIONS had gone stale, and three findings that did not survive contact with the tree

Housekeeping, and it turned up more than housekeeping usually does.

**PB37's NEXT ACTION had become a regression instruction.** It said to "flip the seven §15.68.3 r5b rows to
DOCUMENTED-NON-SUPPORT in the next batch's `record_verdicts.py` pass". PB64 increment T6 landed on 2026-08-28 and
IMPLEMENTED the LOCALE keyword on both NUMVAL-C and TEST-NUMVAL-C; measured against the live inventory,
`AR-15.68.3-L2.1` through `L2.7` are CONFORMS and OK on `2002/pb64t6_numvalc_locale` plus the `NumvalCLocaleScan*`
unit tests. Executing the old instruction would have re-verdicted seven tested rows to documented non-support,
un-done T6, and left the register describing a compiler that does not exist — while contradicting CONFORMANCE.md
§5's "A.4.9 claimed whole". Plan §0 carried the same rot in two sentences: that "eight rows ... are
DOCUMENTED-NON-SUPPORT and now CLOSED", and that "COBOLNET1518 rejects the phrase at bind time" — COBOLNET1518
was DELETED with the claim. Both struck.

The determination itself did not become wrong; its ROLE changed, and saying which is the difference between
closing a note and deleting one. It licensed *documented non-support* while the keyword was unimplemented; it now
licenses the *claim*, because an element must be optional-and-claimed for T6's implementation to COMPLETE the
A.4.9 module rather than merely add an extension. And it gained the ground it should have rested on from the
start: §A.4.1's catch-all — "Any associated syntax rules, general rules, other rules, exception conditions, and
I-O status values are also optional, even if not explicitly listed." §15.94 r1 makes §15.68's argument rules the
*associated rules* of A.4.9 item 12, so they are optional by the annex's own terms and the editorial-gap
inference is no longer load-bearing. It is kept because it explains WHY the list looks incomplete.

**PB55's figures were re-measured rather than copied**, which is the whole point of that instruction. The L-row
population is still 234, across 80 nested sub-lists — but the flattened TAILS went from 12 to **14 with changed
membership** (`GR-8.4.6.2.1 L2` and `GR-9.3.6 L4` joined; none left), and the ADJUDICATED L-rows went from 9 to
**27** (15 CONFORMS · 8 PARTIAL · 3 NOT-IMPLEMENTED · 1 NEEDS-OWNER-DECISION), which makes step 3's migration
three times the job the owner decision's text assumes. 20 L-rows carry a test-ref. And 220 of the 234 are in
state GAP, so the absorption drops the GAP count by 220 in one commit — annotated in the note as BOOKKEEPING,
because nothing is verified by that number and an unannotated burn-down would show it as the largest week of
progress in the project's history.

**Step 0 got wider on measurement.** It was written as "harvest §8.3.2.2's General rules first — six L-rows with
no parent". Absorbing an L-row into a parent that does not exist DESTROYS it, so I asked the inventory how many
such orphans there are: **32 L-rows across ten missing base rows in five clauses** — §8.3.2.2 (6, and its General
rules are entirely unharvested), §9.3.6 (15), §14.8.3.3 (5), §15.93.4 (4), §15.95.4 (2). Step 0 now harvests all
ten. The paired extractor guard — "an L-row whose top-level base row is absent" FAILS the build — is deliberately
one that starts RED on 32 real rows and goes green when step 0 lands, rather than a check that starts green and
might one day catch something. The owner's scheduling answer (absorption runs AFTER the defect burn-down) is
recorded, so the note is scheduled work and not an open question.

**Then the three registrations, and two of them changed shape under probing.**

PB194 — mode-set drift — held up exactly. `NumericRenderer` and `ExpressionBinder.CheckLiteral` both write
`is ArithmeticMode.StandardDecimal or ArithmeticMode.Standard`; `IntrinsicBinder` at :2380 and :2433 writes
`== ArithmeticMode.StandardDecimal`. Under `ARITHMETIC IS STANDARD` the SMALLEST-/LARGEST-ALGEBRAIC range screens
therefore measure against binary64's ±308 while every value in that compilation is a decimal128 SDIDI
intermediate, and the diagnostic text says "outside the native (binary64) intermediate's range" in a program that
selected a standard mode. Measured: at `--std 2014` with `ARITHMETIC IS STANDARD`, `IF 1.0E+400 > X` compiles and
prints BIG — so one arithmetic mode gets two different bounds depending on which file the path reached. One rule
written down in more than one place; the extraction is the fix, plus a drift test so a fourth copy cannot appear.

PB195 — also held up, and now has both arms measured. Same source, `--std 2023`: native rejects `1.0E+400` with
COBOLNET1661 ("the IEEE binary64 range"), `ARITHMETIC IS STANDARD-DECIMAL` compiles it and prints BIG. The
literal is an OPERAND in a relation condition, the position PB99 moved onto the exact §8.3.3.3.3 value, so the
binary64 screen is a bound the native lane no longer earns — and it is a hard error, so there is no workaround
short of rewriting the program. Registered in its own right (a defect recorded only inside a design item is one
the work register cannot rank) and it closes with the D-B landing.

PB196 — **the filed premise was false.** It said OVERRIDE was missing from `deleteFileStatement` and would bind
as a file-name, rejecting legal source. `CobolIO.g4:513` reads `DELETE FILE OVERRIDE? fileName`, and the probe
compiles and runs: `DELETE FILE OVERRIDE F-A.` → `OVR=00`. Implementing from the finding would have "fixed" a
grammar that is already correct. The real defect is one layer down: `BoundKeyedDeleteFile` carries no `Override`,
so the parser consumes the token and the binder drops it, and GR18's "If the OVERRIDE phrase is specified, the
file attributes are not checked" has no carrier below the parse tree. It is harmless TODAY — and only because
D-E's determination makes the validated set empty, so OVERRIDE skips a check that never runs. Harmless by
coincidence is not harmless by construction: the moment PB193's store lands, a program that wrote OVERRIDE to opt
out gets checked anyway, silently. Flagged `process_only` because the register ranks on what a defect does to a
user's program and this one does nothing yet — with the note saying outright that the flag must be re-read when
the store lands, so nobody reads it as a downgrade.

**One more inherited citation, and it was the bad kind.** RV-15.27.3-2 attributed to "Annex E" the remark that
STANDARD-BINARY "has not been implemented as of the writing of this revision by any COBOL provider". The remark
is Annex F.2's — but the sentence AS QUOTED, singular, is F.2 **item 5**, which is about the VALIDATE facility.
A real clause answering a different question, which `--check` would happily have passed. The true source is F.2
item 3: "STANDARD-BINARY arithmetic and STANDARD BINARY Intermediate Data Item. **These features** have not been
implemented ..." — which also carries the reevaluation note that makes a decline REVISITABLE. Repaired in place.

And `inventory-schema.json`'s DOCUMENTED-NON-SUPPORT meaning was widened from "an optional module ... per ISO
4.2.16" to name §4.2.6 and §4.2.7 explicitly, because the verdict already carried processor-dependent declines and
rules whose stated range no host can satisfy — neither of which is an optional module.

Gate green (filter ~SpecTraceability|~CorpusRunner): Conformance 932/932, Unit 5047/5047, Characterization 33/33.
`work.py check` 252 items well-formed — after it caught PB196 filed with no harm flag at all, which would have
made it invisible to `work.py next` forever.

