## Entry 043 — 2026-03-14: DISPLAY + COMP-3 Codec — Bit-Accurate Numeric Storage

User provided exact nibble-level COMP-3 and byte-level DISPLAY codec design.
Implemented in PicRuntime as testable C# methods.

**DISPLAY numeric codec:**
- DecodeDisplayNumeric: ASCII digit bytes → decimal, handles leading/trailing +/-
- EncodeDisplayNumeric: decimal → right-justified ASCII digits with sign byte

**COMP-3 (packed decimal) codec:**
- DecodeComp3: two BCD digits per byte (high/low nibbles), last low nibble = sign
  (0x0C = positive, 0x0D = negative, 0x0F = unsigned positive)
- EncodeComp3: decimal → packed nibble pairs, sign nibble in last byte

Both codecs handle scale via FractionDigits (implied decimal point) and truncation
to TotalDigits. Unified DecodeNumeric/EncodeNumeric dispatches by usage.

MoveNumeric: decode source → encode destination. Handles cross-format moves
(e.g., DISPLAY numeric → COMP-3) through the canonical decimal intermediate.

NC101A verified: compiles successfully.

---

