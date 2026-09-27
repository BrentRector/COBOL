# DESIGN — Target Unified Data Model

> **Status:** ✅ IMPLEMENTED — the unified `StorageForm` data model is in the tree; the one outstanding piece is
> the structural `Place` segments (§2.2 item 4), scheduled next. Where the implemented design differs from the
> sketch below, stated as the current design: §2.2 item 1 (`RenamesPlace` stays direct — no single inner); §2.3
> (the REDEFINES facts are written once through the ONE named `RedefinesClass.Classify` mutator; the cell
> forcer's re-classification is a real second write by design; the Tier-C guards collapse into `ComputeTier` +
> `IsImageCapable`); §2.7 (the sentinel discriminant lives on `DataItem.Pending`, not on an Analyze result — see
> the italic note); `StoreAsImage` is retained as the named read-only projection of `Storage` (one definition
> over its pattern repetitions); the §2.4 image-fact caching is not yet materialized (the ordering hazard died
> with the mutable flag); and §2.5's pipeline is realized as `BindPipeline` + `GroupTail`, one validated DAG.
> **Scope:** the typed-native storage model spanning `DataItem` / `PicInfo` / `Place` / `NumProfile`, from
> bind → emit → runtime. Companion designs (pass-pipeline, emitter decomposition, diagnostics registry, editions)
> are cross-referenced but owned elsewhere.
> **Upholds the hard invariants:** typed-native data only (byte[] confined to a genuine Tier-C / file boundary);
> spec-first (ISO/IEC 1989:2023, cited by §); battery stays green every phase; singular pattern; no-god-class;
> C# 14 / .NET 10; four-editions-in-one.

---

## 1. The current problem (grounded in the code)

The compiler has **one canonical lvalue contract** (`Place`, `Binding/Model/Place.cs`) but **no canonical value
representation**. A single COBOL value can be stored five structurally different ways, and *which one* is decided by
facts scattered across three layers and, for the pivotal case, **mutated late**:

| Representation | Where it lives today | Decided by |
|---|---|---|
| Native `long` / `ulong` / `Int128` / `UInt128` | `PicInfo.ClrType` → `DataItem.ElementType` (`DataItem.cs:242,304`); the unsigned carriers are the R10 full-container-range rule (`PicInfo.IsUnsignedLongBinary` / `IsUnsignedWideBinary`) | PICTURE analysis (early) |
| Native `float` / `double` | `PicInfo.ClrType` (`PicInfo.cs:244`) | PICTURE/USAGE analysis (early) |
| `string` character image | `DataItem.StoreAsImage` flips `ElementType` to `"string"` (`DataItem.cs:304`) | **late mutation** |
| Tier-B `(offset,width)` window over one shared `string` | `RedefViewPlace` + `RedefinesClass.Tier` (`Place.cs:82`, `RedefinesModel.cs:46`) | REDEFINES classification pass |
| Out-of-line `CobolDynTable<T>` | `DataItem.FieldType` (`DataItem.cs:309`) | OCCURS DYNAMIC (early) |
| ~~(declared, unimplemented) Tier-C `byte[]`~~ | **RESOLVED — no such representation exists.** `RedefinesTier.ByteCanonical` and `StorageForm.TierCWindow` are DELETED (Step D, kb/Work PB164): a mixed-USAGE pun is an ordinary Tier-B byte window, because every numeric usage has a pinned `NumericByteForm`. | — |

### 1.1 The pivotal defect: `StoreAsImage` is a late-mutated, cross-layer flag

