## Entry 1834 — 2026-10-01 14:13 PDT — Golden lane gl4, the witness-reuse batch: 9 rows closed by goldens and pins (GAP 691 → 682), 12 not closable, 4 draft errors fixed, leads PB1905-PB1906

The witness-reuse batch (golden lane gl4, the owner's 2026-10-01 instruction to dispatch it after wave 1001) landed
as two clusters, `gl4-misc-p2` and `gl4-misc-p5`, plus a one-line tooling fix. **22 inventory rows → 9 closed, 12
not-closable, 1 still owned by an open defect; GAP 691 → 682; CONFORMS test-needed 58 → 49.** Every draft that
reached the tree passed: 10 corpus programs (7 positive, 3 negative), 6 Conformance pin facts and 8 Unit theory
cases, all named in the lander gate's trx.

**Pipeline.** Writers drafted, a refuter attacked each candidate close, a fixer applied the refuter's corrections and
a validator compiled and ran every draft on a compiler pinned at `b8e076a24`. The refuter overturned **8 of the 9
candidate closes**: seven `does-not-exercise-rule` and one `expected-value`. Examples: the three live-clock
LEAP-SECOND goldens cannot fail on a seconds value of 60, so they were taken out of every `test-ref` and kept as layout
witnesses only. The writer's "WHEN-COMPILED cannot be pinned" was false, because `IntrinsicBinder.CompileClock` is
reachable from the Unit assembly. An INITIAL program's EXIT PROGRAM arm was unexercised. GR-8.5.1.11.3-5's
"not-closable, a fatal exception ends the run unit" was false, because a declarative that RESUMEs keeps the run unit
going (§14.6.13.1.3 5) with NOTE 2).

**gl4-misc-p2 (10 rows; 3 closed).** GR-7.3.17.4-3, GR-7.3.17.4-2 and DOC-A.1-111 are pinned at
2016-12-31T23:59:59.9999999+00:00, the one instant where a 60 could arise, on every arm (ON specified, ON implied by a
bare `>>LEAP-SECOND`, OFF specified, OFF implied):
- run time, through `COBOLNET_CLOCK`, in the new `LeapSecondReportedSecondsPinTests` at 2002 and 2023;
- the WHEN-COMPILED bake, through `CompileClock`, in four new `WhenCompiledStampTests` theories.

The expected values are derived from the layout rules: `23595999`, `2016123123595999+0000`, `20161231T235959`. The
DOC-A.1-111 row of `docs/CONFORMANCE.md` now names the pins in its Test cell, which had been a dash. GR-13.18.27.4-3's
group-subject positive and its negative landed as witnesses only, recording no verdict. The row stays with the open
kb/Work/PB1523, whose elementary-subject form still crashes the backend; PB1523 gains a paragraph naming the witnesses.

**gl4-misc-p5 (12 rows; 6 closed).** These goldens close GR-8.5.1.11.3-2, -5, -6 and -7 (a variable-length item's
lifetime, through `2014/l1_vldi_initial_program_reverts`, `_fatal_exception_resumed` and `_reduce_regrow`) and the
TYPEDEF negatives SR-13.18.58.3-3 (COBOLNET1529) and GR-13.18.58.4-2 (COBOLNET1639).

**Draft errors fixed here, each re-derived with cite.py.**
- `l1g4_leap_second_off_explicit_2002` declared `01 CD`, which COBOL-2002 reserves (§8.3.2.1 1), "Reserved words
  shall not be used as user-defined words or system-names"). It is renamed CDT and its `.out` is unchanged. The
  validator did not run `LeapSecondReportedSecondsPinTests`, which had the same `01 CD` in its two 2002 arms; the
  lander fixed it before the first run.
- The two TYPEDEF negatives started their `*> reject-at:` header in column 1, but every other line is fixed form.
  Auto-detection read the file as fixed, so the header itself drew COBOLNET2616 + COBOL0001 (§6.3.1 sequence area,
  §6.3.3 indicator area). Line 1 now starts in column 7.

**Not closable (12; they stay open).**
- Six are undefined-result rules with no required value to pin: GR-14.6.10-1, GR-14.9.22.4-13, GR-14.9.22.4-18,
  GR-14.9.22.4-21, GR-14.9.48.4-18 and GR-14.9.51.4-13.
- GR-11.9.7.4-4: only the COBOL entry convention exists, so an inherited convention cannot be told from the default.
- GR-9.1.13.3-1: no implementor-defined letter I-O status is provided, so nothing can be observed.
- GR-8.5.1.11.3-1 and -3: freeing at finalization, or at the end of the run unit, has no statement left to observe it.
- GR-14.9.51.4-16: WRITE RETRY's conflict branch needs a second run unit, which the corpus runner cannot provide.
- DOC-A.1-220: the "when are the resources freed" determination is unobservable by design. It is routed to a
  `docs/CONFORMANCE.md` §8 derivation on the DOC-A.1-19 precedent, which this landing does not do.

**Held back:** nothing. Every validated draft landed, after the four draft-error fixes above (two `01 CD` renames, two header indents).

**New leads.** PB1905: the reserved-word generator's provenance says "removed post-85" for words reserved through
2002 or 2014; the flags are right and only the text is wrong. PB1906: COBOLNET1639 tells the user a TYPEDEF
type-name has "no declaration". The code is right and the wording is not, and two sites word that verdict
separately. **Tooling:** `drift_rules.py` crashed on a Windows console printing its first rule containing ⛔; its
stdout is now UTF-8.

**Gates.** Windows lander gate GREEN (Conformance 9,727/9,727 · Unit 30,192/30,192 · Characterization 35/35, run
`20261001T210426Z-fd43b1`); Linux gate GREEN (`=== LINUX GATE: GREEN (legs unit characterization conformance; HEAD 720b6cd69) ===`, the same tree as the landed head).

