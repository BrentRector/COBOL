## Entry 1935 — 2026-10-08 04:15 PDT — Train 1039b: waves 1038/1039's T, U, V, X, Y, G, A, A2, N, Z — one steering path, one shell-location parser, the branch lifecycle, one corpus rule, GO TO before a conditional, the exact nested product, the two-literal warning and one literal-alias resolution, one identified object per receiving property; R2 batch 1 filed (PB2596, PB2597, PB2598, PB2599, PB2600, PB2601, PB2602, PB2610, PB2611, PB1900, PB289, PB1470, PB1544, PB2078 landed; PB2612 retired; PB2605–PB2607, PB2613–PB2658 filed)

**T — PB2596, PB2597 (landed).** The operator steers the loop through ONE validated path, `steer.ps1`, which writes
`handoff.operator.json`; `next_unit.py` rule 0 ranks it above every handoff and check, the supervisor re-validates it
(an invalid one is set aside loudly), consumes it when its unit starts and archives an orphan `handoff.json` instead of
deleting it; `orchestrate.ps1 -Unit` writes through `steer.ps1`. The context policy has two levels: a soft cap
(`-MaxContextTokens`, STOP-UNIT only, so a running fleet finishes and lands) and a hard cap or STOP (plus the fleet
stop and the grace kill); `dispatch_guard.py` rule 0 refuses a launch from a loop unit once STOP-UNIT exists. The train
merge ported its tests into main's split `test_orchestrate_*.ps1` parts and put rule 0 ahead of main's campaign rule.
**Train review:** a `campaign` steer with no campaign lane was rewritten to a wave without its `operator` flag, so it
was never consumed and rule 0 chose it on every later iteration, ahead of an owner question; the replacement keeps the
flag (a new `test_orchestrate_steer.ps1` check, red before, 22/22 now).

**U — PB2598, PB2599 (landed); PB2605 (filed).** The session-start skill-review check treats a review as covering a
SKILL SET (`account.skill_review`, the one place the rule lives), so a freshly seeded account no longer re-asks the
owner. `scripts/hooks/shell_location.py` is the one parser of both shells' location changes; the build guard,
`worktree_rm_allow`, `forbidden_commands` and `status_guard` share it and their own `cd` parsers are deleted. PB2605:
`devlog_staged.py` checks the main checkout's index, not the committing tree. **Train review:** `worktree_rm_allow`
granted a `git rm -r` carrying a command substitution or a redirect (`git rm -r -- "$(rm -rf ~/x)"`,
`git rm -r x >E:/COBOL/src/a.cs`), which the shell runs or writes before git starts; the hole predates the rewrite. A
git command with `$`, a backquote, `<` or `>` is now never covered (five self-test cases, four red before).

**V — PB2600, PB2601, PB2602 (landed); PB2606, PB2607 (filed).** `prune_worktrees.py` is the one branch survey: every
branch and detached worktree is IN FLIGHT, WAITING TO LAND, LANDED or DECISION, with its evidence; `--apply` removes
only LANDED rows, and `--apply --abandon <b>` a clean DECISION row whose kb/Work note records it ABANDONED; the bundle
archive is gone (owner, 00:52). Every lander template ends a refused push with `READY-TO-PUSH <worktree> <sha>`;
`coord.py loop-state` and `units/land.md` carry every WAITING TO LAND branch in one train. **Train review, two ways
`--apply` could delete work:** (1) LANDED meant "every added file and every declared C# name is on main", which a
branch that only MODIFIED existing files also satisfies, so an unlanded method-body fix would have been deleted after
the next push; LANDED is now proven by `git merge-tree --write-tree origin/main <branch>` giving main's own tree (any git
failure is a no), and the missing names stay as CHECK evidence. (2) A `git status` that failed (a corrupt index,
dubious ownership) read as clean at the survey and at removal, so `worktree remove --force` took an untracked file; it
now counts as dirty, and a failed `branch -D` is reported. Self-test arms `modified-only` and `unreadable` are red on
the old logic; MANDATORY-PRACTICES O10's content classes restated.

