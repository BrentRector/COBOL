## Entry 1955 — 2026-10-11 00:53 PDT — Train 1056b: THROUGH-alias element moves, edition-spelled literal names and Annex C option words, the six-cluster train, off-loop checkpoints, the Linux gate's guard scratch; PB2917+PB2936 ejected

Train 1056b is the second night-stop train (owner, 2026-10-10 21:30 PDT: "stop new work. Get everything in progress
to main"; "do not try and do 15 branches at once"). It was dispatched with six clusters (A to F) and lands five of
them plus one lander fix. `train_apply.py` brought the six in; A, B and C each stopped on a conflict with train
1055's landings and were resolved by reading both sides (below). Every implementer gated leg 1 only (batched gating,
PB2515), so this train's whole-population gate was each cluster's first population check.

**B — PB1938: the element moves of a variable-length group MOVE reach a level-66 THROUGH alias's tables.** `MOVE
SALIAS TO DST`, where the alias spans a table `ST PIC 9(4) OCCURS 2` and the receiver holds a dynamic-capacity table
of `99V99`, aborted at run time with the not-implemented exception for §14.6.9.2's element moves. The root was in
`MoveBinder.TableCursor`, which walks the table's parent chain up to the group operand. A THROUGH alias owns no
subordinates (§13.18.45.4 GR2), so the walk found nothing. `VariableLengthCompatibility.CorrespondingTables` now
returns `TableSite(Table, Part)` pairs: the span part each alias atom was laid from, or null for an ordinary group.
`MoveBinder.ReachElement` is the one reach for both kinds of group, and reaches an alias table as the record's table
at the outer occurrences its part names, through `ReferenceResolver.ResolveItemAt`. `RenamesSpanPart.SubscriptsFor`
now takes a typed `Position` (both callers changed). Goldens: `2014/pb1938_through_alias_vlg_element_moves`
(sender alias, receiver alias, and a table leaf under an OCCURS group inside the range) and the negative
`pb1938-through-alias-element-move-illegal` (COBOLNET0819 from the element pair's Table 16 check). Merge: train
1055's PB2818 deleted `Laid.Unplaced`, and B added `Laid.Parts` beside it; the merge keeps PB2818's deletion and B's
field.

**C — PB2840 + PB2845: literal names are spelled in the edition's Annex C, and directive option words are folded by
Annex C rather than by the host.** Below 2023, `INVOKE O "X<final sigma>"` did not find `METHOD-ID X<sigma>` (Annex
E.2 item 14 deleted that pair's mapping only at 2023), and the sweep found the CALL, CANCEL, ADDRESS OF and AS-literal
arms broken in the same way. `ExternalizedNames.FormWritten` (Form, then `CobolNames.EditionSpelling`) is now the one
producer for CALL, CANCEL and ADDRESS OF targets and for AS literals, and `OoBinder.OoMethodNameOf` does the same
for INVOKE. `ExternalizedName.Peek` takes the edition instead of a hard-coded 2023. PB2845: the `>>FLAG-nn` option
words, `>>COBOL-WORDS` keywords, alphabet figuratives, class-condition keywords, CONVERT format words and
`FigurativeConstants.KindOf` fold with `CobolNames.UpperFold`. The run-time word tables (exception names, EC
profiles, the file, sort and report-detail registries, switches) key on `CobolNames`. `CobolNameFoldDriftTests` now
scans every `src/Cobol.Net.*` project. Goldens: `2002/pb2840_literal_names_edition_spelled`,
`2023/pb2840_literal_names_two_words_at_2023` and `2023/pb2845_flag_option_word_annex_c_fold`, plus two negatives.
Merge: train 1055's PB2588 changed `Peek` and `OoRepositoryScope.Build` to take the AS phrase and a constant
reading, and C added an edition parameter to both; the merged signatures carry both
(`Peek(phrase, constants, dialectLevel)`, `Build(site, variables, dialectLevel)`). DOC-A.1-68 takes C's row.

The train fixed two things in C's commit. The drift test's rename (`..._InTheCompilerOrTheFrontend` to
`..._InAnyProject`) left inventory row GR-8.1.3.2-3 naming a missing test, red on both gates
(`SpecTraceabilityInventoryDriftTests.EveryTestRef_ResolvesToARealTest`). A witness-only `record_verdicts` batch
retires the old name and adds the new one, so `audit_witness_loss` reads it as RETIRED, not LOST. Second, the
report's lead 3: the INVOKE SR3/SR4 messages (OoBinder, nine arms) paraphrased the rule as "literal-1 shall name a
method"; they now quote the standard's "shall be the name of a method" (`cite.py --check` OK for §14.9.23.3 3) and
4) d)), with the two `pb1136-*.err` fixtures updated to match.

**D — PB2981 + PB2980: a land unit's train holds at most six branches, and there is one reports directory.**
`prune_worktrees.py --train` names the next train: at most `TRAIN_MAX = 6` WAITING TO LAND rows, ordered by harm,
then compiler before process, then oldest first; the rest are held. `units/land.md` carries exactly that train.
`coord.reports_dir()` is now the one reports directory, and `check_practices.py` refuses a rendered spec whose report
path points anywhere else.

**E — PB2590: checkpoint writes are off the supervisor's event loop, and the orchestrate self-tests read a scratch
repository.** `Write-Checkpoint` ran `checkpoint.py`, which probes every worktree, synchronously inside a unit's
event loop. For that whole time the loop read no stdout, ticked no watchdog and checked no STOP. Writes now run in
the background, one at a time; only the final write waits for completion. The test harness gives every supervisor
a scratch `-RepoDir`. This is the root fix for the startup self-test's load sensitivity. Its new case 4k' was red 8
of 10 runs with the synchronous write restored and green 10 of 10 with the fix. All orchestrate self-tests were
green in all three of this train's Windows gates.

**F — PB2974 filed, plus train 1056's ledger trend point.** The point list grows from 191 to 200, with no duplicate
sha.

**The lander's fix — PB2989: the Linux gate's guard scratch now sits beside the clone instead of inside it.** The
first Linux gate printed `leg selftests-built: RED … SELF-TESTS: NOT RUN … git hash-object --stdin-paths returned
non-zero exit status 128`. The guard leg wrote its TMPDIR inside the clone, while `selftests-built` was keying its
inputs by every untracked file in that same clone. A temp file deleted between `ls-files` and `hash-object` failed
the leg. The guard scratch is now `$snap.guard-tmp`. The red was main's own and is timing-dependent (the same
commands re-run in the clone returned rc 0); CI does not have it, because CI runs the guard as a separate job.

**A — PB2917 + PB2936 (the sidecar lock-file host as one primitive under two policies) is EJECTED.** Gate 2 failed
A's new `SidecarColumnLockHostDriftTests.ManyTestersSweepingOneDeadFileNeverRefuseEachOther` on Windows ("Round
11: a dead holder's file was read as a live one — outcomes Refused, Refused, Held, Refused"). The same test was green
in gate 1 and in eight parallel runs alone, so the failure depends on timing. `HostFile.ProbeLockFile` retries a
Windows "access denied" only `DeletePendingProbes = 5` times with `Sleep(1)`, then calls the holder unobservable,
which means refused. A stress harness (4 to 8 threads probing and sweeping one dead file, 3,000 rounds) saw about
400 access-denied opens in 72,000 probes on this ReFS volume, some with the name still listed. Telling a
delete-pending file from a genuinely denied one therefore needs more than a bound or a directory listing. The
review also found a correctness defect: a lock directory a user can enter but not list (a 0711 parent) throws from
`TestOthers`'s enumeration (SidecarColumnLockHost.cs:76) and from `SidecarRegionLocks.Look`, and both throws map to
Unavailable. That silently grants an OPEN or READ WITH LOCK that §9.1.15 2) / §9.1.13.8 1) refuse. Two mechanisms
are past the train's fix bar, so A goes back to a finisher from branch `worktree-wf_8f6c2cfa-5a7-1`, with that test
as its first leg-1 red. PB2917 and PB2936 stay open on main.

**The train.** Gate 3 (d8c663148): `=== BUILD-LOCAL GATE: GREEN — Conformance 11,460/11,460 · Unit 33,360/33,360 ·
Characterization 36/36` and `=== LINUX GATE: GREEN` on the same head. There were three whole-population runs. Run 1
had the C test-ref red plus PB2989. Run 2 had A's timing red and the witness loss from the hand-edited test-ref.
Semgrep was unchanged (44 / 2 / 268). GAP 103 → 103: no row was re-verdicted, and the one witness retirement keeps
GR-8.1.3.2-3 CONFORMS. The arch oracle differs from baseline c95e453f2fe8 in 11 of 7,761 cases, all from C or B. The
seven ADDED cases are B's two goldens and C's five. The four DIAGNOSTICS cases are C's SR3/SR4 message text:
`pb1136-invoke-active-class-method`, `pb1136-invoke-self-method`, `l1-invoke-dotnet-member-typed` (SR4 b)) and
`pb1548-new-without-base` (SR3). No emitted C# changed for an existing program. The baseline was re-recorded. Notes
filed from C's report leads: PB2990 (the PARAMETER phrase's environment-variable key uses the host's upper-case
mapping and needs a DOC-A.1-49/55 determination) and PB2991 (CALL/INVOKE by identifier below 2023 still map by the
2023 table and need a run-unit edition fact).

