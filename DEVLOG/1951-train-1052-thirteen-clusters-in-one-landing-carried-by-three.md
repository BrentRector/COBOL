## Entry 1951 — 2026-10-10 16:45 PDT — Train 1052: thirteen clusters in one landing, carried by three lander phases and a phase-2 resumer

The loop's land unit carried every branch `prune_worktrees.py` listed WAITING TO LAND into one train (MANDATORY-PRACTICES
O11; owner 2026-10-08: never solo landings): thirteen clusters from waves 1050, 1051 and 1052, fifteen branches (three
were ancestors of others and land through them). That is twice the 4-6 band, and one 220-turn lander on train 1045's
eight clusters had hit its turn cap after its second gate, so the unit cut the ONE landing at the brief's own
checkpoints into fresh lander phases that each resume from the last one's committed branch and STATUS.md (owner
2026-10-08: every cap hit is a design failure, kb/Work PB2852): phase 1a brought in clusters 1-7 (brief steps 1-2,
about 60 turns), phase 1b clusters 8-13 with one in-train fix and the lead notes (about 105 turns), and phase 2 took
the lease, ran two whole-population gate runs on Windows and Linux, attributed and fixed their reds and ran a
four-shard review with five fixes in the train, then SPLIT at about 195 turns before push-main. A phase-2 resumer
recorded the one withdrawn review fix, ran the third gate, the oracle, this entry and the push. PB2852 now records the
measured turns per phase: phase 2 carried both the gate-and-fix loop and the review-and-fix loop, and a 13-cluster
train needs those as separate phases.

**51V — PB2914 (PB2897 retired into it), PB2515, PB2514: self-test reuse, one budget, HANG.** Under a fleet every
implementer gate went RED before a test leg ran, because `scripts/self_tests.py` gave each script self-test a fixed
900-s limit while N gates ran their self-test phases at once (467 s beside four gates, 71-76 s quiet). The runner now
traces what each self-test reads (`scripts/self_test_inputs.py`) and records every PASS with that content; only the
implementer gate reuses an unchanged PASS, while the lander, CI and the Linux gate run every one. Self-tests share one
machine-wide budget of one OS lock per core; the fixed limit is deleted, and a run still going an hour after it takes
its place is HANG, never RED. PB2515 and PB2514: the owner made batched gating and the gate cap of 3 permanent on
2026-10-10, so they are `gate_slot.py`'s coded defaults (`DEFAULT_SCOPE`, `DEFAULT_SLOTS`), with `gate_slot.py clear`
for the bridging settings. Gate run 1 found `SelfTestDiscoveryDriftTests.TheRunner_ProvesEveryArm` red on both hosts
(two expected arm texts were the pre-review wording), fixed in the train; the review (tooling C1) found that an
UNTRACKED file a self-test read was keyed as `-` whatever it held, so editing a not-yet-added helper reused the old
PASS (a false LEG 1 GREEN). Fixed in the train: every untracked, not-ignored input is keyed by its hash-object id,
with a new arm. PB2915 filed (a script's `--help` crashes on a cp1252 pipe).

**51W — PB2912, PB2911: the ledger measures what remains to v1.0; the GAP campaign.** The owner's Completion Ledger
headlined the held re-architecture program, showed none of the owner's five buckets and tracked GAP only.
`ledger_plan.py` now measures what remains per bucket (counts, harm, ready/waiting/blocked, the lane that plans each
note, loud flags, an estimate from named inputs), and every trend point carries its own sha's counts (173 points
back-filled, no recorded value changed). The hand-written in-flight section is deleted. PB2911, the GAP campaign lead
(owner 2026-10-10, "compiler and GAP first"): the 51 open notes holding GAP rows join its cluster; plan section 0
carries the PRIORITY and permanent GATE POLICY bullets. Phase 1a fixed a stale `.gitignore` comment; the review
(tooling C2) found that `gen_ledger.py`'s `missed_points` back-filled only inventory-touching landings, so a
register-only landing lost its trend point. Fixed in the train.

**51Y — PB2707: the R2 relaunch is built from disk, and a batch is sized to the quota.** `r2_collect.py --launch`
builds the plan from disk and writes the batch's `launch.js` (the 95-KB transcribed `launch-args.json` is gone);
`r2_cost.py` measures the per-line and per-verdict rates from transcripts, and `r2_inputs.py --batch` and `--launch`
refuse an over-budget plan.

**50B — PB2886: the owner's Ollama one-shot experiment, recorded.** One easy item (PB2695) given to a local model and
the answer evaluated; the evaluation and the raw prompt and response are under
`docs/rearchitecture/evidence/fleet-optimization/`. Notes and evidence only.