**X — PB2611 (landed), PB2612 (retired as PB2610's duplicate).** Battery #89 was NOT GREEN with every compiler leg
green because `battery.sh` never fetched the per-worktree GPL corpus. `scripts/external_corpus.py#ensure` is the one
rule (fetch when the marker is absent; any failure is the named red `EXTERNAL CORPUS FETCH FAILED, POPULATION
UNMEASURED`), applied by `run_gate_legs`, `battery.sh`, `record_impact_map.py`, `linux-gate.sh` and `session_start.py`;
`ExternalCorpusFetchTests` locks it. With the corpus the differential ran 1,323 cases with 7 flips, all ruled EXPECTED,
and the baseline was re-recorded with exactly those rows (`--write-baseline` now requires `--record`).

**Y — PB2610 (landed).** COBOLNET2967 refused `GO TO P2 IF A = 1 DISPLAY 'Y'.` at every edition, but §14.9.17.3 SR2
binds only "a consecutive sequence of imperative statements within a sentence", and §14.5.1 makes an unterminated IF,
EVALUATE, SIZE ERROR or AT END a conditional statement, which ends that sequence. `StatementPosition.EndsImperativeRun`
asks the existing §14.5.1 classifier `ConditionalStatements.IsConditional`; COBOLNET2968 (STOP RUN) shares it. Golden
`85/pb2610_go_to_before_conditional`; SR-14.9.17.3-2 and SR-14.9.42.3-1 re-verdicted CONFORMS.

**G — PB1900, PB289 (landed).** `COMPUTE R = A * B - C * D` over PIC 9(21) operands (exact result 1) stored 10000000:
each nested product past 38 digits was rounded to the 34-digit intermediate before the difference, and the same fault
showed in IF, EVALUATE, FUNCTION ABS, a three-factor difference and a scaled quotient. §8.8.1.3 leaves native
arithmetic to the implementor and GnuCOBOL's `cob_decimal_mul`/`_add`/`_sub` are exact (CLAUDE.md rule 1), so a nested
native product is now EXACT: `CobolWide` (signed 256-bit at a compile-time scale, EC-SIZE-OVERFLOW instead of a wrap)
carried as `NumX.Wide` where the digit bounds prove 77 digits suffice, settled ONCE by `NumericRenderer.Settle`;
quotients and powers stay on the SDIDI. PB289: an arithmetic-expression argument of CALL and INVOKE is the final
transfer into the formal's description (§14.2.3 GR9/GR10), built by `ReceiverContext.Of`, so `1 / 3` into a PIC
S9(5)V9(20) formal is 0.33333333333333333333. Found and filed: PB2616 (a subscript expression wraps in `long`) and
PB2617 (`A ** 2` over a fractional base is binary64). **Train review:** a FLOATING-POINT numeric-edited resultant's
scale is only the mask's hint (§14.6.8.4 1): the value normalizes into the mask), yet `Settle`, the `MulAtScale` arm and
the outermost `Divide` rounded at it: `COMPUTE FE = A * A` (A = 10**20) into `-9.9(5)E+99` stored 0.00000E+00,
`A * A - A` 9.99999E+29 and `1 / 3000000` 3.30000E-07. `ReceiverContext.RoundsAtItsScale` is now the one predicate,
`FloatLanding` is written in its terms, and the sibling MAX/MIN selection asks it too (MAX(1.234E-07, 1E-10) stored
1.20000E-07). Golden `2023/pb1900_float_edited_resultant_scale` fails before and passes after.

**A — PB1470, PB1544 (landed).** §8.8.4.2.1's closing sentence, "A relation condition shall contain at least one
reference to an operand that is not a literal", is asked where a relation is WRITTEN (`ConditionBinder.WrittenRelational`)
and, by determination D-RELLITERAL (GnuCOBOL warns under -Wall), is WARNING COBOLNET3146 at every edition; row
GR-8.8.4.2.1-13 DIVERGES → CONFORMS. PB1544: a constant-name and a symbolic-character are resolved by the ONE
`ExpressionBinder.LiteralAliasOf` at every procedure position; `ConstantOperand`, `Sr23ConstantIsNumeric`,
`NonNumericConstantExpr` and two `BoundStringLiteral` copies are deleted, and `CALL USING BY CONTENT <constant>` takes
Format 1's literal refusal. The train's first gate was red on two `SpecTraceabilityInventoryDriftTests` (malformed
test-refs, fixed in A) and its second on WITNESS LOSS (three rows still cited the deleted members); the batch
`train1039b-A-witnesses` re-sited them to `LiteralAliasOf` / `AliasOperand` with the deleted members recorded as
retired witnesses (0 unexcused).

**A2 — PB2078 (landed).** A receiving object property identifies ONE object: identifier-3 is evaluated once into an
identified-object temporary and both the GET and the SET invoke on it (§14.6.4 7), §8.4.3.9.4 GR3), which made the
SelectedByValue flag and the COBOLNET0899 refusal dead (deleted). `ReceiverBracketEmitter` places the accessors by each
statement's own identification rule; five sibling wrong answers fixed (UNSTRING, INSPECT, PERFORM VARYING that never
terminated, STRING POINTER and CALL RETURNING printing the old value in NOT ON). **Train review:** `CallEmitter.EmitCall`
ran the property SET inside the activation's try, so a condition the SET method raised (its own CALL of a missing
program) took the OUTER CALL's ON EXCEPTION and the run continued, although the program was successfully called
(§14.9.4.4 3) i)); the same CALL inside the callee terminates the run unit. The SET now runs after the catch arms on a
successful return only (`CallPropertyReceiverPartitionTests`, `ReceiverBracketDriftTests`; the third gate was red on that
drift test's old count of two Settle sites, the lander's own change, fixed).

