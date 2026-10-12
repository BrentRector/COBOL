## Entry 1289 — 2026-08-17 21:23 PDT — PB60: the SDIDI Dec branch — the NUMVAL family's standard-mode value is the one scan lifted exactly

**Where the session started.** `work.py next` put PB60 first with three rows left and a frozen apply plan; §0 said
no battery was owed. Spec first (`cite.py --check` on all four): §15.4.1 places a numeric function's returned
value, under a standard arithmetic mode, "in a temporary standard data item in the intermediate form defined for
the arithmetic mode in effect" — the SDIDI of §8.8.1.5.2, 34 digits; §15.67.4 r1 fixes the value as "the numeric
value represented by argument-1"; and §15.69.4 r3 says it outright for NUMVAL-F ("If standard-decimal arithmetic
is in effect, the returned value is the numeric value represented by argument-1", where r2 grants native and
standard-binary only an approximation). So under `ARITHMETIC IS STANDARD-DECIMAL` the value is exact, at its own
scale, in every channel — no working scale, no receiver, no ≥6/≥9 floor.

**Measured before (probe p08b, `--std 2023`, SD):** the receiver-less `DISPLAY FUNCTION NUMVAL("1.2345678")`
rendered `1.234567`, NUMVAL-C `1234.567890`, NUMVAL-F `0.001234567` — the NATIVE item-92 working scale applied
to a standard-mode program — and a 34-digit argument (legal under the r4 cap) rendered
`170141183460469231731687303715884.105727`, the Int128 saturation sentinel of a ×10⁶ rescale printed as if it
were the value. The receiver-bearing channels (COMPUTE/MOVE at 20 fraction digits, the relation) were already
right from the 08-09 threading; the defect was isolated to the channels no receiver reaches.

**The fix — the one-scan makes it a projection.** `CobolIntrinsics.NumvalDec / NumvalCDec / NumvalFDec` lift
`NvScan`/`NvfScan`'s (sign, unscaled, frac[, exp]) to `CobolDec` at the parsed scale; NUMVAL-F's E-exponent goes
through a new `CobolDec.FromParsed(sig, exp, mode)` — the same `Round34`/`Clamp` funnel every SDIDI result uses,
because a 4-digit exponent can leave decimal128 (`1E+9999` ⇒ EC-SIZE-OVERFLOW; a ≤34-digit significand never
rounds). The three reject branches were EXTRACTED (`NumvalReject` / `NumvalCReject` / `NumvalFReject` /
`NumvalCInvalidCurrency`) so the native and standard-mode projections share one message per family instead of a
copy each. `IntrinsicRenderer.RenderDec` gained the three arms and now routes the NUMVAL family UNCONDITIONALLY
under the standard modes, beside the four inexact-EAE functions (the argument is a string — no carrier question);
the digit cap is still passed explicitly, exactly as the TEST- twins receive it. The native `case "Numval"` arms
are untouched and their comment now says so.

**After:** `1.2345678` / `1234.5678901234` / `0.00123456789012345` / the 34 digits exact / `NUMVAL-F("1E+40")` =
1 and forty zeros. Golden `pb60_numval_standard_decimal` (19 rows — the three receiver-less channels, N34, F40, the
parsed-scale text form `1.500`, the sign forms, the shared reject projections `-12-`/35 digits ⇒ 0, and the SDIDI
value through relation, MOVE and COMPUTE at 20 digits, as a MAX argument, in an SDIDI sum, as a subscript, MOVEd
to alphanumeric and to a numeric-edited receiver, and as an EVALUATE subject) matched its spec-derived `.out` on
the first run. Unit: four NUMVAL tests in `CobolIntrinsicsDecTests`, including the native-agreement theory (Dec at
the native working scale is digit-identical to the native projection on the shared domain) and the reject-parity
theory. Gates: Conformance 889/889 (Intrinsic|Corpus|Arithmetic|Numeric|SpecTraceability|Standard) · Unit Dec
tests + the intrinsic drift filters green · characterization 33/33 · SpecTraceability 10/10 · CLI probes.
Inventory: RV-15.67.4-1 → CONFORMS and RV-15.69.4-3 → CONFORMS (GAP 4145 → 4143). Docs: `COBOLNET_NUMERIC_DESIGN.md`
D3 gains the NUMVAL-under-standard-modes paragraph (and loses a stale "staged LOUD COBOLNET0899" sentence PB56 had
already retired), CONFORMANCE.md item 92 bounds its working-scale determination to NATIVE, the apply plan marks
its section 1 landed, PB60 sits at 11 of 13 with RV-15.69.4-2/-3 now LISTED among its rows (PB56 had reassigned
them "to PB60" without PB60 listing them — the register hole rule 8 warns about).

**Three siblings surfaced by the probes — registered, not folded in (the owner's standing priority is fewer bugs,
and each is a distinct mechanism):** **PB74** — under SD `COMPUTE X5 = 10 ** 100 ON SIZE ERROR …` runs NOT ON
SIZE ERROR and stores 0: `CobolDec.ToUnscaled`'s widening arm zeroes an out-of-carrier value ("keep only digits a
≤38-digit store could use") and `TryStore(CobolDec)` capacity-checks that 0 (§14.7.5 case 3 / storing rule 1);
NUMVAL-F(`"1E+40"`) into 9(5) takes the same path. **PB75** — an SDIDI range overflow OUTSIDE an arithmetic
statement (`IF 10 ** 100000 > 5`, `DISPLAY FUNCTION NUMVAL-F("1E+9999")`) escapes as a raw CLR `CobolSizeError`
stack trace, exit 127, where §14.6.13.1.3 / §14.6.12 want the abnormal-termination surface — `ProgramTable.RunMain`
catches every fatal family but this one. **PB76** — `COMPUTE R9 ROUNDED = 10 ** -20` under SD stores 0.000000001:
`DivRemPow10`'s far-below-precision marker `(0, 1, 2)` reads as an exact half-tie, so NEAREST rounding lifts a
sub-precision value to one unit (and the marker's sign is wrong for negatives). PB76 and PB74 land next, in that
order; PB60's remaining native NUMVAL-F row (RV-15.69.4-2) follows.

**Friction, honestly:** the Bash cwd resets between calls in this environment, so a relative `cd tests/…` after a
scratchpad probe silently failed and my first golden write went nowhere — absolute paths from here on. Two of my
own probe legs were my errors, not the compiler's (`VALUE "ABCDE"` on an OCCURS item initializes EACH occurrence,
so `EL(3)` = "A"; `"  -0.5CR  "` carries two signs and is malformed) — worth recording because both looked like
findings for a minute.

