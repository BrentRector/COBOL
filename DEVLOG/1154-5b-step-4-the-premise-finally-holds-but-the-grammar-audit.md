## Entry 1154 — 2026-08-03 15:39 PDT — §5b step 4: the premise finally holds — but the grammar audit could not run at all

Step 4 says the grammar↔spec audit and the inventory's FMT+SR are the same body of work counted twice, and that
unifying them before the SR mass avoids auditing ~1,670 rules a second time. **This premise CHECKS OUT** — the
first of §5b's four to survive its own measurement. The audit scopes itself as "1,659 items: 321 general formats
+ 1,338 syntax rules"; the catalog holds FMT **322** + SR **1,352** = **1,674**. The deltas of 1 and 14 are
exactly the denominator corrections §0 already records from the extractor's heading-spelling fixes. Same
territory, counted independently.

**⛔ BUT THE AUDIT COULD NOT RUN, AND HAD NOT BEEN ABLE TO SINCE DE-PAGING.** It is
`.claude/workflows/spec-grammar-conformance.js` — a WORKFLOW, not the "skill" §5b calls it — and it was broken
two ways at once, both inherited from the removal of page numbers:

1. **It read `r['page']` off every catalog rule. 0 of 3,861 rows carry one.** §0 records the change outright
   ("Rows are CLAUSE-keyed (no page field)") and warns that "anything page-keyed must be re-keyed onto the clause
   hierarchy"; several scripts were re-keyed at the time and this workflow was missed.
2. **It matched sections EXACTLY.** Every catalog row is a SUB-clause — `14.9.1.2`, `14.9.1.3`, `14.9.1.4` — so
   the workflow's own documented example args (`"14.9.1,14.9.2"`) selected **zero rules and printed nothing**.
   Not an error: nothing. Each agent would have been handed no page and no rules and told to get on with it.

That second one is this session's recurring shape in yet another register: **an empty result standing in for a
negative one.** The `page` KeyError would at least have been loud; the silent empty selection is what would have
produced confident, evidence-free grammar verdicts.

**THE FIX — `scripts/spec/clause_page.py`**, the inverse nobody had written. Every figure/geometry tool in
`scripts/spec/` runs page→clause (it walks the PDF and asks what a page carries); nothing ran clause→page,
because the catalog used to stamp the page and callers just read it. The resolver reuses
`render_figure.headings()` rather than re-implementing the bold-numbered-heading test, returns the PDF page and
the printed folio with the heading text as corroboration, renders with `--render`, and **exits non-zero when a
clause does not resolve** — the whole point, given what it replaces. Verified both directions: `14.9.1` → folio
576 / `13.18.40` → 441 / `14.9.39` → 729 / `15.3.3.2` → 801, and a nonexistent clause exits 1 with a named
message. The workflow now calls it and prefix-matches: `14.9.1` yields **19 rules, where it yielded 0**.

**SWEPT FOR SIBLINGS** (rule 4). No other consumer of the removed catalog `page` field exists — the remaining
`['page']` uses in `audit_figure_*`, `figure_geometry` and `merge_reconciliation` are on those tools' OWN figure
and reconciliation records, which are page-keyed by design because they measure the printed page. And no other
exact-match-on-section lookup exists.

**WHAT IS STILL TO DO:** the unification itself. The workflow now runs; it does not yet emit `record_verdicts.py`
batch files alongside its report, which is what turns one pass into both a grammar finding and an inventory
verdict. That is step 4 proper, and its precondition is now met rather than assumed.

