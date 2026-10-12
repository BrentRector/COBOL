## Entry 1027 — 2026-07-26 18:02 PDT — The reconciliation pilot: 34 pages, 5 confirmed defects, and GOBACK was falsely restrictive in the transcription

The owner directed a systematic PDF-vs-markdown reconciliation after four extractor bugs traced back to
transcription artefacts. This entry is the harness, the pilot, and the repairs.

**The harness.** `scripts/spec/page_workunit.py` emits a self-contained unit per printed page: the page rendered
at N dpi, the markdown slice between anchor `page-N` and `page-N+1`, and a JSON manifest (source line, line count,
whether the slice holds figure content, which anchors are present). Everything an agent needs is on disk, so units
fan out with no shared index to miscount — `feedback_agent_dispatch` applied. Renders batch through one PyMuPDF
open, which matters at 1,261 pages. A missing page anchor is itself reported as a finding.

**The workflow** is compare → adversarially verify. Finders classify a discrepancy as missing-text /
incorrect-text / misplaced-content / missing-figure / incorrect-figure / missing-heading / ocr-error, with a
severity, and must quote BOTH sources verbatim so a verifier can check without re-reading the page. They are told
explicitly what is NOT a discrepancy — the page-break scaffolding is deliberate navigational furniture, not
content — because a false finding sends the next session to "fix" correct text. Every claim is then re-checked by
an agent prompted to REFUTE it, defaulting to refuted under uncertainty.

**Pilot: 34 of 1,261 pages, stratified** across figure pages (158 exist), tables (118), rule blocks (496), Annex
A.1 items (93) and front matter. 14 agents, 811k tokens, 9 minutes. **7 claimed → 5 confirmed, 2 refuted.**

The two refusals matter as much as the confirmations: the verifier threw out a claim that was pure markdown
formatting, and another where the "misplaced" content was actually present. The adversarial layer is doing its job
rather than rubber-stamping.

**⛔ NORMATIVE — GOBACK was falsely restrictive (page 661, §14.9.18.2).** The printed figure encloses its two
stacked alternatives in CHOICE INDICATORS — the pair of `|` bars just inside the outer bracket, confirmed on a 2×
crop of the printed page. The markdown rendered a plain LaTeX bracket. Per §5.2.6.4 a plain bracket around stacked
alternatives means *at most ONE of them*; with the indicators it means **zero or more, each at most once, in any
order**. So the transcription said `GOBACK` may carry the raising-phrase OR the status-phrase, when the standard
says it may carry **both**. This is precisely the failure mode recorded in `feedback_spec_diagrams_render_pdf` —
the transcription loses choice indicators, and the loss is always toward rejecting legal source. Re-rendered in the
established house style (ASCII art with the bars, plus a figure-notes block).

**STRUCTURAL — CALL Format 2 (page 619).** The outer BY-phrase brace OPENED but never CLOSED, and the ellipsis sat
inside it directly after the BY CONTENT operand brace. Per §5.2.7 an ellipsis binds to the delimiter immediately to
its left, so as drawn it repeated only the operand — meaning a CALL could never carry a second BY phrase. Added the
closing brace column, moved the ellipsis outside it.

**STRUCTURAL — Annex D character list (page 1027).** The label `Telugu:` was glued to the END of the Tamil range
line, leaving the Telugu code-point ranges with no script label at all across the page break. Anything deriving the
permitted-character table for user-defined words would mis-attribute them.

**STRUCTURAL — 18 mis-levelled headings.** Four-level `13.x.y.z` subsections transcribed at `##` instead of `###`,
making them SIBLINGS of their parent clause rather than children. 236 were already correct, so this is an anomaly,
not house style — and the agent proved it with a control case: page 491 prints `13.18.41.4 General rules` in
typography identical to page 475's `13.18.40.4 General rules`, yet the markdown renders one `###` and the other
`##`. Same PDF appearance, different markdown level. It also correctly noted the PDF does NOT distinguish these
levels typographically at all — the level comes from the dotted numbering — so my original framing of that evidence
was wrong. The sweep caught 13.18.40.4/.5/.6, which had placed General rules, Editing rules and Precedence rules
outside the PICTURE clause for any tool walking the heading tree.

**COSMETIC — page 2** dropped the standalone `© ISO/IEC 2023` body line; a doubled blank line marked the slot.

**Integrity after repair:** page anchors 1261 → 1261 (load-bearing for `render-spec-page.py`), rule catalog
unchanged at 3,469 with no parse gaps — confirming the heading promotion is level-agnostic to the extractor, as
the verifier predicted.

**What the pilot says about the full sweep.** 5 confirmed defects in 34 pages is roughly one per seven pages.
Extrapolated across 1,261 pages that is on the order of 185 discrepancies, and the figure-class ones are the
dangerous kind — a lost choice indicator does not look wrong, it looks like a narrower language. The full sweep is
~37× this run: on the order of 500 agents and 30M tokens. That is an owner decision on cost, not mine to take, and
the pilot exists precisely so the decision is made against a measured rate rather than a guess.

