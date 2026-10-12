## Entry 1034 — 2026-07-26 23:02 PDT — The seven ON/OFF directive notes now agree, and an invented notation rule is deleted

Batch 1 begins: the normative findings, where the falsely-restrictive defects live. First sub-batch, the seven
ON/OFF compiler-directive figure notes.

These notes are the transcription's OWN editorial annotations rather than ISO text, which is what makes them
dangerous — they read as authoritative, they are what an implementer reaches for instead of squinting at a figure,
and four of the seven misdescribed the printed page.

**Verified by cropping the figure regions at 300 dpi and reading them.** LEAP-SECOND (p106): the directive word
underlined, `ON` bare, `OFF` underlined. FLAG-14 (p102): the directive word and all twelve option names
underlined, `ALL` bare, `ON` bare, `OFF` underlined. The `>>` indicator is not underlined in either. Per §5.2.2 an
underlined uppercase word is REQUIRED and per §5.2.3 a non-underlined one is OPTIONAL — so claiming `ON` is
underlined turns a legal omission into an apparent syntax error.

**Repaired as a family, which was the whole point.** The seven disagreed with each other about the same notation:
two called `ON` required, two correctly called it un-underlined, one called it a typesetting omission, and one
invented a rule to explain it. Fixing them individually would have relocated the contradiction rather than removed
it. A mechanical check after the edit confirms all seven now say the same thing.

**⛔ An invented rule, deleted rather than reworded.** PROPAGATE's note asserted that in compiler-directive formats
the underlined alternative marks the default. **No such rule exists in the standard.** LISTING falsifies it
outright — `OFF` is the underlined alternative there, yet §7.3.18.3 GR2 makes `>>LISTING ON` the default — and the
transcription's own POP note contradicts it. An implementer taking it at face value would read defaults off the
underlining of every directive format and get LISTING, REF-MOD-ZERO-LENGTH and TURN wrong. Its absence from the
whole file is asserted after writing, not assumed.

**Where the standard itself is doubtful, the note now says so.** FLAG-14's un-underlined `ALL` is hard to read as
a true optional word, since §7.3.15.4 GR4a gives `ALL` distinct semantics. It is recorded AS PRINTED and flagged,
not silently "corrected" by adding an underline the page does not have. Recording ISO's own defects rather than
coding around them is the standing rule, and the reason is practical: a future maintainer has to be able to check
the note against the page.

Gates: `--check` 0 · anchors 1261 · catalog 3,790 · the invented rule at 0 occurrences · seven notes agreeing.

**What this does NOT fix, and I want that on the record.** The notes are now right; `CobolParserCore.g4` has not
been touched. Batch 1's exit criterion runs the grammar-impact workflow over the normative pages, and every
INHERITED verdict becomes a fix-queue COMPILER bug. `ON SIZE ERROR` is the standing candidate — the grammar
requires the very token this batch has just confirmed is optional.

