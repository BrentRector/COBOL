## Entry 1062 — 2026-07-27 21:16 PDT — The reconciliation ledger is CLOSED; a transcription agent was still talking to its operator

All 210 confirmed findings are repaired or superseded. The figure classes closed by construction when the sweep
landed; the 130 non-figure findings went 130 → 88 → 42 → 24 → 11 → 0 as the tooling was corrected and then the
real defects were fixed. Zero normative, zero structural, zero cosmetic outstanding.

**The worst of the last eleven was not a transcription error at all — it was the TRANSCRIBER.** Page 1260
carried, in the published standard: *"The page appears to be blank except for the watermark/footer text at the
bottom. Since you've instructed me to ignore watermark text, and there is no other visible content on this
page, there is no text to extract."* An agent's reply to its operator, sitting in the document. Two lines of
Annex E.3.2 had also been stranded on the BACK COVER, duplicating text already correctly present at E.3.2.

**C4 added to the Addendum.** The standard prints `FUNCTION-ID F-1.` in the F.1 example programs — without the
separator period the paragraph header requires, so it is invalid COBOL as printed. The transcription had
already corrected it, SILENTLY, which is the actual defect: the document's own rule is that a departure from
the printed standard is flagged in place and listed in the Addendum, so it stays reversible. Now flagged,
listed and gated, exactly like C1 (`EMD-START`) and C3 (`locae-name-1`).

The rest were small and each verified against the page first: a continuation table that invented its own
headings (`Symbol / Description` where the standard prints `Word / Meaning`); the closing definition of syntax
rule 14 glued onto item c), so it read as qualifying only the national PICTURE rather than all three; a
full-width paragraph absorbed into the preceding bullet; stray emphasis delimiters around an operand ordinal
and a hyphen; a hyphenation split across a line break; two wrapped continuation characters left detached in an
EXAMPLE table; and a fractional-seconds format printed with five `s` transcribed with four.

**On the measurement I used to drive this down.** The "still detectable" probe tests whether a finding's
recorded `markdown_says` still appears in the file, and it OVER-REPORTS: it matched surrounding context that
had never changed, so it reported three findings as open immediately after I had fixed all three. It was fine
as a burn-down instrument and useless as proof — every close was confirmed by looking at the specific text, not
by the counter going down. Worth remembering the next time a number is doing the reassuring.