**52B — PB2484: the run-unit file lock binds on every non-Linux Unix.** On macOS `OfdRegionLocks.AbiMatches` answered
false and §9.1.15's file lock did not bind other run units (§9.1.15 1)-3), §14.9.27.4 25)). `RunUnitFileLock` is now
only the policy over a host seam, `IColumnLockHost`: the Linux open-file-description host (moved unchanged), none on
Windows, and the new `SidecarColumnLockHost` elsewhere, a lock file per holder beside the physical file, published by
rename, swept when its holder died. Witness `SidecarColumnLockHostDriftTests` (144 pairs against Table 19, a race, the
dead-holder sweep), run on every host. The review found two cross-user and race arms: C1 (the lock directory takes the
first holder's umask, so a second OS user cannot publish and is granted) extends PB2917; C2 (a dead holder's file
under another tester's exclusive probe reads as a live holder, a spurious '61') was fixed in the train with a shared
re-probe, which turned `TwoSimultaneousExclusiveTakes_NeverBothHold` red on Linux 10 of 10, so the code was withdrawn
and C2 is PB2917's third arm with the withdrawn shape and three leads. PB2936 (the macOS record-lock and store-mutex
residue PB2660 had carried while landed) is its own open note.

**52C — PB2618: a part-store into an object property keeps the rest.** `STRING ... INTO` a property, and any store
through a reference modifier, replaced the whole property with the receiving temporary's spaces (§14.9.43.4 7),
§8.4.3.3.4 5)). A new store kind, WriteInPart, makes the binder GET before and SET after; CONFORMANCE.md D-PROP1
records that a WITH NO GET class stays accepted with spaces in the unreached positions. The review (numeric 52C-1)
reproduced that the temporary was never reset, so a second execution kept the first one's bytes: fixed in the train
(the temporary is reset before the statement), golden `2002/pb2618_no_get_part_store_every_execution`.

**52D — PB2694: COMPUTE renders a root quotient per receiver.** `COMPUTE FE ROUNDED, X = 1 / HUGE` stored +0.00E+00
in the floating-point edited FE, and the re-probe found fixed receivers wrong too (a NEAREST-EVEN tie read from a cut
quotient). `EmitCompute` rendered a root quotient once for the receiver set; it now renders a root `*` or `/` per
receiver, `ReceiverContext.OfSet` is the one receiver-set rule (five hand-written copies replaced), and a nested
quotient is formed for a FloatEdited context. Golden `2023/pb2694_multi_receiver_root_quotient` (seven cases).
PB2923 filed (the DIVIDE REMAINDER subsidiary quotient for a float-edited receiver, an adjudication).

**52E — PB2698: ABS(-2^127) is a size error, and the unsigned-wide unary arms.** `AbsScaled` negated
`Int128.MinValue` in place and took NOT ON SIZE ERROR; it now raises the size condition (§15.7.4 1) b), §14.7.5).
Sibling arm: an unsigned-wide operand reached ABS/SIGN/INTEGER/INTEGER-PART/FRACTION-PART as UInt128 for Int128
parameters (CS1503 on conforming source); additive runtime twins `SignOfU`, `IntegerPartU`, `FractionPartU`. Also
fixed: `PowNativeIntDec` on MinValue, and Floor/Truncate at a negative scale through `ExactMul`. PB2918 and PB2919
filed.

**52F — PB2764: LOCALE-DATE and LOCALE-TIME render through one LC_TIME snapshot.** ar-EG dates carried U+200F marks,
es-US times a no-break space, and fa-IR and th-TH formatted a Gregorian argument in their own calendars. The new
`TimeFacts` snapshot normalizes the locale strings once and is the one d_fmt/t_fmt renderer, with Gregorian year,
month and day (§15.52.4 2), §15.53.4 2), CONFORMANCE.md L12, L13). The Linux gate found its es-US lines pinned the
Windows ICU's designator bytes ('a. m.' against Linux's 'a.m.'): fixed in the train, the tests now check the rule,
not the host's data. PB2750 extended (LocaleFacts' public raw members, a runtime-major removal).

**52G — PB2771: SwitchStore caches both arms; Names is the one closed set.** An external switch absent at its first
interrogation was re-read later, so an ON set mid-run turned it on (DOC-A.1-191: read at the first interrogation);
absent now latches OFF like empty, and `SwitchStore.Names` single-sources the switch rows of `ImplementorNames`.

**52H — PB2841: CONTINUE AFTER a non-finite interval takes its kind's rule.** `CobolTiming.ContinueAfter`'s own
EC-DATA-NOT-FINITE screen ended the run unit for a FLOAT-LONG -infinity; §14.6.13.2 3) raises it only for a standard
floating-point sender, which the sending read already does. The screen is removed: -infinity is a non-positive
interval (§14.9.9.4 1)), +infinity the maximum, NaN zero (DOC-A.1-39). A unit test that could not fail was replaced.

