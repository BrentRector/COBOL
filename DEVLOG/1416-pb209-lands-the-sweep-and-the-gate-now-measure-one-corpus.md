## Entry 1416 — 2026-08-31 08:54 PDT — PB209 lands: the sweep and the gate now measure ONE corpus, the drift test was made to print `external: 2` before it was trusted, and battery #39 is recorded green

Battery #39 stopped one row short of green (DEVLOG 1415) because its differential found two shapes that the
landing waves' reachability evidence had declared absent. This entry closes that: the instrument is repaired
structurally, every one of the six new diagnostics is re-measured against the population that actually exists,
the differential is re-run, the baseline is regenerated with both rows attributed, and #39 becomes the current
battery reference.

### The defect was not the parser — it was the POPULATION PREDICATE, and that is worse

[[PB209]]'s fix shape said "lift the `AT_DATA` extraction out of `gnucobol_differential.py` into a shared
module". Measured before implementing, it was already out: `scripts/gnucobol_extract.py` has parsed the `.at`
wrappers since the corpus landed, and the differential has always imported `parse_file` from it. There was no
second parser to delete.

What was duplicated is sharper and more embarrassing. The differential's worker re-derived *"a case is a group
with a COBOL member AND a compile check"* **inline in `run_case`** — so the only executable definition of *the
corpus* lived inside a function whose whole purpose is to be pickled into a `ProcessPoolExecutor`. Nothing else
could call it. Every reachability sweep therefore had to invent its own definition of the same noun, and the
one it invented was `find … -name '*.cob'`, which returns **two files** over a tree holding 1,323 programs.

That is [[feedback_one_rule_one_place]] in its most expensive form: one rule, written down twice, in units
three orders of magnitude apart, with only one of the two spellings ever exercised by a gate.

### One definition, two readers

`gnucobol_extract.py` now owns the population as an API rather than as a private inline test:

- `primary_source(g)` — THE predicate, returning the `(member, compile-check)` pair rather than a bool, because
  that pair is exactly what `run_case` needs; returning a bool would have left the caller re-deriving the half
  it uses, which is how the second spelling appeared in the first place.
- `differential_cases(src)` — the population. `len()` of it IS the differential's `cases run`.
- `iter_programs(src)` → `CorpusProgram(case_id, at_file, filename, title, text)` — every COBOL member of every
  case (**1,611 programs across 1,323 cases**; a case can carry a CALLed subprogram), which is what a sweep
  must read and what a per-case list would under-count.

`gnucobol_differential.py` filters with `differential_cases()` **before** dispatch, so `len(payload)` is the
case count and the progress counter stops counting groups it will silently drop. One non-obvious constraint is
now written down in the code: the `import gnucobol_extract` is module-level, because `run_case` is shipped to a
`ProcessPoolExecutor` and a Windows spawn re-imports the module rather than inheriting `main()`'s locals — an
import inside `main()` leaves every worker with no `gx` and manufactures 1,323 identical `RUNNER_ERROR`s.

### `scripts/corpus_sweep.py` — the reachability question, promoted from shell one-liner to instrument

There was no sweep script to fix. "A corpus-wide mechanical scan" was a command typed per wave and never kept,
which is precisely why its population could be wrong twice with nobody able to look between. It is now
committed, with three properties the defect demands:

1. **The external half is EXTRACTED, never globbed** — `gx.iter_programs`, the same call the gate makes.
2. **Every population prints its program count, always.** Measured: **conformance 966 · nist-programs 459 ·
   nist-copylib 53 · characterization 17 · version-matrix 0 · differential 0 · gnucobol-external 1,611 =
   3,106 programs.** Compare the withdrawn claim: *1,448 files*. A population contributing two files can no
   longer be invisible, because the line is printed whether anyone asked for it or not.
3. **It refuses rather than reporting a zero.** When the population check fails, a `--pattern` sweep prints its
   census and then **declines to report hit counts at all**. The clean zero from a broken reader is the whole
   defect; an instrument that can produce one is not fixed.

It also carries `--codes COBOLNETnnnn`, which answers the other half of the same question — *does this new
screen FIRE on the corpus* is a compile question no grep can answer — from the differential's own per-case
report. That is the rule-5 shape: the next new diagnostic's reachability is one command, not a new one-liner.

### The drift test, and the red it was made to show first

`ExternalCorpusPopulationDriftTests` asserts that the sweep's external population, recomputed live from the
`.at` wrappers, equals the differential's **committed** per-case baseline — `1323 == 1323`. The two sides are
produced independently (one read live from the corpus, one written by the gate and committed), so their
agreement is evidence rather than a tautology. It drives the sweep through `ProcessObserver.ObserveOrThrow`
rather than growing a seventh private process launcher, and it treats a missing interpreter or an absent corpus
as a LOUD failure and never a skip: a population that cannot be measured is not a population of zero.

⛔ **Proved failing before it was trusted** ([[feedback_green_gates_arent_evidence]]). `verify_population` was
temporarily repointed at the OLD reader — a `*.cob` glob over `tests/external/gnucobol` — and the instrument
reported `{"baseline": 1323, "external": 2, "state": "drift"}`, exit 1, with
`SweepExternalPopulation_EqualsTheDifferentialsCommittedCaseCount` **FAILED**. That `external: 2` is the literal
PB209 number, produced by the literal PB209 reader, printed by the new instrument. Restored; 3/3 green.
`TheDriftCheck_ActuallyFails_WhenTheExtractionIsEmptied` keeps that red permanently reachable and additionally
asserts the pattern sweep refuses instead of answering zero.

