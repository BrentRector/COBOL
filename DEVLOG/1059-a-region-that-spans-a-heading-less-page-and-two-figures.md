## Entry 1059 — 2026-07-27 20:12 PDT — A region that spans a heading-less page, and two figures written as prose

427 figures generated plus one documented correction deliberately kept; six clauses left.

**A GENERATOR bug, not a transcription one.** The screen description entry's `where screen-attribute-clauses
is:` figures were never found, because `continues_from_previous` asked only the IMMEDIATELY preceding page
whether a general-format region was still open — and the page between carries no heading at all, so the region
read as closed. Walking back to the last page that HAS a heading found eight more figures across the standard
(475 → 483) and resolved §13.17.2 outright. Worth noting which way that error ran: the tool was under-reporting
figures, and the transcription was right.

**§12.3.6.2 had two figures written as PROSE.** The `where locale-phrase-1 is:` and `locale-phrase-2` groups
were transcribed as bullet lists — "A brace group containing: `locale-name-1`, `LOCALE`, `SYSTEM-DEFAULT`,
`USER-DEFAULT`" — where the printed page draws two braces. Replaced with the generated figures.

**That surfaced a rule the sweep needed: a DOCUMENTED CORRECTION outranks the printed page.** The standard
prints `locae-name-1` there and the transcription corrects it to `locale-name-1` under Addendum C3. Regenerating
from the page would silently revert it, and the word gate would report it forever as an unexplained difference.
A figure whose region references `see the Addendum (Cn)` is now kept as transcribed and counted as such —
neither reverted nor left looking like an unresolved defect. The correction had to be re-applied by hand to the
generated figure, width-compensated so the brace still closes in the right column; `verify_publishable.py`
confirms C3 is still flagged in place and listed.

Six clauses remain (§8.4.3.1.2 · §10.6.1 · §12.3.7.2 · §13.16.2 · §14.2.1 · §14.9.39.2), all the same shape:
the printed page and the transcription disagree on how many figures the clause holds.

