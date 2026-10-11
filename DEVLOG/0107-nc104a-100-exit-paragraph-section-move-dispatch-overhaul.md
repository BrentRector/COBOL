## Entry 107 — 2026-03-18: NC104A 100% — EXIT PARAGRAPH/SECTION, MOVE Dispatch Overhaul

NC104A (MOVE statement, Format 1) passes 141/141. Started at 10 failures, eliminated
all 10 through systematic fixes across grammar, runtime, semantic pipeline, and CIL emission.

Also implemented EXIT PARAGRAPH and EXIT SECTION (from CLAUDE.md known gaps list).

**EXIT PARAGRAPH / EXIT SECTION:**
- Added BoundExitParagraphStatement, BoundExitSectionStatement bound nodes.
- BoundTreeBuilder: extended exit statement binding for PARAGRAPH/SECTION tokens (grammar
  already parsed them).
- Binder: each paragraph now creates an explicit end block (`_paragraphEndBlock`). EXIT
  PARAGRAPH jumps there. EXIT SECTION computes section-exit return index from SemanticModel
  section-paragraph membership and emits IrReturnConst to skip remaining section paragraphs.
- Key insight: user's proposed label-based scopes assumed single-method model, but paragraphs
  are separate IrMethods. EXIT PARAGRAPH uses IrJump within the method; EXIT SECTION uses
  IrReturnConst to tell the dispatcher to skip ahead. No new IR instructions, no dispatcher
  changes, no emitter changes.
- 5 integration tests added covering nested PERFORM, PERFORM VARYING, section boundaries.

**Grammar fixes for NC104A:**
- XXXXX084 → STANDARD in NIST preprocessor (label clause placeholder).
- `dataRecordsClause`: `DATA RECORD IS name+` — obsolete COBOL-74 FD clause, parsed and ignored.
- `blankWhenZeroClause`: parser rule changed from `BLANK WHEN ZERO` (three tokens) to
  `BLANK_WHEN_ZERO` (single composite lexer token). The lexer was producing a composite token
  but the parser expected three separate tokens — they could never match.

**PicRuntime overflow fix:**
- `(long)scaled` → `decimal.Truncate(scaled).ToString("F0")` at 3 sites in PicRuntime.
- PIC 9V9(17) scales values by 10^17, overflowing Int64. Decimal holds up to 28 digits.
- Fixed in FormatNumericEdited, FormatByEditPattern, and EncodeDisplay.

**CR/DB storage length fix:**
- PicDescriptorFactory: CR and DB were not incrementing `insertionChars`.
- PIC 9(5)CR had storageLength=5 instead of 7. FormatByEditPattern produced correct
  7-char string but it was truncated to 5 during output.

**BLANK WHEN ZERO full pipeline threading:**
- The flag was dead on arrival: SemanticBuilder extracted it from the grammar, but it
  never reached the runtime PicDescriptor.
- Path: blankWhenZeroClause → SemanticBuilder → PicUsageResolver (new parameter) →
  PicDescriptorFactory → PicLayout (new BlankWhenZero property) → CompilerPicDescriptorFactory
  (was hardcoded `false`, now reads from PicLayout).
- Also moved BlankWhenZero check before EditPattern delegation in FormatNumericEdited.

**MOVE dispatch overhaul in CilEmitter:**
- Previous dispatch used `IsNumericLike()` broadly, which includes NumericEdited. This caused
  NumericEdited sources to be decoded as numeric (stripping formatting) in contexts where
  COBOL treats them as alphanumeric.
- New dispatch order:
  1. `dstCat == AlphanumericEdited`: split by `srcCat == Numeric` (convert to display then
     edit) vs everything else (raw bytes then edit).
  2. `NumericEdited → NumericEdited`: MoveNumericToNumericEdited (de-edit, re-edit).
  3. `NumericEdited → Numeric`: MoveNumericEditedToNumeric.
  4. `NumericEdited → Alphanumeric(Like)`: raw byte copy (MoveFieldToField).
  5. Generic numeric/alphanumeric rules unchanged.
- Added `MoveAlphanumericToAlphanumericEdited` dispatch (was falling through to raw byte copy).
- Rewrote `MoveNumericToAlphanumericEdited`: converts to display string, writes to temp
  buffer, then applies alphanumeric edit pattern via MoveAlphanumericToAlphanumericEdited.

**AI performance this session:**
- Good: identified the `(long)scaled` overflow pattern and swept all 3 instances at once.
- Good: traced the BLANK WHEN ZERO flag through 5 layers to find the exact break point.
- Needed correction: initial AlphanumericEdited dispatch was too broad (caught all sources
  including numeric). User provided the split-by-source-category fix.
- Needed correction: didn't initially realize NumericEdited→AlphanumericEdited should use
  raw bytes, not numeric decoding.

153 integration tests (+5 EXIT), 1 skip, all green.
NC101A 94/94, NC102A 39/39, NC103A 103/103, NC104A 141/141,
NC106A 127/127, NC116A 67/67, NC118A 30/30, NC171A 109/109, NC176A 125/125.
Total NIST: 835 kernel tests passing at 100%.

---

