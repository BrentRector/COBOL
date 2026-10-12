## Entry 1660 — 2026-09-23 03:20 PDT — Landing train 56: wave 56 (HA, HB, HC, HD, HE, HF), nineteen notes landed, GAP −23 to 2069

Train 56 carries six clusters, one commit per cluster. They were composed on main at Train 54, gated there, then rebased onto Train 55 and gated again, because the pipelined lander waited for Train 55 to land first. The rebase conflicts were all whole-element additions: the diagnostic catalog and the corpus manifests (2002, 2014, negative). Both sides were kept. The inventory was auto-merged. It was checked by rebuilding it from origin/main's inventory and all fifteen batches, and the result was byte-identical.

**HA: PB357 + PB333 + PB318 + PB330 + PB417 (OPEN/START screens).**
- START WITH LENGTH used to narrow arithmetic-expression-1 to `int` when it emitted the value. LENGTH 2.5 and LENGTH 4294967297 both positioned with status '00'.
- The count now travels unnarrowed as a `StartKeyLength`. `IndexedConnector.Start` asks §14.9.41.4 GR14 once, using `SetAmount.Land` for integrality, and those cases give '23'.
- The START KEY operator is screened against the §8.8.4.2.2 general-relation set before `MapOperator` folds the spelling. `NOT >=`, `NOT <=` and `EQUAL THAN` now draw COBOLNET0862.
- OPEN gains its §14.9.27.3 screens. SR1 (a report file opened INPUT or I-O) is COBOLNET2371. SR2 (EXTEND on non-sequential access or on a LINAGE file) is COBOLNET2372.
- The exception-checking PERFORM bans now count OPEN and INITIALIZE operands, following the multi-operand wording of GR20 and §14.9.20.4 GR3. That fixed the INITIALIZE arm too, which was PB417.
- `2023/pb326_ec_report_file_mode` lost its ARM 2, an OPEN INPUT of a report file that SR1 makes illegal.
- 8 rows closed.
- Leads from the re-probe:
  - GO TO … DEPENDING narrows its selector to `int`, so 4294967297 goes to P1.
  - The WRITE ADVANCING and RETRY counts are narrowed the same way.
  - Relation conditions accept `NOT >=` everywhere; this needs a dialect decision.
  - §13.18.46.3 SR3 (READ/WRITE/… on a report file) is not screened.
  - The COBOL-85 START operator set needs a VCR check.

**HB: PB361 + PB362 + PB363 (declaratives structure).**
- §14.9.49.3 SR3 and SR4 are now checked by section identity in `ResolveProcedureOperand`, the one procedure-name funnel. A `ProcedureReferenceKind` defaults to restricted, and only PERFORM (SR4) and RESUME (SR3) claim an exemption.
- SR4 is an error (COBOLNET2376). SR3 is a warning (COBOLNET2375), under determination D-DECLREF.
- A misplaced USE is COBOLNET2377 (SR1), where it used to be a "not implemented" deferral.
- A USE BEFORE REPORTING procedure now refuses GENERATE, INITIATE and TERMINATE (SR10), and any store into a control data item (SR11). Both are COBOLNET2378.
- Lander composition with main's refusal ledger (PB1029):
  - The PERFORM and RESUME resolution arms keep `BoundRejected.Reported` and pass the new kind.
  - The SR11 check runs before `drewError` is read, so its error counts as the statement's own.
- Train 55's PB1010 method DECLARATIVES auto-merged beside it.
- 5 rows closed. PB369 (Format-2 GLOBAL USE) stays open.
- Leads: COBOLNET0897 is a bare-string code, and an SR11 store through a REDEFINES alias is not detected.

**HC: PB307 + PB304 + PB306 (intrinsic run-unit state).**
- FUNCTION RANDOM's sequence moved from a process static to `RunUnit.Random` (§15.75.3 r4). A new drift test fails on any writable static in Cobol.Net.Runtime that is not documented as process-lifetime.
- That test was red at the train gate on `PointerImage.s_nextBase`, which main's PB970 arm 2 had added after HC's base. The lander documented it in the test's process-lifetime list: its bases key the process-wide area and name tables beside it, so restarting it per run unit would give distinct pointers equal images.
- `RemDec` now raises REM's own §15.77.3 r2.
- CONCAT and BASECONVERT of PIC 9 USAGE NATIONAL items were typed alphanumeric. The note called this latent, but it was live, and it is fixed through one reader, `IsNationalArgument`.
- Witness batches only; the GAP does not move.
- Leads:
  - `ResetCurrent` leaves the switch, locale and report-flow state alone.
  - `CobolTable.Scratch<T>.Slot` races between concurrent run units.
  - The BYTE-LENGTH message misnames a function argument.

