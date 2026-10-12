## Entry 926 — 2026-07-19 19:40 PDT — The markdown spec's syntax DIAGRAMS are lossy: root cause, the first fix, a render helper, and a full 195-figure audit

Following the fork adjudication (925), the owner directed checking the canonical PDF, then clarified the concern
precisely: **no content is omitted from `specs/ISO_COBOL.md` — the loss is in the DIAGRAMS, and we should fix
them.** That reframes 925's incidental discovery into a work item.

**Root cause established.** The PDF's text layer is encoded with a custom font mapping and extracts as pure
mojibake — `get_text()` on any page returns runs like `'\x0c\x16\x12Ȁ\x0c\x08\x06\x03ͳͻͺͻǣʹͲʹ͵'`, and a search
for *"TURN directive shall not"* across all 1261 pages returns **zero** hits despite the sentence being plainly
present. So the markdown was necessarily produced by **OCR of rendered page images**. Prose rule text survived
that process faithfully (verified — several apparent garbles are in the printed standard too, and must not be
"corrected"). **General-format syntax diagrams did not survive**: rather than being transcribed as diagrams they
were replaced by English prose beginning `> **Figure: …`, and that prose silently drops structure.

**Mapping established:** the markdown's `<a id="page-N">` anchors correspond **1:1** to PDF pages — 1261 anchors,
1261 pages, and the anchor immediately preceding the PICTURE figure is `page-471` while the figure is on PDF page
471. Offset zero. That makes every figure mechanically checkable.

**First fix landed** (submodule `7af5cd0`), verified by rendering page 471 at 600 dpi and looking at it:
- **DECISION-CHANGING** — the Format 1 `EDITING` … `FOR` group's **choice-indicator bars** were dropped. The
  prose read *"a left curly brace containing two stacked options: NEGATIVE IS literal-2 (top) and POSITIVE IS
  literal-3 (bottom)"*, which denotes an exclusive either/or. The printed figure encloses the pair in `| |`, and
  per 5.2.6.4 bars inside braces mean *"one or more … but any single alternative only once … in any order."* So
  one FOR phrase may carry NEGATIVE, POSITIVE, or **both**.
- **STRUCTURAL** — the prose invented a second square bracket where the figure has a brace. True nesting is
  `[ EDITING character-1 { IS literal-1 / FOR {| … |} } ] …`.

The first defect is the sharpest possible illustration of why this matters: **that one dropped pair of bars
manufactured a spec ambiguity out of nothing**, which propagated into a research packet, survived two adversarial
verification lenses, was escalated to me as an owner-decision-grade fork, and was one step from consuming an owner
decision on a question the standard answers plainly. No amount of careful *reasoning* over the markdown could have
caught it — only looking at the printed page could.

**Added `scripts/render-spec-page.py`** (page N == anchor page-N → PNG at any dpi). This is now the standing
method whenever a general-format diagram is load-bearing, recorded in memory as
[[feedback_spec_diagrams_render_pdf]].

**In flight:** a 14-agent pass over all **195** prose-described figures (137 distinct pages), partitioned one
worklist file per agent per [[feedback_workflow_agent_dispatch]], grading each OK / COSMETIC / STRUCTURAL /
DECISION-CHANGING and emitting a corrected rendering for every defect. Its rollup answers the question that
decides the remediation strategy: **is the loss systematic?** If choice indicators are dropped everywhere they
occur, the transcription needs regenerating, not patching — and every prior spec-derivation that leaned on a
figure becomes suspect and needs re-checking.

