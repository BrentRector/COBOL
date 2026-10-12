## Entry 1056 — 2026-07-27 18:46 PDT — 179 → 16, and every step of it was my own tooling being wrong

Drove the sweep's residue down. I said last entry that the 179 should be treated with suspicion because the
comparator's first four failures had all been mine rather than the transcription's. That held: **every
reduction below came from fixing my own tool, not from repairing the standard.**

- **Page furniture was being swept INTO figures.** `**ISO/IEC 1989:2023 (E)**` passes the figure test — all
  upper case, no plain lower-case word — so a running header inside a clause region counted as figure content,
  and a page break falling inside a figure split it into two spans matching nothing. Made transparent. 179→158.
- **Tagged figures were discarded as HTML.** Parts of the transcription already carry their underlining, and
  `<u>NULL</u>` starts with `<`, so the clauses CLOSEST to the target form were reported as having no figure
  at all. Tags are stripped before the test now. Together with a lead-line merge: →100.
- **A Format label does not identify a figure.** The qualification clause ends with two unnumbered
  `where … is:` definitions that inherit the last label, so keying on (clause, Format) merged three distinct
  figures into one and the clause looked short by two. Grouped by CLAUSE and paired in reading order. →29.
- **Typography counted as content.** The separator period is attached on one side and free on the other
  (`FACTORY.` vs `<u>FACTORY</u>.`); `/` separates stacked alternatives inline; `⌐ ¬` are ASCII-art corners;
  `&nbsp;` is indentation; and the standard sets a dash four ways (hyphen, figure dash, en dash, minus sign).
  None of those is a word. →16.

**The last one was the interesting one, because no rule works.** One printed figure is sometimes set as
several blocks — UNSTRING is two fences plus a bare `[ END-UNSTRING ]` — and consecutive figures are sometimes
separated by nothing but a figure note. I tried merging across notes: it fixed UNSTRING and broke the
file-control entry, whose formats are ALSO separated by notes alone. There is no local signal.

So the grouping is no longer guessed, it is SOLVED: `regroup` searches for the partition of markdown spans
into contiguous groups whose words equal the printed figures', using the gate itself as the decision
procedure, and reports rather than picks when more than one partition matches. That is the same move as
keying on the clause instead of the page — stop inventing a heuristic where the structure already answers.

**475 figures keyed · 412 replaceable · 16 groups (63 figures) left.** Not applied: a half-converted document
is worse than either end. The residue is now small enough to read clause by clause against the printed page,
and at least two entries look like genuine transcription defects rather than tooling — §7.3.15.2 joins
`VALUE-EDITING` where print sets a figure dash, and §14.9.1.2 Format 3 carries a neighbouring figure's words.

