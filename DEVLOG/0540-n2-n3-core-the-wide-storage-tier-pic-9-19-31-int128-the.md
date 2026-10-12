## Entry 540 — 2026-06-10 10:38 PDT — N2+N3-core: the WIDE storage tier (PIC 9(19..31) → Int128) + the FIRST bind-side edition gates (EditionContext, COBOLNET08xx)

**Waves 2 and 3-core of per-edition numerics:**
- **N2 — wide storage:** `PicInfo.IsWide` (Numeric, non-float, Digits>18 — a DERIVED property, not the design's
  stored `WidePrecision` flag: `Digits` already lives on PicInfo and a parallel flag could drift; doc-synced in
  DATA_MODEL D7) drives `ClrType` → `Int128` and the initializer; literals beyond long emit
  `Int128.Parse("…")` (`EmitText.IntLiteral` — C# has no Int128 literal form); the existing width-aware
  `Narrow()` store boundary takes wide receivers straight. A `PIC 9(31)` item now stores, computes, MOVEs and
  DISPLAYs exactly through the N1 carrier (34-digit intermediates verified; high-order store truncation per
  §14.7 verified).
- **N3-core — the first BIND-SIDE edition gates (the EditionValidator seam is now REAL):** new
  `Binding/EditionContext` — the targeted `--std` edition + bind-time rejection diagnostics in the
  **COBOLNET08xx** band — threaded `CompilerDriver → CSharpEmitter.Emit → DataBinder/StatementBinder`; a new
  `Outcome.BindError` fails the compile on any entry. Gates shipped per ISO §8.3.1.2/§13.18.40: a fixed-point
  PICTURE or literal of 19–31 digits is **rejected at `--std 85` naming the required edition**
  (COBOLNET0802 "…require --std 2002 or later"); >31 digits **rejected at every edition** (COBOLNET0801). The
  literal chokepoint covers the expression, comparison-operand, and MOVE-source paths.
- **Tests:** `WideNumericTests` (9 facts — SPEC-PINNED: the legacy is an 85-only/28-digit-decimal reference and
  cannot oracle this tier): exact 19/22/31-digit storage+arithmetic, signed separate wide, 34-digit intermediate
  truncation, and the gates BOTH ways asserted via `EditionHarness.AssertHasDiagnostic` — the diagnostic-content
  assertion's first real producer. `constructs.json` gains `pic-wide-19-digits-2002` (introducedIn 2002) — the
  matrix proves reject-at-85/compile-at-2002+ across all four editions. `CobolNetCompiler` gained a
  `dialectLevel` knob (default 85 for the differential harness).
**453 conformance + 15 unit green** (incl. all 54 NC byte-matches at 85 — the gates change nothing legal-at-85).
Remaining: N3-rest (per-edition composite-of-operands compile check; ROUNDED MODE IS + DEFAULT ROUNDED +
INTERMEDIATE ROUNDING gated 2014+ per the brief) and N4 (ARITHMETIC modes incl. STANDARD-DECIMAL — the owner's
end-to-end directive unlocks the design's owner-gate). The 4-scout brief (full spec/legacy/surface reports) is at
the workflow output `wytmlapq6` for the next session.

