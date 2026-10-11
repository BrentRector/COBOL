## Entry 1721 — 2026-09-26 18:50 PDT — Wave-62 train 63: zero divisor terminates, edition compile-time arithmetic, concatenation operands, PB1604 retired

Train 63 carried four clusters in one landing, in manifest order. Three were planned: w62m, w62l and w62k. The
fourth, w62n (PB1604), finished before the landing gate started, so it came in after cluster 3. The train was kept
below the 4–6 band on purpose, so that it would land before the weekly quota reset.

**w62m — PB1605: an unchecked zero divisor terminates the run unit on every value lane.** The note held on
re-probe. With no SIZE ERROR phrase and EC-SIZE checking off, `COMPUTE A = B / Z`, `DIVIDE Z INTO A` and
`DIVIDE B BY Z GIVING A REMAINDER R` stored 000 and ran on. A FLOAT-LONG quotient stored +Infinity (NaN for 0/0), and
`IF B / Z > 1` took a branch. The re-probe also found a third arm the note missed: the SDIDI lane (`CobolDec.Div`)
already terminated, so the three lanes disagreed. §14.7.5 case 2 and no-phrase rule 2 make EC-SIZE-ZERO-DIVIDE exist
whether or not checking is enabled. §14.6.13.1.3 item 8 leaves only the disposition to the implementor, and
CONFORMANCE.md DOC-A.1-70 already determined it to be termination. The GnuCOBOL 3.2 survey was inconclusive: it
continues with the receiver unchanged, but its own FIXME says the intended behaviour is "fatal->abort". So the code
was wrong and the determination stood. **The owner confirmed it in chat on 2026-09-26: "termination is the correct
answer."** No code or doc change followed from the confirmation. The fix gives `CobolSizeError.ZeroDivide()` as the
ONE raise, thrown checked or not by all three quotient kernels:
- `CobolNum.Divide` (scaled Int128; it used to `return 0`);
- the new `CobolFloat.Div` (binary64), which `NumericRenderer.CombineReal` now renders for an unchecked quotient
  instead of a bare `/`;
- `CobolDec.Div` (SDIDI; it had its own raise).

The checked twins `DivideOrThrow` and `DivChecked` compose those kernels and add only the checks a checked context
owes. That way neither can drop the zero test again. Pins: `SizeErrorDispositionTests.ZeroDivisor_*` (13 rows: both
native lanes plus SDIDI, plus a phrase-path control where `A=007 FR=9` per §14.7.5 phrase rule 1) and
`ZeroDivisorLaneTests` (9 unit cases). There is no golden, because the outcome is exit 1 plus stderr. A witness-only
batch added 8 test-refs and 6 code-locations to GR-14.7.5-L3.2, GR-14.7.5-2, DOC-A.1-70 and GR-14.6.13.1.3-8. The
note is landed with `closes_rows: []` and its reason.

**w62l — PB1592: compile-time arithmetic follows the edition.** Annex E.2 6) and 21) make standard arithmetic the
prescribed compile-time mode at 2002/2014, and make the mode implementor-defined only at 2023. The ONE evaluator
(`CompileTimeExpressionEvaluator`, which now takes `EditionInfo`) selects its arithmetic through
`CompileTimeArithmetic.For`. That reads the new runtime behaviour-register member
`DialectBehavior.CompileTimeArithmeticImplementorDefined` (VCR row 12, `compile-time-arithmetic-mode`). Below 2023 it
chooses the SDIDI engine (34 digits, NEAREST-AWAY-FROM-ZERO per §11.9.11.2); at 2023 it chooses the documented
System.Decimal, and DOC-A.1-29 is narrowed to 2023. Both consumer arms, the CONSTANT binder and the
`>>DEFINE`/`>>IF`/`>>EVALUATE` stage, are fixed by construction. Values ride a mode-independent `CobolDec` carrier
(`CtNumeric`), and `CtValue` numeric equality became value equality with a canonical hash.

⚠ **Structural:** `Cobol.Net.Frontend` now references `Cobol.Net.Runtime`, so that there is one arithmetic engine
and not a second SDIDI (CLAUDE.md rule 5). There is no cycle: the Runtime references nothing in the solution. The
orchestrator checked that before dispatch, and the lander recorded the edge in `docs/COBOLNET_PROJECT_ORG_DESIGN.md`
§A point 1 and §1.4, together with the rule that the Runtime must never reference the Frontend.

The same carrier fixed three siblings:
- A sole literal is now bounded only by its §8.3.3.3.2 capacity. A legal 30-digit `CONSTANT AS` literal had been
  refused at every edition.
- An operand outside the mode's range is reported as §7.3.6.2 SR2 (it was mis-cited SR1 b). That is PB1369's leg on
  this path; PB1369 is annotated for a re-verdict and was not minted again.
