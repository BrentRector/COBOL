## Entry 1291 — 2026-08-17 21:38 PDT — PB74: the SDIDI final transfer's masked overflow — ON SIZE ERROR now fires under STANDARD-DECIMAL

The second of entry 1289's three registered siblings, landed with its own goldens. Under STANDARD-DECIMAL,
`COMPUTE X5 = 10 ** 100 ON SIZE ERROR … NOT ON SIZE ERROR …` into `PIC 9(5)` ran the NOT branch and stored 0;
`10 ** 40` likewise; `10 ** 37` into `9(5)V99` too; `FUNCTION NUMVAL-F("1E+40")` — now an exact SDIDI after
entry 1289 — took the same road. §14.7.5 case 3 makes the size error exist ("further from zero than permitted
for the associated resultant data item"), storing rule 1 keeps the receiver unchanged, and no-phrase rule 4 names
the condition EC-SIZE-TRUNCATION.

**Mechanism.** `CobolDec.ToUnscaled`'s widening arm — "keep only digits a ≤38-digit store could ever use" — did
`sig %= 10^max(0, 38 − shift)` and returned 0 when nothing survived; `CobolNum.TryStore(CobolDec)` then
capacity-checked THAT 0, passed it, and stored it. The emitter's checked numeric-edited transfer
(`DecToUnscaled` → `EditTryFormat`) saw the same 0. The unchecked half is right — a MOVE or a no-phrase store
keeps low-order digits, the same high-order truncation the store applies to a value that overflows its picture —
so the fix is a CHECKED sibling, not a change to the unchecked one: `ToUnscaledChecked` raises `CobolSizeError`
EC-SIZE-TRUNCATION for an out-of-carrier magnitude (no fixed-point receiver can hold what Int128 cannot);
`TryStore(CobolDec)` and the emitter's checked edited path (`RuntimeApi.DecToUnscaledChecked` in `Aligned(true)`)
ride it, one shared `ToUnscaledCore` beneath both. Sweep while there: the three PROHIBITED raises
(`Round34Wide`, `RoundFromRemainder`, `CobolNum.RescaleChecked`) threw `CobolSizeError` with the DEFAULT name, so
under `>>TURN EC-SIZE CHECKING ON` a `FUNCTION EXCEPTION-STATUS` after a PROHIBITED inexact transfer to an edited
receiver read EC-SIZE-OVERFLOW (probe p14: `EDITED-PROHIBITED EC=EC-SIZE-OVERFLOW`, `INTERMEDIATE-PROHIBITED
EC=EC-SIZE-OVERFLOW`) where §14.7.4.3 r7 and §11.9.11.2 r3d say EC-SIZE-TRUNCATION; each now names it.

**After.** Goldens `pb74_sdidi_store_size_error` (10 rows — 10¹⁰⁰ / 10⁴⁰ / 10³⁰ / 10³⁷-into-V99 / an edited
receiver / NUMVAL-F("1E+40"), each SE with the receiver's prior contents printed unchanged; a fitting `10 ** 4`
taking NOT ON SIZE ERROR; PROH and ECOV reading EC-SIZE-TRUNCATION) and `pb74_sdidi_intermediate_prohibited_ec`
(3 rows — INTERMEDIATE ROUNDING IS PROHIBITED: `2 / 3` latches EC-SIZE-TRUNCATION, `2 / 4` is exact) both
matched their spec-derived `.out` first run; unit: two PB74 tests in `CobolDecRoundingTests` (the checked vs
unchecked transfer, `TryStore(CobolDec)` false with the receiver untouched). Gates: Conformance 852/852
(Arithmetic|Corpus|Numeric|Standard|Rounded|Size|Exception|SpecTraceability|Edited) · Unit 43/43 (the two Dec
classes) · characterization 33/33. `COBOLNET_NUMERIC_DESIGN.md` D3 records the checked/unchecked pair and the
level-3 names. The no-phrase, checking-off store of 10¹⁰⁰ still stores 0 — the implementor's §14.6.13.1.3 #8
disposition, the low-order digits, the same as the Int128 path's — and PB75 (the raw-crash disposition of an
SDIDI overflow OUTSIDE an arithmetic statement) is untouched by this: it is the EC model's, not the transfer's.

