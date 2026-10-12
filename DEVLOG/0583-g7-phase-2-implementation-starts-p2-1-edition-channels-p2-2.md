## Entry 583 — 2026-07-03 15:52 PDT — G7 Phase-2 IMPLEMENTATION STARTS: P2.1 edition channels + P2.2 validator pass + P2.3 the 0900 band

**Roadmap Phase 1 begins (autonomous drive, owner: "start and run as far as you can").** First checkpoint =
the validator INFRASTRUCTURE, exactly per the in-repo plan (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation
plan" P2.1–P2.3):
- **P2.1 channels + policy seam** (`Binding/EditionContext.cs`): ctor `(int dialectLevel, bool permissive =
  false)`; `Permissive` (the §10 #1 axis — strict default, permissive = the documented migration mode, owner
  decision 4); `Warnings` + `Warning(code,msg)` (the non-failing channel — `Diagnostics` stays ERRORS-ONLY,
  the DEVLOG-578 blocker fixed); `HasErrors`; and THE severity seam `Removed(code,msg)` = error strict /
  warning permissive (one policy, several emit sites — feedback_singular_pattern).
- **Carriers:** `CompilerDriver.Options.Permissive`; `Result.Warnings` on EVERY outcome (all five return
  sites); CLI `--permissive` flag (orthogonal to `--std`/`--nist`; the nist⇒85 logic untouched); Program.cs
  prints warnings to stderr ALWAYS (success included).
- **P2.2 the validator pass** (`Validation/EditionValidator.cs`): derives the generated
  `CobolParserCoreBaseVisitor<object?>` (no listener is generated — ANTLR runs -no-listener -visitor); hooked
  in `CompilerDriver.Compile` between EditionContext construction and Emit with the fail-fast
  `if (edition.HasErrors) → BindError` BEFORE Emit (a removed construct may have no emit path). Skeleton this
  commit — the P2.6 gates and the P2.4 cobolWord funnel land on it next.
- **P2.3 the band** (`Validation/EditionCodes.cs`): 0900 introduction / 0901 reserved word / 0902 removed /
  0903 obsolete-archaic; 0801/0802/0873/0810/0811/0882 KEPT (tests/history pin them — they migrate onto
  `Removed()` without renumbering).
- **Tests (same commit, feedback_conformance_tests_per_feature):** `EditionContextTests` — Removed() is an
  error strict / a warning permissive (same code), Warning() never fails on either axis, 0900 fails on BOTH
  axes (introduction gating is unaffected by permissive), and the driver carries empty-but-present Warnings on
  a clean compile both ways. Unit 34/34 (5 new) + conformance 1074/1074 + full guard-fast green.
In flight in parallel: the OO re-scout workflow (3 read-only scouts + a writer regenerating the lost
oo-plan brief into `docs/COBOLNET_OO_DESIGN.md`).

