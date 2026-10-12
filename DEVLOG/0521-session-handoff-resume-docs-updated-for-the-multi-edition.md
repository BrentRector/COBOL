## Entry 521 — 2026-06-09 17:55 PDT — Session handoff: resume docs updated for the multi-edition mission

Owner directive: get all session-resume docs updated so a fresh session continues from the current state to FULL
ISO-2023 AND all prior-edition (85/2002/2014) support. Updated:
- **`resume-prompt.md`** (the live kickoff): restated the MISSION as two interlocking tracks — T1 the
  version-correctness framework (`VERSION_CHANGE_REFERENCE.md` + `VERSION_TEST_MATRIX_DESIGN.md`), T2 the feature/NIST
  corpus drive; refreshed STATE to DEVLOG 520 (15 NC green, 307 conformance / 15 unit, CI fixed + greenfield gated,
  default `--standard` 2023); added the VERSION-CORRECTNESS FRAMEWORK section incl. the DEVLOG-520 key finding (the
  greenfield only ENABLES post-85 features — REMOVAL/reserved-word gating is absent, the Phase-2 `EditionValidator`
  worklist); rewrote RESUME AT into TRACK 1 (version-matrix Phase 1/2) + TRACK 2 (the NC corpus drive, NC116A SIGN
  precedence / NC219A collating / NC114M editing / NC171A Int128 / the verb backlog).
- **`CLAUDE.md`**: STATE snapshot → DEVLOG 520; added the multi-edition mission + the two version docs.
- **CORRECTION** (accuracy): the "RECEIVE is a data name in 4 NIST-85 programs" claim (advisor review / DEVLOG 519 /
  the design doc) was a GREP ARTIFACT — the matches were `SPACING-RECEIVE` (hyphenated → a `\b` word-boundary false
  match), NOT a bare-`RECEIVE` collision. The continuity sweep's zero NC breaks is consistent with no live collision.
  INV-1 class (b) (later-reserved-word collisions) remains spec-correct but is currently LATENT in the NC corpus (and
  the greenfield does not yet edition-gate reserved words anyway). Fixed the claim in `VERSION_TEST_MATRIX_DESIGN.md`.

Doc-only. Tree green (conformance 307 / unit 15; legacy guard unaffected).

