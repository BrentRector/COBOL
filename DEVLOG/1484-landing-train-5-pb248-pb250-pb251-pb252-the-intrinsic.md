## Entry 1484 — 2026-09-05 03:25 PDT — Landing train 5: PB248 + PB250 + PB251 + PB252 (the intrinsic-argument screen, `>>COBOL-WORDS`'s reach, the NUMVAL carrier, and the exact Int128 boundary)

**PB248 — the integer-argument screen had no floating-point arm, in four places.** `FUNCTION
TEST-DATE-YYYYMMDD(F)` over a `COMP-2` holding 20240229.9 compiled clean and answered 0 — "the date is valid" —
and `FUNCTION TEST-DATE-YYYYMMDD(2.02402299E7)` did the same from a literal. The ISO §15.3 type-6 screen tested a
data item's declared SCALE alone, and a floating-point item is PICTURE-less, so `PicInfo.FloatItem` builds it at
scale 0 and the test was vacuously satisfied; the literal arm opened `if (text.Contains('E')) return false;` and
was disarmed for every E-form literal by construction. Re-probing found two sites the note had not named: the
always-integral EXPRESSION walk had the same blind spot (`FUNCTION CHAR(F + 1)`), and so did
`IntrinsicResultType.IsIntegerOperand` — the compiler's "ONE is-this-an-integer classifier", which is what
enforces every statement rule saying "shall be an integer", so `PERFORM … F TIMES` over 3.7 iterated three times
with no diagnostic (§14.9.28.3 SR2). The rule now lives once, in `PicInfo.IsIntegerDescription`, read by five
callers; the literal test moved onto `NumericLiteral.TryParseExact`, the existing one exact parser, so `1.0E2`
(=100) stays admitted and `2.02402299E7` does not. ⚠ The note's own citation needed correcting first: §5.5 2)b)2.
says exactly the right thing but scopes itself to "a syntax rule", and §5.3.1 gives intrinsic functions argument
rules *instead* — so §15.3 type 6 governs and §5.5 corroborates, while for the `PERFORM … TIMES` sibling §5.5
governs directly. ⛔ **OWNER-VISIBLE: this reverses the strict verdict for a floating-point operand at an integer
argument position from ACCEPT to REJECT, and two landed goldens moved because of it.** `2023/pb21_float_arg_integer_family`
is RETIRED (`git rm`, de-registered from the 2023 manifest): its header derived the old behaviour's legality as
"the integer-ness is a VALUE property, not a class the argument screen can reject on", which contradicts the
landed PB40 reading of the same clause — the same shape as the 'i'/'n' numeric-edited pair the owner resolved on
2026-08-02. `2023/pb2_float_argument_exact_family` is NARROWED: its MOD pair now takes INTEGER operands (§15.64.3
r1 is "shall be integers") while its REM pair stays floating (§15.77.3 r1 is "shall be of class numeric"), and its
`.out` is unchanged and still byte-identical. No legal source is newly refused — `--permissive` accepts every one
of those programs with a warning — and the retired golden's real subject, two Roslyn crashes (CS0117 and CS1503)
still reachable under `--permissive`, is preserved in the new
`FloatIntegerArgumentPermissiveTests`, which asserts the strict rejection, the permissive warning, and that the
float lane's values equal the fixed-point lane's. Six fixtures, a drift test over the whole schema table proven to
fail on all ten float usages before the fix, and a value-domain golden that closes §15.100.3 r1's long-unpinned
REJECT edge. Five rows PARTIAL → CONFORMS. No new diagnostic code — a float operand is the same rule as a scaled
one, so it is the same COBOLNET1627, and the PERFORM sibling keeps COBOLNET1646.

