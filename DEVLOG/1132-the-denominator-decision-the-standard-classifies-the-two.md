## Entry 1132 - 2026-07-30 14:25 PDT - The denominator decision: the standard classifies the two clauses itself, so the question was not the one being asked

The outstanding owner decision was posed as: the catalog does not model §13.18.40.5 "Editing rules",
§13.18.40.6 "Precedence rules" or §5.3 "Rules"; **those are rule KINDS, not spelling variants, so admitting them
changes what the denominator MEANS.** That framing is what made it an owner decision rather than a bug.

**The premise does not survive reading §5.3.** The clause that would have introduced the new kinds is the clause
that rules them out, and it says so in two sentences I validated verbatim with `cite.py --check`:

> §5.3.3 — "The rules of the PICTURE clause specified in 13.18.40.5, Editing rules, are **General rules**."
> §5.3.2 — "The rules of the PICTURE clause specified in 13.18.40.6, Precedence rules, are **syntax rules**."

So they are GR and SR wearing a heading spelling the extractor's map did not know — **the same defect class as
the pluralisation fix that moved 3,790 → 3,846**, not a change to what the denominator means. This is
`feedback_validate_the_premise_not_only_the_rule` again: the citation in the question was fine, and the premise
underneath it was wrong. Decision taken: admit both, TYPED BY §5.3; exclude §5.3 itself, which defines the
taxonomy, carries no numbered rules of its own, and is the very clause that types the other two.

**The effect is +15, and I had told the owner +17.** Correcting it is the more useful half. §13.18.40.5 yields 15
general rules. §13.18.40.6 yields **zero** — its content is unnumbered prose plus Table 10 and Table 11, and the
normative content IS those two precedence matrices, which have no per-rule ordinal to extract. My "2" came from
an ad-hoc line-range scan that ran past the clause boundary into the following section; the extractor, which
parses the structure properly, found none. A number produced by a throwaway regex over a guessed line range is
not a measurement, and I presented it as one. **Denominator 3,846 → 3,861** (GR 1498 → 1513, SR unchanged).

**Both dispositions are DECLARED IN THE EXTRACTOR, not left to reappear as warnings.** `EXCLUDED_BLOCKS` carries
§5.3 with its reason and `KNOWN_EMPTY_BLOCKS` carries §13.18.40.6 with its reason, each printed as a factual
line rather than an alarm. The argument is the one §0 already makes about line-number code-locations: a guard
that reports an expected condition on every run is a guard people learn to ignore, and the whole value of the
unrecognised-heading check is that it fires only when something is genuinely missing. Anything not in those two
dicts still fails loudly.

**One PARSE GAP remains and it is not mine:** §15.4 "Returned values" yields zero rules. I confirmed it is
pre-existing by running the extractor on the stashed pre-change tree rather than assuming it — the same check
that kept PB9 from being misfiled as a regression earlier today. It is unowned; naming it here is not closing it.

Inventory rebuilt: **3,861 rows, 3,813 GAP**, all 184 existing adjudications preserved (`build_inventory.py`
preserves adjudicated fields across a rebuild, which is exactly what makes the denominator safe to correct
mid-review). `SpecTraceabilityInventoryDriftTests` green.

