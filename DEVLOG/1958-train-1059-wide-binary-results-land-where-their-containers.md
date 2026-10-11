## Entry 1958 — 2026-10-11 12:19 PDT — Train 1059: wide binary results land where their containers hold them, float stores past the carrier raise, data-item program names take the edition's spelling, and --nist keeps its columns

Train 1059 is the land unit's train of six wave-1059 clusters, in `prune_worktrees.py --train` order: E PB2986, F PB2990,
C PB2966, B PB2965, D PB2967, G PB2991. Every base was `f91823991`, two trains behind origin/main `8399765be`. Main did not
move under the landing lease, so the final rebase was a no-op. Every implementer gated leg 1 only (batched gating,
PB2515), so this train's whole-population gate was each cluster's first population check. It found two reds, both
fixed in the owning cluster's commit. No cluster was ejected or dropped.

**E — PB2986: the --nist placeholder substitution is a mapped stage.** `NistPreprocessor.Process` was string to string
and ran after the source-line map was built, so a substitute longer or shorter than its placeholder (XXXXX058 is 8
characters, `"CONTROL"` 9) shifted every later column on its line. The re-probe reproduced it: `a15.cob` under `--nist
NC101A` reported `(7,32)` for a `)` in column 31. The stage now takes and returns `MappedText`, through one substitution
loop on `OriginWriter`: unchanged text is appended as slices that keep their origins, and a substitute takes its
placeholder's position. `Frontend.Preprocess` runs it before building the map, and the NIST-only line-count throw is
gone because the `MappedText` constructor asserts the invariant for every mapped stage. The substitutions themselves are
unchanged (the ASSIGN group became a lookbehind that matches the same spans). The witness is
`DiagnosticPositionTests.Nist_PlaceholderSubstitution_KeepsTheColumnsToItsRight`: a longer, a shorter, a SELECT-entry and
a deleted placeholder, plus one in COPY library text. DESIGN-cc-in-copy §3.5 loses its "one exception".

**F — PB2990: the PARAMETER phrase names its environment variable by the Annex C fold.** `>>DEFINE` and `>>DISPLAY` share
one lookup, `ParameterText`, which used the host's `ToUpperInvariant`. That fold joins and separates words differently
from Annex C: the Kelvin-sign spelling of LAKE read `PB2990-LA<Kelvin>E`. It now reads `CobolNames.UpperFold(name)`, the
project's one upper-case key of a COBOL word (§7.3.11.4 4), §7.3.12.4 3), §8.1.3.2 3) and 4) b)). CONFORMANCE DOC-A.1-49
and DOC-A.1-55 record the determination. `ParameterVariableNameTests` (17 cases) asserts the recorded variable NAME,
not an environment value. `CobolNameFoldDriftTests` now also refuses `GetEnvironmentVariable(... ToUpperInvariant())`.

**C — PB2966: an unsigned-wide SDIDI operand and landing.** Under ARITHMETIC IS STANDARD-DECIMAL, `COMPUTE Q = W + 0`
with W = 2^128 - 1 in an unsigned 16-byte COMP-5 took ON SIZE ERROR. There were two roots. First,
`NumericRenderer.CombineCore` narrowed every operand through `DeU` (CobolNum.Widen, loud above 2^127 - 1) before choosing
the lane, so the SDIDI lane's rounding lift never saw an unsigned-wide operand. The lane decision now comes first, and
the native funnel runs after it; `Power` and unary minus had the same arm and are fixed too. Second, `CobolDec`'s landings
return Int128, so a result in [2^127, 2^128) could not reach a receiver that holds it. The new
`CobolDec.ToUnscaledMagnitude` (UInt128) serves `CobolNum.Store` / `TryStore` for an unsigned 16-byte BinaryCapacity
receiver. Citations: §8.8.1.5.2 1), §14.7.5 3) and §13.18.60.4. Golden: `2023/pb2966_sdidi_unsigned_wide_operand_and_landing`.
Unit tests: `CobolDecWideLiftTests` (+5) and `SdidiOperandLiftDriftTests`. The implementer's two further leads are now
notes: PB3017 (DIVIDE ... REMAINDER's Int128-only kernel) and PB3018 (the native Dec lane rounds an unsigned-wide operand
at 34 digits where a signed one lifts exactly to 38).