**PB250 — `>>COBOL-WORDS` reached only the words the lexer tokenizes, and the design doc said that was
everything.** The directive was applied by ONE mechanism, the post-lex token retype, which can reach a word only
when the lexer makes it a keyword token; `DESIGN-cobol-words-directive` §2 asserted that the only words without a
keyword token type are intrinsic-function names, and `CobolKeywordTokens` asserted in its own summary that "every
reserved word and context-sensitive word is a literal lexer token". Nothing measured either claim and both were
false. Measured against ISO §8.9 ∪ §8.10 (552 words): 447 were in the token map, **17 are lexed but were invisible
to it** — ANTLR publishes a literal NAME only for a token defined by exactly one literal, so
`ZERO : 'ZERO'|'ZEROS'|'ZEROES'` and `PIC : 'PICTURE'|'PIC'` publish none — and **88 are no token at all**,
ANYCASE and LOCALE among them. For all 105 the directive was ACCEPTED and SILENTLY INERT in both directions:
`EQUATE "LOCALE" WITH "LOCALITY"` then `TEST-NUMVAL-C(X LOCALITY FR)` rejected legal source with an arity error
naming the renamed keyword, and `UNDEFINE "ANYCASE"` still ate the user's own data item as the keyword, switching
on case-insensitive currency matching with no diagnostic. The fix is the second mechanism, once, for the whole
class: `CobolWordsMap.Resolve` is the ONE reading of §7.3.10.4 GR2/GR3/GR4 and every by-name classifier calls it —
`IntrinsicBinder.KeywordWordOf` (the funnel every §15 phrase word is read through, so TRIM, FIND-STRING,
SUBSTITUTE, CONVERT, MODULE-NAME, LENGTH and NUMVAL-C/TEST-NUMVAL-C are covered by one call),
`CobolParserCoreBase.Word` (the one text comparison behind every parser predicate), the SET-statement locale
categories, the ALPHABET coded-set names, and `CALL … AS NESTED` in both of its arms. The token arm was fixed
mechanically too: `CobolKeywordTokens` falls back to asking the LEXER what it makes of a word, so a multi-spelling
rule resolves without a hand-maintained list, and the de-reserve retype now matches the token TEXT because one
token type carries several words while SR3 names one. Probing the sweep found a third arm: SR3's category test
asked the lexer vocabulary whether a word is context-sensitive, so it rejected every legal directive naming one of
the 31 §8.10 words this compiler does not tokenize — a generated §8.10 table, single-sourced from the spec section
and drift-tested against it, now answers SR3/SR4. Along the way `gen-reserved-words.ps1` turned out to be
non-reproducible: it emitted the wrong namespace and its OCR continuation-join had not kept up with a
transcription repair, so a re-run silently un-reserved FLOAT-NOT-A-NUMBER-QUIET and -SIGNALING at 2023; both
fixed, and it now reproduces the committed §8.9 tables byte-identically. One self-inflicted regression is recorded
honestly: applying `Resolve` unconditionally at `KeywordWordOf` broke `FUNCTION TRIM(X LEFTMOST)` after
`SUBSTITUTE "LEADING" BY "LEFTMOST"`, because the retype had already resolved and re-spelled that word — the
directive must be applied to a word EXACTLY ONCE, and `CobolWordsRewriter.CanonicalWordOf`/`TokenIs` now own that
distinction. Six goldens, two drift-test files (the reach invariant proven red first — it names exactly the 17
words), `FMT-15.94.2` PARTIAL → CONFORMS. No new diagnostic code: once the words resolve, COBOLNET1623,
COBOLNET1504 and COBOLNET1639 already say the right thing.

