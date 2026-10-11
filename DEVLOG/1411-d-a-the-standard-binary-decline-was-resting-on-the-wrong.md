## Entry 1411 — 2026-08-31 04:42 PDT — D-A: the standard-binary decline was resting on the wrong clause, screened in the wrong place, and applied to the wrong rows — all three measured, none of them guessed

The decision was easy and had been sitting unmade: ARITHMETIC IS STANDARD-BINARY is declined. No shipping COBOL
implements it, which the standard itself says. What took the work was that every part of HOW it was recorded
turned out to be wrong in a different way.

**Wrong ground.** `CONFORMANCE.md` §2 row 2 read "Obsolete feature (A.3 NOTE 1)". Obsolescence licenses nothing.
Annex F.2's own preamble says a conforming implementation "shall support obsolete language elements EXCEPT for
elements that are also optional or processor-dependent" — the exception is carrying the whole argument, and the
exception is PROCESSOR-DEPENDENCE: A.3 item 2, "The ARITHMETIC IS STANDARD-BINARY clause in the OPTIONS paragraph
is dependent on the capabilities of the processor", with §4.2.6 making the decision "within an implementor's
discretion" and requiring both a compile-time warning and documentation of the absence. The obsolescence
corroborates and grants nothing. The row now says so, and it says one more thing the old wording hid: F.2 item 3
adds that "interest in these facilities will be REEVALUATED during the next revision", so the decline is recorded
as revisitable rather than permanent.

**Wrong place.** The precondition was to check whether `METHOD-ID … OPTIONS. ARITHMETIC IS STANDARD-BINARY.`
draws the screen. It does not — at `--std 2014` it compiled with NO DIAGNOSTIC AT ALL, exit 0. At 2023 it drew
only the obsolescence WARNING, which reads like a handled case. Then, because every bug is a pattern, the sweep
went over all six grammar productions that carry an `optionsParagraph`, and found a SECOND hollow arm the finding
had not named: INTERFACE-ID, silent the same way. And it was a different bug — the method arm was a screen in the
wrong place, while `OoDriver.BindInterfaceData` never called `OptionsBinder` at all, so the interface's OPTIONS
clauses had no effect on its prototypes either. Fixing only the screen would have left it hollow.

The fix is the single construction point: the screen moved into `OptionsBinder.ArithmeticOf`, where the clause is
READ, and the `DataBinder` twin is deleted. Every unit kind's paragraph flows through there, so all six
productions are covered by construction instead of by six remembered call sites — and screening the CLAUSE rather
than the resulting MODEL keeps §11.9.4 GR1 inheritance quiet, so a contained unit that merely inherits a mode is
not diagnosed twice. Measured after: all seven arms draw exactly one COBOLNET0806. The drift test reads the
options-bearing productions OUT OF THE .g4 FILES and demands a fixture per production; removing the
`interfaceDefinition` fixture makes it fail with the two sets printed side by side, which is how I know it works.

**Wrong rows, and this is the part that kept moving.** The plan said "~20 rows conditioned solely on the mode,
carrying five different states". 41 rules MENTION standard-binary across five states — that is where the estimate
came from — but mentioning is not being conditioned on, and the predicate took four drafts to get right:

1. Word-sense instead of mode-sense: "standard-binary" also names a FLOAT-BINARY USAGE (§11.9.8), so the first
   draft selected three §11.9.8.3 syntax rules that have nothing to do with arithmetic.
2. Over-broad exclusion: dropping any rule that said "standard decimal" also dropped AR-15.43.3-3, whose only
   such mention is "standard decimal floating-point USAGE" — a usage again, and that rule IS solely
   mode-conditioned.
3. Requiring a trailing "arithmetic" after standard-decimal then let GR-14.7.7-3 and GR-14.9.26.4-3 back in,
   whose text reads "standard-decimal OR standard-binary arithmetic is in effect" — rules with reachable content
   under a mode we DO support.
4. And all three of those drafts were TEXT-ONLY, so every one of them missed the six rules of §8.8.1.4 — the
   clause whose TITLE is the mode, and whose rules never repeat the phrase (GR-8.8.1.4.2-1 just says "Any operand
   of an arithmetic expression that is not already in SBIDI is converted into SBIDI form").

What caught the fourth was not re-reading the predicate. It was PB146, an old note that had listed seven
§8.8.1.4 rows BY HAND while the derived selector had taken one. A hand list is not authority — but a hand list
the derived selector disagrees with is a signal worth chasing, and chasing it added the CLAUSE arm. Final
population: 16 rows, 10 named by text and 6 by clause, which had been carrying four different verdicts between
them (NOT-IMPLEMENTED 9, blank 2, NEEDS-OWNER-DECISION 4, CONFORMS 1). Four answers to one question. All sixteen
now carry the ONE determination; GAP 3656 → 3641.

The selector is DATA, in `inventory-schema.json` under `derived-verdicts`, read by both the Python batch
generator and the C# drift test — because writing one predicate twice in two languages is precisely the drift
PB194 records. The drift test also asserts the selector is still SHARP at both edges: it must not select the
§11.9.8.3 usage rules, must not select the both-arms rules, MUST still select AR-15.43.3-3, and MUST still select
the six §8.8.1.4 rules by clause. Four assertions, one per draft that was wrong.

Two smaller things rode along. `DigitCapFlag` was a two-state ternary whose else-branch quietly handed
standard-binary the NATIVE 31-digit cap where §15.93.4 r1b sub-note 3 says 35 — unreachable, which is exactly why
nothing had ever contradicted it. It is now `ArithmeticModes.NumvalDigitCap`, exhaustive over the enum with the
spec's three sub-notes cited per arm, and the test walks every mode. And the four negative cases NAME their
functions (E, PI, SQRT, TEST-NUMVAL) rather than testing the clause alone, because a row whose subject is a
function must close on evidence that mentions that function.

Those cases also produced the most useful failure of the leg. I tried to falsify them by swapping the 2002 case's
expected diagnostic to COBOLNET0806 — and it still PASSED, because 0806 fires at 2002 too, alongside the 0900
introduction gate. The negative harness pins one substring per case and simply cannot see the difference, so a
unit test now pins the per-edition diagnostics WITH THEIR ABSENCES: 2002 → 0900 + 0806, 2014 → 0806 alone and no
0900, 2023 → 0806 + 0903 and no 0900. It caught its own second bug immediately — it read only `Result.Errors`
and reported the 0903 obsolescence flag missing on a compiler that emits it perfectly, because 0903 is a WARNING
and rides the other channel.

PB67 and PB146 close on this. §1 gains the paragraph explaining why this is a hard error while SCREEN, MCS,
COMMIT/ROLLBACK and VALIDATE only warn: those facilities are ADDITIVE and a program still means what it says with
them inert, whereas §8.8.1.4.1 makes standard-binary "a method of evaluating an arithmetic expression, an
arithmetic statement, the SUM clause, and certain integer and numeric functions" for the whole unit — so ignoring
it would run the entire program under a different arithmetic and surface as wrong VALUES, not a missing facility.
§4.2.6 ¶3 allows either severity; this records which we chose and why. Gate green: Conformance 1157/1157 on the
filter, Unit 5064/5064, Characterization 33/33.

