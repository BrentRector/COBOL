## Entry 1634 — 2026-09-22 06:58 PDT — Battery #83's record audited on re-entry: every leg re-verified from its own artifacts, the full CI matrix confirmed green on the verification branch — and the ONE claim it got wrong was the complement sweep, corrected here

**Battery #83 had already run and landed when this session re-entered it** (`80052c312` re-baseline +
`a2a15c30c` record, DEVLOG 1632, plan §0 rotated to CURRENT = #83). The predecessor agent was cut off before it
returned its result, and its scratchpad report described a landing that **did not happen** — three commits it
names (`9948bf049`, `4f040ef39`, `47e1de51f`) exist in no ref of this repository, and the two CI runs it says
it "stopped" both completed. So nothing in that prose was taken on trust. **Every number in the record was
re-derived from the run's own artifacts, and the one claim that did not survive is corrected below.**

**What re-verified, from `scratchpad/battery-83/` and from GitHub, not from prose.** `summary.txt`,
`conformance.log`, `unit.log`, `characterization.log`, `guard.log`, `citations.log` and `gnucobol.log` carry
exactly the figures §0 reports: Conformance **7776 / 7776**, Unit **28643 / 28643**, Characterization **33 / 33**,
build `0 Warning(s) 0 Error(s)`, `WITNESSES: ALL GREEN`, `guard-compiler --self-test: ALL GREEN`,
`NIST (cobol): 364 MATCH, 0 REGRESSION(S)`, `NIST AUDIT: CLEAN`, guard `=== ALL GREEN ===`, all three static
audits at 0, and the differential's `1323 cases` / `2 PER-CASE FLIP(S)` with the four totals
580 / 466 / 237 / 40. The compiler under all of it is unchanged: `git diff c982251df HEAD -- src/ tests/` is
**the baseline TSV and nothing else**, so the battery head and today's head compile identically.

⭐ **And the two things the record ASSERTED about its own determinism and its own gate are now measured rather
than quoted.** (1) The re-baseline run's report and the first run's report were diffed **per case, not by
totals**: `gnucobol-report.json` vs `gnucobol-rebaseline.json` is **0 verdict differences over all 1,323 cases**,
0 keys in one and not the other — identical totals are consistent with offsetting flips, which is the whole
reason a per-case diff exists. (2) The record claimed the landing "routes to the FULL CI matrix". The run on
`main` (**35734508503**) in fact ran `changes` + `ci-gate` and skipped everything else — but that is the
already-verified short-circuit doing its job, because `push-main.sh` had already put the same sha through the
**full matrix on `ci/a2a15c30cfb0`** (run **35732937405**, `success`): Windows build + tests, all five Windows
conformance shards, all five **Linux** greenfield conformance shards, Linux greenfield unit + characterization,
the Guard leg, INV-1-strong, the shard population guard, `ci-gate`. So the Linux legs and the Release build —
the ones a Windows Debug battery cannot reach — did see this batch. The conclusion stands; the stated mechanism
did not, and is corrected.

⛔ **THE ONE WRONG CLAIM, AND IT IS THE SHAPE `feedback_measure_the_selectors_complement` NAMES.** §0 and entry
1632 both state that **"exactly ONE of the 1,611 extracted external programs declares a SPECIAL-NAMES CLASS at
all"**, and conclude that SR4's whole reachable external population is the case that flipped. **It is false.**
The sweep behind it asked for `^\s*(\*>)?\s*CLASS\s+[A-Za-z0-9-]+\s+IS\s` — and **the CLASS clause's `IS` is
OPTIONAL**, so a selector built from the subject case (which spells `CLASS EVEN IS 49 51 …`) reported the
subject as the whole population, by construction. Re-run here without the `IS`
(`^\s*(\*>)?\s*CLASS\s+[A-Za-z0-9-]+\s`), the external population is **THREE**, not one:

- `run_fundamental:9066` — the case that flipped.
- ⭐ **`syn_misc:869` ("CLASS duplicate values") — a REAL second SPECIAL-NAMES CLASS declaration**, written
  `CLASS CHECK-VALID 'a' THRU 'z' 'A' THRU 'Z' 'cdef'` with no `IS`, and used as `IF X IS CHECK-VALID` over
  `01 X PIC X(8)`. Its identifier-1 is of category **alphanumeric**, which §8.8.4.4.3 SR4 does not reach, and it
  is **`AGREE_ACCEPT` in battery #82's baseline and in #83's** — it did not move.
- `syn_misc:7276` ("VALIDATE parsing") — a **false positive**: a VALIDATE *data-description* clause
  `CLASS IS ALPHABETIC-UPPER` inside an `05 IN-NAME PIC A(20)` entry, not a SPECIAL-NAMES clause.
  `WE_REJECT_THEY_ACCEPT` on the A.4.14 VALIDATE decline in both batteries, unchanged and unrelated.

**The corrected population is STRONGER evidence for the same conclusion, which is why it is worth correcting
rather than deleting.** The external corpus contains one class-name-1 class condition the standard PERMITS and
one it FORBIDS; train 44's new `ClassConditionModel` table moved the second and left the first alone. That is
the probe on the axis the flipped case holds fixed (`feedback_probe_the_shape_the_subject_hides`): it shows the
screen discriminating on the operand's **category**, exactly as SR4 is written, instead of refusing
class-name-1 conditions wholesale. A one-case population could not have shown that in either direction.

**What changed in this commit.** §0's CURRENT battery bullet now carries the corrected sweep, both extra cases
and their unchanged verdicts, and says plainly that the first selector was wrong. Entry 1632's complement
paragraph is **left standing** with a `⚠ CORRECTED BY ENTRY 1634` blockquote above it — the wrong sentence is
part of the record, and rewriting history to look right is the failure this log exists to prevent. Nothing else
in 1632 moves: both flips, both `--check`ed clause numbers (§8.8.4.4.3 rule **4**, §8.3.2.2 rule **2**,
§8.4.6.3 — all three re-run through `cite.py --check` here, all three OK), the two-row baseline diff and the
recorded MISS against #82's "0 flips" requirement are all confirmed exactly as written.

**The ledger refresh 1632 recorded as owed LANDED while this correction was in CI** — `0ae7522b0`, "Ledger v59
at battery #83", by another agent: the in-flight narrative for train 45 plus the batch's `ledger-trend.json`
point, with every number on the page computed by `gen_ledger.py`. This commit is rebased on top of it, so the
debt 1632 booked is discharged in the repository. **What is NOT verifiable from git, and is therefore recorded
as still owed rather than assumed:** the PUBLISH of that regenerated page to the existing Conformance Ledger
artifact URL, which leaves no trace in the tree.

**No `kb/Work/` note was opened.** No compiler defect is involved — the corrected claim is about a measurement
in a record, and this entry is the correction. `python scripts/spec/work.py check` → all 991 notes well-formed,
unchanged. GAP is **2276** before and after; this commit is docs-only.

