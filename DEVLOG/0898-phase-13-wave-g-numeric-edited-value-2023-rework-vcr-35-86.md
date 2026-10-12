## Entry 898 — 2026-07-18 19:10 PDT — PHASE-13 Wave G — numeric-edited VALUE 2023 rework (VCR 35 + 86); VCR 34 deferred (scout drift)

Landed the first two of the numeric-edited-VALUE cluster (ISO §13.18.63 SR6/SR11; Annex E.2 item 28 + E.3.3 item 43):

- **VCR 35 — figurative ZERO edits per PICTURE at ≥2023** (`ValueInitializer.cs`). A figurative `ZERO`/`ZEROES`
  (with or without `ALL`) on a numeric-edited item is now treated IDENTICALLY to the numeric literal zero — edited
  per the PICTURE (`$ZZ9.99 VALUE ZERO` → `$  0.00`), not the pre-2023 left-justified `0000000`. A `DialectLevel >=
  2023` branch (a value-emission difference, not accept/reject); below 2023 falls through to the historical zero-fill.
  Crucially, `BLANK WHEN ZERO` now effects the init (§13.18.63 NOTE 2 — the figurative-ZERO init is a numeric literal),
  so `$ZZ9.99 BLANK WHEN ZERO VALUE ZERO` → 7 spaces. Extracted the ONE `FigurativeKind` detector (byte-neutral
  refactor of the existing ALL-strip, shared with `FigurativeInitializer`).
- **VCR 86 — non-zero numeric literal VALUE is a 2023 capability** (`DataBinder.cs`, introduction gate). Below 2023 a
  numeric-edited VALUE required an alphanumeric edited-image literal; ≥2023 a numeric literal is edit-composed. Gated
  via the ONE `ConstructRegistry` (new `value-numeric-literal-numeric-edited-2023` row → **COBOLNET0900** below 2023).
  **⚠ SR6 spec-derivation:** SR6 exempts "the figurative constant ZERO or ZEROES **and the integer and decimal forms
  of the literal zero**" at ALL editions — so `VALUE 0`/`VALUE 0.00` are NOT gated (only a non-zero numeric literal is).
  A binder-side `IsNonZeroNumericLiteral` (no CodeGen dependency — avoids a Binding→CodeGen layering violation),
  scoped to the ITEM VALUE (line 1897), never a level-88 condition value.

**VCR 34 (SR7 class/length reject) DEFERRED — scout drift caught.** The remaining-waves scout specified VCR 34 as
`COBOLNET1570` when a numeric-edited VALUE literal's `length == pic.Length`. Spec-first review found two problems: (1)
SR4/SR5 say the literal "shall not **exceed** the size" (`<=`, not `==`) — the scout's `==` would wrongly reject a
short alphanumeric edited-image literal the spec allows; (2) the class-mismatch leg (a national `N"…"` literal on a
DISPLAY numeric-edited item) is ALREADY rejected at all editions by `COBOLNET0898` (`DataBinder.ValidateValueCategory`
line 1006). So VCR 34's genuine remaining surface is only the ≥2023 length-`<=` check for an alphanumeric edited-image
literal — a narrower, precise change deferred rather than shipping the scout's over-strict `==`. Recorded for the Wave I
review / a follow-up. VCR 36's numeric auto-supply falls out of VCR 86; its VALUE-EDITING FLAG-14 twin is Wave D/H.

**Golden.** `tests/conformance/2023/value_numeric_edited` (GreenfieldOnly — the frozen legacy gives the pre-2023
zero-fill and has no numeric-literal-on-numeric-edited path): FIG=`$  0.00` · BWZ=7 spaces · LIT0=`$  0.00` · NUM=
`$ 12.50`. Below-2023 negative is the matrix row (`VALUE 12.5` at 85/2002/2014 → COBOLNET0900). CLI-probed the
non-zero gate + the literal-zero exemption (`VALUE 0` clean at 2014).

**Blast-radius sweep (VCR 86 changes acceptance below 2023).** Swept the greenfield corpus for a numeric-EDITED item
with a non-zero numeric literal VALUE. Verified the E.3.3 item 43 introduction claim against the spec directly ("It is
now permitted to allow numeric-edited data items to be assigned values specified as numeric literals", spec :50338) —
the gate IS spec-correct. Exactly one corpus program was affected: `func_expr_arg.cob` (2014) declared
`01 WS-ED PIC Z9 VALUE 34` — genuinely non-conforming at 2014. Fixed the SOURCE (it is invalid, not a compiler bug per
[[feedback_compiler_bugs]]): WS-ED stays numeric-edited (the test exercises numeric-edited de-editing via
`CHAR(WS-ED)`) but is now initialized `MOVE 34 TO WS-ED` — compiles at 2014, byte-identical output. No other hits
(plain `PIC 9(n)`/`9V99` are category numeric, not numeric-edited).