**B — PB2965: STANDARD-DECIMAL unary functions with an equivalent arithmetic expression round a wide argument.**
`FUNCTION ABS(S)` with S = -2^127 in `PIC S9(19) COMP-5` took ON SIZE ERROR on the exact Int128 body. ABS (§15.7.4 1),
SIGN (§15.81.4 1), INTEGER-PART (§15.49.4 1) and FRACTION-PART (§15.42.4 1) each have an equivalent arithmetic
expression, so under a standard mode their value equals that expression in the SDIDI (§15.4.1 1), and the argument is
rounded at the lift (§8.8.1.5.2 1). INTEGER (§15.44.4 1) and MAX/MIN have no such expression and stay exact.
`NumericRenderer.LiftRounds` is now the one "does this lift round" predicate: `DecOperand` acts on it, and
`IntrinsicRenderer.ArgumentRoundsAtLift` asks it for the new `EquivalentExpressionUnaryArms` set. Golden:
`2023/pb2965_sdidi_equivalent_expression_functions_round_wide_argument`, which fails on the pre-fix compiler.
`ExactCarrierBoundaryDriftTests` invariant 4 pins the set. Merge resolution: B and C both edited NUMERIC_DESIGN, the
CONFORMANCE DOC-A.1-179 row and the 2023 manifest. The lander kept both sides (C's witness line and B's new paragraph;
the row merged character-wise, since the two clusters' edits were disjoint).

**D — PB2967: a float result past the Int128 carrier is a size error in a store, never a saturated value.** The checked
float-to-fixed landings returned Int128.MaxValue or Int128.MinValue as a sentinel "so the capacity check raises". A
16-byte COMP-5 receiver's capacity IS the carrier, so the sentinel was stored as a value under NOT ON SIZE ERROR:
VARIANCE, EXP10(45), `A ** 1.5`, COMP-2 ±1E40 and NUMVAL-F("1E+39"), into signed, unsigned and scaled receivers alike.
`CobolFloat.ToScaled`, the checked `CobolIntrinsics.FromDouble` / `FromDoubleBounded` and the checked `Rescaled`
(NUMVAL-F) now raise `CobolFloat.BeyondCarrier()` (EC-SIZE-TRUNCATION, §14.7.5 3)), the twin of the SDIDI lane's
`ToUnscaledChecked`. Intermediate consumers keep a saturating form, `CobolFloat.ToScaledSaturating`, through the new
`RuntimeApi.FloatToScaledIntermediate`. Goldens: `2002/pb2967_float_result_wide_binary_receiver_size_error` and
`2014/pb2967_numval_f_wide_binary_receiver_size_error`, both observed storing on the pre-fix compiler. **Fixed in the
train:** D renamed `CarrierLandingFormTests.ToScaled_PastTheCarrier_SaturatesChecked_LandsLowOrderDigitsUnchecked`, which
inventory row GR-14.6.8.2-4 cites, so `SpecTraceabilityInventoryDriftTests.EveryTestRef_ResolvesToARealTest` went red on
both hosts. D's leg-1 gate never ran that test. The lander's first fix rewrote the reference by hand, which the WITNESS
LOSS audit then refused as an unexcused loss. The fix that landed is a witness-only `record_verdicts.py` record: it
retires the old name with its reason and adds the renamed test. D's lead 2 (intermediate float consumers still saturate
where the SDIDI lane raises EC-SIZE-OVERFLOW) is now PB3019, and its lead 1 is folded into PB3017.

**G — PB2991: a program or method name held in a data item is formed in the compiled program's edition spelling.** Below
2023, `INVOKE U WS-METHOD` holding `X<final sigma>` missed `METHOD-ID. X<sigma>`. The same happened for CALL, CANCEL, SET
TO ENTRY, ADDRESS OF PROGRAM and ADDRESS OF FUNCTION through an identifier. The run time compares names by the 2023 Annex
C table and has no edition, so the edition spelling PB2840 gives a literal at bind time never reached a data item's
value. `ExternalizedNames.FormWritten` became `FormInEdition` (every caller changed). `CobolNames.HasEditionSpelling` is
the one below-2023 question. `RuntimeApi.ExternalizedNameInEdition` wraps every emitted data-item name read below 2023,
and is the identity at 2023. `ExternalizedNameFormationDriftTests` pins the funnel at all six name-rendering sites.
Goldens: `2002/pb2991_data_item_names_edition_spelled`, `2014/pb2991_function_address_data_item_edition_spelled` and
`2023/pb2991_data_item_names_two_words_at_2023`. **Fixed in the train:** G deleted the `FormWritten` line from
`PublicAPI.Unshipped.txt` and D added a line beside it. The file merges by union (`.gitattributes`), so the deleted line
came back, and gate 1 failed to build on both hosts with RS0017. The stale line was removed in G's commit.

**The train.** `train_apply.py` brought the six in. It stopped at B (three doc and manifest conflicts) and at G (the
CONFORMANCE DOC-A.1-68 row, against main's edit of DOC-A.1-75 in the same hunk); the lander resolved both by hand, and
the code-citation, doc-citation and work-register audits passed. There were four whole-population runs: 1, a build failure (RS0017, G
with D); 2, one Unit red on both hosts (D's inventory reference); 3, Linux GREEN with Windows red only on WITNESS LOSS;
and 4, run `20261011T190502Z-5a2fb9` on head `9dc4d50d8`, which was `GREEN — Conformance 11,468/11,468 · Unit
33,432/33,432 · Characterization 36/36`, every population EXACT, WITNESS LOSS GREEN (1 retired), SELF-TESTS GREEN (74
ran). The Linux gate on the same head was GREEN (selftests, unit, characterization, conformance, guard, selftests-built).
Semgrep: no count moved (no-biginteger 44, no-decimal 2). The arch-oracle against `bb79e17a3fb8` was DIFFERENT in 32 of
7,768 cases, all explained by class. Twenty-four (16 corpus programs below 2023, one at 85 and seven NIST IC programs)
differ only by G's `ExternalizedNames.FormInEdition(<name>, <edition>)` wrap around a data-item name, checked line by
line. One, `2023/pb84_sdidi_intermediate_consumers`, is D's intermediate landing changing from `CobolFloat.ToScaled` to
`ToScaledSaturating`, the same values for an intermediate consumer. Seven are the train's new goldens. The baseline is
re-recorded as `9dc4d50d8ab9`. The lander's review read every changed source file: the renderer lane order, the
`LiftRounds` predicate (equivalent to the old `DecOperand` arms), `ToUnscaledMagnitude` and the unsigned `TryStore` arm,
every checked float-landing caller, G's six funnelled sites and E's substitution loop. It found no correctness defect
beyond the two reds above. Inventory: one witness re-sited and no verdict changed, so the GAP stays 103. No diagnostic
codes were claimed (every cluster returned its range). New notes: PB3017, PB3018 and PB3019.

