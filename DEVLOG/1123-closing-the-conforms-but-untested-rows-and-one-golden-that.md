## Entry 1123 - 2026-07-30 00:55 PDT - Closing the CONFORMS-but-untested rows, and one golden that must NOT pin its own numbers

**SEVEN ROWS CLOSED, GAP 3773 → 3766**, with two spec-derived goldens. These were the category the schema split
exists to express: the rule verified against the code, no test yet pinning it. Three of the seven had a test-ref
already — and it was a NIST golden, which is a REGRESSION NET and cannot close a conformance row (CLAUDE.md
rule 1). The gate had been refusing them all along, which is exactly what it is for.

**`pb1-numeric-arg-family`** (negative) — PRESENT-VALUE, RANDOM and RANGE each screened for §15.3 class numeric.
All three were already enforced by PB1's `Verified` table but had no fixture of their own, so each row rested on
a SIBLING function's fixture. That is not coverage: it proves the mechanism works, not that this function is
wired into it. One fixture, three rejections, each naming its own clause (§15.74.3 r1 / §15.75.3 r1 / §15.76.3 r1).

**`pb1_intrinsic_formats`** (positive) — the general formats of ORD-MAX, ORD-MIN and PRESENT-VALUE, every value
from the spec: `ORD-MAX(3 9 1)` = 2, `ORD-MIN(3 9 1)` = 3, `ORD-MAX(7 7 2)` = **1** (§15.71.4 r3, leftmost wins on
a tie), and §15.74.4 r1 at rate 1.0 giving 100/2 = **50** for one occurrence and 100/2 + 100/4 = **75** for two.

**⛔ AND THE ONE THAT COULD NOT BE PINNED THE OBVIOUS WAY.** §15.75.3 r5 — "subsequent references without
specifying argument-1 return the next number in the current sequence". The obvious golden displays those numbers.
That would have been **pinning the implementation, not the rule**: §15.4.1 makes RANDOM's values
implementor-defined, and §15.75.4 r2 promises only that a given seed always yields the SAME sequence. A golden
carrying our own doubles would fail the day the generator legitimately changed, while proving nothing about the
rule. So it is asserted STRUCTURALLY — the two unseeded references DIFFER (the sequence advances), and a re-seed
REPRODUCES the first (r2) — printing `SEQ-ADVANCES` / `SEED-REPRODUCES`. Both are properties the standard
guarantees; neither is a number the standard leaves to us.

⚠ **A drafting error worth the line.** My first `.out` for the PRESENT-VALUE rows read `000050.00`, a
human-readable form. The receiver was `PIC S9(6)V99`, so DISPLAY renders the zoned overpunch `0000500{`. The
VALUES were right and the RENDERING assumption was wrong — I had written the expectation from arithmetic without
asking what DISPLAY does with a signed DISPLAY item. Fixed by making the receiver UNSIGNED, so the golden is
about the FORMATS rather than about zoned rendering, which has its own goldens. Deriving the value from the spec
is only half of writing a golden; the other half is deriving how it will be SHOWN.

`session-probe` no longer prints a "test-needed" line at all: zero CONFORMS-but-untested.

**GATES.** Conformance **4146/4146**, zero skipped, nothing red. Unit 963/963. Corpus 233 positive · 135 negative.