**PB251 — NUMVAL and NUMVAL-C return the value their definition fixes, in every arithmetic mode.** §15.4.1's
closing sentence grants an implementor-defined returned value only "unless otherwise specified in the function
definition", and §15.67.4 r1 / §15.68.4 r1 are exactly that specification, written once with no arithmetic-mode
qualification. The native lane took the latitude anyway, materializing the value at `max(receiver scale, 6)`
fraction digits: `DISPLAY FUNCTION NUMVAL("0.1234567")` printed **0.123456**, and a receiver was no protection
either — it bounds the STATEMENT's result, not an OPERAND's precision, so
`COMPUTE R = FUNCTION NUMVAL("0.1234567") * 10000000` stored **1234560** into `PIC 9(9)` where r1 owes 1234567.
No compile-time working scale could have fixed it: an argument with *i* integer digits needs *i* + *ws* digits in
the `Int128` carrier and §15.67.3 r3 permits *i* = 31, so the only safe *ws* is 7 — while `NUMVAL("0.123456789")`
needs 9. **The fix is the carrier, not a bigger floor:** `RenderNum` routes both functions to the SDIDI projection
*before* the arithmetic-mode dispatch and the native working-scale case arms are gone, because §15.4.1 leaves the
implementor only "the characteristics and representation" and the SDIDI is the one representation that holds every
conforming argument without a working scale at all. NUMVAL-F stays on the float lane — §15.69.4 r2 grants it, and
only it, "an approximation" under native, which is why r3 has to state its standard-decimal value separately.
Three consequences landed with it. `ReceiverContext.NumvalScaleFloor` is now `SdidiLandingScaleFloor`: it was
never a §15.67 rule, and the name alone kept a truncating lane looking spec-derived. The three now-unreachable
`Int128` value projections (`Numval`, `NumvalC`, `NumvalCLocale`) are deleted — leaving a second copy of one rule
is precisely how this survived, PB60 having fixed the standard-mode projection and left the native one wrong for a
year — and while re-pointing them a latent trap closed: the four SDIDI bodies defaulted `digitCap` to 34 where
every other entry point defaults to the native 31, which would have silently admitted a 34-digit argument under
native arithmetic the moment they served it. And RV-15.4.1-2's real finding — that "the ONE argument intake" was
five, with different value semantics — is answered structurally: `IntrinsicRenderer`'s argument seams now declare
`INTAKE(EXACT | LIFTED | ALIGNED | LANDED | INTEGRAL | APPROXIMATED | PREDICATE)`, and
`IntrinsicArgumentIntakeContractDriftTests` derives the set from the source so a SIXTH seam is a red test rather
than a hole in a list (proved red once by removing a marker). Goldens `pb251_numval_native_value_fixed` — fifteen
of whose lines are byte-identical to `pb60_numval_standard_decimal`'s for the same references, the rule stated as
a test — and `pb251_implementor_defined_argument`. RV-15.4.1-2 and RV-15.4.1-L2.2 PARTIAL → CONFORMS, and six
further §15.67.3/§15.68.3 rows had their `#Numval` / `#NumvalC` code-locations re-pointed at the surviving SDIDI
twins. Found in passing and reported, not taken: `FUNCTION COMBINED-DATETIME(d, <arithmetic expression>)` under
STANDARD-DECIMAL still truncates argument-2 at the landing floor and stops equalling its §15.17.4 r1 equivalent
arithmetic expression (filed as PB620).

**PB252 — the exact Int128 carrier wrapped in three functions, and the SDIDI routing missed two more.**
`COMPUTE R = FUNCTION SUM(P P P Q)` over four operands each individually inside the carrier stored
**−4028236692093846346337460743179** — the two's-complement wrap of 3×10³¹−3, sign-flipped — with the statement's
`ON SIZE ERROR` phrase **not taken**, because the wrapped value fits a 31-digit receiver where the true one needs
32. `FUNCTION RANGE` did the same and returned a **negative range**, a value §15.76.4 r1 cannot produce, and
`FUNCTION MEAN` inherited it from the shared `SumScaled` body. PB32 had written the policy — a wrap is never a
conforming answer — but implemented it only for MEDIAN and MIDRANGE, and named the helpers after MEDIAN's halving
(`ScaleForHalving`, `AddForHalving`), which is exactly why their identical siblings twenty lines above looked like
a different problem for eight months. The primitives are now named for the OPERATION they guard, over one
`SizeEscape` raise site, and `ExactCarrierBoundaryDriftTests` closes the whole exact-carrier entry-point set by
reflection: a new exact function is boundary-pinned or exempted with a reason, or the build goes red. The second
half is the same shape one layer up: the always-SDIDI routing under a standard arithmetic mode was an inline name
list, PB62 had added the summing family to it, and **MOD and REM were never added** — so a conforming
`FUNCTION REM(A B)` with `A PIC 9(31)` beside `B PIC 9V9(18)` terminated the run unit with EC-SIZE-OVERFLOW where
§15.4.1 rule 1 fixes the value at 0.5 exactly. The membership rule is structural — *does the native arm
cross-align its arguments to one common scale?* — so it is now `CrossAlignedNativeArms`, derived and drift-tested
against the switch that defines it, beside `StandardValueFixedByRule` for the members that are there for their own
cited reasons. ⚠ Two premises had to be corrected first. The note's MOD witness is **impossible from conforming
source** (§15.64.3 r1 makes both arguments integers, so no common scale is ever formed, and the compiler already
rejects the repro with COBOLNET1627) — REM is the live half; MOD is routed with it because the property, not the
anecdote, decides. And the shipped runtime message cited **§8.8.1.2 rule 7**, a real clause about arithmetic
expressions being exempt from the composite-of-operands limit — the rule that makes a carrier escape the size
error condition is **§14.7.5 rule 5**, and the sweep found every *other* citation of r7 in the tree correct. Two
goldens, both verified to fail on the pre-fix build (`RANGE-SIGN=NEGATIVE`; the standard-decimal program aborts
before its first `DISPLAY`), and neither pins an implementor determination: the SUM case is forced by §14.7.5 rule
3 with no latitude, the RANGE case is a disjunction over the only two conforming outcomes, and the never-negative
leg is a consequence of §15.59.4/§15.63.4. `RV-15.88.4-1` and `RV-15.4.1-L2.1` PARTIAL → CONFORMS, with all four
of L2.1's recorded residues re-measured at HEAD rather than taken on trust, and `RV-15.76.4-1` re-verdicted
CONFORMS → CONFORMS with its evidence widened — its code-location and its only test-ref were both the float lane,
so nothing had ever exercised the native arm that returned the negative range.

