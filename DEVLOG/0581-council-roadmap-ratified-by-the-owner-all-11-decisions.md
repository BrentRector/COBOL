## Entry 581 — 2026-07-03 14:05 PDT — Council roadmap RATIFIED by the owner — all 11 decisions resolved; the doc is now the G7→G8 execution roadmap

**Minutes after the 580 checkpoint, the owner resolved the entire §5 decision packet in one message:** "You
already have the ISO docs: docs\\ISO+IEC+1989-2023_ for X_952804 COBOL.pdf and docs\\ISO_COBOL.md. All others,
use defaults."

- **Decision #1 (standards acquisition) = NO acquisition.** The ISO docs in hand are the authority — verified
  present at `specs/ISO+IEC+1989-2023_ for X_952804 COBOL.pdf` + `specs/ISO_COBOL.md` (the owner wrote `docs\`;
  they live in `specs/`, the private submodule — same files). Consequence: prior-edition (85/2002/2014) edges
  are adjudicated from the 2023 spec's own Annex E change ledger + the legacy inventory as interim authority,
  and the provisional-confidence markers (TYPEDEF edge, VCR Table-7 full growth, RW row 130d, 2014
  reserved-word confidence) become the PERMANENT policy rather than a wait-state — the roadmap Phase-4
  "blocked on standards acquisition" track is re-scoped accordingly.
- **Decisions #2–#11 = the council defaults, adopted as written.** Notables: JSON/XML → vendor-dialect,
  deferred post-G8, scrubbed from the ISO matrix (SSOT §16 G7 note updated — this also resolves the Q5
  JSON-XML leg; SCREEN = documented non-support per the A.4 line); --permissive = supported documented
  migration mode; strict-by-default stands; **#6 = the OO/M2 grammar grant is standing** (re-confirming the
  2026-06-11 feature-wave grant; same log + full-guard + committed-regen discipline; memory
  `never-change-grammar-without-user-approval` updated); Q13 = differential files → pinned goldens, legacy
  archived at a git tag; INV-1-strong at 2023 required for G7 exit; VALIDATE/MCS/commit-rollback/locale/
  extended-letters/A.4.8/A.4.13 = documented non-support with the ISO §4.2 conformance document at G8 Cut 3;
  the full architectural review + packaging/distribution = a separate post-G8 phase.
- **Docs updated in this change set (process rule #4):** the roadmap banner ADVISORY→RATIFIED + a §5
  ratification note; DOC_INDEX row → LIVE/RATIFIED; resume-prompt STATE → 581 (RESUME = roadmap Phase 0 →
  Phase 1); SSOT §16 G7/G8 sections gained ratified-refinement notes (JSON/XML removal, the 2023 behavioral
  leg, G8's three serial cuts + the equivalence-proof precondition).

**The plan of record is now: `resume-prompt.md` (kickoff head) → `docs/COMPLETION_ROADMAP_COUNCIL.md`
(ratified phase plan, exit criteria) → `docs/VERSION_TEST_MATRIX_DESIGN.md` "Phase-2 implementation plan"
(P2.1–P2.8 mechanics) over the SSOT.** Next session (or next in this one): roadmap Phase 0 — clean full
rebuild + baseline re-verification (1074 conformance + 29 unit + guard-fast) after the 22-day idle gap.

