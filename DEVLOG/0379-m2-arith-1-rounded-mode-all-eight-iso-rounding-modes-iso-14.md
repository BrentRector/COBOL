## Entry 379 — M2-ARITH-1: `ROUNDED MODE` — all eight ISO rounding modes (ISO §14.9.4, COBOL-2002)

Closes the audit-flagged 🐛 "only 2 of 8 rounding modes implemented". The arithmetic pipeline already
threaded a `roundingMode` **int** end-to-end (IR → emit → `PicRuntime`), but only mode 0 (truncation) and
mode 1 (nearest-away) were ever produced or honored. Two concentrated changes finished it:

**Runtime (the heart of the fix).** `PicRuntime.ApplyScalingAndRounding` now routes both the fraction-scale
and trailing-P paths through one `RoundToIntegerByMode(scaled, mode)` helper covering all eight modes:
TRUNCATION (0, toward zero), NEAREST-AWAY-FROM-ZERO (1, default ROUNDED), AWAY-FROM-ZERO (2, ceil/floor of
magnitude), NEAREST-EVEN (3, banker's), NEAREST-TOWARD-ZERO (4, `sign·ceil(|x|−0.5)`), PROHIBITED (5),
TOWARD-GREATER (6, +inf), TOWARD-LESSER (7, −inf). Named `Round*` const ordinals are the single source of
truth shared with the compiler.

**Source end (per-statement `ROUNDED MODE IS …`).** New COBOL-2002 reserved words for the eight mode names
(corpus-checked — TRUNCATION appears only in comment lines, PROHIBITED nowhere); grammar `roundedPhrase :
ROUNDED (MODE IS? roundingModeName)?` replacing the bare `ROUNDED?` in the three arithmetic target rules
(receivingArithmeticOperand / multiplyByOperand / computeStore); `BindRounded` maps the mode name → ordinal;
`BoundArithmeticTarget` gains `RoundingMode`; `ArithmeticLowerer`'s seven `IsRounded ? 1 : 0` sites become
`target.RoundingMode`. The IR/emit layers needed **no** change — they were already mode-agnostic.

Verified all eight on one program (rounding 2.25 / 2.21 / 2.20 to one decimal): the three NEAREST-* modes
split the 2.25 tie 2.3 / 2.2 / 2.2; AWAY=2.3 vs TOWARD-LESSER=2.2 on 2.21; bare ROUNDED = 2.3 (= NEAREST-AWAY);
PROHIBITED stores an exact 2.20 → 2.2. Conformance `tests/conformance/2002/rounded_modes`.

**Deferred (noted in plan):** (a) the OPTIONS `DEFAULT ROUNDED MODE` clause is still parsed-not-applied —
bare ROUNDED uses the fixed NEAREST-AWAY default rather than the program default; (b) PROHIBITED currently
truncates without raising EC-SIZE-TRUNCATION when the result is inexact (the error-signaling needs the
arithmetic store paths to detect precision loss — cross-cutting plumbing); (c) ADD/SUBTRACT CORRESPONDING
keep bool ROUNDED (no MODE).

**Transparency / a self-inflicted detour.** Before realizing the implementation was correct I spent a long
bisection chasing a phantom "silently dropped COMPUTE statement" — an 11-statement test showed only 10 lines.
It reproduced on a stashed clean `main` too, which (wrongly) convinced me it was a pre-existing compiler bug.
Root cause: every "failing" throwaway program reused `PROGRAM-ID. ROUNDM`, and I had compiled a dozen of them
into the same temp directory. `dotnet e:/tmp/X.dll` loads the entry assembly by its **identity** ("ROUNDM"),
so the host served a *stale earlier* ROUNDM assembly from that directory — the freshly-emitted IL (verified:
no stale strings in the new dll) was never run. A unique PROGRAM-ID produced the correct output immediately.
Lesson recorded: give every throwaway compiled-COBOL test a unique PROGRAM-ID (or a clean output dir).

Guard ALL GREEN: **1047 unit / 471 integration / 364 NIST**, 0 regressions.

