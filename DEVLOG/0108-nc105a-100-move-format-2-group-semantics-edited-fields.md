## Entry 108 — 2026-03-18: NC105A 100% — MOVE Format 2, Group Semantics, Edited Fields

NC105A (MOVE Format 2, MOVE CORRESPONDING, editing) passes 129/129 executed (3 deleted
by NIST — obsolete MOVE ALL literal TO numeric). Started at 32 failures, eliminated all 32.

**Six root causes, three loci of change:**

**1. JUSTIFIED RIGHT (F1-8):**
- Threaded from grammar (justifiedClause) → SemanticBuilder → DataSymbol.IsJustifiedRight
  → PicDescriptor.IsJustifiedRight → CIL emission → runtime MoveAlphanumericToAlphanumeric.
- Right-justified, left-padded with spaces when destination has JUSTIFIED RIGHT.

**2. Group MOVE semantics (F1-10/16/17/20/36/37/38):**
- Added PicDescriptor.IsGroup flag, set in CompilerPicDescriptorFactory for group items.
- CilEmitter guard: `if (srcPic.IsGroup || dstPic.IsGroup)` → MoveAlphanumericToAlphanumeric.
- Group items are ALWAYS alphanumeric for MOVE/COMPARE. No numeric formatting, no editing.

**3. COMP truncation by PIC digit count (F1-108/109):**
- EncodeCompBinary: added `raw = raw % Pow10(pic.TotalDigits)` after scaling.
- COBOL truncates by PIC digit count (PIC 9 → mod 10), not by binary capacity.

**4. Figurative MOVE to edited fields (F1-60/62/66/72/75):**
- MoveFigurativeToField: NumericEdited ZERO → FormatNumericEdited(0).
- AlphanumericEdited figuratives → fill source buffer with figurative byte,
  then MoveAlphanumericToAlphanumericEdited for edit pattern application.

**5. Numeric-edited formatting fixes:**
- B(15): PicDescriptorFactory now uses ParseRepeatCount for B (was pos++ only).
- Floating symbol comma suppression: FindFloatingPlacement scans the full floating
  zone including suppressed commas/Bs, placing the symbol adjacent to digits.
- Asterisk-fill: suppressed commas get '*' not space in asterisk patterns.

**6. Literal MOVE paths:**
- String literal to NumericEdited: Binder routes through IrPicMoveLiteralNumeric
  (was IrMoveStringToField raw copy).
- String literal to Numeric: CilEmitter routes through MoveStringLiteralToNumeric
  (new runtime method: writes string to temp buffer, calls MoveAlphanumericToNumeric).
- Numeric literal to alphanumeric: preserves original digit text via
  BoundLiteralExpression.OriginalText. MOVE 00000 TO X(20) → "00000" not "0".

**7. HIGH-VALUE comparison encoding (F1-67):**
- CompareFieldToString and CompareFieldToField: changed Encoding.ASCII to Encoding.Latin1.
- ASCII maps 0x80-0xFF to '?', breaking HIGH-VALUE comparisons. Latin1 preserves
  the full byte range 0x00-0xFF.

**8. CilEmitter else-fallback:**
- Changed final else branch from raw byte copy (MoveFieldToField) to
  MoveAlphanumericToAlphanumeric, which honors JUSTIFIED RIGHT.

**AI performance this session:**
- Good: traced HIGH-VALUE failure to Encoding.ASCII vs Latin1 — a single line fix
  for a subtle encoding mismatch.
- Good: identified 6 root causes from 32 failures, fixed all systematically.
- User correction needed: initial AlphanumericEdited dispatch was too broad.
- User provided the architectural breakdown into three loci of change.

---

