## Entry 629 — 2026-07-06 15:38 PDT — Phase 5: FUNCTION TRIM (§15.96) — LANDED COMPLETELY (all forms + the 2023 edition gate)

TRIM is the one §15 intrinsic whose argument list carries a phrase keyword, so it needed a bespoke bind path —
and it is implemented in FULL, not scoped down. (The owner's standing correction this session: *every feature is
worth doing well and must be done well — this is a production, decades-maintained compiler.* I had started to
stage TRIM's argument-2 form as "residue"; reversed — it is landed.)

- **The LEADING/TRAILING phrase** — a bare `SUB_IDENTIFIER` segment in the SUBSCRIPT-mode token stream, extracted
  in the new `BindTrim` (space- OR comma-separated both work) onto `BoundIntrinsicCall.TrimMode`
  (0 = both, rule 3; 1 = LEADING, rule 1; 2 = TRAILING, rule 2). The remaining segments bind as ordinary operands.
- **The default-space form (§15.96.4 rule 3.a)** — no argument-2 ⇒ delete spaces. `CobolIntrinsics.Trim(string,
  long mode, params string[] chars)`; empty char set ⇒ space.
- **The argument-2 char-set form (§15.96.3 rule 2)** — one-or-more single-character delete arguments; the delete
  set is each argument-2's character (variadic; `Trim(s, mode, "0", …)`). `TRIM("0042" LEADING "0")` ⇒ `42`.
- **The EDITION GATE done right** — argument-2 is a **2023 enhancement** (Annex E.3.3 item 31 / VCR row 74: 2014
  TRIM removed only spaces); so a custom argument-2 below 2023 is rejected by name+edition (COBOLNET1502), while
  the space-trimming form binds from 2014. `BindTrim` gates only the argument-2 leg, keeping the 2014 space form
  live.

Existing `DeferredFunction_InWindow_FailsLoud` retargeted off TRIM (now implemented) to FORMATTED-CURRENT-DATE
(still Deferred). Golden `tests/conformance/2023/intrinsics_trim.cob` (both/leading/trailing + the argument-2
"0"), **GreenfieldOnly** — the frozen legacy trims only spaces (`ZERO=0042` vs the greenfield `ZERO=42`). +5
`IntrinsicFunctionDifferentialTests` (the three space forms at 2014, the 2023 argument-2, the 2014 gate + the
2014 space form still binding). Battery: **1881 conformance (+6) · 216 unit · legacy integration 61 GREEN**;
greenfield-only (catalog/renderer/runtime + the TRIM bind path). Phase-5 intrinsics now live: CONCAT, BASECONVERT
(628), TRIM (629); residue = FIND-STRING/SUBSTITUTE/CONVERT (keyword-arg forms) + MODULE-NAME/SMALLEST-ALGEBRAIC
+ the 2014 date family, each to be done in full when reached.

