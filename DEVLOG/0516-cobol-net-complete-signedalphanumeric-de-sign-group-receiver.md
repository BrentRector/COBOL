## Entry 516 — 2026-06-09 15:36 PDT — COBOL.NET: complete signed→alphanumeric de-sign — group receiver + REDEFINES-canonical source (ISO §14.9.25.4 GR6a)

Targeting the closest mismatch (NC116A, diff 34), its 5 GF-14 / GF-17 failures were signed→alphanumeric MOVEs that
kept their sign (`+91275`, `8036-`, `R127`, `8362A`). DEVLOG 509 added the de-sign (drop the operational sign,
§14.9.25.4 GR6a / §8.8.4.2.5) but only for the elementary-numeric and StoreAsImage source paths into an elementary
receiver. Two gaps in the SAME rule remained, both confirmed by repro:
- **RedefViewPlace source** (`OperandText.FieldAsString`): a signed numeric that is the *canonical* of a REDEFINES is
  stored as its sign-aware character image and read through a `RedefViewPlace` window; that branch returned the raw
  window, ignoring `deSign`. It now decodes and re-emits the magnitude digits (`FormatUnsignedDisplay(ParseDisplay…)`),
  the same de-sign the StoreAsImage branch already did — the singular pattern, one rule, every storage shape. (This is
  NC116A's GF-14: `DS-LS-5`/`DS-TS-4`/… are all REDEFINED by a group, so they resolve through a view.)
- **Group receiver** (`EmitGroupMove`): §8.8.4.1 treats an alphanumeric group as elementary alphanumeric, so a signed
  numeric → group also drops its sign; the group-move path wasn't passing `deSign:true`. Fixed.

NC116A went 34 → 10 (the four GF-14 cases now pass); its sole remaining failure is GF-17 (PRECEDENCE OF SUBORDINATE
SIGN CLAUSE — a *separate* data-description feature: a subordinate `SIGN LEADING SEPARATE` must override an ancestor's
`SIGN TRAILING` and store the sign in its own byte position for a REDEFINES to read; the next slice). No NC program
greened by this fix alone, but it advanced NC116A and is correct/complete for the whole de-sign rule.

**Tests — SPEC-ANCHORED** (`SignedAlphanumericMoveDifferentialTests` +2): the REDEFINES-canonical source case
(spec + legacy agree) and the group-receiver case (**SPEC-ONLY** — the legacy oracle is non-conformant for a group
receiver: it copies the raw overpunch image `04N` rather than de-signing, the DISPLAY-trailing-trim precedent). The
result is derived from §14.9.25.4 GR6a / §8.8.4.1. Conformance 292 → 294; 14 unit; no NC regressions (sweep held 15
green). Greenfield-only.

NOTE: the earlier WSL (Linux) greenfield builds had contaminated the shared `obj/bin`, so a Windows incremental
`dotnet test` briefly reused a pre-edit compiler — a clean rebuild resolved it. Don't interleave WSL/Windows builds in
the same tree without a clean (`reference_wsl_linux_repro`); CI is unaffected (fresh per-platform checkout).