### The re-measurement: four claims survive, two were false

`corpus_sweep.py --codes COBOLNET1698,…,COBOLNET1703`, over the 1,323 cases the differential compiles:

| diagnostic | fires on | the wave's claim |
|---|---|---|
| COBOLNET1698 redefines-variable-length | 0 cases | ✅ stands — now on a measurement |
| COBOLNET1699 control-operand-shape | 0 cases | ✅ stands |
| COBOLNET1700 report-control-operand-index | 0 cases | ✅ stands |
| **COBOLNET1701** redefines-target-occurs | **1 — `syn_redefines:172`** | ⛔ FALSE, withdrawn |
| **COBOLNET1702** group-value-subordinate | **1 — `run_subscripts:351`** | ⛔ FALSE, withdrawn |
| COBOLNET1703 group-value-subject-shape | 0 cases | ✅ stands |

The sweep-side confirmation comes from the other direction: the SR5 sentence-1 pattern (an `OCCURS` entry
immediately REDEFINEd) now returns **5 hits over 3,106 programs** — three conformance fixtures plus
`syn_redefines:172` and `syn_redefines:219` in the external corpus, where the old instrument returned zero for
that half. `syn_redefines:219` ("REDEFINES: with variable occurrence") was already `AGREE_REJECT` on
COBOLNET0855 and never flipped: a second live external candidate for the same family, caught by a different
screen, sitting in a corpus the evidence claimed to have read. The corrections are written into [[PB177]] (its
four codes, one refuted) and [[PB184]] (whose scan was scoped correctly and simply never listed the external
corpus at all), and the `no .cob under tests/` phrasing in `DESIGN-data-model.md` §7 is replaced by the
instrument that can carry the claim.

Nothing NEW was found by any of this, and that is the point. **The compiler's behaviour was never in question —
the evidence was**, and it is the evidence that was repaired.

### Battery #39 recorded — and the two trees are named honestly

The four greenfield/NIST legs are NOT re-run: they were earned on tree `3bc4e4db` (DEVLOG 1415) and this
landing touches only `scripts/`, `tests/Cobol.Net.Tests.Unit/`, `kb/` and `docs/` — **no `src/` file**, so no
compiler behaviour they measured can have moved. The one leg whose INPUTS did change is the differential, whose
population is now supplied by the shared extractor, and that leg was re-run whole:

```
evidence control: accept and reject are distinguishable on this build.
external corpus population: 1323 case(s) from 36 .at wrapper(s) - gnucobol_extract.differential_cases
cases run: 1323
  WE_REJECT_THEY_ACCEPT    573
  AGREE_ACCEPT             473
  AGREE_REJECT             206
  WE_ACCEPT_THEY_REJECT    71
=== PER-CASE DIFF vs tests/external/gnucobol-verdict-baseline.tsv ===
    FLIP     run_subscripts:351           AGREE_ACCEPT -> WE_REJECT_THEY_ACCEPT   … error COBOLNET1702 …
    FLIP     syn_redefines:172            AGREE_ACCEPT -> WE_REJECT_THEY_ACCEPT   … error COBOLNET1701 …
=== DIFFERENTIAL: 2 PER-CASE FLIP(S) ===
```

No NEW or REMOVED case, and no case without a compiler verdict. Both rejections are SPEC-CORRECT, on clause
text re-derived and `--check`ed at landing rather than inherited from the diagnostic:

- **`syn_redefines:172`**, whose `AT_SETUP` title is literally *"REDEFINES: with OCCURS"* → COBOLNET1701.
  §13.18.44.3 **SR5 sentence 1**: *"The data description entry for data-name-2 shall not contain an OCCURS
  clause."* **GnuCOBOL only WARNS here, and its own testsuite carries the comment that this "should be a
  dialect option, currently it is _always_ a warning"** — so `WE_REJECT_THEY_ACCEPT` is the CONFORMING verdict
  and their acceptance is documented latitude on their side, not a defect on ours.
- **`run_subscripts:351`** → COBOLNET1702. §13.18.63.3 **SR13 sentence 2**: *"The VALUE clause shall not be
  specified at subordinate levels within this group."*

`tests/external/gnucobol-verdict-baseline.tsv` is regenerated in this commit and the delta is **exactly those
two rows** — `2 insertions / 2 deletions`, 1,323 rows before and after, no other row moved. A confirming re-run
against the regenerated baseline printed `=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`. Plan §0 records #39 as the
current battery reference with both trees named, and demotes #38.

### What this cost, honestly

The differential was run three times (measure, regenerate, confirm) at roughly eight minutes each, to avoid the
one shortcut available: writing the baseline from the same run that discovered the flips and calling the result
verified. #38's block already records that regenerating from the run's own report is correct *when the gate
read those verdicts*; here the population definition itself had changed underneath, so a fresh confirming run
was the only thing that could distinguish "the baseline encodes what we measured" from "the baseline encodes
what we wrote". The extra sixteen minutes bought the difference between a recorded verdict and a circular one.

