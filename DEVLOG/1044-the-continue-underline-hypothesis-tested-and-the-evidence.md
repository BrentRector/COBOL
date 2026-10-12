## Entry 1044 — 2026-07-27 02:31 PDT — The CONTINUE underline: hypothesis tested, and the evidence goes the other way

The owner pushed back on my override at 14.9.9.2 with a genuinely good hypothesis: CONTINUE is a no-operation
statement and the standard talks about "implicit CONTINUE statements", so maybe the missing underline is
meaningful — only `AFTER` and `SECONDS` underlined inside the optional bracket.

Worth testing rather than defending, and there was a decisive test available: **census every statement's own name
in its own general format. 49 of 50 are underlined; CONTINUE is the sole exception.**

The "implicit CONTINUE" reading does not survive contact with the text either. The term appears **13 times**
(pages 634, 655, 656, 666, 719, 720, 737, 808, 809, 1123) and always denotes a CONTROL-FLOW device — a conceptual
no-op that control is transferred TO, after `END-PERFORM`, before the next separator period, following a statement
that raised a nonfatal exception. It is never written in source. General rule 2 on p634 governs their BEHAVIOUR,
not this format's notation. And §5.2.3's optional word is about SOURCE SPELLING: if `CONTINUE` were one, then
`AFTER 5 SECONDS` alone would be a legal CONTINUE statement, which syntax rule 1 forbids. The hypothesis also
self-contradicts — if `CONTINUE` were omissible, an *implicit* CONTINUE would be writable.

**The owner's reading of the figure was right in the part that matters**, and it strengthens yesterday's call:
`AFTER` and `SECONDS` ARE underlined on p634 and bare on p732, so p634 is the trustworthy page for those two
words and the grammar correctly keeps them required.

The note now carries the evidence instead of the assertion — which is the point of overriding the as-printed rule
in that one place at all.

**None of this was checkable before today.** The 13 references came from grepping the standard, and the
50-statement census from measuring printed pages. Both are things the decoded PDF made possible this session.

