## Entry 1093 — 2026-07-28 22:06 PDT — A line number is not a citation: re-keying the VCR onto the clause hierarchy, and two blind spots in `cite.py`

The one red blocking the `phase-14` → `main` merge was `VcrDriftTests.EverySpecLineRef_IsWithinTheSpec`. Its
message was arithmetic — the VCR appendix cited spec LINE numbers reaching 50,407 in a file that now has 47,195
lines. What the arithmetic hid is the part that matters: those references had been pointing at the WRONG SENTENCE
for weeks before they pointed past the end. De-paging removed 1,260 page anchors and 758 horizontal rules; the
figure sweep regenerated 483 figures. Every one of those edits silently slid ~180 citations. A dangling reference
is loud. A reference that resolves to the wrong sentence is not, and that is the failure this class produces
99% of the time.

**Recovering what the references meant.** The line numbers were keyed to a spec revision that no longer exists in
the working tree — `specs/` was a private submodule until 2026-07-27. `git blame` put all 135 appendix rows on a
single commit (`0d744cba`, 2026-06-09), whose submodule gitlink is `c1435f3` — 53,731 lines, still reachable in
`specs-private`. Extracting it turned an archaeology problem into a lookup: read the line each reference pointed
at, take the clause that contained it THERE, and assert the same sentence still sits inside that same clause now.

**The citation form.** Each row now carries `§CLAUSE ` + a backticked VERBATIM FRAGMENT of the sentence the row
was written from. The clause number is the standard's own identifier and does not move; the fragment pins the
reference inside the clause. Where the standard prints an item number the fragment CONTAINS it (`2) ALIGN
clause`), so the item number cannot drift silently either — it is inside the checked text. 174 citations, all
verified; two more rows (130d/130e) that had never had an appendix entry were added, and 130c was corrected from
GR6a to GR4, which the main table had already retracted and the appendix had not.

**Choosing WHICH reference to keep was the interesting part.** Rows 1–7 all cite Annex E.2 item 1 and differ only
by which bullet of its seven-item removal list they mean; rows 10/11/19/32 cite an item whose bullets are its
content, not separate sites. The rule that separates them is computable rather than editorial: **a head cited by
more than one row does not discriminate between those rows**, so those rows keep their bullets and every other
row keeps its head. Three generator bugs came out of getting that right, each producing a plausible-looking wrong
answer: `**` emphasis made `5) **Compiler-directive words.**` test as lowercase and demoted the whole item; a
nested list broken across a printed page (`2. the source element invokes any method.`) read as a top-level item;
and resolving a reference by first-hit text search collapsed §D.18.1, §D.18.3.1 and §11.9.5.2 into §8.8.1.4.1,
because "NOTE The STANDARD-BINARY mode of arithmetic is an obsolete feature." is printed verbatim in six clauses.
That last one is the reason resolution keys on the OLD CLAUSE and only uses text to confirm.

**Two real defects in `scripts/spec/cite.py`, both found by trying to validate what I had written.** Its heading
grammar required a dotted-decimal number and a title, so:

- **every annex clause was uncitable.** `--check E.2 …` answered "there is no clause §E.2", which reads as a bad
  citation rather than a blind tool. That is 336 clause headings — the whole of Annexes A–G, including the two
  annexes this entire ledger is built from.
- **every term definition was uncitable.** All 178 headings of clause 3 are a bare number (`### 3.74`), the term
  itself being the bold line beneath.

Both are fixed, with negative controls: a wrong clause still fails, and the body-clause case still passes. This
matters beyond the VCR — CLAUDE.md rule 1 requires `--check` on every citation, and until now the tool would have
answered "no such clause" for any annex or term citation anyone tried to validate.

**The drift test is replaced, not repaired.** `EverySpecLineRef_IsWithinTheSpec` is gone. In its place:

- `EverySpecCitation_ResolvesInTheSpec` — every `§clause` token in the WHOLE document names a real clause (283
  tokens, 56 distinct, all resolving), and every appendix fragment is still inside the clause it names. This is
  the `cite.py --check` contract, in the battery.
- `NoSpecLineNumberIsCited_InTheVcr` — a spec LINE number may never come back. `@2023`-style edition markers are
  excluded by shape, not by a hand-maintained list.

Each was proven to fail once before being trusted: an invented clause → red, a real clause with a wrong quote →
red, a resurrected `@45803` → red, then restored → green. The document-wide scope is deliberate; had the test
only covered the appendix, the same rot would simply have regrown in the main table, which is exactly where it
had already spread.

**The sweep.** Twelve inline `@NNNNN` references in the main table went the same way, including four §8.9 ABSENCE
citations — an absence has no sentence to quote, so those name the alphabetical neighbours that bracket the gap
("the list runs REPOSITORY → RESERVE with no RERUN"), which is evidence a reader can check. One of them corrected
a real mis-citation: the MOVE ALL-literal rule is §14.9.25.3 **SR**5, a syntax rule, not the "GR5" the row
claimed. Two sibling files still carry line-style references (`COMPLETION_ROADMAP_COUNCIL.md` 35,
`PHASE-13-remaining-waves-scout.md` 33); they are keyed to different revisions and are the next pass. `DEVLOG.md`
keeps its two — it is the historical record and describes what was true when written.

Gates: wave-local Conformance filter 1837/1837 green; FULL Conformance 3931/3931 with NOTHING red (11m 55s), against 3929/3930 before the change. The battery is entirely green for the first time this phase.

