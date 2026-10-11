## Entry 1054 — 2026-07-27 18:24 PDT — The ASSIGN stretch was never the row-model question I reported it as

I had been reporting the ASSIGN case as a limitation needing an owner decision — "a vertically centred label
takes a row of its own; separating that from ACCEPT's `AT` needs a two-dimensional row model". The owner asked
the question that dissolved it: **why does `{ device-name-1 / literal-1 }` occupy four lines when
`LOCK MODE IS { MANUAL / AUTOMATIC }`, identical in shape, occupies three?**

Measured, the answer is exact. Four text rows fall inside the inner brace's span (161.6–179.9):

    165.6  device-name-1   inner brace, alternative 1
    169.8  TO              inner brace, its OWN label, on its point row (mid piece at 170.7)
    175.1  ASSIGN          OUTER brace, its label, on the OUTER point row (mid piece at 176.0)
    179.8  literal-1       inner brace, alternative 2

So the inner brace is shaped exactly like LOCK MODE — two alternatives plus a label on its point row, which is
three rows. The fourth line is imported: a label belonging to the ENCLOSING brace that happens to land between
the inner brace's alternatives. Nothing encloses `LOCK MODE IS`, so nothing intrudes on it.

That is a targeted rule, not a row-model rewrite: **an outer enclosure's label may not subdivide an inner one**
— snap it to the nearer neighbouring row (`ASSIGN` is 4.7 pt from `literal-1`, 5.3 pt from `TO`, so it lands on
the `literal-1` row). A brace's OWN label is exempt, and that exemption is exactly what keeps ACCEPT Format 3
right: `LINE NUMBER` also sits outside its brace and inside its span, but on that brace's own point row.

475 figures, zero collisions; ACCEPT, START, COMPUTE, USAGE, the relation condition and the function-identifier
all unchanged.

**The lesson is about how I reported it.** I had described the symptom accurately and then asserted a cause —
"needs a 2-D row model" — that I had not established. That framing survived three messages and would have
bought a large piece of work. The owner's question was a comparison I could have made myself: two figures of
the same shape, different row counts, one difference between them. When something looks like it needs an
architectural answer, first find the nearest case where it does NOT happen.

