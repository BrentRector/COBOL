## Entry 1061 — 2026-07-27 21:05 PDT — PAGES ARE GONE: the transcription is clause-structured

`specs/ISO_COBOL.md` no longer has a page in it. **1,260 page anchors, 1,260 `## Page N` headings and 1,248
running headers removed; zero content words lost** — the only words the conservation check reports missing are
`##`, `Page` and the `#page-NNNN` link targets themselves.

The owner's directive was stated mid-session — *pages are not a thing in Markdown; use links within the
document* — and the order it forced was: repair the page-break damage FIRST, because the anchors are what tell
you where the damage is, and only then take the scaffolding down.

**What the references became.** The TOC's 892 linked entries were `[title](#section-x) …… [N](#page-N)`; the
page number was redundant beside a working section link, so it went. Four entries had never been linked at all,
because the clauses they name (`8.4.3.2 Function-identifier`, `8.4.4 Condition-name`, `A.1`) were transcribed
as bold text rather than headings and so had no anchor to point at — fixed earlier this session, linked now.

The INDEX was the real gate: 3,243 page references, the only place a page number carried information a section
link did not already have. Each became a clause link, by `scripts/spec/deindex_pages.py`, in order of
precision: the term IS a clause title (48.8%), the term was located on the referenced page (3.6%), the clause
the page opens with (45.7%), and 60 that resolve to nothing keep their printed number as plain text. **The
approximation is stated in the document**, in a note at the head of the index, rather than buried in a commit
message — a reader has to be able to tell an exact reference from an approximate one.

Result: 3,720 section links, **zero dangling**.

**Re-keying what depended on pages, and refusing to let it pass silently.** Both publication gates now delimit
on the document structure — clause 1 for where the front matter ends, the Introduction for the in-position
acknowledgment — and both are green. The three page-bucketed audits were the danger: with no anchors,
`audit_figure_text` and `audit_underlining` reported **0 defects on 0 pages**, which reads exactly like a clean
run. They HALT now. `extract_rule_catalog.py` halts too — it stamps a page on every rule and would have
recorded 0 for all 3,790, and a column of zeroes in the P14 denominator is worse than no column.

That is the third time this session a tool has been one edit away from a silent all-clear, and the same rule
keeps paying: **when the ground a check stands on is removed, the check must fail, not pass.**

