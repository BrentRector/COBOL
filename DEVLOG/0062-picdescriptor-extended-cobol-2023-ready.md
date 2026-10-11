## Entry 062 — 2026-03-15: PicDescriptor Extended — COBOL 2023 Ready

Extended PicDescriptor with ISO-complete fields for sign storage, editing,
P scaling, and display options:

- **SignStorageKind**: None, LeadingSeparate, TrailingSeparate, LeadingOverpunch, TrailingOverpunch
- **EditingKind**: None, ZeroSuppress, Currency, CreditDebit, Custom
- **LeadingScaleDigits / TrailingScaleDigits**: P scaling (implied powers of 10)
- **BlankWhenZero**: BLANK WHEN ZERO clause

PicRuntime encode/decode updated to use new fields:
- EncodeDisplay: P scaling, separate sign positioning, BlankWhenZero
- DecodeDisplay: P scaling, BlankWhenZero
- FormatNumericEdited: new method for formatting into edited pictures
  (zero-suppress, currency, CR/DB)
- MoveNumericToNumericEdited: now uses FormatNumericEdited

PicDescriptorFactory updated to populate new fields from DataSymbol.
CilEmitter EmitLoadPicDescriptor passes all 14 fields to constructor.
Single constructor on PicDescriptor — no backward-compat overloads needed.

35/35 category tests still pass. NC101A: 241 lines, 21/60 — unchanged
(behavior-preserving refactor).

---

---

