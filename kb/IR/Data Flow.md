---
title: IR — Data Flow (Types, Place, REDEFINES)
area: ir
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Compiler/Binding/ReferenceResolver.cs
  - src/Cobol.Net.Compiler/Binding/Model/DataItem.cs
  - src/Cobol.Net.Compiler/Binding/Model/Place.cs
  - src/Cobol.Net.Compiler/Binding/Passes/StorageFormPass.cs
  - docs/COBOLNET_DATA_MODEL_DESIGN.md
  - docs/COBOLNET_REDEFINES_DESIGN.md
tags:
  - cobolsharp
  - ir
---

# IR — Data Flow (Types, Place, REDEFINES)

COBOL data maps to **typed-native C#** — a COBOL record *is* a `record struct`, an elementary item *is* a native
field; there is no byte substrate (`byte[]` is confined to a genuine Tier-C REDEFINES or a file boundary). Design:
[[docs/COBOLNET_DATA_MODEL_DESIGN]] + [[docs/rearchitecture/DESIGN-data-model]]. See
[[kb/Architecture/High-Level Design]].

## PIC → .NET type (elementary items)
| COBOL | .NET storage |
|---|---|
| `PIC 9(n)` fixed-point, ≤18 digits | `long` holding the **unscaled** value (scale = compile-time metadata on `PicInfo`) |
| `PIC 9(19..38)` | `Int128` (`WidePrecision` flag) |
| `PIC X(n)` / alphanumeric / edited / national | `string` of exactly n chars |
| `COMP-1` / `COMP-2` | `float` / `double` |
| `COMP-5` | native int by width, binary wrap |
| INDEX-name (INDEXED BY) | `long` holding a 1-based occurrence number |
| level-88 | `static bool` predicate over the parent Place |

## Shape
A 01/77 item → a static (program) or instance (OO) field. A **group → a nested `record struct`** (`_T_<csname>`).
**`OCCURS n` → an array** `T[]`; a 2-D OCCURS is array-of-struct-containing-array (`Rows[i-1].Cols[j-1]`), never a
flattened 1-D — subscripts are 1-based COBOL, emitted `[expr-1]`. `OCCURS DYNAMIC` → a `CobolDynTable<T>` with a
`CapacityRegisterPlace` view. Example: `VAL OF ITEMS(2) OF WS-REC` → `WsRec.Items[2-1].Val`.

## Reference resolution — the Place path
`ReferenceResolver.Resolve(dataReference) → Place` is the single operand entry point, in two phases:
- **(A) syntactic flatten** — walk `cobolWord dataReferenceSuffix*` into base name + OF/IN qualifiers + the raw
  SUBSCRIPT-mode token group (a `(…)` group is a ref-mod iff it contains `SUB_COLON`, else a subscript list).
- **(B) semantic resolve** — resolve the qualified name to a `DataItem` (right-to-left narrowing, ISO §8.4.2.2),
  interpret subscripts to index expressions, attach each to its OCCURS level (outer→inner), wrap in a ref-mod
  decorator if present.

The resulting `Place` is rendered by `CodeGen.PlaceRenderer`; the binder never emits C# text. CALL BY REFERENCE passes
the receiver Place's address (`ref WsRec.Count`). See [[kb/IR/Node Types]].

## REDEFINES / RENAMES overlay — the 4-tier one-canonical-backing model
Because two typed reps cannot both be live over one storage without a shared `byte[]`, a "redefines class" has
**exactly ONE stored backing (the canonical)** and every other view is a **computed accessor** over it (priority
cascade D>C>B>A):
- **A — Alias** (identical PIC+USAGE / RENAMES no-THRU): one typed field, others pass-through.
- **B — StringCanonical** (the whole class has a byte image — since Step D that is every non-alias, non-rejected
  case, mixed-USAGE puns included): canonical = one `string` of class-max width under the char==byte convention;
  each view is a typed `(offset,width)` window (`RedefViewPlace`) holding the leaf's pinned `NumericByteForm`
  bytes — zoned digits for DISPLAY, radix-2 for BINARY/COMP-5, BCD for PACKED, big-endian IEEE for the float
  family, the 8-byte occurrence number for INDEX.
- ~~**C — ByteCanonical**~~ — **DELETED (Step D, kb/Work PB164):** no class-scoped `byte[]`, no RedefCodec; the
  mixed-USAGE pun it was reserved for is an ordinary Tier-B byte window.
- **D — Reject loud** (object/pointer/strongly-typed/ODO puns — already spec-illegal).

RENAMES folds into the same tiers as a composed view (`RenamesPlace` distributes writes across the spanned leaves).

## Whole-group-as-alphanumeric
A numeric DISPLAY leaf is a native `long` **unless** it lives under a group referenced as a whole operand — then MOVE
GR4 fills the group without regard to elementary items, so the leaf must materialize as its `string` zoned image. This
decision (`StorageForm`, e.g. `CharImage(Numeric)`) is computed once by `StorageFormPass` after procedure binding, and
the group's image is built on demand (`AsImage()`/`FromImage()`) at the byte boundary via `Encoding.Latin1` — never
persisted.

## Key concepts
- Typed-native only: PIC→CLR field, group→`record struct`, OCCURS→array; no byte-array substrate.
- `long` holds the unscaled value; scale is `PicInfo` metadata; `Int128` above 18 digits.
- Every operand → one `Place` via the two-phase `ReferenceResolver`; consumed identically by all verbs.
- One canonical backing per redefines class; views are computed accessors (4-tier A>B>C>D).
- Whole-group image = `StorageForm.CharImage`, materialized only at byte boundaries.

## See also
- [[kb/IR/Node Types]] — `Place` and its kinds/decorators.
- [[kb/Runtime/Execution Model]] — `CobolNum`, `CobolString`, `CobolDynTable` at runtime.
- [[kb/Spec/Language Features]] — the PICTURE/USAGE/OCCURS surface.
- [[kb/Semantics/Validation Rules]] — MOVE/category rules over these types.

## Backlinks
- [[kb/IR/MOC]] · [[kb/Index]] — link here.
- Lookup: [[kb/Spec/Lookup/Grammar]] (data types) · [[kb/Spec/Lookup/IR Mapping]] (data-movement nodes).