**HD: PB360 + PB349 (file registry, sort-merge ECs).**
- `FileRegistry.Unlock` was the last statement entry still using `TryGetValue`-and-return. A sibling sweep found nine more, and all ten now go through `Require`.
- EC-FLOW-RELEASE, EC-FLOW-RETURN and EC-SORT-MERGE-RETURN now have raise sites. The sort store tracks the procedure phase and the §14.9.34.4 GR3 at-end latch.
- The program's RELEASE and RETURN statements render checked entries. The implicit USING/GIVING transfers stay unchecked.
- Train 55's PB993 SORT/MERGE termination auto-merged beside it.
- 3 rows closed.
- Leads: EC-SORT-MERGE-ACTIVE, -RELEASE and -SEQUENCE have no raise sites, and no drift test checks that every turnable name has a raise site.

**HE: PB859 + PB910 + PB232 + PB226 (naming and literal screens).**
- One pre-bind screen, `IntegerOperandPass` (COBOLNET2386), enforces the nonzero half of §5.5 1). The zero-permitting positions sit in one cited table, and a reflection drift test checks it. OCCURS 0 TIMES used to reach Roslyn as CS0029.
- ENTRY-CONVENTION is now read: GR3 and SR1 are COBOLNET2385, and A.1-64 is documented as Not provided.
- Lander composition: main's PB764 made the clause's operand `entryConventionName`, so `EntryConventionOf` reads `ec.entryConventionName()`.
- PB226's residue now cites SR11.
- 6 rows closed. PB919 stays open.
- Leads:
  - A symbolic character in a CLASS clause draws a generic parse error.
  - A zero CONSTANT used as an OCCURS bound bypasses the §5.5 screen.
  - The second half of SR-13.18.38.3-28 is unclaimed.
  - IBM's `BLOCK CONTAINS 0` needs an owner decision on leniency.

**HF: PB992 + PB162 (CALL residue).**
- A usage-DISPLAY numeric item written through a character channel now stores its character image. The channels are a group MOVE, a BY REFERENCE argument or formal, and a RETURNING receiver.
- Measured values changed as follows: H [000]→[   ], N [000]→[ABC], S [012B]→[12AB].
- PB162: the five Format-1 narrowing sites go through one helper. Under `--permissive` they warn and bind with Format-2 semantics.
- Lander composition:
  - Refused spellings return `BoundRejected.Reported`.
  - `CallAbi.Text` runs the OMITTED arm first, then main's pointer-content adaptation.
- The first train gate was red on `DefectiveRowCoverageDriftTests`. w56f's batch had left GR-14.2.3-8 PARTIAL on a pointer residual that it said "PB970 still claims", but PB970 had since landed and released the row. The lander re-adjudicated the row:
  - A POINTER under a same-length character formal is not a GR8 case. §14.8.2.3.2 requires a pointer argument passed by reference to meet a pointer formal of the same category.
  - Format 1 refuses a pointer BY REFERENCE (COBOLNET1679, probed on the train tree).
  - The BY CONTENT image is GR9's, and PB970's note records that GR8's only remaining residual was PB992's mechanism.
- The row is closed CONFORMS, with every witness restated. PB992's `closes_rows` names it.
- 1 row closed. PB165 stays open.

**The train.**
- Gate: one Debug build, then:
  - The whole Conformance assembly, 8,329 / 8,329. This includes CorpusRunner, NIST and the version matrix.
  - The unfiltered Unit assembly, 29,075 / 29,075.
  - Characterization, 33 / 33.
  - The legacy Integration assembly, 503 passed + 1 skipped.
  - All on the rebased tree.
- Before the rebase, the first whole gate was 8,314 Conformance green and Unit red on exactly the two composition findings above. Both were resolved before the re-gate.
- Semgrep baseline counts are unchanged, witness-loss is 0 unexcused, and `work.py check` is clean.
- **GAP 2092 → 2069 (−23).**
- No cluster was dropped.

