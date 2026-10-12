## Entry 1078 — 2026-07-28 14:17 PDT — The underline said `>>` was a required word; it is not a word at all

Owner: "the markdown 7.3.9.2 underlines all of `>>CALL-CONVENTION` but the spec does not underline the `>>`
part." Correct, and it is normative — §5.2.2/§5.2.3 make UNDERLINING the test for a required word, so
underlining the indicator states something about the language that the printed page does not.

**Measured, folio 59:** the token `>>CALL-CONVENTION` spans x 81.0–185.5; the underline rule spans 95.0–184.4.
It starts AFTER the indicator. Five figures were affected — CALL-CONVENTION, COBOL-WORDS, DISPLAY, WHEN,
LEAP-SECOND — and two of them CONTRADICTED THEIR OWN FIGURE NOTE, which already said in prose that the `>>` is
not underlined.

**Root cause.** `figure_extract.is_underlined` is a per-WORD boolean and `get_text("words")` splits on
whitespace. The rule covers 86% of the fused token, over the 55% threshold, so the whole thing came back
underlined. §7.3.16.2 was already correct for the only reason that matters here: that page prints `>> IF` with
a space, so whitespace tokenization had already separated them.

**The owner's framing is what made the fix principled rather than a geometry hack.** §7.3 SR5: "A compiler
directive is composed of the compiler directive indicator, optionally followed by the COBOL character space,
followed by compiler-instruction. The compiler directive indicator shall be treated as though it were followed
by a space if no space is specified after the indicator." So `>>CALL-CONVENTION` IS two tokens — whitespace
tokenization simply cannot see a boundary the standard declares. Measuring per CHARACTER finds it.

**Two false positives the first attempt produced, both caught by the sweep's own gate.**

- `I-O-CONTROL` came out as `I` + `-O-CONTROL`. A rule is drawn under the visible STROKE, so it starts inside
  the first glyph's box — 2.2 pt of side bearing on folio 333 — and a bare centre test read the `I` as
  unlined. Fixed with MIN_CHAR_OVERLAP = 1.0 pt, which separates the two measured populations cleanly: 2.2 pt
  of bearing (underlined) against 0.0 pt for the `>>` the rule stops at (not underlined).
- Then `AREAS` came out as `AREA` + `S`, `RECORDS` likewise, and §13.14.2's three clause names collapsed to
  `code-claus`, `e`, `e`, `e`. Those are **bracket FEET** — 3.7 pt rules flanking the word, which this file's
  own header warns cannot be told from underlines by width. A foot can never cover 55% of a word, but it
  trivially covers ONE GLYPH, so per-character measurement is dangerous in a way per-word measurement is not.
  ⛔ The guard: per-character measurement may only ever **TRIM** a word the per-word test already accepted,
  against only the rules that accepted it. Promotion is now impossible by construction.

**One gate bug fixed in the same pass, and it was pointing the wrong way.** `sweep_figures.words_of` replaced
every tag with a SPACE, so `>><u>CALL-CONVENTION</u>` tokenized as two words against a markdown form whose
RENDERED TEXT IS IDENTICAL. A gate reporting a spurious difference hides real ones behind it. `<u>` is
zero-width — underlining never introduces a word boundary — so it is now stripped to nothing while every other
tag still becomes a space. With that, the five real changes moved from "words differ, refused" into
"replaceable", which is what let them land at all.

**Result:** 43 figures regenerated, `--check` clean, zero `<u>>>` left. Proved the generator is what holds the
repair by stashing the three script changes and re-running against the FIXED markdown: the old code
regenerates the defect on all five. Gates: lint CLEAN · sweep 484/484 · publishable 47,195 · acknowledgment
verbatim · §8.9 == printed.

