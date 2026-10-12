## Entry 417 — S4: signed / scaled numerics → a typed .NET `decimal` (byte-identical)

The second numeric representation lands: a standalone elementary **signed or scaled** numeric item with a VALUE
(DISPLAY/COMP/BINARY, ≤18 digits) flips to a native .NET `decimal` — exactly the complement of the unsigned-integer
`long` slice. One shared predicate, `IrTypedFieldLocation.IsDecimalRepresented(pic)` (`IsSigned || fraction || P-scale`),
decides the representation so the Binder's field-type choice and every emit cell stay in lock-step;
`IrTypedFieldDef` gained `IsDecimal`/`DecimalInit`.

Byte-identity strategy = lean on the byte codec, never re-derive its rules:
- **VALUE init** and **MOVE-literal**: round-trip the literal through the destination codec at COMPILE time
  (`EncodeNumeric`→`DecodeNumeric`) → the exact stored decimal (sign + implied-decimal scale + truncation applied
  identically to the byte path), then `EmitLoadDecimal`.
- **DISPLAY**: materialize the decimal to its byte image and call the byte path's own `GetDisplayString` — so sign
  overpunch (`1.50`→`0015{`, `-42`→`0004K`) and implied-decimal scaling match exactly, without re-implementing them.
- **COMPARE / arithmetic**: already flow through the usage-agnostic materialize path; made the materialize load a
  `decimal` field directly (no `long`→`decimal` widening) and the receiver epilogue store the decoded `decimal`
  directly (no `decimal`→`long` narrowing).

Verified end-to-end with `--typed-fields`: `_T_WS-A/B/C/N` emit as `Decimal`, and a probe over VALUE/DISPLAY/ADD/
COMPUTE/COMPARE/MOVE-literal is byte-for-byte identical to the byte path. New flip test
`SignedScaledNumeric_FlipsToTypedDecimal_AcrossOps_ByteIdentical` — 14 flip tests, all flag-on≡flag-off. Field→field
MOVE of a decimal is deliberately **loud-guarded** (deferred — it needs the dst-codec encode/decode round-trip).
Guard **ALL GREEN: 1196 unit / 495 integration / 364 NIST**. NEXT: decimal field→field MOVE (the unified
encode/decode round-trip, retiring the guard), then OCCURS → `CobolTable<T>`.

