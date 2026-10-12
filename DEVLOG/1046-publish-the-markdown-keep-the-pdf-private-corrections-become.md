## Entry 1046 — 2026-07-27 03:48 PDT — Publish the Markdown, keep the PDF private; corrections become an Addendum

Owner decision after a second legal analysis: **the Markdown is published, the repaired PDF stays private.** Three
things follow.

**The per-copy licence stamp is gone.** The OCR pass had captured it ONCE — at the end of the cover page,
concatenated onto the genuine `© ISO/IEC 2023` line. One occurrence in 53,000 lines, carrying the purchaser's
name, ANSI order number and download date. That is the kind of thing a human review misses and a grep does not,
and it is exactly why the check below exists. Only the stamp was removed: the copyright line is genuine cover
content — it precedes the stamp in the printed page's own text order, which I verified rather than assumed.

**Policy change, at the owner's instruction: correct the standard's defects rather than only flag them.** Three
are now corrected — `EMD-START` → `END-START` (p236), `CONTINUE` shown underlined (p634), `locae-name-1` →
`locale-name-1` (p315). The C1 reasoning is the one that generalises: **a reserved-word list is consumed as
data**, so a compiler generated from the printed list would reserve a non-word and fail to reserve the real scope
terminator. Recording that as-printed serves nobody.

**The Addendum is the actual deliverable.** Every departure is listed *with the printed form beside it*, so any
correction can be REVERSED if it later proves mistaken — which is precisely the owner's stated reason for asking
for it. It also records three defects deliberately NOT corrected: D1 FLAG-14's un-underlined `ALL`; D2 the
p634/p732 contradiction; D3 the un-underlined `EXCEPTION` in `LAST EXCEPTION` — which, unlike D2, is printed
CONSISTENTLY on both its pages, so it is a deliberate-looking oddity rather than a slip, and is left alone. The
distinction between D2 and D3 is the whole reason for having a "not corrected" section at all.

**New gate `verify_publishable.py`, deliberately runs WITHOUT the PDF** so it can execute in the public
repository, where the PDF is absent by design. It checks: no per-copy licence data (six patterns, so a partial
survival is caught as readily as the whole line); the acknowledgment opens the file; and **every correction is
both flagged in place AND listed in the Addendum, in both directions** — because the Addendum only makes a
correction reversible while the cross-references hold, and a note or an entry can be edited away independently of
the other.

Proven able to fail: renaming C3 to C9 in the Addendum alone reported *both* halves of the break — "C3 referenced
but missing", "C9 listed but never referenced" — and exited 1.

It complements rather than duplicates `verify_acknowledgment.py`, which checks the acknowledgment word-for-word
against the printed page and therefore needs the PDF.

