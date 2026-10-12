## Entry 1047 — 2026-07-27 12:35 PDT — The transcription is PUBLISHED; and D3 turns out not to be a defect

**Owner instruction: move the Markdown to the public repository.** Done, and the shape of the move was decided by
counting references rather than by preference: `specs/ISO_COBOL.md` is cited **1,663** times across docs, skills,
workflows and scripts; the PDF is referenced **9** times, nearly all through one glob constant. So the SUBMODULE
moved, not the file — `specs/` is now an ordinary public directory holding the transcription, and the private
submodule is `specs-private/` holding only the licensed PDF. Every spec citation in the project keeps working,
and 9 lines changed instead of 1,663.

**The Markdown was ADDED FRESH, not grafted.** The private repository's history contains the per-copy licence
stamp — it survived one OCR pass, on the cover page, concatenated onto the genuine copyright line. Copying that
history into a public repository is precisely what the split exists to prevent. Verified after publishing by
fetching the file back from GitHub and grepping it: **0 occurrences** of the stamp.

**A self-inflicted break worth recording.** The bulk edit that repointed the PDF path injected real newlines into
string literals and left seven scripts syntactically broken. Caught by parsing every script under `scripts/` with
`ast.parse` rather than by running one and assuming the rest were fine — the kind of check that costs three lines
and would otherwise have shipped a broken toolchain into a public repository.

**D3 is not a defect.** I had recorded `EXCEPTION` being un-underlined in `LAST EXCEPTION` as a doubtful oddity,
on the basis of two pages. Checking every site changed the answer twice over. All THREE general-format sites
agree (653 EXIT PROGRAM, 661 GOBACK, 756 SEND); the apparent fourth (761) is the SET statement,
`SET LAST EXCEPTION TO OFF`, where `EXCEPTION` belongs to SET's own syntax and is correctly underlined — not a
counter-example at all. And the standard's own prose settles it: **page 111 writes "as though a
`GOBACK RAISING LAST` statement were executed"** — the short spelling, without `EXCEPTION`. Pages 579, 581 and
1123 write the full form. Both spellings occur and mean the same thing, which is exactly what §5.2.3 predicts of
an optional word. Reclassified from "recorded but not corrected" to **verified correct as printed**.

That is the second time today the honest answer came from checking every occurrence instead of the two the
finding named — the first being the `AFTER`/`SECONDS` contradiction, which reverted a wrong grammar change.

D1 (FLAG-14's un-underlined `ALL`) is still open and still recorded as printed.

