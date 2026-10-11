## Entry 509 — COBOL.NET: a signed numeric → alphanumeric drops its sign (ISO §14.9.25.4 GR6a / §8.8.4.2.5)

The OCCURS image facility (Entry 508) let NC211A reach GF-23, which then FAILed: `MOVE WRK-DS-01V00 TO IF-ELEM(i)`
(WRK-DS-01V00 is `PIC S9`, +1) emitted the zoned overpunch image "A" instead of the digit "1", so the table image was
"ZA WA 1" not "Z1 W1 1". This is the known-latent the resume prompt flagged. The spec is decisive: **§14.9.25.4 GR6a**
— "if the sending operand is described as being signed numeric, the operational sign is not moved; if the operational
sign occupies a separate character position, that character is not moved" — so a signed numeric moved to an
alphanumeric receiver transfers its **de-signed magnitude digits**. A numeric-vs-alphanumeric comparison follows the
same rule: **§8.8.4.2.5** says the numeric is "treated as though it were moved, according to the rules of the MOVE
statement, to an elementary [alphanumeric] data item."

**Implementation.** `OperandText.AsString`/`FieldAsString` gained a `deSign` flag: a signed numeric source renders via
`CobolNum.FormatUnsignedDisplay(value, digits)` (the magnitude digits) instead of `FormatDisplay` (the sign-aware
zoned image). `FormatDisplay` already yields the magnitude for an unsigned item, so the flag is a no-op there. It is
set at exactly two sites — the MOVE-to-alphanumeric receiver (`ConvertSource`) and the alphanumeric (string) branch of
`RenderRelational` — and left UNSET for DISPLAY, which shows the sign-aware image (so `DISPLAY` of `S9` +1 stays "A").
A `StoreAsImage` numeric leaf de-signs by decode-then-reformat (`FormatUnsignedDisplay(ParseDisplay(img))`). The DISPLAY
emitter's `Select(OperandText.AsString)` method-group had to become an explicit lambda (the new optional parameter
breaks method-group conversion) — a useful compile-time confirmation that DISPLAY stays sign-aware.

**Verification basis (spec-to-code traceability).** The MOVE de-signing is verified by **golden + legacy + spec** (the
3 MOVE differential tests pass on both compilers, and NC211A's GF-23/28/29 now PASS against the NIST golden). The
COMPARISON de-signing is **spec-pinned**: `IF <S9(2) -5> = "05"` is `EQ` per §8.8.4.2.5 (de-signed "05" = "05"), but the
'85-era differential oracle is non-conformant to ISO 2023 here — it compares the overpunch image ("0N") and reports
`NE` — so that test asserts the spec value on COBOL.NET only (the DISPLAY-trailing-trim precedent, `feedback_use_the_spec`).
Two boundary-guard tests confirm the de-sign is scoped to the alphanumeric branch ONLY: a signed numeric vs a NUMERIC
literal stays algebraic (`-5 ≠ 5`, `-5 = -5`; §8.8.4.2.1). `SignedAlphanumericMoveDifferentialTests` (+6). Conformance
266 → 272; 14 unit; the 7 green NC golden programs unaffected. Greenfield-only.

NC211A advanced from diff-39 to diff-21 (GF-23/28/29 PASS). It now FAILs only on three remaining, distinct features:
`ALL "literal"` (repeating a multi-character literal — FIG-TEST-1 expects "ABCABC", gets the raw `ALL"AB`), a
`MIXED CONDITIONS` case at GF-48, and NEXT SENTENCE — the next slices toward greening NC211A.

