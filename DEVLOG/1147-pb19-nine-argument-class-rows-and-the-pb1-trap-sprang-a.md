## Entry 1147 — 2026-08-02 21:01 PDT — PB19: nine argument-class rows, and the PB1 trap sprang a second time one layer down

PB19 is PB1's designed residue coming due for §15.45–15.57: `IntrinsicArgumentRules.Verified` is deliberately
partial and grows as the review adjudicates each clause, so a function's absence is a real finding rather than an
artifact. Nine rows landed, each read at its own clause and `cite.py --check`ed — never inherited from the
`ArgKinds` hint column, which is the mistake that made PB1's first attempt reject twelve legal corpus programs.

`'i'` — INTEGER-OF-DATE (§15.46.3 r1), INTEGER-OF-DAY (§15.47.3 r1).
`'n'` — INTEGER-PART (§15.49.3 r1), LOG (§15.55.3 r1), LOG10 (§15.56.3 r1).
`'s'` — LOWER-CASE (§15.57.3 r1), UPPER-CASE (§15.97.3 r1), INTEGER-OF-FORMATTED-DATE (§15.48.3 r1/r3).
`'b'` — INTEGER-OF-BOOLEAN (§15.45.3 r1), on a class arm that did not previously exist.

The INTEGER-PART row is the one that shows what "the table grows per clause" costs: §15.49.3 r1 is the same
sentence as INTEGER's §15.44.3 r1 and FRACTION-PART's §15.42.3 r1, both of which were already registered. The
rule was enforced for two of three identical functions.

### The ordering was load-bearing, and I proved it instead of asserting it

§15.45.3 r1 is the only rule in the catalogue naming class BOOLEAN, and `Admissible` had no arm able to say it — a
screen cannot reject what its vocabulary cannot describe. But adding the arm depended on PB20: Annex D's own
worked example passes a REFERENCE-MODIFIED bit item, and until PB20 every ref-mod result was typed class
ALPHANUMERIC on the authority of a clause that does not exist.

I did not take that on trust. I temporarily restored the pre-PB20 rule, rebuilt, and confirmed
`FUNCTION INTEGER-OF-BOOLEAN(BITS(1:6))` is REJECTED under it — then restored and confirmed it computes 42
(101010₂). Landing PB19 first would have shipped a screen that rejects the specification's own sample program.

### And then the corpus caught me anyway

The full run came back **2 failures**, both INTEGER-OF-BOOLEAN, both on programs that are correct:

```
COMPUTE N-3 = FUNCTION INTEGER-OF-BOOLEAN(B"00000101")          2002/intrinsics_boolean_conv
IntegerOfBoolean_RoundTrip_Spec                                  a spec-derived unit test
```

The row was right. The defect was one layer down, in the thing that answers "what class is this operand":
`ClassOf` returned a flat `CobolClass.Alphanumeric` for every `BoundStringLiteral`, **ignoring the `Category` the
record actually carries** — Alphanumeric by default, but `National` for `N"…"` and `Boolean` for `B"…"`.

**A screen is only as good as its classifier, and a classifier that flattens a category is how a correct rule
rejects a correct program.** PB1's original disaster was asserting unaudited ROWS; this was an audited row over a
lossy CLASSIFIER — the same failure one level down, and invisible to every review of the rows themselves. It took
the corpus to find it, which is exactly what the corpus is for.

Two things make me more confident in the fix than in the first attempt. The nested-function shape,
`INTEGER-OF-BOOLEAN(BOOLEAN-OF-INTEGER(200, 8))`, was ALREADY correct — `IntrinsicType.Boolean` flows to
`PicCategory.Boolean` through `ResultCategory` — so only the literal arm was broken and only it was touched. And
the fix incidentally corrects `N"…"` literals, which had been classed alphanumeric everywhere; I checked the
consequence rather than assuming it, and it is a no-op for verdicts: `'s'` admits National already, and `'n'`
rejected a national literal before and after, just for the right reason now.

### Two residues, deliberately not rounded up

- **LOWER-CASE / UPPER-CASE stay PARTIAL.** §15.57.3 r1 and §15.97.3 r1 are one sentence with two halves — "of
  class alphabetic, alphanumeric, or national" AND "at least one character position in length". This screens the
  class half. Half a rule enforced is PARTIAL, exactly as REVERSE's `AR-15.78.3-1` already records.
- **INTEGER-OF-FORMATTED-DATE's r3 stays PARTIAL.** "A data item of the same type as argument-1" is a
  CROSS-ARGUMENT rule that a per-position screen cannot express — the sibling of `AR-15.79.3-3`. The `'s'` row
  closes the class half, which is the half that was silently accepting a class-numeric argument-2.

Also cleared: the FACTORIAL row's comment still carried the de-editing reading the owner overturned earlier today
(DEVLOG 1142) — the last stale copy of it in this file.

