## Entry 531 — 2026-06-10 02:27 PDT — VERSION MATRIX PHASE 1: constructs.json + EditionHarness + the FULL INV-1 continuity sweep (342/342 clean) — first matrix catch fixes the JSON/XML grammar stubs

**Track 1 Phase 1 (the four-compilers-in-one framework) — all three deliverables landed:**
- **`tests/version-matrix/constructs.json`** — THE canonical construct catalogue (design §10 #5: the
  VERSION_CHANGE_REFERENCE tables and the future in-code registry are renderings of THIS file). 12 seeded rows:
  the 85 nucleus baseline; 2002 — ALLOCATE, FREE, INVOKE, GOBACK RETURNING, STOP RUN WITH …STATUS, BASED,
  PROCEDURE DIVISION…RETURNING; 2014 — JSON GENERATE, XML GENERATE; 2023 — DELETE FILE, TYPE IS.
  `VersionMatrixTests` now LOADS the file (inline catalogue retired) → 48 (construct × edition) cells, the
  expected outcome computed from introducedIn/removedIn, all green: every post-85 construct REJECTED below its
  introducing edition and COMPILING at it.
- **`EditionHarness`** — the ONE per-edition compile path (`Compile`/`CompileNist`/`GetDiagnostics`/
  `AssertHasDiagnostic`) every edition-targeted test shares. Diagnostic-content assertions are scaffolded; the
  negative corpus asserts compile-failure today and upgrades to edition-NAMING diagnostics when the Phase-2
  EditionValidator replaces bare grammar-gate parse errors.
- **The FULL INV-1 continuity sweep** (`scripts/version-continuity-sweep.sh`, all suites): **342 NIST programs
  compile at --std 85 AND at 2002/2014/2023 — ZERO breaks.** (117 don't compile at 85 yet — greenfield feature
  gaps, not edition issues; they join the witness set as features land.) Representative INV-1 rows per suite
  family (NC/IF/SM/IC/SQ/RL/IX/ST) locked into `VersionMatrixTests` (85-conditional: a row not yet compiling at
  85 cannot witness a break).
**First matrix catch (the framework doing its job):** the `jsonStatement`/`xmlStatement` grammar rules were
pre-seam PLACEHOLDERS — `JSON (dataReference|literal)+` — which accept NO conforming program: the
json-generate-2014 row failed at its OWN introducing edition. Replaced with the real seam-level surface (ISO 2023
§14.9 statement heads: JSON GENERATE…FROM + COUNT, JSON PARSE…INTO, XML GENERATE…FROM, XML PARSE with its
mandatory PROCESSING PROCEDURE [THRU], ON/NOT-ON EXCEPTION phrases, END-JSON/END-XML; detail phrases ride the
JSON/XML subsystem wave; the binder loud-fails BY NAME). New lexer tokens PARSE + PROCESSING are mirrored into
`cobolWord` + `_dataNameTokens` so they stay legal user words (corpus-checked — the reserved-word continuity
hazard the matrix exists to police). Also fixed the stop-run-status row's own source (`WITH NORMAL STATUS`, not
`RETURNING` — §14.9.42). **Shared front-end touched ⇒ the legacy guard re-ran: ALL GREEN (364 NIST / 1204 unit /
536 integration). Greenfield: 409 conformance + 15 unit green.** Phase 2 next: the EditionValidator +
ConstructDialectStatus registry (removal/reserved-word gating + edition-naming diagnostics) and DialectLevel
threaded into the binder (bind-side only, per the dual-backend discipline).

