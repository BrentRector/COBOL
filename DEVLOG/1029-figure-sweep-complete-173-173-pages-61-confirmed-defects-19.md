## Entry 1029 — 2026-07-26 20:05 PDT — Figure sweep complete: 173/173 pages, 61 confirmed defects, 19 of them normative and every one falsely restrictive

The second run closed the coverage hole from Entry 1028: 109 pages, 41 claims, 33 confirmed, 8 refuted. Combined
with the first run the figure/diagram target is **fully covered - 173 of 173 pages**, plus 105 non-figure pages
swept accidentally by the mis-typed first list, which were not wasted.

**Totals: 88 claims -> 61 CONFIRMED, 27 refuted, 0 unverified. 19 normative, 32 structural, 10 cosmetic.**

**Every one of the 19 normative findings is the same failure mode: falsely restrictive.** Not one makes the
markdown more permissive than the standard. Three mechanisms:

- **Lost choice-indicator bars** (p373, p607) - a bracket without bars means AT MOST ONE alternative; with them it
  means zero or more, each once, in any order. Same defect already confirmed in GOBACK.
- **Lost outer brackets** (p320 CURRENCY SIGN and LOCALE, p389 report-group USAGE) - the bracket that makes a
  whole clause OPTIONAL was dropped, so an optional clause reads as mandatory.
- **Misstated underlining** (p102, p106, p111, p203, p363, p471, p533, p606, p607, p644, p703, p756) - the figure
  notes claim words are underlined (required) that the printed page leaves un-underlined (optional). p644 is the
  sharpest for us: `ON` is an optional word in ON SIZE ERROR, so bare `SIZE ERROR ...` and `NOT SIZE ERROR ...`
  are legal COBOL, and three figure notes say otherwise.

A compiler built from these notes rejects valid programs. That is the whole reason the owner insisted the catalog
be validated against the PDF rather than the transcription.

**A merge bug caught before it corrupted the ledger.** `merge_reconciliation.py` matched verdicts to claims on
`(page, kind)` alone. Pages carry TWO findings of the same kind (p286 and p203 each do), so a single-slot index
silently bound one verdict to the wrong claim and left the other looking unverified - a wrong verdict attached
with full confidence, which is the same class as the duplicate rule-id in Entry 1025. Verify files carry the
CLAIM, so matching is now on claim text with an ordered fallback. 0 unverified across all 88.

**The refutation rate is the quality signal.** 27 of 88 claims did not survive - and the verifiers did not merely
vote. They measured underline rules in pixels, corrected finders' section citations, and on two occasions accepted
a finding while REJECTING its stated consequence: on the p286 compilation-group ellipsis the verifier confirmed
the diagram is transcribed wrongly but demonstrated that both nestings collapse to the same language
(`[ {choice} ... ]` and `[ {choice} ] ...` both mean zero or more source units), so it downgraded the severity to
transcription-fidelity rather than semantic. That is the standard I want: confirm the defect, refuse the
overstated impact.

Next: apply the 61 repairs. The seven ON/OFF directive notes go as one group, then the bracket and bar losses,
then the structural and cosmetic items. Nothing here is applied yet - the ledger is the durable record and every
finding carries its verifier's reasoning.

