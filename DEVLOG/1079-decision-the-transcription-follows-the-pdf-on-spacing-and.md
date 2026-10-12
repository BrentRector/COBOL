## Entry 1079 — 2026-07-28 14:21 PDT — Decision: the transcription follows the PDF on `>>` spacing, and does not normalise

No code. Records an owner decision so it is not re-litigated.

**The question.** §7.3 SR5 makes `>>KEYWORD` and `>> KEYWORD` equivalent, and the standard prints both — 29
spaced, 42 glued. Should the transcription adopt ONE convention for consistency, letting the note cover the
alternative?

**Decided: follow the PDF.** Three reasons, weightiest first:

- **The generator would have to deviate from what it measures.** The 484 figures are generated from the
  printed page and `sweep_figures --check` gates them by comparing words against that measurement. Normalising
  means baking the deviation into the generator — blunting the check exactly where we deviate — or a post-pass
  that every future sweep re-undoes. The underline bug in entry 1078 was findable BECAUSE the generator
  reproduces the page faithfully: five figures disagreed with their own notes and the measurement settled it.
- **Two mechanisms for one convention.** Figures are generated; the syntax rules are hand prose. The
  normalisation would have to be applied in both places and kept in agreement forever.
- **Fidelity is what makes the document citable.** A reader comparing against a paper copy would find 42
  mismatches with no way to tell a deliberate normalisation from an error — the same doubt that made the
  `>>CALL-CONVENTION` underline worth reporting. The Addendum is for DEFECTS; this is a legal alternative
  spelling, not one.

The ambiguity is removed the cheaper way instead: the marked editorial note now at §7.3 names the rule, quotes
both citations (SR5 and the floating-indicator table), and says outright that neither spelling is preferred and
a conforming implementation accepts both. The reader learns something true about the standard rather than
seeing it smoothed over.

Recorded in `spec-reconciliation/TRANSCRIPTION-STATE.md` beside the measurement, so the next session finds the
decision where it would find the question.

