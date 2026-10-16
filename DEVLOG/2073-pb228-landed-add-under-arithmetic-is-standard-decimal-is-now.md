## Entry 2073 — 2026-10-16 07:51 PDT — PB228 landed: ADD under ARITHMETIC IS STANDARD-DECIMAL is now pinned in all three formats, and the SUBTRACT twins' grouping too

kb/Work PB228 (wave 1091 I) was an evidence gap. No golden ran an ADD under `ARITHMETIC IS STANDARD-DECIMAL`, so the
second paragraph of §14.9.2.4 GR1, GR2 and GR3 had no evidence. That paragraph is the standard-decimal equivalence and
the operand order it fixes, with GIVING's TO operand last. Native addition is exact, so this order cannot be seen
under native arithmetic. It shows only when every addition rounds to an SDIDI's 34 digits (§8.8.1.5.2, §8.8.1.5.3,
NEAREST-AWAY-FROM-ZERO implied by §11.9.11.2 GR3a).

**Re-probe.** On this tree the compiler already gave every required value. NumericRenderer's single shared
`if (StandardDecimal)` branch is correct for ADD TO, ADD GIVING and ADD CORRESPONDING, and BindAdd appends the TO
operand after the operand list, as GR2 requires. No compiler code changed. One design trap is worth recording. The
obvious probe uses a 34-digit PICTURE, and COBOLNET0801 refuses it because ISO caps fixed-point items at 31 digits. The
goldens therefore make the order visible in two other ways. The first is cancellation: X = 10**30, Y = -(10**30) and
Z = 0.0004, where X + Z rounds back to X. The second is a carry: a 35-digit pair sum rounds up into the receiver's
units digit. These 35-digit composites are legal only because §14.7.7 GR2's 31-digit composite cap is a native-only
rule. Without the OPTIONS paragraph the same statements stop on COBOLNET0805.

**Goldens.**
- `2014/pb228_add_standard_decimal` (PB228ASD) covers GR1 and GR2. `ADD X Z Y TO R1` gives 0.0000, where native gives
  0.0004. `ADD X Y Z TO R2` gives 0.0008, which only "(initial-evaluation + identifier-2)" produces; folding the
  receiver in first gives 0.0004. The GIVING legs give 0.0000 and 0.0004 by operand order, and with operand-2 last
  `ADD X Z TO Y GIVING` gives 0.0000.
- The same golden covers GR3. An ADD CORRESPONDING pair stores ...0005, where exact native arithmetic stores ...0004.
- `2002/pb228_add_corresponding_values` (PB228ACN) gives GR3/GR5 value evidence at COBOL-2002. Until now GR3 had value
  evidence only at 85 and 2014.
- Negative `pb228-add-standard-decimal-below-2014` is rejected at 85 and 2002 with COBOLNET0900.

**Sibling sweep.** The same gap existed on the SUBTRACT arm. §14.9.44.4 GR1/GR2 were CONFORMS, but their grouping was
pinned only by a probe and by values that are exact in every order. `2014/pb228_subtract_standard_decimal_order`
(PB228SSD) now gives 0.0008 for FROM and for GIVING, where native and left-to-right evaluation give 0.0004. The
printed `(operand-1 - operand-2)` contradiction in SUBTRACT CORRESPONDING is already resolved on GR-14.9.44.4-3 and
was left alone.

**Rows.** Batch `w1091i-PB228` moves GR-14.9.2.4-1, -2 and -3 from PARTIAL to CONFORMS and adds witnesses only to
GR-14.9.44.4-1 and -2. GAP goes from 97 to 94. PB228 is landed with those three rows in `closes_rows`.