- A final result over 31 digits is refused under GR3.

The first CONFIRMED INV-3 behaviour variant is `arithmetic-intermediate-precision-2023`: T2=007 at 2002/2014 and 008
at 2023. Goldens: `2002/pb1592_ct_arith_standard` and `negative/pb1592-ct-result-over-literal-capacity`. The batch
re-witnessed DOC-A.1-29 (editions → 2023), which is the row it closes.

**w62k — PB1406: concatenation operands and class follow §8.8.3.** `concatOperand` gained a `cobolWord` arm
(`CobolExpressions.g4`), so a constant-name (§13.10.3 SR2) and a symbolic-character (§12.3.7.4 GR11 a) are operands.
`"X" & SYM-A`, `SYM-A & "X"` and `"X" & K` used to be refused; they now fold to `XA` / `AX` / `XQ`. `ConcatFolder.Walk`
is the ONE computation behind `ClassOf`, `Peek` and `Fold`, and it applies §8.8.3.3 GR1 pairwise down the flattened
left-recursive format. So `SPACE & SPACE & N"AB"` and `ZERO & ZERO & B"1"` are now COBOLNET1540, where they used to
run `[  AB]` / `[0010]`.

Every fold site names a `LiteralEnvironment` (private constructor; `Program`, `SpecialNames(nationalPhrase, sr11)`
and `Unscoped`). STOP, INVOKE, OPTIONS, the boolean channel and the two SPECIAL-NAMES sites therefore can no longer
drop the national table. A SPECIAL-NAMES fold takes GR10's native extreme of the clause's NATIONAL phrase (U+FFFF).
`DataBinder.Switches.OperandLiteralClass` no longer classes a concatenation by its leading prefix.
`ExternalizedName.Screen` now takes a required `env`.

New codes: COBOLNET2473 (a word operand that stands for no literal) and COBOLNET2474 (§12.3.7.3 SR11 symbolic
operand), both inside the cluster's 2473–2475 range. `ConcatOperandTests` has 22 cases. Goldens:
`2002/pb1406_concat_word_operands` plus three negatives. Rows closed (4): FMT-8.8.3.1, SR-8.8.3.2-1, GR-8.8.3.3-1
and GR-8.8.3.3-2, all → CONFORMS.

**w62n — PB1604: retired, premise refuted.** An OCCURS DYNAMIC table cannot be in a file record (§8.5.1.9.1 3):
"other than the file section"), and COBOLNET1526 already refuses one on every route: direct, TYPE, SAME AS and SD. The
note's "VaryMax 2 < VaryMin 8" came from the `FileModel.MaxDynamicExtent` dynamic-table arm, which PB981 added and
which only ever ran on refused source. The cluster removed the dead dynamic-table arms of the record-sizing family
(the Max walk, its Min twin `HasVaryingTableBeneath`, and the `RecordLayout.ContiguousPlaceOf` key window). It also
removed the PB1277 unit case that pinned the arm on illegal source; that helper now asserts a clean bind, and the
witness was retired by batch. A 4-route COBOLNET1526 theory pins the invariant. No codes were used.

**The train.** Each cluster was brought in as `git diff <base> <branch>`, with the inventory hunk excluded and each
implementer's `record_verdicts` batch re-applied in order on the merged tree. The generated `ConstructRegistry.g.cs`
was regenerated after the constructs.json merge (247 rows). There was one conflict: the 2002 corpus manifest, where
each side added a whole element. Both were kept (516 → 517 entries, no duplicates). The conflict-marker checks were
empty after every cluster.

The landing gate was the WHOLE Conformance assembly: Conformance 9,040/9,040, Unit 29,484/29,484 and
Characterization 33/33, all GREEN. A first attempt with `~Cobol.Net.Tests` was refused by `filter_population.py` as
a DEAD term, because the namespace is `CobolNet.Tests`, so no gate ran on it. The legacy Integration assembly: 503 passed, 1 skipped of 504. semgrep verify: PASS, no count changed. `work.py check`: ✓ 1,559 items.

The pre-push review (the full-code pass over the train diff) found 0 confirmed findings, so no cluster was dropped.
GAP 1065 → 1061. Leads filed as PB1625 (the `BitString` / `CobolBool` duplicate kernel), PB1626 (`>>DEFINE …
PARAMETER` classification through System.Decimal, owner latitude), PB1627 (ALL literal-1 concatenation rejects
figurative and word operands — PB1406's second arm), PB1628 (CURRENCY SIGN with a symbolic-character operand; a
binding-order sibling of the landed PB1558, so minted rather than appended) and PB1629 (COBOLNET1526 positioned at the
typedef line). PB1369 was annotated to re-verdict SR-7.3.6.2-2 and GR-7.3.6.3-2.