A numeric `USAGE DISPLAY` leaf is stored as a native `long`/`Int128` **unless** it lives under a group referenced as
a whole operand — then it must become a `string` (ISO §14.9.25.4 MOVE GR4 fills a group "without consideration for the
individual elementary items", so the leaf can receive spaces a `long` cannot hold). That fact is only known **after
PROCEDURE DIVISION binding**. Today it is realized by mutating `DataItem.StoreAsImage` (`DataItem.cs:169`, a public
`get; set;`) from **7+ sites across three layers**:

- binder data pass — `DataBinder.cs:255,1562,1577`, `DataBinder.Linkage.cs:275`, `DataBinder.Reports.cs:352`
- binder procedure pass — `StatementBinder.MoveFigurative.cs:123,262`
- **the emitter writes the binder's data model** — `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`),
  driven from `WholeGroupReferenced`, which `ReferenceResolver` itself mutates *during* procedure resolution
  (`ReferenceResolver.cs:280,303`), plus an OO re-sync (`CSharpEmitter.Oo.cs:697`) and a CompilerTemp re-sync
  (`CSharpEmitter.Call.cs:118`).

Consequences, all called out by the survey/critique:

1. **Cross-layer write-back** — CodeGen mutates the Binding data model (`MarkStoreAsImage`), so *emit correctness is
   order-dependent on a bind-time side effect*. The data model has no single owning phase.
2. **Reconciled by C# overload resolution.** Because the field type is undecided when the binder emits reference text,
   the runtime exposes *polymorphic overloads* so the SAME emitted string compiles either way:
   `CobolTable.Occ(long)` / `Occ(string)` (`CobolTable.cs:48,51`); `CobolNum.StoreDisplay(…, long)` /
   `(…, Int128)` / `(…, string)` (`CobolNum.cs:227,231,235`); `FormatDisplay(Int128,…)` / `(string,…)`
   (`CobolNum.cs:212,220`). Elegant, but it means a representation bug is **invisible until the generated C# hits
   Roslyn** and picks the wrong overload. ⛔ **Or picks NONE**, which is how kb/Work PB201 landed: `PositionRead` emitted `CobolTable.Occ(<field>)` for every carrier, and a `double`, an unscaled `Int128`/`ulong`, a `ManagedPointer` or a group's `record struct` has no overload at all — CS1503 in the default lane, on legal source. The guard is now the carrier list `ReferenceResolver.{Unscaled,Scaled}PositionCarriers`, held to the runtime methods' real overloads by `PositionCarrierOverloadDriftTests`.
3. **Recompute-on-read.** `IsCharacterImage` / `IsImageCapable` / `ImageWidth` / `StrongRoot` are recursive
   computed properties (`DataItem.cs:74-83,245-300`) re-walked at ~119 sites — each an O(subtree) walk, and each
   silently sensitive to whenever `StoreAsImage` last flipped.

### 1.2 Secondary structural problems

- **`DataItem` is a mutable god-struct** (`DataItem.cs`, 326 loc): ~20 `set`/`init` members mixing an *immutable*
  declared shape (level, name, PIC, children) with *pass-computed* facts (`RedefinesTarget`, `Class`, `ClassOffset`,
  `IsCanonical`, `StoreAsImage`, `TypeName`, `StrongType`, `Uid`). Validity is phase-dependent with no guard.
- **Implicit pass ordering.** `DataBinder.BindResolve` runs ~15 passes ordered only by call sequence + comments
  (`DataBinder.cs:210-232`); the *real* middle-end orchestration (another ~12 passes incl. `MarkStoreAsImage`) is
  hidden inside `CSharpEmitter.CallEmitRunUnit` (`CSharpEmitter.Call.cs`). No pass declares what it requires/produces.
- **Duplicated physical-width geometry.** `DataItem.ImageWidth` (`:283`), `OdoModel.PhysicalWidth` (`:155`), and
  `FieldEmitter`'s layout each compute record character offsets/widths independently and *must agree*
  (`OdoModel.cs:154` comment: "mirrors FieldEmitter's physical layout"). Sort/Keyed add two more copies.
- **`RedefinesClass.Tier` / `.Width` / `ClassOffset`** are set-then-overwritten across passes (mutable temporal state).
- ~~**Tier-C is declared but unimplemented**~~ — **RESOLVED (Step D, kb/Work PB164).** The enum member, the
  `TierCWindow` storage form and every guard deferring to them are gone; the "unimplemented byte codec" turned out
  to be a predicate that stopped at USAGE DISPLAY, not a missing representation.
- **PicInfo carries dead skeleton scaffolding** (`IsUnimplementedSkeleton => false`, `SkeletonReached`,
  three `ReferenceEquals` sentinel singletons — `PicInfo.cs:175,216,691-705`), and its 230-line `Analyze` scanner
  makes it not a pure value record.
- **Latent correctness bug (critique):** apostrophe-delimited VALUE literals are silently miscompiled because the
  VALUE-emit path hard-codes the `"` delimiter (`EmitCore.cs:162-168`, `FieldEmitter.cs:329-332`). This is a
  data-model-adjacent symptom of ad-hoc value handling and is folded into this design's cleanup.

---

## 2. The target design

### 2.0 One-paragraph thesis

Introduce **`StorageForm`** — a single, closed, computed discriminator that names *exactly how a value is stored*
(native int / native float / character image / Tier-B window / out-of-line table / object-ref /
pointer / index). It is computed **once**, by a **`StorageFormPass`** that runs after all facts (including
procedure-division whole-group use) are collected, and stored **init-only** on `DataItem`. Every downstream reader —
`FieldEmitter`, `NumericRenderer`, `OperandText`, `Place` construction — asks `StorageForm`, never re-infers from
`(Pic, StoreAsImage, Class.Tier, IsDynamicTable)`. `StoreAsImage` the mutable flag is **deleted**; the emitter's
`MarkStoreAsImage` write-back is **deleted**. The runtime overload bridge remains as a convenience but is **no longer
load-bearing** — by emit time the form is known, so the emitter *chooses* the read/write expression explicitly.

### 2.1 `StorageForm` — the canonical value-representation discriminator

New file `Binding/Model/StorageForm.cs`. A sealed abstract record with one case per physical representation. It is a
**value classification** (no C# strings, no emit logic) — the emitter interprets it.

```csharp
namespace CobolNet.Binding.Model;

/// The canonical storage representation of an elementary item or table element (COBOLNET_DESIGN §3/§4/§14.4).
/// Computed ONCE by StorageFormPass; init-only on DataItem. Closed set — a new representation is a new case,
/// and every switch over it is exhaustive (a missing arm is a COMPILE error, not a runtime LoudStmt).
public abstract record StorageForm
{
    /// Does this leaf contribute character positions to an enclosing group's AsImage()/FromImage() codec (§14.4)?
    public abstract bool IsCharacterImage { get; }
    /// Character-image width contributed (meaningful only when IsCharacterImage); 0 otherwise.
    public abstract int ImageWidth { get; }

    // ── The cases ─────────────────────────────────────────────────────────────────────────────────────────
    // NativeInt/NativeFloat/IndexCell carry a precomputed `Width` = ElementaryImageWidth(Pic)
    // = Digits + (1 if SIGN IS SEPARATE, ISO §13.18.52). A bare (Wide, Digits) shape cannot reproduce the
    // separate-sign +1, and that width is load-bearing even for a NON-promoted native leaf (a group's image
    // sums its native children's widths), so the width is carried explicitly on each native case.
    /// Native scaled integer: long (≤18 digits) or Int128 (>18). NOT character-imageable as itself, but IS
    /// image-CAPABLE (its zoned digit image is derived on demand). Width = digits + separate-sign.
    public sealed record NativeInt(bool Wide, int Digits, int Width) : StorageForm { … }
    /// Native IEEE float (COMP-1/FLOAT-SHORT) or double. Its record-image bytes are the big-endian IEEE
    /// interchange encoding (kb/Work PB164 wave 2, §13.18.60.4 GR13-GR15) — it participates like any other leaf.
    public sealed record NativeFloat(bool Single, int Width) : StorageForm { … }
    /// A C# string of exactly Width characters: alphanumeric / numeric-edited / national / boolean, OR a
    /// numeric-DISPLAY leaf promoted to its zoned image because it is used under a whole-group operand (§14.9.25.4 GR4).
    /// Category is retained so the numeric pipeline decodes/encodes zoned images (ParseDisplay/FormatDisplay).
    public sealed record CharImage(int Width, PicCategory Category) : StorageForm { … }
    /// A Tier-B REDEFINES view: a typed (offset, width) window over the class's ONE shared string backing.
    public sealed record TierBWindow(RedefinesClass Class, int Offset, int Width) : StorageForm { … }
    // (No Tier-C form. A mixed-USAGE REDEFINES view is a TierBWindow like any other — Step D, kb/Work PB164.)
    /// An out-of-line OCCURS DYNAMIC table (CobolDynTable<T>, D9); Element is the per-occurrence element's form.
    public sealed record DynamicTable(StorageForm Element) : StorageForm { … }
    /// A .NET object reference (typed class or universal CobolObject?). Zero character positions.
    public sealed record ObjectRef(string? ClassName) : StorageForm { … }
    /// A data pointer (ManagedPointer). Zero character positions.
    public sealed record PointerRef : StorageForm { … }
    /// A USAGE INDEX cell (long occurrence number). Zero character positions. (Width is 0 — an index has no PIC
    /// digits — but carried uniformly with the other native cases.)
    public sealed record IndexCell(int Width) : StorageForm { … }
}
```

**A dynamic-length elementary item (§8.5.1.10) is `StorageForm.DynamicString(Category, MaxSize)` — a native
`string`, never a byte image.** Its MAXIMUM SIZE is §8.5.1.10.1's "smallest of" three candidates — the LIMIT phrase,
the largest integer the PREFIXED length field of its dynamic-length-structure-name can hold, and the implementor
maximum (`docs/CONFORMANCE.md` §7 DOC-A.1-62) — computed by the ONE producer `CobolDynString.MaxSizeOf` and carried
as `DataItem.DynMaxSize` to every store, SET SIZE and argument adapter. The structure the DYNAMIC LENGTH clause names
is a SPECIAL-NAMES declaration (§12.3.7.2 dynamic-length-structure-clause; kb/Work PB829), bound into
`DataBinder.DynamicLengthStructures` (inherited by contained source elements, §8.4.6.1) as a
`Model/DynamicLengthStructure` and linked from the item as `DataItem.DynStructure`: it decides the PREFIXED candidate
(§12.3.7.4 GR18 — SHORT PREFIXED 65535, SIGNED SHORT PREFIXED 32767, the 32-bit fields past the implementor
maximum), §13.18.19.3 SR4's bound on the LIMIT phrase, and — as part of the DYNAMIC LENGTH clause — §8.5.3.1's
same-type test. The layout itself (length field, GR19's binary-zero delimiter) is RECORDED on the model, not
materialized: §8.5.1.10.3 leaves the item's location to the implementor, and no WiseOwl COBOL operation gives the item a
byte image a program can address (a variable-length group has no fixed record window — the record-area image of
one is the loud `TierCIsland` stage). A physical-structure-name is an implementor set WiseOwl COBOL leaves EMPTY
(§12.3.7.3 SR32; determination D-DL3 in `docs/CONFORMANCE.md` §3), refused as COBOLNET2257.

**Key rule (unifies invariant #1 + #2):** `CharImage` is the ONE case that subsumes *every* string-stored leaf,
including a numeric-DISPLAY leaf promoted by whole-group use and an image-stored Tier-B binary/packed view. The
promotion is a `StorageForm.NativeInt → CharImage` transition **inside `StorageFormPass`**, never a mutable bool flip.

The old boolean triad is replaced by pure functions on the item's form (or cached fields — see §2.4):

- `DataItem.IsCharacterImage` → `Storage.IsCharacterImage` (leaf) / `Children.All(c => c.Storage.IsCharacterImage)` (group)
- `DataItem.ImageWidth` → owned by `RecordLayout` (§2.6), reads `Storage.ImageWidth`
- `DataItem.StoreAsImage` → **deleted**; the equivalent query is `Storage is CharImage { Category: not Alphanumeric }`
  where a call site truly needs "numeric leaf stored as image" (there are ~5 such sites; the rest just needed the type).

### 2.2 The canonical `Place` lvalue model

`Place` stays the ONE lvalue contract (`Read()` → rvalue C# expr, `Write(rhs)` → C# store statement). The
consolidation:

1. **One file, one decorator base.** Move `OdoGroupPlace` (currently in `OdoModel.cs:89`) into `Place.cs`. Introduce
   `abstract record PlaceDecorator(Place Inner) : Place` forwarding `Pic`/`Item` (and, as the default, `Read`/`Write`);
   make `NumericImagePlace`, `RefModPlace`, `OdoGroupPlace` derive from it. Leaf places (`MemberPlace`, `DynTablePlace`,
   `RedefViewPlace`, `CapacityRegisterPlace`) stay direct. *(`RenamesPlace` stays direct too: it composes N spanned
   leaves with no single inner, and its `Pic`/`Item` are the level-66 ALIAS's own (§13.18.45 — the alias is its own
   elementary view), so a forwarding base fits nothing it does; deriving it would have meant overriding every
   forwarded member, i.e. inheritance without reuse.)*
   **A `Place` answers TWO questions about the item, and they are not the same question.** ISO §13.18.45.4 GR1
   writes them as two clauses of one sentence — "all of the data attributes of data-name-2 become the data
   attributes of data-name-1 **and the storage area occupied by data-name-2 becomes the storage area occupied by
   data-name-1**". `Place.Item` answers the attributes-and-storage half (what a decoration forwards).
   `Place.DenotedItem` answers WHICH DATA ITEM, and is `null` when the reference denotes one no data description
   entry declares: `RefModPlace` overrides it by §8.4.3.3.4 GR5 ("Reference modification creates a unique data
   item that is a subset of the data item referenced by identifier-1"), and the non-THROUGH level-66 alias — which
   resolves to the RENAMED item's own place, because GR1 shares both attributes and storage — records itself in the
   init-only `Place.DenotesAs`, set by the ONE resolver. It is a property rather than another `Place` kind because
   nothing about the ACCESS changes; a new kind would have to be re-handled in every renderer arm to render
   identically. **Every rule of the form "shall BE the data item …" asks `DenotedItem`** (§14.9.41.3 SR5's RELATIVE
   KEY identity and SR6's record key are the measured ones), and so does every "is this a WHOLE item" test, which
   is the same question negated — twenty callers had written that out by hand as `p is not RefModPlace` beside a
   `p.Item` test. `PlaceDenotedItemDriftTests` holds the roster of which kinds forward and which override, with the
   reason, and forbids a twenty-first hand-written copy.

2. **`Place` is built from `StorageForm`, not re-inference.** `ReferenceResolver` selects the concrete place from the
   resolved item's `Storage`:
   - `NativeInt`/`NativeFloat`/`CharImage`(non-numeric) → `MemberPlace`
   - `CharImage`(numeric, i.e. a promoted DISPLAY leaf) used numerically → `MemberPlace` whose numeric read/write goes
     through `NumericImagePlace` (the ParseDisplay/FormatDisplay bridge) — but the *decision* is now read off
     `Storage`, not the late `StoreAsImage` flag, so `NumericImagePlace`'s remark about "decided AFTER this text is
     produced" (`Place.cs:156-159`) is **retired**.
   - `TierBWindow` → `RedefViewPlace`
   - `DynamicTable` element → `DynTablePlace`
   - a level-66 view → `RenamesPlace`
3. **`CapacityRegisterPlace`** becomes a proper read-only view type (it already throws on `Write`); mark it
   `IReadOnlyPlace` so the store-polarity analysis can reject a write at bind time structurally rather than by comment.
4. **Longer-term (owner-gated, see open Q3):** replace the raw-string `Path`/`OffsetExpr` fields with structured
   segments (`item + subscript BoundExpr[]`) so the binder stops assembling emit-time C# (`Initialize.cs:327-385`,
   `TryExpandAll`). This restores the §2 bind/emit boundary. **Phase-gated** — not required to land StorageForm.

### 2.3 The REDEFINES tier model (unchanged semantics, owned representation)

The lattice is now **3 tiers — A Alias ⊑ B StringCanonical ⊑ D Rejected** (`RedefinesModel.cs`, `enum
RedefinesTier`). Tier C (`ByteCanonical`) and `StorageForm.TierCWindow` are **DELETED**: Step D's arm-1
dissolution (kb/Work PB164) established that a mixed-USAGE pun is not a separate representation at all — every
numeric usage has a pinned `NumericByteForm` (zoned, radix-2, BCD, IEEE, the R40 index bytes), so a mixed-usage
class is an ORDINARY Tier-B byte-window class over the one string backing. There was never a second codec to
build; there was a predicate that stopped short.

- Tier classification produces `StorageForm` for each member: Tier-A view → the canonical's form; Tier-B view →
  `TierBWindow`; Tier-D → a rejection diagnostic (no form emitted).
- **Tier A means ONE FIELD SERVES EVERY MEMBER, so the members must agree on the UNIT** (kb/Work PB203). The arm
  compared CLR carrier type and `ImageWidth`, and neither says what the field HOLDS: a `USAGE BIT` leaf's carrier
  is its PICTURE 1(n) boolean positions while its `ImageWidth` is the `ceil(n/8)` bytes it occupies, so
  `01 A PIC X(1). 01 B REDEFINES A PIC 1(8) USAGE BIT.` matched on both and aliased A's one CHARACTER. The arm now
  also requires bit-ness agreement and, between two bit leaves, an equal boolean-position count.
- **A Tier-B member that is a BIT item takes a BIT window, not a byte one** (kb/Work PB203; §13.18.44.4 GR1 states
  the association in BITS and §14.6.8.6's NOTE says a usage-bit item "need not occupy an integral number of
  bytes"). `RedefViewPlace.Bit` carries the absolute bit offset + boolean-position count and
  `CobolBits.ReadWindow`/`WriteWindow` touch only those bits, which is what makes two same-level bit members that
  SHARE a byte independent receivers. The offsets come from `DataItem.ClassBitOffset`, and every window — in the
  resolver, the INITIALIZE cursor and the MOVE CORRESPONDING cursor — is built by the ONE factory
  `RedefViewPlace.For`; those three each had their own copy of the offset law, which is how the bit unit came to
  be missing from all three at once. Full derivation in `docs/COBOLNET_DATA_MODEL_DESIGN.md` D19.
- `RedefinesClass.Tier`/`.Width` become **init-only**, set once by `RedefinesClassifier` (§2.5) — no set-then-overwrite.
- **Tier-C decision — AS BUILT (Step D, LANDED):** there is no Tier C. A genuine mixed-USAGE REDEFINES pun is
  ADMITTED as Tier B, and `ComputeTier`'s remaining reason arms carry only the boundaries that survive — the
  2-byte national overlay (D-N1/D-N2) and the dynamic-table reject (§13.18.44 SR5). `RedefinesTier.ByteCanonical`
  and `StorageForm.TierCWindow` are deleted outright rather than left quarantined, since a name nothing can
  return is a name the next reader has to re-derive. There is **no** `RedefinesClassifier` type; classification
  lives in `DataBinder.ClassifyRedefinesClasses` + `ComputeTier`, applied through the ONE
  `RedefinesClass.Classify` mutator, with the reason threaded to references by `ExpressionBinder.RefFailure`.
  The de-facto loud backstop is `ReferenceResolver.PlaceForItem` returning null → the caller fails loud.
  - **What P11 Step C consolidated** is the SEPARATE *classless* mixed-usage-GROUP image island — a plain group
    (not a REDEFINES pun) whose leaves are not uniformly character-imageable (a float/COMP-5/INDEX/BINARY-* leaf
    under `DataItem.IsImageCapable`, or a COMP/binary leaf under the stricter `DataItem.IsCharacterImage`) has no
    whole-group character image, so ~12 emit-time verbs (MOVE/STRING/UNSTRING/INSPECT/ACCEPT/DISPLAY/CALL/SORT-key/
    record-area distribution/FILE-STATUS) staged loud with copy-pasted, drifted message text. Step C routes every
    such guard's message through the ONE `Binding/Model/TierCIsland.Reason` source, **preserving each site's own
    predicate** (P1 `IsImageCapable` vs P2 `IsCharacterImage` — no lossy single-predicate collapse) and its
    operation-specific lead + offending-leaf descriptor. `TierCRejectionTests` locks that every shape still fails
    loud through the one reason (the "Tier-C" substring). The OO/SORT/UDF bind-time conformance guards keep their
    context-specific messages by design. ⚠ The scout's risk-3 note recorded one such variant as deliberate —
    *"UDF rejects Binary/Packed too"* — and that is NO LONGER TRUE: it was a hand-rolled DISPLAY-only usage
    union, not a decision, and PB164's F8 replaced it with the derived `DataItem.ElementImageCapable`
    predicate (§14.2.2 SR5 imposes no usage restriction on a RETURNING item). `UdfReturningResidue`'s only
    surviving LEAF screen is the pointer/object class (kb/Work PB199).
- **Step D — RE-BASED 2026-08-30 (the four-reader design scout; kb/Work PB164's last codegen half). THE
  PREMISE INVERTED: there is NO byte codec to build.** The earlier sketch ("a confined `byte[]` codec,
  `CobolByteImage`, `TierCWindow.Read/Write`") described a world V59 already dissolved: a Tier-B class's
  geometry is ALREADY byte-form end to end — `ElementaryImageWidth` returns `StorageWidth` for every pinned
  `NumericByteForm`, `AssignClassOffsets`/`RecordLayout` advance in those units, the backing seeds from
  `ImageInitOf` (byte images), and a promoted member reads/writes through `FormatImage`/`ParseImage` windows
  (pinned by `RedefinesClassificationTests.TierB_BinaryLeafPun_StringCanonicalOverItsTrueBytes` and the
  `v59_byte_image` golden). The spec derivation is one-directional: §13.18.44.4 GR1 associates storage AT THE
  BIT ("the number of bits required by the data item"), GR2 grants EVERY entry's name unconditioned reference
  to that storage, §13.18.60.4 GR2 makes representation a function of THE USAGE CLAUSE alone, §13.18.44 and
  Annex A.2 contain NO undefined-result escape, and §14.6.13.2 r2 (EC-DATA-INCOMPATIBLE) presupposes the
  alias wrote the item's REAL storage — so one representation per item, REDEFINES views included, exactly
  what CONFORMANCE items 205/207/208/211 already promise. **Step D is therefore a WIDENING + LANE
  COMPLETION, in this order:**
  1. **The spec-required Tier-D arm FIRST** ([[PB179]]): `ComputeTier` gains the §13.18.44.3 SR12/SR14
     rejection for pointer/object/strongly-typed leaves — today they classify Tier B with ZERO-WIDTH windows
     (the "no such items exist" comment is stale). A bind diagnostic, negative fixtures per side.
  1b. **§13.18.44.3 SR17, the same posture** ([[PB177]] arm C, LANDED): "Neither data-name-2 nor the subject of
     the entry shall be a variable-length group or a dynamic-length elementary item" — a SYMMETRIC rule, so
     `DataBinder.Sr17Shape` is tested on BOTH sides per WRITTEN ENTRY, beside the SR12/SR14 screen and before
     the dissolve loop (a per-class screen lets a nested entry's violation escape into the outer class's
     staged-loud arm). "Variable-length group" is §8.5.1.12.1's defined term — a group with a dynamic-length
     elementary item or a dynamic-CAPACITY table subordinate — so the predicate is
     `ReferenceResolver.HasVariableLengthSubordinate`, the standard's own definition, not a second walk.
     COBOLNET1698, two negative fixtures (one per side), and the class TIER is set Rejected as well as the
     diagnostic raised. The underlying defect was not merely an under-rejection: `StorageFormPass.Classify`
     returns `DynamicString` for an `IsDynamicLength` item BEFORE reaching its Tier-B view arm, so such a view
     kept its OWN disjoint native string — two storages for the one area §13.18.44.4 GR1 defines, MEASURED as a
     silent wrong answer (`MOVE "ZZ"` into the view left the redefined item unchanged).
     ⛔ **THE TIER VERDICT IS BELT-AND-BRACES, NOT THE STRUCTURAL BARRIER** — this paragraph and the two code
     comments claimed "rejecting the class makes that path structurally unreachable", and the regression test
     written to hold that claim was asserting on a field its harness never populated. Driven through the whole
     pipeline it FAILS: arm 1b returns before the tier is consulted, so a Rejected class's dynamic-length view
     still classifies as `DynamicString`, and a class dissolved by the nested-anchor loop never reaches the tier
     loop at all. **What prevents the disjoint storage reaching a user's program is the fatal COBOLNET1698
     diagnostic.** The tier is a second, independent verdict at the modelling layer, kept as one; the measured
     behaviour is pinned by `RedefinesClassificationTests` so the claim cannot out-run the code again.
     Citations were repaired in the same change set, and the first repair was itself one-sided:
     COBOLNET1525 cited "SR5" for the dynamic-CAPACITY case on BOTH sides; the repair read SR5's FOURTH sentence
     ("Neither the original definition nor the redefinition shall include an occurs-depending table" —
     COBOLNET0855's rule) and concluded no syntax rule named the case at all. **SR5's FIRST sentence is "The
     data description entry for data-name-2 shall not contain an OCCURS clause"**, and OCCURS DYNAMIC is Format
     4 OF the OCCURS clause, so the OBJECT side is named outright — it is now **COBOLNET1701** per written
     entry, over EVERY OCCURS format (the fixed-OCCURS object had been screened nowhere and compiled clean).
     COBOLNET1525 is NARROWED to the SUBJECT that is itself a dynamic-capacity table — the one side of the
     family no syntax rule names — and cites what actually decides it, §13.18.44.4 GR1's storage association
     against §8.5.1.9.1's "may vary during execution".
  2. **The live Tier-B lane defects** ([[PB180]] ACCEPT's DisplayTextWidth store into a StorageWidth window;
     [[PB181]] the CALL boundary's text-reader corruption of window bytes) — they corrupt TODAY's shipping
     Tier B and the widened members inherit them.
  3. **Dissolve `ComputeTier` arm 1** (float/COMP-5/BINARY-*/INDEX): the classifier's own Tier-B mark
     (`HasImageByteForm && Usage != Display`) already admits them textually — but treat that mark as
     UNPROVEN (it has been dead behind the reject; the first Tier-B COMP-5 window is a first execution),
     and complete the lanes the widened members need, each with a byte-level pin:
     - the FLOAT arm-order fixes (`NumericRenderer` `IsFloat` before `StoreAsImage`; `ArithmeticEmitter`'s
       float receiver cast; `MoveEmitter`; `ValueInitializer`) and the `NumericImagePlace` float lane —
       today they emit UNCOMPILABLE C# (CS0030/CS1503) for a promoted float leaf;
     - the UNSIGNED WIDE lane (`ParseImage` has no unsigned twin; the `StoreAsImage` arm precedes the
       `IsUnsignedWideBinary` arms — a wide COMP-5 window would decode SIGNED);
     - the `Length: 0` seeds (`ImageInitOf` has no float/index arm — a float/INDEX member seeds "" where
       4/8 bytes are due; every `pic.Length`-keyed lane needs the StorageWidth answer for the
       PICTURE-less shapes).
  4. **The SECOND gate deliberately** (`ForceStringCanonical` — EXTERNAL/BASED/ADDRESS-OF classes reject
     every non-DISPLAY leaf, stricter than `ComputeTier`; the two-arm shape's eighth instance): widen it in
     the same wave or record its narrower posture as an explicit staged residue — never leave the pair
     silently divergent. The UDF RETURNING screen (`UdfBinder`, COBOLNET1510) and the
     `{ Category: Numeric, IsFloat: false }` copies the scout catalogued are the same sweep.
     ⛔ **AS BUILT, THE PAIR NO LONGER EXISTS** (kb/Work PB231, 2026-09-04). "Widen it in the same wave"
     was the right instruction and it was still not enough — PB203 widened `ComputeTier` to USAGE BIT
     leaves and the cell gate stayed narrow, the NINTH instance of the same shape on the same pair. The
     structural answer replaced both hand-written lists with ONE allow-list predicate,
     `DataBinder.ByteWindowResidueOf` (data-model design **D-BW1**), which each caller wraps in its own
     surface phrasing; `ByteWindowResidueDriftTests` pins the four surfaces — REDEFINES, EXTERNAL,
     BASED+ALLOCATE, ADDRESS OF — to one verdict per leaf. A future widening is one edit by construction,
     which is the point: prefer the shape that makes the NEXT case automatic.
  5. **NATIONAL stays rejected THIS wave, with the reason corrected**: the old premise ("no single-byte
     char-window overlay") dissolves under byte-form windows, but D-N1's 2-byte-per-position REDEFINES
     layout is an undischarged A.1 obligation of its own — reject with the honest reason, tracked residue.
  6. **Truth maintenance in the same change set**: `ComputeTier`'s stale "ZONED digit-image" comment and its
     §12.4.6.4.4 GR2 miscitation (the clause DERIVES same-record-area FROM redefinition and its only
     representational content — "aligned on the leftmost byte position" — argues FOR bytes);
     `StorageForm.CharImage`'s pre-V59 doc; `RedefinesTier.Rejected`/`ByteCanonical`'s docs (`ByteCanonical`
     and `StorageForm.TierCWindow` are DELETED — dissolving the tier removes `TierCWindow`'s only stated
     parity obligation, and a quarantined form nothing assigns is the zero-fan-out trap); item 208's
     unqualified REDEFINES promise gains its truth; the phantom §8.8.4.1.1 sweep ([[PB182]] — 18 sites,
     §8.8.4.2.1 is the real clause — "For comparison, an alphanumeric group item shall be treated as an
     elementary alphanumeric data item"; §8.8.4.2.3 SR2, which that sweep first named, is the identifier-CLASS
     syntax rule and says nothing about groups ([[PB741]])).
  7. **Migration + goldens**: the corpus applies ZERO pressure — and that claim is now stated in the units
     that can carry it. ⛔ "No `.cob` under `tests/`" is the WRONG INSTRUMENT ([[PB209]]): that glob finds two
     files in the external corpus, whose 1,323 programs are `AT_DATA` heredocs inside 36 `.at` wrappers. Ask
     `scripts/corpus_sweep.py`, which reads all seven populations (3,106 programs) through the same extractor
     the differential compiles with, and `--codes` for whether a screen actually FIRES. Measured that way, no
     population trips the Tier-C arm and the differential has no such row — every behavioral fact is NEW and spec-derived, so the goldens
     are hand-authored discriminating pins (FUNCTION ORD byte reads over each widened leaf kind's window;
     the multi-01 FD/SD implicit class — 507 corpus constructs, none currently non-DISPLAY — gets its own
     golden; the `RedefinesTierBDifferentialTests` baked goldens are hash-keyed to source: ADD cases, never
     edit). The on-disk record-layout hazard V59 documented repeats for the widened usages and rides the
     same disclosure. Owner-documentable determinations to record: the SR8 splice/seed PAD character
     (space today) becoming window-visible data, and the Latin-1 char==byte backing vs the UTF-16
     alphanumeric repertoire (the backing is STORAGE, never text — state where the boundary lanes are).
  The drift locks (`V59ImagePredicateDriftTests`' inventory asserts, `StorageFormEnum` parity) fire on this
  edit BY DESIGN — update them deliberately with the wave, never silence them.

### 2.4 `DataItem` — slim core + init-only pass facts

Split the god-struct into a **mostly-immutable declared core** plus **init-only pass-computed facts**, all in
`Binding/Model/`:

- **Declared core (immutable after `BindEntries`)**: `Level`, `CobolName`, `CsName`, `Pic`, `OwnSign`, `OwnUsage`,
  `RawValue`, `Occurs`, `OccursSpec`, `IndexNames`, `Justified`, `BlankWhenZero`, `Children`, `Own88s`,
  `RedefinesTargetName`, `Renames`, `IsTypedef`, `TypedefStrong`, `Parent`. (`Parent`/`Uid` set during tree build,
  then frozen.)
- **Pass-computed facts → init-only, each written by exactly ONE named pass** (asserted by the pipeline, §2.5):
  `TypeRefName`/`TypeName`/`StrongType` (TypedefExpander), `RedefinesTarget`/`Class`/`ClassOffset`/`IsCanonical`
  (RedefinesClassifier), `IsBased` (PointerBinder), **`Storage`** (StorageFormPass).
- **Deleted:** `StoreAsImage` (the mutable flag). **Replaced:** the recursive `IsCharacterImage`/`IsImageCapable`/
  `ImageWidth`/`StrongRoot` computed properties become **cached init-only fields** filled bottom-up in a single
  O(n) walk by StorageFormPass (image facts) and TypedefExpander (`StrongRoot`). This removes the ~119 per-access
  re-walks (critique efficiency finding).
- `ElementType`/`FieldType`/`ClrType` become thin projections of `Storage` (+ `Occurs` for the `[]` wrap), not of
  `Pic` + `StoreAsImage`.

`SameType`/`TypeAnchor`/`EquivalentTypeDeclarations` move to a `StrongTypeModel` static helper — they are
strong-typing logic, not core shape.

**`StrongTypeModel` owns ISO §8.5.3.1's same-type test, whole, and every rule that spends it asks THAT.** The clause
has two alternatives over typed ITEMS resting on one relation over type DECLARATIONS, so the model has two entry
points and nothing else:

- `EquivalentTypeDeclarations(a, b)` — *"Two type declarations are considered equivalent when they have the same
  type-name, both have the same presence or absence of the EXTERNAL clause and the STRONG phrase, and for each
  elementary item in one type declaration there is a corresponding elementary item in the other type declaration,
  starting at the same relative byte or bit position and having the same length in bytes or bits"*, each
  corresponding pair carrying the same ALIGNED / BLANK WHEN ZERO / DYNAMIC LENGTH / JUSTIFIED / PICTURE / SIGN /
  SYNCHRONIZED / USAGE clauses. It takes a declaration in EITHER form the model holds one: a TYPE-clause SUBJECT
  (whose subtree `ExpandType` cloned through the ONE `CopyEntryDescription`, so it IS the declaration) or a
  `TypeDecls` TEMPLATE (also fully expanded — `ExpandTypes` walks the templates). Geometry comes from
  `BitLayout.StartBitWithin`, the ONE §8.5.1.6.3 cursor walk; the clause list is §8.5.3.1's own, one conjunct each;
  the PICTURE/SIGN/USAGE conjunct is `PicInfo`'s record equality (so a later analysis axis is included by
  construction), with §8.5.3.1's currency / decimal-point / LOCALE exceptions falling out of the analysis the
  analyzer already performs.
- `SameType(a, b)` — the two alternatives. An item that IS its own `TypeAnchor` is *"described with a TYPE clause"*
  (alternative 1: equivalent declarations, nothing more); one that is not is *"described as a subordinate item in a
  type declaration"* (alternative 2: equivalent declarations **plus** the same relative position and length). A
  MIXED pair is covered by neither and is not of the same type.

⛔ **A TYPE-NAME MATCH IS ONE CONJUNCT OF ONE HALF, and a member-NAME PATH is no part of the rule at all.** The
predicate was exactly those two things until kb/Work PB427, licensed by §8.5.3.1's informative NOTE read as the
criterion, and it was wrong in BOTH directions: it ADMITTED a MOVE between two non-equivalent same-named
declarations in different source elements (alphabetic characters deposited into two `PIC 9(3)` items, silently) and
REFUSED a legal move between two differently-named subgroups at the same relative position. Cross-source-element
equivalence is therefore no longer deferred — it is what the walk decides.

**The consumers, all on the one predicate:** §14.9.25.3 SR2 (`CheckStrongMove`, and through `MoveBinder` every
implicit move — `… FROM`, `… INTO`), §8.8.4.2.3 SR1 + §8.8.4.2.12 (`CheckRelationalOperands`, the ONE relation
chokepoint), and the activation boundary's one sentence written three times — §14.8.2.2 (arguments), §14.8.3.2
(returning items), §9.3.8.2.3 rule 7 (interface conformance) — in `OoConformance.StrongTypeMismatch`, reached from
BOTH `DescriptionMismatch` (BY REFERENCE, RETURNING, override and prototype signatures) and `ContentMismatch` (BY
CONTENT / BY VALUE), because §14.8.2.2's sentence qualifies neither mode. That last one also completes
§8.5.1.12.1's *"Two fixed-length groups are always compatible, unless they are strongly typed and have different
type definitions"*, whose strong half `VariableLengthCompatibility` deliberately leaves to its caller.

#### 2.4.1 Restricted data-pointers — the strong-typing overlay's pointer half

Annex D.9.2.2 states the guarantee the whole strong-typing overlay exists to provide: a restricted data-pointer
"may contain only the predefined address NULL or the address of a data item of a specific type", and restricted
data-pointers "provide type safety by precluding the treatment of data of one type as data of another type … the
existing restrictions for enforcing the integrity of strongly-typed group items cannot be circumvented by the use
of addresses, pointers, and based items."

**The model has TWO sources, and only one of them involves grammar** (Annex D.9.2.2 items 1 and 2):

1. A data description entry `USAGE POINTER TO type-name-1` (§13.18.60.2 general format; §13.18.60.4 GR23). Carried
   on `PicInfo.RestrictedTypeName`, read by `StrongTypeModel.PointerRestriction`.
2. `ADDRESS OF identifier-1` where identifier-1 is a strongly-typed group item or another restricted data-pointer.
   Normative at §8.4.3.11.4 GR2. **This source needs no grammar at all** and was violable from the day strong
   TYPEDEF landed — `StrongTypeModel.AddressOfRestriction`.

A restriction carries a **`TypeRestriction`** — the type NAME the restriction is spelled with (§13.18.60.4 GR23
states it as `type-name-1`) together with the type DECLARATION that name resolves to. A restriction is to a TYPE,
not to a member position, so `SameRestriction` is declaration equivalence and deliberately NOT `SameType`, whose
second alternative answers the different question of whether two OPERANDS occupy corresponding positions. Within one
source element §13.18.58 makes a type-name unique, so the name alone decides; ACROSS source elements two
declarations may share a name and differ, which is why the declaration travels with it —
`DataBinder.ResolveRestrictedTypes` (the tail of the ONE `ExpandTypes` pass) attaches it to
`PicInfo.RestrictedTypeDecl`, and `SameRestriction` compares names always and declarations whenever both sides
resolved to one. Nothing about the restriction network is deferred any more (kb/Work PB427); §14.8.2.3.2's CALL
screen is consequently no longer scoped to the AS-NESTED loop, because the reason for that scoping was the
name-only test.

**The declaration shape is not the obvious one.** §13.18.60.3 SR18 — "If type-name-1 is specified, the TYPEDEF
clause shall be specified for the subject of the entry" — makes `01 P USAGE POINTER TO T.` itself nonconforming:
the restricted pointer is declared as a **type declaration** and reached through a TYPE clause. With §8.5.3.1
("Elementary type definitions shall not be specified with the STRONG phrase") the only legal shape is a GROUP
strong typedef, a TYPEDEF'd `USAGE POINTER TO` entry, and a `TYPE` item of it — pinned by
`tests/conformance/2023/pb153_restricted_pointer`.

**Consumption screens** (all in the 0869 pointer band, where `PtrBinder` already reports every other §14.9.39 /
§14.9.3 operand rule): `PtrBinder.BindAllocate` — §14.9.3.3 SR5 and its SR4 converse, in both the based form and
the CHARACTERS form (SR5 asks `StrongGroupType`, SR4 the weaker `TypedItemType`: SR5's antecedent is a STRONGLY-TYPED
GROUP, SR4's only a TYPED item, and conflating them rejects legal source); `PtrBinder.BindSetAddress` — §14.9.39.3
SR19 and SR20 in **both** of its arms (`SET ADDRESS OF based TO ptr` and `SET ptr TO ADDRESS OF x` are separate code
paths); `CallBinder` — §14.8.2.3.2's "If either is a restricted pointer, both shall be restricted and of the same
type", over EVERY Format-2 callee whose formals are bound (the clause carries no AS-NESTED qualification, and the
restriction now resolves to its own element's declaration).

⛔ The asymmetry `StrongTypeModel` already documents is load-bearing here: a leaf subordinate to a strong group is
NOT itself strongly typed, so `SET P TO ADDRESS OF <leaf of a strong record>` is **unrestricted and legal**. An
over-eager screen would break it. (kb/Work PB153.)

### 2.5 The explicit bind pipeline (the pass contract)

Replace `BindResolve`'s comment-ordered calls **and** the emitter-hidden binder passes with a declared pipeline in
`Binding/Passes/`. (Full pipeline design is the companion `DESIGN-pass-pipeline.md`; this section fixes only the
**data-model-relevant ordering** this dimension depends on.)

```csharp
interface IBindPass { string Name { get; } PassPhase Requires { get; } PassPhase Produces { get; } void Run(BindModel m); }
```

Ordered, with the **data-model dependency chain made explicit**:

0. **`SynthesizeImpliedPictures`** — ISO §13.16.3 SR9's VALUE-implied PICTURE (§2.7a). FIRST, and the placement
    is load-bearing in both directions: **before** the clone, over the WRITTEN entries, so a TYPEDEF template owns
    its implied PICTURE when its description is copied — §13.18.57.4 GR3: "that implicit PICTURE clause becomes
    part of the description of the subject of the entry" (kb/Work PB1300: run after the clone, the subject's OWN
    VALUE implied its picture instead, `01 W TYPE U VALUE "Q"` over `U VALUE "ABCD"` binding as X(1)); and
    **before** usage inheritance so the entry is already PICTURE-bearing when GR1 and the §13.18.60.3 screens
    reach it. An entry still pending a TYPE / SAME AS reference is not picture-less in SR9's sense and waits for
    the copy.
1. `ExpandTypesPass` (TYPEDEF/TYPE clone; produces TypeName/StrongType; sets StrongRoot cache)
2. `UsageInheritancePass` (merge of `InheritUsageClauses` + `ResolveIndexItems`, renamed — §2.7)
3. `SignInheritancePass`
4. `RedefinesClassifier` (produces Class/Tier/ClassOffset/IsCanonical/Width; Tier-C verdict)
5. `StrongTypeCheckPass`
6. `OdoResolvePass`, `DynamicResolvePass` (produce OccursSpec.Depending/CapacityRegister)
7. `FileResolvePass`, `ReportResolvePass`, `Linkage/Pointer/Oo` binders
8. **procedure binding** (StatementBinder → BoundProgram)
9. **`UsageCollectionPass`** — walks the BOUND tree, collects `WholeGroupReferenced` (moved OUT of
   `ReferenceResolver`'s mid-resolve mutation and OUT of the emitter). **This is the new owner of that fact.**
10. **`StorageFormPass`** — the LAST data-model pass. Computes `DataItem.Storage` for every item: base form from
    `Pic`/`Usage`/tier/dynamic, then applies the whole-group promotion (`NativeInt → CharImage`) for numeric-DISPLAY
    leaves under a `WholeGroupReferenced` group. Fills the image-fact caches bottom-up.
11. **emit** — reads `Storage` only; no data-model writes.

The pipeline asserts at startup that no pass reads a fact before its producing pass (a `PassPhase` enum guards
width/offset/form reads). This is where "implicit pass-ordering" — the prime smell — is structurally killed.

#### 2.5.1 The ONE data-description copy — a CLASSIFIED field set, never a hand list (landed; kb/Work PB522)

⛔ **Both GR-1s state an EXCLUSION list, so the default is "carried".** ISO §13.18.57.4 GR1 — the TYPE clause's
effect "is as though the data description identified by type-name-1 had been coded in place of the TYPE clause,
*excluding* the level-number, name, alignment, and the GLOBAL, SELECT WHEN, and TYPEDEF clauses" — and
§13.18.49.4 GR1, the same sentence for SAME AS excluding only the level-number, name, CONSTANT RECORD, EXTERNAL,
GLOBAL, REDEFINES and SELECT WHEN. §13.18.58.4 GR1 then reproduces a type declaration's SUBORDINATE entries whole.

**The shape that failed.** `ExpandTypesPass` had TWO copiers — `CopyEntryDescription` (the entry copy onto a
subject) and `CloneItem` (the per-node subordinate clone) — and each spelled its own field list in an object
initializer. That inverts the standard's default: a clause absent from a list is not a decision, it is an
omission, with no diagnostic and no site at which it can be read. `DataItem.GroupUsage` — the one field that
makes an item a bit or national group — was in NEITHER, so `01 R TYPE T` over a `GROUP-USAGE NATIONAL` template
bound as an ordinary alphanumeric group: wrong class, wrong category, `FUNCTION LENGTH` in alphanumeric instead
of national positions (§15.50.4 r2 vs r3), and — because §13.16.4 GR1/GR2 imply the clause for every
SUBORDINATE group — a false antecedent that silently re-categorized the whole subtree. SYNCHRONIZED (§13.18.55),
ALIGNED (§13.18.1), ANY LENGTH (§13.18.2) and DYNAMIC LENGTH (§13.18.19) were lost the same way.

**The shape now.**

1. **One copy.** `DataBinder.CopyEntryDescription(from, to, scope)` is the only place a clause travels, for
   all five carriers: the TYPE subject, the SAME AS subject, `CloneItem`'s reproduced subordinate, and the
   compiler temporary's root and subtree (`CreateCompilerTemp` / `CloneTempNode`, kb/Work PB888). Each copier's
   initializer carries nothing but identity, the level and the `MemberOnly` fields. `scope` is a
   `DescriptionCopyScope` — `TypeSubject` · `Entry` · `CompilerTemp` — the one axis on which the rules make the
   copies differ: §13.18.57.4 GR1 is the ONLY GR-1 that excludes "alignment" (its GR2d re-aligns the subject "as
   though it were a level 1 item"), and that word names both alignment clauses, SYNCHRONIZED *and* ALIGNED, so
   only `TypeSubject` leaves them behind; a compiler temporary (§8.4.3.2.4 GR1 "the description, class, and
   category", §8.4.3.4.4 GR1, §14.9.25.4 GR1's intermediate) takes the whole description except the
   `EntryOnly` clauses.
2. **The classification lives on the field.** Every stored `DataItem` property carries
   `[DescriptionCopy(DescriptionCopyKind.…, "<the §/GR, or the pass that owns the fact>")]` —
   `Clause` (travels) · `Alignment` (travels except onto a TYPE subject) · `EntryOnly` (travels with every entry
   copy but not onto a compiler temporary — VALUE, which a temp stored by its pre-op before any read cannot
   observe and whose Format-2 FROM subscripts would address an OCCURS a temp's root never reproduces, ANY
   LENGTH, §13.18.2.3 SR2's linkage-section parameter shape, and BASED — in neither GR-1 exclusion list, so a
   TYPE / SAME AS subject of a based description is based, §13.18.57.4 GR4 only deciding whose clause applies;
   a composed BASED is placement-screened at the copy site by `ScreenComposedBased`, COBOLNET2510, kb/Work
   PB1300) · `MemberOnly` (a reproduced subordinate
   only — the name, OCCURS, REDEFINES, a nested TYPE/SAME AS reference, the declaration cursor) · `CopyWritten`
   (the copy derives it: `ValueIsCopied`, `ExternalFromType`) · `None` (excluded by a named rule, or owned by a
   post-build pass). Adding a field to `DataItem` is therefore a CHOICE made where the field is declared.
3. **The classification is behaviourally true.** `DescriptionCopyCompletenessDriftTests` (Unit) fails when a
   stored property has no classification, when a `Clause`/`Alignment` field is not transferred by the copy, when
   a `None`/`MemberOnly` field is — once per `DescriptionCopyScope` — when any of `CloneItem`,
   `CreateCompilerTemp` or `CloneTempNode` re-spells a clause of its own, and when ANY site in the compiler
   outside the one copy hand-copies two or more clause fields from one source (`X = src.X`). It asserts it could
   give every property a distinct value first (so the audit is evidence about all of them), and drives its own
   failure branches over a copy that re-introduces the PB522 omission and over the pre-PB888 temp initializer.

`conformance:2002/pb522_group_usage_travels_with_the_description` measures the clauses that exist today, each
against a byte-identical inline control; the drift test is what makes the NEXT one automatic.

`conformance:2002/pb888_compiler_temp_takes_the_whole_description` pins the temporary's arm: a user-defined
function returning a `GROUP-USAGE NATIONAL` group measures what its working-storage twin measures, and a
two-receiver MOVE of a national-group table element gives each receiver what the one-receiver MOVE gives.

### 2.6 `RecordLayout` — one physical-width authority

New `Binding/Model/RecordLayout.cs`: the single owner of character offset/width geometry over the record tree, reading
`StorageForm.ImageWidth`. Exposes `ImageWidth(item)`, `PhysicalWidth(group)` (tier-aware), `OffsetOf(leaf)`,
and the two key-operand rules — `KeyIndexOfKeyItem(...)` (ISO §12.4.5.12.4 GR4's identical-byte-position
correspondence, which answers §14.9.30.3 SR11 and §14.9.41.3 SR6 a)) and `GenericKeyIndex(...)`
(§14.9.41.3 SR6 b)'s three conditions), plus `IsSubjectToOccurs(item)`, the one predicate for the
"shall not be subject to any OCCURS clauses" rules. Those two rules were ONE method until kb/Work PB354:
the conflation is what kept SR6 b) 2.'s class/category/usage condition unwritable (it has no meaning for
READ) and let SR11 accept a short item where the rule names the key itself.
Delete the four independent copies: `DataItem.ImageWidth` recursion (`:283`),
`OdoModel.PhysicalWidth` (`:155`), `Sort` geometry (`Sort.cs:483-540`), `KeyedIo` geometry (`KeyedIo.cs:335-375`).
`FieldEmitter` and the codec consume `RecordLayout` so they cannot drift.

### 2.7 `PicInfo` / `PictureAnalyzer` / `NumProfile`

- `PicInfo` becomes a **pure analyzed-picture value record**. Extract the 230-line `Analyze` scanner
  (`PicInfo.cs:333-562`) into `Binding/PictureAnalyzer.cs` (`PicInfo Analyze(...)`). Delete the dead skeleton
  scaffolding (`IsUnimplementedSkeleton => false`, `SkeletonReached`, and the three `ReferenceEquals` sentinel
  singletons `NationalUsagePending`/`BitUsagePending`/`RecoveryItem` — replace with a proper `PicAnalysis` result
  discriminant `Ok | GroupUsageShed | Recover`). *(The discriminant lives on `DataItem`, not on an Analyze
  result: the two Pending sentinels never come from `Analyze` (they arise for PICTURE-LESS entries, where Analyze
  is not called), and the group-vs-elementary verdict is unknowable until the forest completes — the state is
  DataItem state encoded as Pic reference-identity. It is `DataItem.Pending : PicPending {None, NationalUsage,
  BitUsage}` (MakeItem writes; CloneItem carries; `ResolveIndexItems` adjudicates + clears), and `Recover` is the
  `PicInfo.Recovery(int)` factory — a plain value shared by the analyzer's five inline recovery paths and the 0881
  elementary arm. `ParseUsage` lives in `PictureAnalyzer` too, its constant-false `out bool skeleton` overload
  deleted.)*
- Rename `ResolveIndexItems` → fold into `UsageInheritancePass` (§2.5 step 2); it does USAGE-marker resolution, not
  index-only work.

#### 2.7.1 `UsageInheritancePass` — ISO §13.18.60.4 GR1 as an EQUIVALENCE (landed; kb/Work PB495)

⛔ **The rule is not "a group's usage influences its leaves"; it is that the clause APPLIES TO each of them.** GR1
therefore states an equivalence, and the pass is built to make it true by construction rather than case by case:

```
01 G USAGE u.  05 A ⟨pic⟩.        ≡        01 G.  05 A ⟨pic⟩ USAGE u.
```

— the same item, so the same verdict, the same diagnostic CODE and the same width, for **every** member of the
`Usage` enum and every picture. `UsageInheritanceDriftTests` (Conformance) enumerates the enum and asserts it.

**ONE derivation, three consumers.** The pass is one pre-order walk (`UsageInheritanceWalk` →
`UsageInheritanceGroup` / `UsageInheritanceElementary`) over one derivation of an elementary item's EFFECTIVE
usage — its own clause, else the nearest enclosing one (a `GROUP-USAGE` clause's implied BIT/NATIONAL included,
§13.18.29.3 SR2/SR3). That derivation feeds:

1. **the group-header shed** — GR1's "and not to the group itself". Entry bind cannot tell a picture-less
   elementary item from a group header (the subordinates are not parsed yet), so it synthesizes the usage's
   representation optimistically and records the fact on `DataItem.PicIsUsageSynthesized`; the walk carries that
   profile DOWN to each picture-less leaf and clears it from the header. **Derived from the fact, never from a
   list of shapes** — the previous shed tested four reference-identity/category patterns and the leaf application
   a disjoint three-member usage set, and every usage in neither fell through in SILENCE.
2. **the leaf's representation** — `ApplyEffectiveUsage`, which is also the ONE site the §13.18.49.3 GR3 SAME-AS
   ancestor transform calls (it carried its own copy of the same three-member list).
3. **the screens** — `PictureAnalyzer.ScreenUsageAgainstPicture` (§13.18.60.3 SR3 / SR5 / SR12 / SR20), extracted
   from `Analyze` so the written-clause path and the inherited path run the SAME code, plus §13.16.3 SR8's
   prohibition over the ONE set `UsageFamilies.IsPictureless` and §13.18.60.3 SR2's equality check
   (`COBOLNET1927`, against the nearest enclosing WRITTEN clause, comparing resolved `Usage` members so SR6's
   COMP/COMPUTATIONAL abbreviation and this implementation's BINARY identification are already folded).
4. **the clause's PHRASES** — `ApplyEffectiveNoSign`, the §13.18.60.2 `PACKED-DECIMAL [ WITH NO SIGN ]` phrase.
   ⛔ **A phrase is part of the USAGE clause, not a clause of its own**, so GR1 hands it down with the word and
   the SAME site adjudicates both spellings: §13.18.40.3 SR31's `'S'` prohibition (`COBOLNET1566`) and
   §13.18.60.4 GR11's sign-free width (`PicInfo.PackedNoSign`). It runs AFTER `ApplyEffectiveUsage`, because its
   subject is the representation the item really takes. The phrase reaches it on `DataItem.OwnNoSign` — a
   `DescriptionCopyKind.Clause` field, so the TYPE / SAME AS copy carries it and the §13.18.49.4 GR3 ancestor
   transform records it rather than applying it (one adjudication, in this pass, not two). Only the
   GENERAL-FORMAT half stays at entry bind: which usage WORD the phrase may be written on (`COBOLNET1565`),
   which is a fact about the written clause and needs no forest. Before kb/Work PB570 the whole phrase was a
   BindEntry local adjudicated against the entry's own PICTURE — which a group header has not got — so
   `01 GRP USAGE PACKED-DECIMAL WITH NO SIGN. 05 EG PIC 9(4).` kept the 3-byte SIGNED layout and a subordinate
   `PIC S9(4)` drew no diagnostic at all, at every nesting depth.

**What the shape buys.** A `Usage` member added to the enum inherits, sheds and is screened without anyone
touching this pass. The one hand-written table left is `UsageFamilies.IsPictureless`, and because it drives BOTH
arms a wrong entry in it is invisible to any behavioural test — so it is checked against §13.16.3 SR8's own
sentence, re-read out of `specs/ISO_COBOL.md`, by `PicturelessUsageSetDriftTests` (Unit).
`UsageInheritanceDriftTests` asserts the equivalence over the usage WORD × picture matrix **and over the
clause's keyword-only PHRASES** (`UsageClausePhrases`, read off the §13.18.60.2 figure; phrases carrying an
operand are excluded by predicate, since the two arms would not be the same program). The phrase axis is what
PB570's defect would have had to pass: the word-only matrix was green throughout, because it never wrote one.

**Boundary.** A picture-less elementary item whose effective usage REQUIRES a picture (`01 G USAGE COMP. 05 A.`,
and the clause-less `01 G. 05 A.`) is NOT this pass's rule: that is §13.16.3 SR8's second sentence, screened by
the `CheckPictureRequired` closing guard; its SR9 VALUE-implied exception is §2.7a, applied one pass EARLIER.

#### 2.7.2 `CheckSignClauses` — ISO §13.18.52.3 SR1/SR2, the SIGN clause's subject (landed; kb/Work PB537)

SR1 admits a SIGN clause only on "a numeric data or screen description entry whose picture character-string
contains the symbol 'S'", "a numeric report group description entry whose picture character-string contains the
symbol 'S'", or "an alphanumeric group item, national group item, or strongly-typed group item"; SR2 requires "the
usage of an elementary item for which the SIGN clause is specified" to be display or national. **ONE
elementary-subject test** (`DataBinder.SignClauseElementaryDefect`) is read by both entry kinds; the group bullet
reads the one group-kind classifier (`ItemCategory.GroupKindsOf`, so a `GROUP-USAGE BIT` group — declared or
inherited by §13.16.4 GR1 — and a variable-length group are refused). The data-description arm is a POST-FOREST
pass after `UsageInheritancePass` (group-ness, the group kind and an inherited usage are all settled there) and
before `InheritSignClauses` (a refused clause is cleared and must not shed onto subordinates). ⛔ **Its subject is
the entry that WROTE the clause** — entry bind records it (`_signClauseWritten`) — because `DataItem.OwnSign` is
also written by the TYPE copy (§13.18.57.4 GR1) and the SAME AS ancestor transform (§13.18.49.4 GR5); screening
those would report a template once per reference site and refuse a SAME AS of an alphanumeric item under a signed
group. The report arm screens the printable item where its picture is analyzed (`BindReportEntry`). The former
float-edited special case (reported under the PICTURE code `COBOLNET1658`) was this rule's numeric-edited arm and
is gone; the §13.16.3 SR19 LOCALE screen is a different rule and stays.

#### 2.7a `SynthesizeImpliedPictures` — ISO §13.16.3 SR9, the VALUE-implied PICTURE (landed; kb/Work PB504/PB831)

⛔ **SR9 is APPLIED, never excused.** The rule says the PICTURE clause *may be omitted* when the data-item format
of the VALUE clause supplies a non-zero-length alphanumeric, boolean or national literal, and that a clause is
then *implied* — `X(length)` / `1(length)` / `N(length)`. So the compiler's obligation is to SYNTHESIZE that
clause, and the entry then stops being picture-less; the §13.16.3 SR8 guard downstream is left with exactly SR8's
own population and needs no carve-out. (It once had one, for the figurative spellings, and that carve-out both
mis-derived `ALL literal-1`'s length — a silent wrong answer, kb/Work PB831 — and admitted `ALL ""`, which SR9
excludes by name.)

**The design is one sentence: build the string the rule spells out, then go through the SAME door a written
clause goes through.** `PictureAnalyzer.Analyze` is called with the entry's OWN usage and SIGN clause and
`explicitUsage = "this entry wrote a USAGE clause"` — argument for argument what `BindEntry` passes — so from
that moment the item is indistinguishable from one whose source wrote `PICTURE X(n)`. §13.18.60.4 GR1
inheritance, the §13.18.60.3 SR3/SR5/SR12/SR20 screens, SIGN inheritance, the VALUE initializer and the emitter
all reach it through their existing single sites, each firing ONCE. The standard licenses this reading directly:
§13.18.60.3 SR13 a) and SR20 are both written over the "explicit or **implicit** picture character-string".

**The two derivations it owns, and where they come from.**

| question | rule | answer |
|---|---|---|
| which arm | §13.16.3 SR9 a/b/c | the literal's CLASS — `CobolLiteral.ClassOf`, the one §8.3.3.2/§8.3.3.4/§8.3.3.5 table |
| what length | §13.16.3 SR9, "as specified in 8.3.3" | `CobolLiteral.Decode(…).Length`, i.e. the literal's CHARACTERS (2 hex digits per alphanumeric character, 4 per national, 4 boolean characters per hex digit) |
| a figurative constant's length | §8.3.3.6.4 GR3 b/c | ONE character, except `ALL literal-1`, which takes literal-1's length |
| a figurative constant's class | §8.3.3.6.4 GR1, GR4 | national in a national context, boolean for the ZERO format in a bit context, else alphanumeric |

The figurative admission itself is the kb/Work PB828 adjudication, ratified in that note: §8.3.3.1 makes a
figurative constant a literal and §8.3.3.6 is a subclause OF §8.3.3, so SR9's length reference resolves for it.

**Forest and idempotence.** The pass walks `CompositionForest()`, because SR9's subject is the entry AS COMPOSED:
a TYPEDEF template's own entry needs the implied clause (the SR8 guard reports over it) and so does the `TYPE`
clone (the emitter lays that one out). The edition gate fires once per SOURCE entry (`TypeAnchor is null`), and
an item that already has a `Pic` is skipped, so a forest that yields one item twice cannot double-report.

**Edition.** COBOL-85 had no such grant, so the construct row `value-implied-picture-2002` rejects below 2002
through the canonical `ConstructRegistry.Check` funnel (COBOLNET0900). SR9's boolean and national arms are gated
a second time and independently by `boolean-data-2002` / `national-data-2002`, the literal forms themselves being
2002 additions.

**Drift.** `ImpliedPictureTests.Sr9_ImpliedPictureText_MatchesTheStandardsThreeArms` re-reads SR9 a/b/c out of
`specs/ISO_COBOL.md` on every run and asserts the class→symbol table, for the same reason
`PicturelessUsageSetDriftTests` exists: a wrong symbol moves the synthesis and every behavioural expectation
together, so the standard's own sentence is the only independent oracle.
- `NumProfile` (runtime) stays the runtime projection of `PicInfo`. Today `PicInfo` re-materializes it as an
  initializer STRING (`PicInfo.cs:301`). Keep that boundary (Binding must not depend on a runtime value type for its
  own logic) but generate it through the emitter's `RuntimeApi` façade (companion emitter design) so a
  `NumProfile` field rename breaks one file, not silently at generated-compile time.
  - **Where the boundary actually runs (clarified when V59 step 2 landed).** It separates BUILDING a runtime value
    struct (still forbidden in Binding — the profile is emitted as text) from NAMING a runtime ENUM. `PicInfo`
    already named one (`CobolEdit.EditRule` on `EditingRules`), and `PicInfo.ByteForm` / `PicInfo.Truncation`
    now return `NumericByteForm` / `NumericTruncation` rather than the initializer's former inline STRING
    switch. Typing them is what lets `NumericByteFormDriftTests` compare the mapping against the enum instead
    of against generated text, and what makes a renamed member a compile error in one file.

### 2.8 Value-literal decoding (folds the apostrophe latent bug)

Route **every** VALUE / figurative-literal recognition through ONE `CobolLiteral.Decode` in a low layer both Binding
and CodeGen reference (companion `Common/` codec). Delete the hard-coded `"`-delimiter tests at `EmitCore.cs:162-168`,
`FieldEmitter.cs:329-332`, `CSharpEmitter.ReportWriter.cs:99` and the triplicated `DecodeCobolString`
(`StatementBinder.cs:1814`, `EmitCore.cs:133`, `DataBinder.cs:719`). Add conformance goldens for apostrophe-delimited
elementary / group / `ALL 'x'` / Report-Writer SOURCE VALUEs. This closes the confirmed silent-miscompile and enforces
"one string-literal boundary" as part of the data-model cleanup.

---

## 3. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| create | — | `Binding/Model/StorageForm.cs` | The one computed value-representation discriminator (§2.1); replaces the `(Pic, StoreAsImage, Class.Tier, IsDynamicTable)` scatter. |
| create | — | `Binding/Passes/StorageFormPass.cs` | Computes `DataItem.Storage` ONCE after all facts; owns the numeric-DISPLAY→image promotion (§2.5 step 10). |
| create | — | `Binding/Passes/UsageCollectionPass.cs` | New owner of `WholeGroupReferenced`, collected from the BOUND tree — removes the `ReferenceResolver` mid-resolve mutation and the emitter's write-back. |
| create | — | `Binding/Passes/IBindPass.cs` + `BindPipeline.cs` | Declared, asserted pass order with Requires/Produces (§2.5); kills implicit ordering. |
| create | — | `Binding/Model/RecordLayout.cs` | Single physical offset/width authority (§2.6). |
| create | — | `Binding/PictureAnalyzer.cs` | The extracted 230-line PICTURE scanner; makes `PicInfo` a pure value record (§2.7). |
| create | — | `Binding/Model/StrongTypeModel.cs` | Homes §8.5.3.1's `SameType`/`EquivalentTypeDeclarations`/`TypeAnchor` moved off `DataItem`. |
| create | — | `Common/CobolLiteral.cs` | One literal decoder; fixes the apostrophe-VALUE silent bug (§2.8). |
| delete | `DataItem.StoreAsImage` setter (`DataItem.cs:169`) | — | Replaced by `Storage is CharImage` (§2.1); removes the late-mutated cross-layer flag. |
| delete | `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`) | — | Cross-layer write-back eliminated; logic moves into StorageFormPass. |
| move | `OdoGroupPlace` (`OdoModel.cs:89`) | `Binding/Place.cs` | Consolidate the Place hierarchy under one file (§2.2). |
| create | — | `Binding/PlaceDecorator` base (in `Place.cs`) | Common base for wrapping places (§2.2). |
| move | `SameType`/`TypeAnchor`/`StrongRoot` logic (`DataItem.cs:74-133`) | `StrongTypeModel` | Strong-typing is not core shape (§2.4). |
| refactor | `DataItem.ImageWidth`/`IsCharacterImage`/`IsImageCapable` recursion (`DataItem.cs:245-300`) | cached init-only fields filled by StorageFormPass; width via `RecordLayout` | Removes ~119 O(subtree) re-walks; single owner (§2.4/§2.6). |
| merge | `OdoModel.PhysicalWidth` + `Sort`/`KeyedIo` geometry | `RecordLayout` | One layout authority; deletes 4 divergent copies (§2.6). |
| refactor | `RedefinesClass.Tier`/`.Width`/`ClassOffset` (`set`) | init-only, written once by `RedefinesClassifier` | Removes set-then-overwrite temporal state (§2.3). |
| delete | ~10 inline Tier-C guards + `RedefinesTier.ByteCanonical` + `StorageForm.TierCWindow` | *(nothing)* | **LANDED (Step D, kb/Work PB164):** the tier dissolved into Tier B rather than being single-sourced — a mixed-USAGE pun needed no separate representation (§2.3). |
| split | `PicInfo.Analyze` (`PicInfo.cs:333-562`) | `PictureAnalyzer.Analyze` | PicInfo becomes a pure value record (§2.7). |
| delete | `PicInfo` skeleton scaffolding (`:175,216,691-705`) | `PicAnalysis` result discriminant | Removes dead sentinels/branches (§2.7). |
| rename | `DataBinder.ResolveIndexItems` | folded into `UsageInheritancePass` | Name matched actual scope (USAGE markers, not index-only) (§2.5/§2.7). |
| delete | `DecodeCobolString` × 3 + hard-coded `"` delimiter tests | `CobolLiteral.Decode` | One decoder; fixes apostrophe VALUE bug (§2.8). |
| refactor | `DataBinder` 9+ public mutable collections (`DataBinder.cs:26-77`) | read-only `BindModel` result object with explicit mutation methods | Encapsulate the blackboard so passes own their writes (§2.4/§2.5). |

---

## 4. Migration — keeping the battery green throughout

The battery (greenfield conformance + unit + 33 characterization + legacy guard NIST 353 MATCH) must stay green **every phase**.
Strategy: introduce the new SSOT **in parallel**, prove byte-equivalence, then flip readers and delete the old fact.

**Phase D0 — Parallel `StorageForm` (no behavior change).**
Add `StorageForm` + `StorageFormPass`, computing `Storage` for every item. Do NOT delete `StoreAsImage` yet — instead
derive it read-only: `bool StoreAsImage => Storage is StorageForm.CharImage { Category: PicCategory.Numeric }`. Add a
unit test asserting, over the whole conformance corpus, that the new pass's per-leaf verdict equals the old
`MarkStoreAsImage` + PIC-derived computation for `ElementType`/`ImageWidth`/`IsCharacterImage`. Battery unchanged.

**Phase D1 — Own `WholeGroupReferenced`.**
Add `UsageCollectionPass` computing `WholeGroupReferenced` from the bound tree. Assert it equals the set the
`ReferenceResolver` mutation produced. Then delete the `ReferenceResolver.cs:280,303` writes and
`CSharpEmitter.MarkStoreAsImage`, having `StorageFormPass` consume the pass's set. Green gate: full battery + legacy
guard (this touches the whole-group path — NC247A/ODO and group-MOVE goldens are the sentinels).

**Phase D2 — Flip readers to `Storage`.**
Migrate the ~119 `StoreAsImage`/`IsCharacterImage`/`ImageWidth`/`FieldType` sites to `Storage`/`RecordLayout`,
file-by-file (FieldEmitter → NumericRenderer → OperandText → Place construction → Sort/Keyed geometry). Each file is
its own commit + battery run. The runtime overload bridge (`Occ`/`StoreDisplay`/`FormatDisplay` triplets) stays
during this phase so emitted text keeps compiling; it becomes redundant-but-harmless once every emit path chooses the
expression from `Storage`.

**Phase D3 — Delete `StoreAsImage` + cache image facts.**
Remove the derived `StoreAsImage`, convert `IsCharacterImage`/`IsImageCapable`/`ImageWidth` to init-only cached
fields filled by StorageFormPass, route width through `RecordLayout`. Delete the 4 duplicate geometry copies.

**Phase D4 — Structural cleanups (independent, each green-gated).**
Slim `DataItem` (immutable core + init-only facts); extract `PictureAnalyzer`; move `StrongTypeModel`; init-only
`RedefinesClass`; single-source Tier-C; `CobolLiteral.Decode` + apostrophe goldens; explicit `BindPipeline` with
Requires/Produces asserts.

**Phase D5 — Retire the overload bridge (optional).**
Once every emit path selects its expression from `Storage`, collapse the `Occ`/`StoreDisplay`/`FormatDisplay`
overload sets to the single form actually reachable per site (or keep them as a documented runtime convenience — see
open Q2). No behavior change; a pure simplification.

**Rollback posture:** D0/D1 are additive with equivalence asserts, so any diff surfaces in the parallel-run test
before a reader is flipped. Each later phase is a self-contained commit with the full battery as the gate.

---

## 5. Risks

1. **Whole-group promotion parity (highest).** `StorageFormPass` must reproduce `MarkStoreAsImage` *exactly*,
   including the fixed-OCCURS-under-whole-group recursion (`CSharpEmitter.cs:59-65`), the OO re-sync
   (`Oo.cs:697`), CompilerTemp clones (`Call.cs:118`), Linkage (`Linkage.cs:275`) and Reports (`Reports.cs:352`)
   sites. *Mitigation:* the D0 corpus-wide equivalence test is mandatory before any deletion; the sentinels are the
   group-MOVE and ODO goldens.
2. **Overload-bridge removal timing.** Removing the runtime overloads before every reader is flipped would break
   generated-C# compilation. *Mitigation:* the bridge is retired LAST (D5), gated on D2 completion.
3. **Tier-C quarantine scope.** Collapsing ~10 guards into one verdict risks accepting a case previously rejected
   piecemeal. *Mitigation:* keep each guard's ISO citation in the single `RejectTierC` reason table; a golden per
   previously-guarded shape.
4. **`RecordLayout` vs emitter drift during migration.** Until FieldEmitter consumes `RecordLayout`, two width
   computations coexist. *Mitigation:* D2 flips FieldEmitter first and asserts width equality across the corpus.
5. **`DataItem` immutability vs existing set-order.** Some facts are legitimately set late (e.g. `Uid` during tree
   build). *Mitigation:* freeze in tiers — core frozen after `BindEntries`, each pass-fact frozen after its pass;
   the pipeline's Requires/Produces asserts catch an out-of-order write.

---

## 6. Open questions for the owner

1. **Tier-C now or later?** Recommend single-source the *rejection* now (§2.3) and schedule the confined-`byte[]`
   codec as a separate increment. Confirm, or direct implementing the codec within this wave (larger, but closes a
   real REDEFINES type-punning gap).
2. **Retire the runtime overload bridge (D5)?** Keeping `Occ`/`StoreDisplay`/`FormatDisplay` polymorphic overloads is
   harmless and reduces churn; removing them makes storage-form selection fully explicit at the emitter. Preference?
3. **Structured `Place` segments (§2.2 item 4)?** Converting `Place.Path`/`OffsetExpr` from raw C# strings to
   structured `item + BoundExpr[]` segments is the clean fix for binder-assembles-emit-C# (`Initialize`/`TryExpandAll`),
   but it is a large, separable change touching every verb. Land it in this wave, or defer to the emitter-decomposition
   wave?
4. **`BindModel` boundary strictness.** Should `Bind()` return a fully read-only `BindModel` (passes mutate only via
   explicit methods), or is init-only-fields-on-DataItem + a thin accessor object sufficient? The former is cleaner
   but a wider refactor of the ~30 public collections.
5. **National / boolean widths under the model.** Today national is one UTF-16 char per position (D-N1) so
   `ImageWidth == Length` (never byte-doubled). `StorageForm.CharImage.Width` inherits that. Confirm national stays
   character-width in the unified model (a future 2-byte layout would be a *new* `StorageForm` case, not a mutation).
