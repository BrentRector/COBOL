## Entry 492 — COBOL.NET REDEFINES/RENAMES (2/n): Tier A — same-storage-type alias (one backing, pass-through views)

Step 2: wire Tier A (ISO §13.18.44; COBOLNET_DESIGN §4.2) — a same-storage-type pun (identical PIC, or
numeric-over-numeric of the same digit count). Exactly what NC101A's pass-path REDEFINES need (e.g.
`WRK-DS-12V00-S REDEFINES WRK-DS-06V06` — a S9(12) reading the same 12-digit DISPLAY long as a S9(6)V9(6), differing
only in implied scale).
- `ReferenceResolver`: a Tier-A view forwards to the canonical's ONE stored field — the `Place` carries the VIEW's
  DataItem (its own Pic/scale/profile), but the access path is the canonical's, so a numeric view reinterprets the
  shared unscaled value via its own scale for free. A RENAMES reference, or a not-yet-wired (Tier-B/C) / Rejected view,
  fails loud (never emits an invalid access).
- `FieldEmitter`: suppress a Tier-A view's stored field (one backing per area, §4.1) via a `SuppressField` predicate
  (also guards the struct initializer + AsImage/FromImage members); the view's `NumProfile` is STILL emitted (D9 — it
  carries its own PICTURE).
- 5 Tier-A differential tests (`RedefinesTierADifferentialTests`): identical-PIC write-original-read-view and
  write-view-read-original (coherence both ways — one backing); numeric-over-numeric same-digits-different-scale;
  scaled-arithmetic-into-shared-storage read as a same-storage integer; signed over-punch alias.

**Harness finding:** the first MPY-6-shaped test (with `ROUNDED`) diffed `…889`/`I` (legacy) vs `…888`/`H` (cobolnet)
— i.e. `MULTIPLY … ROUNDED` currently TRUNCATES; the ROUNDED phrase is not yet threaded into the arithmetic store
(a known G3 numeric gap, deferred per resume-prompt). Decoupled the test to an exact computation; ROUNDED is a
distinct next item on the NC101A path (NC101A's MPY tests use ROUNDED + ON SIZE ERROR). Conformance 156→161 green,
unit 13 green; legacy oracle untouched.

