## Entry 1311 — 2026-08-18 07:22 PDT — PB73 lands: a function's §15.2 type is its Table-16 row; a ref-mod view keeps its boolean or alphabetic category (PB72's erasure reversed)

**PB73 — two derivations PB72 left open (2026-08-09), adjudicated inline.**

- **Q1 — YES, a NUMERIC-typed function is Table 16's Noninteger sender.** §8.4.3.2.4 GR1 gives an intrinsic's
  temporary "the description and category specified by the definition of that intrinsic function in Clause 15";
  §15.2 item 5 gives an INTEGER function "no digits to the right of the decimal point" and item 4 gives a NUMERIC
  function only "an operational sign"; §8.4.3.2.3 SR11 states the principle ("a numeric function shall not be
  specified where an integer operand is required, even though a particular reference … might yield an integer
  value"). Table 16 splits the numeric row on that classification. `MoveBinder`'s intrinsic-sender arm now sets
  `IsNonInteger` from the ONE `IntrinsicResultType.IsIntegerOperand` reader (resolved per call — `MAX(3 -14 8)` is
  integer, `SQRT(16)` is not): strict → COBOLNET0819; `--permissive` → a warning and the former admission (the
  CONFORMANCE.md item-92 text form). Two green artifacts had held the gap open: DA2's inverted
  `NumericIntrinsicInStringContext_RendersItsValue` (re-inverted — refused strict, "1.000000" permissive) and
  `pb60_numval_standard_decimal`'s `MOVE FUNCTION NUMVAL(…) TO A20` leg (now DISPLAY of the function; the sign
  shows). Item 92 records the consequence.
- **Q2 — NO, GR2 does NOT make a display-form boolean's ref-mod view alphanumeric — and the reading reverses
  PB72's alphabetic erasure.** §8.4.3.3.4 GR2/GR3 ("operated upon for purposes of reference modification as if …
  redefined as … alphanumeric") govern the OPERATION's positions; GR6 gives the RESULT "the same class, category,
  and usage as that DEFINED for identifier-1" with an EXHAUSTIVE exception list (edited → un-edited, numeric →
  alphanumeric/national) naming neither boolean nor alphabetic — under PB72's reading every entry of that list would
  be redundant. The COBOL-85 lineage settles it (85's operated-as-alphanumeric rule listed alphabetic; its
  class/category rule did not — IBM's Language Reference still carries both sentences). GR1 gives a boolean item
  boolean positions in either form; GR5a makes them bits only under USAGE BIT. So `RefModPlace.CategoryOf` keeps
  boolean (the code was right; its NOTE now says why), `Table16Operand.Of(Place)` carries the ALPHABETIC rider
  through a view again, and `MOVE A-ITEM(1:2) TO a-boolean` — PB72's admitted RA leg, letters into a boolean item —
  is Table 16's "No" like its unsliced twin. GnuCOBOL's any-slice-is-alphanumeric reading is the `--permissive`
  leniency (`Table16Operand.Lenient`, one place for both leniencies). The first cut of this landing went the other
  way and the gate caught it in one red: `pb19_argument_class_batch5`'s `INTEGER-OF-BOOLEAN(BITS(1:6))` over a
  display-form item — legal source — refused as class alphanumeric.
- **Measured.** Golden `pb73_table16_function_type_and_boolean_view` (thirteen rows: the integer functions' text
  form, SQRT/NUMVAL into PIC 9, both boolean forms' slices into boolean / alphanumeric receivers and as
  INTEGER-OF-BOOLEAN arguments — 2 and 5 — and receiving "11"); six negatives `pb73-move-*` (numeric function →
  alphanumeric / national; bit- and display-form boolean view → numeric; alphabetic view → boolean / numeric);
  `MoveTable16FunctionTypeTests` (strict at 85/2002/2023, permissive text "4", the Integer row untouched);
  `pb72_table16_admitted_cells` loses RA. Verdicts GR-15.2-4, GR-15.2-5, GR-8.4.3.3.4-1, GR-8.4.3.3.4-6 → CONFORMS,
  GR-8.4.3.3.4-2 and SR-14.9.25.3-10 re-recorded (GAP 4011 → 4008). PB72's note carries the correction.

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|Move|Table16|Intrinsic|RefMod|Boolean|Function|Udf|Invoke|Oo|Nist|Class|Condition|Bit|Numval|Display`
**2049/2049** · Unit `Table16|Move|RefMod|Place|Intrinsic|Registry|Manifest|Guard|SpecTraceabilityInventory|Boolean`
**245/245** · CLI probes. Docs: CONFORMANCE.md item 92, COBOLNET_INTRINSICS_DESIGN.md, COBOLNET_DATA_MODEL_DESIGN.md,
kb/Work PB73 → landed, PB72 corrected, plan §0. Battery owed for the PB77 + PB73 batch with the next landing
(battery #9, tree `ac71c27a`, was ALL GREEN: Conformance 4676/4676 · Unit 4218/4218 · Characterization 33/33 ·
NIST 353/0 audit-clean · differential 0 flips).

