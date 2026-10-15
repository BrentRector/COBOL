## PB55: the rule is the unit — 51 flattened nested lists measured and indented, 152 fragment rows folded into their rules

**What was wrong.** Whether a numbered item inside a rule became a row of the traceability inventory depended on how
the transcription happened to indent it. Indented, `extract_rule_catalog.py` folded it into its parent rule;
flattened onto column 0, it became an `L<sublist>.<n>` row of its own. The owner decided on 2026-08-08 that the rule
is the unit (nested items are the parent's body) and scheduled the absorption after the defect burn-down; wave 1084
ran it.

**The re-probe changed the estimate.** The note said "absorb the 234 L-rows". On the base tree there were 215 in 68
sub-lists, and measuring every one on the PDF (the opening item's ordinal x0 against its page's margin: 63.49 pt
even, 80.04 odd, 17.5 pt a level) split them cleanly. 51 sub-lists (152 rows) are printed inside a rule, nearly all
at level 2 under a lettered item: §7.2.3.4 `9) c) 1.` to `6.` sits at x0 98.5 on PDF page 78. The other 17 (63 rows)
are printed at the block's own margin, each under its own prose lead-in ("The size error condition exists in the
following cases:", §14.7.5). Those are rules in their own right, and the standard cites them that way (§9.3.6 rule
5: "ignoring the requirements specified in 4c, 4d, 5c and 5d below"). So 152 rows fold and 63 stay. The note's
step 0 (32 "orphan" rows needing a base row harvested first) does not hold up: measured, every one is either a peer
list or nested under a base that exists.

**The fix.**
- The transcription: the 152 nested items were re-indented as `   N\. `, along with the 163 flattened lettered
  sub-items of the same parent rules. Each line was located on the printed page and measured at level 1 or deeper
  before it was touched. `cite.py` now reports printed paths it used to get wrong (`--check 7.2.3.4 "Each operand
  and operator of a concatenation expression"` gives `9) c) 2.`).
- The guard. The markdown cannot tell a flattened nested list from a peer list, so the difference is now measured
  data. The new `scripts/spec/measure_sublist_levels.py` records the 17 peer lists in
  `scripts/spec/data/sublist-levels.json`, keyed by text, and `extract_rule_catalog.py --check` (run by CI's guard
  job) fails on any sub-list that is not recorded at level 0. Run against the base transcription, it named exactly
  the 51 nested lists.
- The catalog went from 4347 to 4195 rules (exactly 152 fewer). The unharvested-block manifest was re-measured for
  §9.3.6 (21 to 18 ordinals) and §9.3.8.2.3 (8 to 9; it is now one list).
- The inventory changed only through batches. `pb55-fold`: the 36 parent rules took their sub-items' witnesses, and
  each sub-item's record was carried into the parent's notes under its printed path. Three parents went from
  CONFORMS to PARTIAL because a folded sub-item is PARTIAL (`GR-12.3.7.4-7`, `GR-13.18.34.4-7`, `GR-14.9.41.4-17`,
  owned by open PB1092, PB578 and PB370). `pb55-rekey`: §9.3.6's exact-match list moved from L5 to L4 along with its
  text. `pb55-drv-retire`: the folded `GR-14.9.6.4-L2.1` had closed on a §8 derivation, but its parent closes on its
  own test, and a derivation replaces a whole row's test, never sits beside one. So the §8 row was retired and the
  owner-signed determination moved verbatim into the parent's notes. GAP stayed at 101. This is bookkeeping, not
  conformance progress.
- The register. 54 notes had their `inventory_rows` and `closes_rows` re-keyed, each with a banner. Four landed
  notes had closed a parent that is now re-opened, and each drops it from `closes_rows` with the reason stated. The
  drift tests that named moved ids (`DerivedVerdictDriftTests`, and the derivation population in
  `SpecTraceabilityInventoryDriftTests`) and the comments in the code and docs that named them were updated.

**Found on the way, fixed here.** `extract_annex_a2.py` resolved "General rule N" to every row numbered N in the
block, so Annex A.2 items 3, 37 and 41 also named six nested items. Its docstring had explained the extra ids as
"once per edition variant", which the catalog never had. It now resolves only in the primary numbering, with a
self-test case. The committed catalog was also stale: three landings (PB1374, PB1221, PB351+PB1146+PB937) had
corrected the diagram text of FMT-7.3.15.2, FMT-13.18.35.2 and FMT-14.9.38.2 without regenerating it. `--check` now
fails on a catalog that differs from the extractor's own output.

**Gate:** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN ===` (run 20261015T210626Z-6e7548). Some
cases of the touched drift-test classes were in leg 2, so they were also run on the gate's binaries: 59/59 passed,
plus 138/138 in the other spec-reading drift classes.