**51B — PB2843, PB2839, PB2745, PB2741: run-unit paths, injective names, the statement context.** A contained
program of `PROGRAM-ID. WORKER AS "X"` registered under the declared name (§8.3.2.2 2)); every run-unit path is now
built by `ProgramTable.OutermostPath`/`ContainedPath`. The word-to-C# identifier mapping is injective (A-B and A_B
collided, §8.3.2.1). EXCEPTION-STATEMENT and EXCEPTION-LOCATION in a callee's declarative answered the caller's
(§15.32.3, §15.30.3): the statement context moved into `CheckingFlags`. PB2741 discharged. The review fixed one
comment in the train; PB2920 and PB2921 filed.

**51F — PB2846, PB2281, PB2690: the RunModule boundary, and fixed-to-variable-length groups via Reshape. Raises the
runtime major to 2.** A main whose recorded runtime named another major died with an unhandled .NET exception; its
registration now runs inside the RunMain boundary and ends abnormally with EC-PROGRAM-NOT-FOUND. A fixed group
opposite a variable-length one at CALL, INVOKE and RETURNING converts through Reshape/Overlay over the pair's atoms
(§14.8.2.2 2), §8.5.1.12.3, §14.6.9.2 1)); five Shipped members and their span helpers are deleted, so the runtime is
2.0.0. `GroupAtomTable` emits each atom array once as `__GroupAtoms.A<n>`. Phase 1b found `RuntimeAbiPinDriftTests`'
call-ABI pin red, 51F's alone (its leg 1 never ran it): the boundary half now hashes every `GroupAtom[]` field and is
re-pinned. The review fixed the external-repository design doc's version line in the train and added a performance
item to PB2690, which stays open. PB2922 filed.

**The train.** Gate run 1 (0d6b3dd55): Windows RED (51V's stale arm texts, plus two self-test drift tests that hit the
harness's 120-s limit while the Windows and Linux gates and four reviewers shared the host, 46 s and 40 s quiet: PB2882,
environmental, no cluster owned them) and Linux RED (51V, and 52F's ICU bytes). Gate run 2 (3db765779): Linux RED
only on 52B's race test, caused by the train's own C2 fix, withdrawn. Gate run 3 (ae2e2bc49), the Windows gate alone and
then the Linux gate: Windows `GREEN — Conformance 11,341/11,341 · Unit 33,267/33,267 · Characterization 36/36` (the two self-test drift tests green on the quiet host, so PB2882's attribution held); Linux `GREEN (legs selftests unit characterization conformance guard selftests-built)`. No red was an interaction, and no cluster was ejected or dropped.
Review: four shards, seven confirmed findings, five fixed in the train, one recorded in PB2917, one in PB2690.
Oracle (`compare_oracle.py`, baseline f3387ba8d9b5): DIFFERENT on 3,406 of 7,708 cases, every one attributed by class with a normalizing classifier over both captures (`E:\COBOL-coord\scratch\t1052\classify_oracle.py`): 13 ADDED (the train's ten goldens and three negatives); no diagnostic stream changed. Of the 3,393 changed emitted C# files, every module carries 51F's runtime version literal 2.0.0.0 (was 1.0.0.0), and every main program's `Main` calls `ProgramRegistry.RunModule(__CobolModule.EnsureRegistered, ...)` in place of the separate registration and `RunMain` (51F, PB2846); 2,248 also carry 51B's injective identifiers (a module namespace from a file name with `_` now spells it `_u005F_`, PB2839); 15 a METHOD's CALL or CANCEL with the empty caller path, and 18 a run-unit path rooted at the externalized `AS` name (51B, PB2843); 38 emit `__GroupAtoms.A<n>` where an inline atom array stood, 3 of them also converting a fixed group through `ToImage`/`FromImage` over atoms in place of the deleted span helpers (51F, PB2690/PB2281); 3 render a multi-receiver root quotient per receiver (52D, PB2694: ca5b_prohibited_multi_receiver_size_error, l1c04_arith_multiple_receivers, NIST NC252A); 2 GET a property before a part-store and reset a WITH NO GET temporary (52C, PB2618: pb2078_property_receivers_in_phrases, pb1275_property_set_formats); 1 scopes the statement context at a PERFORM baseline (51B, PB2745: exception_statement_ambient). Nothing is unexplained; the baseline is re-recorded as ae2e2bc49663. Semgrep: PASS, no count rose (BigInteger 44, decimal 2, raw diagnostic-code literals 268, bound-node rendered text 3). GAP 103 → 103 (51F's verdict batch re-sited five rows). No diagnostic codes claimed;
every cluster's allocation returned unused. New notes PB2917-PB2923 and PB2936; PB2660, PB2882, PB2426, PB2750, PB2590
and PB2690 extended.

