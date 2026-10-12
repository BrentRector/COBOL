## Entry 042 — 2026-03-14: PIC Runtime — COBOL Semantics as a Testable Library

User's key insight: treat PIC/USAGE semantics as a **library contract** the emitter targets,
not inline IL. This keeps the emitter simple and makes the PIC engine testable in isolation.

**PicDescriptor**: canonical descriptor created from DataSymbol — the emitter never parses
PIC strings. Carries: totalDigits, fractionDigits, isSigned, isNumeric, isAlphanumeric,
hasEditing, storageLength, usage.

**StorageLocation**: binds IrField to PicDescriptor. The emitter uses this to select the
correct runtime helper.

**PicRuntime** (in CobolSharp.Runtime):
- MoveNumeric: DISPLAY numeric → DISPLAY numeric with scale/sign handling
- MoveAlpha: alphanumeric → alphanumeric with space padding/truncation
- MoveNumericToAlpha: numeric → alpha with formatting
- DecodeNumericDisplay / EncodeNumericDisplay: byte-level codec

**IrPicMove**: new IR instruction. MOVE A TO B becomes IrPicMove(srcLocation, dstLocation).
The CIL emitter lowers this to a `call PicRuntime.MoveNumeric(...)` or equivalent.

All gnarly COBOL rules (rounding, truncation, sign handling, editing, ZERO/SPACE fill)
live in PicRuntime as pure C# — unit-testable against reference COBOL compiler output.

NC101A verified: compiles successfully after all changes.

---

