## Entry 041 — 2026-03-14: Record Layout Builder — Byte-Accurate COBOL Storage

User provided the record layout pass design: DataSymbol + PicLayout + UsageKind → IrRecordType
with concrete byte offsets. This is what makes COBOL storage bit-accurate in CIL.

**RecordLayoutBuilder.Build(DataSymbol)** walks the data hierarchy top-down, computing offsets:
- Elementary items get size from PIC/USAGE rules
- Groups span their children
- REDEFINES shares offset with target (record size = max of variants)
- OCCURS multiplies element size (placeholder for DEPENDING ON)

**Storage size rules:**
- DISPLAY numeric: length + sign byte
- Alphanumeric: length bytes
- COMP/BINARY: 2/4/8 bytes by digit count
- COMP-3: (digits+2)/2 bytes (packed with sign nibble)
- COMP-1/COMP-2: 4/8 bytes (float/double)

**IR type mapping:**
- Alphanumeric → ByteArray
- Numeric DISPLAY with fractions → Decimal
- Numeric DISPLAY integer-only → Int32 or Int64
- COMP/BINARY → Int32 or Int64
- COMP-3 → Decimal

This enables the CilEmitter to use ExplicitLayout with FieldOffset for each IrField,
giving bit-accurate COBOL storage in .NET.

**Remaining gap:** The binder pass that walks the parse tree with resolved symbols and
produces IrModule (methods + records + instructions). This is the last bridge before
end-to-end compilation produces running .NET code.

---