**N — PB2613 (filed).** `plan_wave.py#attempts` counts every report naming a note, so wave 1038's eight void stop-file
SPLITs escalated wave 1039's group A to Opus and made five groups FINISH-FIRST against deleted branches; the fix shape
(a void report is neither an attempt nor a predecessor) is in the note.

**Z — PB1754 (R2 batch 1 filed).** R2 batch 1 (pin 8be230068; 7 shards × 5 dimensions plus the duplication pass) is
filed: 31 mechanisms PB2619–PB2649 and, from the triage of 17 invalid records, PB2652–PB2658; `r2_collect.py` no longer
lets a null skeptic correction overwrite a finding, reads `design_ref` by its leading token, and requires a defect harm
that `work.py` ranks. Batch 2 is held for the owner. The train's own leads: PB2614 (dispatch_guard records a read-only
Workflow as write work), PB2615 (non-ASCII argparse help on cp1252), PB2616, PB2617 (G's), PB2618 (STRING INTO a
property loses untouched positions; adjudication first); PB1902 records the abandonment of
`worktree-wf_19837670-9d4-1` with its golden verbatim; PB2609 gains this train's recurrence.

**The train.** Ten clusters, none dropped or ejected; batched gating (every implementer gated leg 1 only). Whole-population
gate runs: 5 — the first lander's two (red: A's test-refs, then A's witness loss), and this lander's three (red: the A2
drift count; GREEN at 7e4a96cad; GREEN at 0342e990a after the T/U/V review fixes): `=== BUILD-LOCAL GATE: GREEN —
Conformance 11,213/11,213 · Unit 32,674/32,674 · Characterization 36/36`. The step-5b review found seven defects (G 1,
A2 1, T 1, U 1, V 3 with two sharing one root), all fixed in their clusters' commits with a test that fails before.
Semgrep unchanged (BigInteger 46, decimal 2, raw diagnostic literal 268). Inventory GAP 145 → 144. No diagnostic codes
claimed. Oracle DIFFERENT, 64 of 7,603 cases, every one explained: 15 ADDED goldens and 1 REMOVED negative (A, A2, G, Y);
DIAGNOSTICS — 22 corpus programs and two negatives gain COBOLNET3146 (A, PB1470: they compare two literals), COBOLNET1762
names the constant-name argument (A, PB1544), COBOLNET2967/2968 say "the imperative statement" (Y); CSHARP — 15 property
programs use the identified-object temporary for the SET (A2), the exact wide product (G, pb91, pb1143), the CALL/INVOKE
argument at the formal's scale (G PB289: pb1114 ×2, pb1422, pb923's MAX now stores at the formal's scale) and the
floating-point edited quotient at the guard scale (G review: pb66, output unchanged). Re-recorded as
`arch-oracle/0342e990a973`. The Linux gate ran GREEN on the gated head. push-main's landing check then STOPPED the
train three times falsely (PB2609): it bound `w1040w-gn1-close`, `w1040u-recursion-depth` and `w1040s-a1-116-118`
to this train's OWN lander dispatch-ledger entries (the dispatch guard records a lander with every id its brief's
files mention; PB2614's lander arm) and dated them by the earliest. The orchestrator released those three entries
with `plan_wave.py --release PB2609` after a simulated check without them passed with 0 stops, and re-ran push-main.

