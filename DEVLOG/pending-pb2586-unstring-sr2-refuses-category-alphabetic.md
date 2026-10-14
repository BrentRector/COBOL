## PB2586: UNSTRING SR2 refuses category alphabetic (PIC A), and admits the slice of a numeric item

ISO 14.9.48.3 SR2 (`cite.py --check` OK): "Identifier-1, identifier-2, identifier-3, and identifier-5 shall reference data
items of category alphanumeric or national." Category alphabetic is a category of its own (8.5.2.1 Table 2). The binder's
screen, `StringUnstringBinder.Sr2OffendingCategory`, matched `PicCategory.Alphanumeric` on the item's PICTURE, and the
storage model also uses that member for PIC A, so a PIC A sender, delimiter operand or DELIMITER IN receiver compiled
clean. The note (filed by the PB850 landing, which found it) kept PIC A admitted on purpose as "a change of verdict for
existing source" and asked for an owner decision.

What the re-probe showed (this build, 0 warnings / 0 errors): `UNSTRING A DELIMITED BY SPACE INTO B` over `A PIC A(5)`,
`DELIMITED BY D` over `D PIC A`, `DELIMITER IN DI` over `DI PIC A` and `UNSTRING A(1:3)` over `A PIC A(5)` all compiled
clean. The decision question does not arise: SR2 is a syntax rule, not implementor latitude, so CLAUDE.md rule 1 (ISO
controls) decides. The corpus exposure is zero: no program under tests/ holds both an UNSTRING and a PIC A item, so no
golden changed verdict. SR4's identifier-4 (the INTO receiver) admits category alphabetic and is untouched; STRING has no
category rule (14.9.43.3 SR1 is a usage rule).

Fix shape: `Sr2OffendingCategory` takes the PLACE and the elementary arm asks the one category predicate
`ItemCategory.IsAlphanumericOrNational(Place)` (the group arms stay: strongly-typed group, bit group). Two sibling arms of
the same fold turned up in the file and were fixed in the same change:

- The reference-modified arm, in the opposite direction. The old screen read the whole item, so `UNSTRING NUM(2:3) ...`
  over `NUM PIC 9(5)` was refused as "category numeric" although 8.4.3.3.4 GR6 c) makes the unique data item category
  alphanumeric: legal source rejected. A slice of a PIC A item stays alphabetic (GR6's list names no rewrite for it,
  adjudicated in PB73) and is now refused.
- The function arm: `FUNCTION CONCAT` over arguments that are all PIC A returns an alphabetic value (15.18.4 r3), carried
  as `ResultIsAlphabetic`, and passed `UnstringSenderCategory`.

Goldens: five negatives `pb2586-unstring-alphabetic-{sender,delimiter,delimiter-in,slice-sender,function-sender}` (one per
dispatch arm, all `reject-at: 2023`, COBOLNET1651) and the positive `2023/pb2586_unstring_sr2_category_operands` (a numeric
slice as sender and as delimiter, a PIC A INTO receiver under SR4, a group holding a PIC A leaf as sender). Row
SR-14.9.48.3-2 was already CONFORMS with the alphabetic fold as a recorded caveat; it was re-recorded (six witnesses added,
caveat closed), so the inventory GAP stays 103. No diagnostic code was needed (COBOLNET1651 is the rule's code).

Self-review (architecture, full code, performance, duplication): one predicate now answers the elementary category
question for all four SR2 operands; the doc comment that recorded the fold as a limitation was replaced; no allocation was
added on a hot path (bind time only). GnuCOBOL accepts PIC A here; that leniency is not made an extension because the
precedence puts the standard first and no owner decision registers one.

Branch review round 1 (R1-1): the CONCAT rider (`IntrinsicBinder.IsAlphabeticArg`) still read PB72's reversed reading, so
`UNSTRING FUNCTION CONCAT(A(1:2) A(3:3))` over a PIC A item was admitted while `UNSTRING A(1:5)` was refused, and
`MOVE FUNCTION CONCAT(A(1:2) A(3:2)) TO a-boolean` passed Table 16 beside the refused `MOVE A(1:2) TO` it. The arm now asks
the one reader `Table16Operand.Of(Place).IsAlphabetic` (an alphabetic slice stays alphabetic, section 8.4.3.3.4 GR6's
exhaustive exception list, PB73). Goldens: negatives `pb2586-unstring-alphabetic-function-slice-sender` (COBOLNET1651) and
`pb2586-move-concat-alphabetic-slices-to-boolean` (COBOLNET0819), and golden leg T5 (a CONCAT mixing a PIC A slice with a
PIC X slice is alphanumeric, a legal sender). Sibling found, not fixed here: `IntrinsicArgumentRules.ClassOfPlace` still
classes a PIC A slice as class alphanumeric (a function-argument class screen), a lead for a note.
