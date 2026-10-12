## Entry 1072 — 2026-07-28 04:25 PDT — Session state captured for a clean resume

No code changed. This entry records where the transcription work stands so a new session can pick it up
without re-deriving it, and so the one remaining item is unambiguous.

**Written down, in the places a new session actually looks:**

- `spec-reconciliation/TRANSCRIPTION-STATE.md` — new. The gate commands with their last-known numbers, a table
  of all fifteen Annex D figures and which generator owns each, the six rules for drawing one, and an
  explicit "already closed, do not redo" list. `DOC_INDEX` points at it as the transcription starting point.
- Plan §0 — the 758 removed page rules, and **Figure D.6 named as the next item** with the reason the lint is
  red at 1 and the note that D.6 belongs in `annex_d_structure.py` (it is a page layout, not a flowchart).
- `DOC_INDEX` — rows for `strip_page_rules.py` and the three `repairs/annex_d_*.py` generators, each carrying
  the trap that makes it non-obvious.
- Memory — `green_gates_arent_evidence` and `lint_rendering`.

**The one lesson worth carrying forward** appeared four times this session, always with every gate green: a
passing check proves nothing if it was never looking at what changed. The audits compared text to the page
while the document rendered as a wall of italics. `sweep_figures --check` proves the markdown matches the
GENERATOR, so when the underline detector had a blind spot both sides shared it and agreed. A `--verify` that
compared against a hand-drawn figure became a tautology the moment `--apply` ran. A splice deleted the
standard's footnotes, and word-conservation reasoning did not apply because a figure replacement is *expected*
to change words. The practice that follows: make a check fail once before trusting it, watch a new guard fire
before relaxing it, and verify a MEASUREMENT against the raw source rather than against the document.

Two small traps recorded for whoever draws D.6: the standard prints `Figure  D.6` with TWO spaces, which
defeats a naive `Figure D\.\d+` regex; and captions sit ABOVE their figure, which may then flow onto the next
page — p1185 holds D.11's caption and chart plus D.12's caption, with D.12's chart on p1186.

State: lint RED at 1 (Figure D.6) · sweep clean 484/484 · publishable at 47,142 lines · acknowledgment
verbatim · 3,811 internal links, zero dangling.

