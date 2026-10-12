## Entry 1169 — 2026-08-04 05:05 PDT — PB24's group half: §15.50.4 r7, and `ImageWidth` was already two thirds of the answer

**The silent wrong answer is gone.** A group of `PIC X(4)` plus a `PIC X DYNAMIC LENGTH` child holding `"XYZ"`
returned **4**; §15.50.4 r7 requires **7**. It now returns 7.

**ROOT CAUSE — a missing arm, in its silent form.** `BindLengthFold` is a `switch` with arms for a ref-modified
place, an ODO group, an ANY LENGTH item, a national dynamic-length item and a dynamic-length **elementary** item.
There was **no arm for a GROUP containing a dynamic descendant**, so such a group fell through to the fixed fold
`BoundNumLiteral(ImageWidth)` — and `DataItem.ImageWidth` returns **0** for a dynamic-length child, because its
width is a runtime fact (§8.5.1.10). Every ingredient was correct on its own; nothing joined them.

⭐ **`ImageWidth` TURNED OUT TO BE EXACTLY r7(a).** The rule splits the sum into (a) the fixed subordinates,
(b) the current lengths of the dynamic-length ones, (c) dynamic-capacity tables at current capacity. `ImageWidth`
already sums the fixed subordinates and contributes zero for each dynamic one — which is precisely (a), by
construction rather than by coincidence, since that zero is what makes the fixed-layout math sound elsewhere. So
the arm **adds to it** instead of recomputing it: a second width walk would be a second thing to keep in
agreement with the first, and this file has spent the whole session paying for duplicated rules.
(b) is one runtime `FUNCTION LENGTH` per dynamic leaf, gathered by a **recursive** walk — r7 says "all
subordinate", not "all immediate children", and the nested case in the golden is what holds that honest.

⚠ **THE CHEAP FIX WAS TRIED FIRST AND DOES NOT WORK.** Routing the group through the runtime string channel —
the trick that made the ref-mod half a one-line change hours earlier — fails here, because the whole-group IMAGE
of a group with a dynamic child is itself staged loud ("Tier-C byte island … no whole-group character image").
Two adjacent halves of one defect, and the same idea solved one and not the other.

⚠ **r7(c) IS STAGED LOUD RATHER THAN SUMMED WRONG.** A subordinate dynamic-capacity table is counted by
`ImageWidth` as ONE occurrence, so folding it would return a plausible number wrong by (capacity − 1) elements —
the "worst defects return a plausible answer" family this project keeps meeting. A loud stage is a worse
experience and a better answer.

**THE GOLDEN PINS WHAT A SHALLOW FIX WOULD PASS.** A fixed group must still fold unchanged (10); TWO dynamic
children must both count (11); a dynamic leaf NESTED in a subordinate group must count (8); and — the one that
matters — **regrowing the child at run time must change the answer** (4+12 = 16, then 4+0 = 4). That last case is
what proves the value tracks content instead of being a new constant, which is exactly how the old defect
looked correct on a single sample.

**GATE.** Conformance corpus 424/424 · Intrinsic+Length+Group+Dynamic 274/274 · characterization 33/33 ·
**full Unit 3639/3639**. ODO re-probed and unchanged — still loud, naming r7, which is correct until r7(c) lands.

**PB24 is now half landed and OFF the actionable list**, and the register did that itself: with `wrong_answer` and
`crashes` both cleared, `Fix next` dropped it from 14 items to 13. What remains is `PHYSICAL` (§15.50.2's optional
keyword, parsed as a second argument — rejects legal source) and the unmeasured r9 rounding.

