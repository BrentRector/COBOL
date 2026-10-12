## Entry 1176 — 2026-08-04 20:24 PDT — "0 per-case flips" was an inference from four totals; it is now a measurement against a committed 1323-row baseline

**Owner decision (2026-08-04): "names+verdicts is our generated results, okay to commit."** That closes the
question entry 1175 left open and turns the GnuCOBOL differential from a leg whose headline claim was *inferred*
into one that is *measured*.

## ⛔ WHAT WAS ACTUALLY WRONG WITH THE OLD CLAIM

§0 has recorded "**0 per-case flips**" after differential runs for weeks. The evidence behind it was that the four
summary totals — 559 / 487 / 176 / 101 over 1323 cases — matched the previous run. **Identical totals are
consistent with offsetting flips**: one case going AGREE_ACCEPT → AGREE_REJECT while another goes the other way
leaves every total untouched. So the claim was a plausible inference presented as a measurement.

It was worse than that, and the worse part is the part worth remembering: **no per-case report from any prior run
had been kept**, so even a careful reviewer had nothing to diff against. `feedback_comprehensive_gate_mechanics`
says "diff the differential PER-CASE" — the instruction existed and the artifact it needed did not.

## ✅ THE FIX IS THE NIST SHAPE, APPLIED TO THE OTHER EXTERNAL NET

`tests/external/gnucobol-verdict-baseline.tsv` — 1323 rows of `id⇥tier⇥verdict`, sorted, committed. It is
deliberately the same shape as `tests/nist/corpus.tsv`: **a flip is a one-line git diff a reviewer can see**,
rather than a number a human compares against memory. That is the same failure `guard-nist-audit.sh` was built to
end when `guard-fast` printed ALL GREEN at 352 MATCH because a program had vanished.

`gnucobol_differential.py` gained `--baseline` (diff) and `--write-baseline` (regenerate). It prints
`=== DIFFERENTIAL: n PER-CASE FLIP(S) ===`, names each flip with the case ID and our first diagnostic, and
`battery.sh` now gates on that verdict LINE — never the exit code (§0's standing rule, earned when `guard-fast`
reported exit 1 on a green run because a chain ended in a `grep -c` that found nothing).

Three design points that are not incidental:

- **An ABSENT baseline is a RED, not a pass.** Returning 0 when there is nothing to compare against is exactly
  how the old claim survived. Silence is not success.
- **NEW / REMOVED cases are reported but are NOT flips.** The fetched corpus can legitimately change, and the
  compiler did not cause that. Conflating the two would either drown a real regression in 1300 false ones after a
  refresh, or let a refresh mask one.
- **Regenerating is a deliberate act with an attribution duty.** `--write-baseline` says so in its own help text
  and in the file's header: rewriting a baseline to clear a red destroys the only record that behaviour moved.
  Titles and keywords are omitted from the file — the diff does not need them, so the committed artifact carries
  nothing but our own generated results.

## 🔬 PROVEN IN THE FAILING DIRECTION BEFORE BEING TRUSTED

`feedback_green_gates_arent_evidence` — a detector nobody has seen fail is not yet a detector. Four cases run
against the real 1323-case report:

| probe | result |
|---|---|
| identical run | **0 flips** |
| one verdict changed | **1 flip**, named, with its diagnostic |
| **two OFFSETTING flips, all four totals unchanged at 559/487/176/101** | **2 flips**, both named |
| one case added + one removed | **0 flips**, reported as a corpus refresh |

The third row is the whole point: that input is indistinguishable from a clean run under the old totals-based
reading, and the new gate names both cases. A full fresh differential run then returned **0 PER-CASE FLIPS**
against the committed baseline — the first time that sentence has been a measurement.

⚙ **The `.gitignore` already had the answer written down and it had gone stale.** Its comment block said "the
COMMITTED artifact is `tests/external/gnucobol-expectations.json`" — a file that has never existed and that
nothing references. The licensing line it records (owner, 2026-07-19) was right all along: *"our reports are
committable; their corpus is not."* The gap was never permission. It was that nobody had built the artifact.