**The train.** Four clusters, one landing, one build, one gate. Rows 3 and 4 overlapped on five files, measured
before the train ran rather than discovered during it, and the two real collisions were both in
`IntrinsicRenderer.RenderDec`. Resolved by the rule and not by keeping both spellings: PB252's derived
`CrossAlignedNativeArms` ∪ `StandardValueFixedByRule` is now the ONE spelling of the always-SDIDI routing decision
(the inline `alwaysDec` name chain is gone), and the NUMVAL pair STAYS in `StandardValueFixedByRule` — not as a
duplicate of PB251's `ValueFixedByDefinition` but because its argument is a STRING, so `AnyDecOrRealRaw` is false
and without the membership `RenderDec`'s early-out would return null under native and PB251's mode-independent arm
could never reach the body it routes to. The ordering is written down at both sites. `CobolIntrinsics.Exact.cs`
(PB251 deletes three Int128 projections, PB252 renames the halving helpers and adds `SizeEscape`),
`COBOLNET_NUMERIC_DESIGN.md` and `CobolIntrinsicsDecTests.cs` merged with both corrections intact. Union filter
`~CobolWords|~ContextSensitive|~Corpus|~Carrier|~DefectiveRow|~FloatIntegerArgument|~Intrinsic|~Locale|~Numeric|~Numval|~Perform|~ReservedWords|~SpecTraceability|~VersionMatrix`,
with the Unit leg unfiltered by design so both new drift suites (`IntrinsicArgumentIntakeContractDriftTests`,
`ExactCarrierBoundaryDriftTests`) and `CarrierLandingFormTests` all ran. Conformance `Passed! - Failed: 0, Passed: 4068, Skipped: 0, Total: 4068` (6 m 11 s); Unit `Failed! - Failed: 2, Passed: 5411, Skipped: 0, Total: 5413` (2 m 2 s); Characterization `Passed! - Failed: 0, Passed: 33, Skipped: 0, Total: 33`. The two Unit reds are the known fresh-worktree shape, named: `ExternalCorpusPopulationDriftTests.TheCensus_NamesEveryPopulationAndItsProgramCount` and `…SweepExternalPopulation_EqualsTheDifferentialsCommittedCaseCount` — there is no GPL external corpus in a fresh worktree and it is never `git add`ed; every implementer's own gate carried the same pair. `GrammarDiagramGeneratorDriftTests.Generator_RunsClean` (PB588) PASSED. Nothing else failed, so no red was attributable to a cluster and nothing was bisected out. Both citation audits at
zero; `scripts/semgrep/verify.py` PASS with every count identical to the baseline. The five verdict batches were
re-applied on the merged tree in cluster order (the inventory JSON was never hand-merged), then
`build_inventory.py` once: **GAP 2893 → 2883**, ten rows PARTIAL → CONFORMS plus six code-location re-points and
one evidence widening. No cluster was dropped, and no cluster claimed a diagnostic code — the next free code is
still COBOLNET1763.

