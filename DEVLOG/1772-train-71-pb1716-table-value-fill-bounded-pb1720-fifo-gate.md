## Entry 1772 — 2026-09-28 22:52 PDT — Train 71: PB1716 (table VALUE fill bounded), PB1720 (FIFO gate cap, half), PB1718 (one population check)

Train 71 carries three clusters of wave 71, the first implementation wave of the PB1708 "order, don't skip" pivot.
All three cut from `bf2e5b43f`, and main had not moved, so each patch applied with no conflict except the generated
`docs/DRIFT_RULES.md`, which was regenerated (229 drift tests), never hand-merged.

**B: PB1716 (pivot M7), a table VALUE fill is now bounded by the phrase.** `TableValueOdometer.Resolve` filled a
Format 2 table VALUE up to a 64,000,000-element defensive cap before the §13.18.63.3 SR23 screen rejected the entry.
The re-probe reproduced it: the 26-line negative `pb505-table-value-dynamic-span-levels` took 25.3 s through the CLI
(the note said 29.6 s). `TableValueOdometer.ElementCount` now counts the elements a phrase names before any fill (the
mixed-radix distance from FROM to TO in the GR12 odometer order, saturating at `long.MaxValue`), `Resolve` fills
exactly that many, and `MaxFillElements` is deleted. The sibling sweep found an under-reject behind the slowness: the
SR23 screen tested only the OUTERMOST OCCURS DYNAMIC clause with no TO phrase and then broke out, so two nested ones
with `FROM (1 1) TO (2 1)` compiled clean after a 23 s fill. SR23's shape now lives once, in `UnboundedBelow`, read by
both the COBOLNET1946 screen and `ElementCount`; `TableValueDim.Ceiling` replaces `DataBinder.SubscriptCeiling`. pb505
rejects in 0.49 s, and the suite's two long poles fall from 266 s and 116 s to 0.1–0.2 s. The goldens are
`2014/pb1716_table_value_element_count` (GR12, GR13 and GR15; each counted phrase overlays a whole-table phrase, so a
miscount shows) and the negative `pb1716-table-value-nested-dynamic-span` (COBOLNET1946 at all four editions), plus
`TableValueElementCountTests`, which checks the count against the odometer walk for every tuple pair of a 2x3x2 table.
Rows re-verdicted CONFORMS: SR-13.18.63.3-23 and GR-13.18.63.4-12; PB1716 is landed and closes both. The battery now
also lists the five slowest tests after PHASE 1, as a report that never touches its verdict. The lead the change
exposes, that a legal VALUE over a 10000 x 10000 table now materializes all 100,000,000 elements where it used to
stop silently (a wrong answer) at 64,000,000, is PB1722 and needs an owner decision on the ceiling.

**E: PB1720 (pivot M2), the cross-worktree FIFO gate cap, landed half.** `scripts/gate_slot.py` is the ticketed
FIFO semaphore of DESIGN-test-build-ci.md §3.14.6. It keeps N slots under `<git common dir>/cobol-gate-slots/`,
built only on OS file locks, so no pid is ever guessed alive. On Windows the holder joins a kill-on-close Job object;
on Linux the inherited slot descriptor keeps an orphaned tree in the cap until it exits. Its five-arm `--self-test`
is green on Windows and under WSL, each arm was seen red on a planted defect, and `GateSlotDriftTests` runs it in
every Unit run, so CI's Linux unit jobs prove the Linux arm. It is not yet wired into any gate; that is M13 (PB1721).
`DEFAULT_SLOTS = 2` is PROVISIONAL, because the 1.25x-of-quiet measurement needs a quiet host and wave 71 was
running; that measurement is PB1720's residual, which is why it is `half`. The design's M13 row now names
`record_impact_map.py` as a slot taker (recorded in PB1721). The three copies of the git-common-dir resolution that
disagree about `.resolve()` are PB1723.

**D: PB1718 (pivot M14), one population check for every whole-assembly run.** The re-probe found that the battery
accepted any `Passed!` line, and that CI summed shard `Total:` lines against a `grep -c` of the listing, which cannot
see a dropped case offset by one that ran twice. Planting `VSTestTestCaseFilter` in the environment ran 31 of 33
Characterization cases with exit 0, and it narrowed `--list-tests` the same way; `RunSettingsFilePath` did too.
`scripts/test_population.py` compares the multiset of trx test definitions with the scrubbed listing and names every
NEVER RAN, RAN TWICE and NOT IN THE POPULATION case (19-arm self-test). `is_scrubbed` is the one scrub rule
(`COBOLNET_GATE_*`, `VSTest*`, `RunSettingsFilePath`). The battery's PHASE 1 now runs scrubbed and prints a population
line per assembly. In CI each Conformance shard lists its own build's population and uploads it with its trx, the
population job runs `check` per platform without a .NET build, and the old count, its `Total:` capture and the
`shard-count-*` artifacts are deleted. Every other `dotnet test` caller under `scripts/` is scrubbed, and three
private listing parsers became one. `GateLegDriftTests` pins arms 3 and 6, and the design's §3.14.3, §3.14.4 and §1.4
were corrected. PB1718 is landed with no inventory row (process tooling). Its CI half is proven only by this train's
push-main run. Its leads are PB1724 (SM206A failed once under load and passed alone 4 of 4), PB1725 (the command guard
treats a read-only command as a verdict command when its arguments merely name one; this lander hit it three times)
and PB1726 (three scripts crash on a cp1252 console).

**The train.** The gate ran whole and unfiltered at Normal priority in the lander's worktree, on the merged tree:
Conformance 9,319/9,319, Unit 29,742/29,742 (the three clusters' new tests add up exactly: 5 + 1 + 2 over the base),
Characterization 33/33, `=== WAVE-LOCAL GATE: GREEN ===`, and the legacy Integration assembly 503 passed, 1 skipped
of 504. semgrep verify PASS (`cobolnet-raw-diagnostic-code-literal` 301 → 300, not locked in). The CI audits were
green locally (code and doc citations 0, evidence supersession 0 unmarked, witness loss 0, drift rules current,
`work.py check` 1,634 well-formed). The lander's review of the train diff found no correctness finding, so no cluster
was dropped. The GAP stays 866 → 866: PB1716's two rows were already CONFORMS. No diagnostic codes were used
(COBOLNET2617–2618 return unused). One friction: the command guard refused a status rewrite chained before
`git add`, the refused commit left cluster E staged, and cluster D was applied on top of it; the lander discarded the
mixed tree and re-applied E and D in order, one commit each.

