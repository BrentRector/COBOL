# WiseOwl COBOL — REDEFINES / RENAMES (storage overlay) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the WiseOwl COBOL rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §4; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

WiseOwl COBOL stores a datum as its VALUE (PIC 9(4)→a long holding unscaled 1234; PIC X(4)→a 4-char string). REDEFINES (ISO 13.18.44) and RENAMES/level-66 (13.18.45) are byte-image REINTERPRETATION: a second, differently-typed VIEW over the same storage. Value model and byte image coincide only for trivial same-USAGE/same-layout puns; every hard case is hard because they diverge (a PIC 9(4) COMP punned as PIC XX is the two binary bytes, unrelated to the long's value; PIC X(20) punned as -9(9).9(9) reinterprets 20 char positions under an edit template). With NO shared byte[], two typed reps cannot both be live, stored, and coherent. THE ONE CORRECT COHERENCE ANSWER: they do not both exist. A "redefines class" (entries sharing a storage area) has exactly ONE stored backing — the canonical — and EVERY other view is a computed accessor over it (backend-neutral; the primary Roslyn backend renders it as a C# property, a CIL backend as get/set method pairs). Never two stored fields per storage area (the incoherence trap; violates feedback_one_mechanism_per_job). IMPLEMENTED MODEL = 3 tiers, one per class, priority cascade D>B>A: A-Alias (every member elementary with the SAME CLR storage type AND image width: identical PIC+USAGE, or numeric-over-numeric of equal width where each view reinterprets the shared unscaled value via its own scale/NumProfile; plus RENAMES-without-THRU): one typed field, other names are pass-through accessors. B-StringCanonical (the whole class has a BYTE IMAGE — every leaf's storage is a pinned NumericByteForm or a character string): canonical = ONE string of class-max width under the char==byte convention; each view = a typed (offset,width) accessor over it. The window holds the leaf's TRUE BYTES, not its digits (V59): zoned characters for DISPLAY, radix-2 for BINARY/COMP-5/BINARY-*, BCD for PACKED-DECIMAL, big-endian IEEE for the float family, the 8-byte occurrence number for INDEX — §13.18.60.4 GR4/GR11/GR13-GR15 leave each representation to the implementor and CONFORMANCE.md items 205-215 pin ours. Since the Step D arm-1 dissolution this is not merely the dominant case, it is EVERY non-alias, non-rejected case, mixed-USAGE puns included. C-ByteCanonical: DELETED (Step D, kb/Work PB164) — there is no such tier and no RedefCodec. It was reserved for mixed-USAGE puns over usages thought to have "no character storage image"; once every numeric usage had a pinned NumericByteForm, those puns became ordinary Tier-B byte windows over the one string backing, so the separate class-scoped byte[] was never needed. D-Reject loud (spec-forbidden/unmodelable: object/pointer/message-tag/strongly-typed rules 12/14; OCCURS DEPENDING ON / variable-length / dynamic-length rules 5/17): a diagnostic, which is conformant since these are already illegal. RENAMES folds into the same tiers as a COMPOSED view over EXISTING fields (it adds no storage; GR1 no-THRU = attribute inheritance = Tier A; GR2 THRU = alphanumeric group view = Tier B composition over the spanned leaves' display images). The THRU alias's CARRIER is one composed string, but the DATA ITEM is a GROUP item for every rule worded "group item": `ItemCategory.GroupKindsOf` answers Alphanumeric for it and `ItemCategory.IsGroupItem` is the category predicate (never the structural `DataItem.IsGroup`, which a level-66 entry fails for want of subordinates), and `VariableLengthCompatibility.AliasAtoms` gives it its §8.5.1.12 layout from `RenamesInfo.Span` (kb/Work PB907). Tier classification reuses the legacy RecordClassificationPass closure shape (byte propagates across the REDEFINES class + to all subordinates, monotone, terminating), re-verdicted to the lattice A⊑B⊑D (join=max tier). Status (IMPLEMENTED): the model is live — `src/Cobol.Net.Compiler/Binding/Model/RedefinesModel.cs` (RenamesInfo / RedefinesTier / RedefinesClass) + `DataItem` (RedefinesTargetName / RedefinesTarget / Renames / Class / IsCanonical / Renames66); `DataBinder` binds level 66, resolves targets post-build, and classifies via `ComputeTier` — Tiers A+B emit and Tier D rejects loud. The owner fork recorded at `COBOLNET_DESIGN.md` §18 #1 (whether Tier C's persistent class-scoped byte[] was the accepted "bytes only at a boundary" realization) is MOOT: the tier dissolved into B rather than being built or collapsed into D, so Tiers A/B stay 100% typed and no byte[] canonical exists anywhere.

> **Tier-B/Tier-C boundary — the character-image rule (see D10).** Tier B (StringCanonical) admits **only usages
> whose storage image IS a character string** — USAGE DISPLAY alphanumeric/DISPLAY-numeric/edited/alphabetic. A
> **fixed-point BINARY (COMP/COMP-4)** leaf is held in **radix 2** (ISO §13.18.60 USAGE GR4 — "a radix of 2 is used
> to represent a numeric item"; the implementor fixes only alignment, byte width, and sign encoding WITHIN that
> radix, never a radix-10 character image), and a **PACKED-DECIMAL (COMP-3)** leaf in **packed BCD** (§13.18.60
> GR11 — "a radix of 10 is used … each digit position shall occupy the minimum possible configuration in computer
> storage", two digits per byte). Neither is a character image, so a class in which such a leaf's bytes are
> reinterpreted by a differently-represented view is **Tier C**, alongside **float (COMP-1/2), COMP-5 (BinaryCapacity
> stores values beyond the PICTURE digit count), and INDEX** — all interim loud-rejected pending the RedefCodec.
> Rationale: a character view over a BINARY/PACKED leaf must read that leaf's TRUE bytes (radix-2 / packed BCD),
> exactly as any conforming implementation and the external GnuCOBOL differential produce them; storing the leaf as
> a radix-10 zoned character image would collapse USAGE BINARY/PACKED into USAGE DISPLAY (the distinct per-usage
> radix mandates forbid it) and yield the wrong storage width and the wrong reinterpreted bytes. The byte[] canonical
> is the ONE true representation for such a class — not a second one (no §4.1 incoherence trap).

## Decisions

### D1. One canonical stored backing per redefines class + every other view is a computed C# accessor (property) over it — the spine.

**Rationale.** The only way two typed reps over one storage stay coherent WITHOUT a shared byte[] is for one to be stored and the others derived on access. This is the direct fix for the bug that triggered the greenfield pivot: typed writes were invisible through another view.

**Rejected alternatives.** Materialize-on-demand for BOTH views (no single owner of truth → ambiguous which write wins). Keep a byte[] per RECORD (the legacy byte engine — the rejected substrate).

### D2. Never emit two stored fields for one redefines class.

**Rationale.** feedback_one_mechanism_per_job (one canonical pattern per result) + correctness: with no shared storage, a write through one stored field would silently not appear through the other.

**Rejected alternatives.** "Store both, sync on every write": O(views) write fan-out, and the sync direction for a partial cross-field-overlap view is undefined.

### D3. Reject [FieldOffset]/StructLayout(Explicit) overlay outright (no exceptions).

**Rationale.** Cannot overlay a reference type (string) on a long; the dominant real pun is alphanumeric↔numeric which it cannot express; even long-over-long overlays VALUES not the byte REINTERPRETATION COBOL means; forces unsafe/blittable constraints; buys nothing Tier A doesn't already cover idiomatically.

**Rejected alternatives.** Use FieldOffset for the rare all-blittable-same-width class — not worth a second mechanism for a case Tier A handles.

### D4. Tier B canonical is a string (not char[]/byte[]).

**Rationale.** A DISPLAY item's external image IS its characters; the runtime (CobolString) is string-based; views compose with Substring/format.

**Rejected alternatives.** char[] (no value semantics, fights CobolString). byte[] (forces encode/decode on every DISPLAY access — Tier C's cost — for no benefit when content is already characters).

### D5. Tier C confines the byte image to the CLASS, never the record; the byte[] is the PERSISTENT canonical (not materialize-on-demand).

**Rationale.** Honors "bytes only at a boundary" by making the boundary the rare mixed-USAGE class, codec-mediated. It is the ONLY storage for such a class so there is nothing to materialize FROM — distinct from the transient materialize-on-demand used by whole-group-alphanumeric (a different subsystem).

**Rejected alternatives.** Demote the whole 01 record to bytes (the legacy island — far wider blast radius).

### D6. RENAMES folds into the same A/B/C tiers as a COMPOSED view over existing fields.

**Rationale.** RENAMES adds no storage (GR1/GR2); the spanned items already have canonical backings; the rename is an accessor that composes them via each leaf's existing read/write so heterogeneous (numeric-in-span) cases work generally.

**Rejected alternatives.** Give RENAMES its own backing — would duplicate storage and need syncing (the two-stored-fields trap again).

### D7. Tier selection reuses the legacy RecordClassificationPass transitive-closure shape, re-verdicted to the lattice A⊑B⊑C⊑D (join = max tier).

> **Implementation note (`DataBinder.ComputeTier`):** the classifier is a direct leaf-scan over the class members — sufficient for the Tier-A/B + interim-reject cases. The legacy closure's subordinate-propagation shape becomes load-bearing when Tier C and deeper nested-REDEFINES chains are implemented; either port it then per this decision, or record here why the leaf-scan provably suffices.

**Rationale.** That fixpoint (byte propagates across the REDEFINES class + to all subordinates; monotone; terminating) IS the class-membership + tier-propagation algorithm; do not re-derive (feedback_one_mechanism_per_job, feedback_north_star_commercial_quality).

**Rejected alternatives.** Write a fresh ad-hoc classifier (risks re-introducing the subtle propagation bugs the legacy pass already solved + corpus-tested).

### D8. SYNC-aware Tier C offsets come from a ported StorageLayoutComputer, not re-derived.

**Rationale.** SYNCHRONIZED inserts alignment slack that shifts offsets in the shared image; the proven computer already does this correctly.

**Rejected alternatives.** Recompute alignment by hand (error-prone; SYNC offset math is exactly what the legacy already proved).

### D9. A view suppresses only its stored VALUE field; a numeric view STILL emits its NumProfile and carries its PICTURE's natural C# surface type.

**Rationale.** EmitArithAssign emits `<CsName> = CobolNum.Store(value, scale, _P_<CsName>)`; if a numeric view is an arithmetic target and ALL its emit were suppressed, _P_<view> would not exist and generated C# would not compile. Keeping the NumProfile + natural type lets the existing FieldNum/ReadAsString/Store/Compare paths operate on a view unchanged.

**Rejected alternatives.** "Views emit nothing" (too absolute — breaks the emitter contract on the first numeric view targeted by arithmetic).

### D10. A fixed-point BINARY/PACKED leaf that shares a storage area with a differently-represented view forces the class to Tier C (ByteCanonical), not Tier B — its true machine image is not a character image.

**Rationale.** USAGE BINARY is held in **radix 2** (§13.18.60.4 GR4 — "a radix of 2 is used to represent a numeric item"; the implementor fixes only alignment, byte width, and sign encoding WITHIN that radix, not the radix itself), and USAGE PACKED-DECIMAL in **packed BCD** (§13.18.60.4 GR11 — "a radix of 10 is used … each digit position shall occupy the minimum possible configuration in computer storage", two digits per byte). Neither is a character string, so a class in which such a leaf's bytes are reinterpreted by a character (DISPLAY/edited/alphanumeric) view — or by any view of a different representation — is a genuine mixed-USAGE pun: Tier C. `DataBinder.ComputeTier` routes it (with float/COMP-5/INDEX) to the interim Tier-C loud-reject pending the RedefCodec, whose `GetBinary/PutBinary` (radix-2, width + endian) and `GetPacked/PutPacked` (BCD nibbles + sign) render each leaf's TRUE representation; a character view then reads the actual bytes exactly as any conforming implementation and the external GnuCOBOL differential produce them. A COMP/COMP-4 leaf occupies the picture-implied binary width (e.g. PIC S9(4) COMP → 2 bytes), a COMP-3 leaf its packed width (⌈(digits+1)/2⌉ bytes) — NOT a `Pic.Digits`-wide character window.

**Rejected alternatives.** Store the BINARY/PACKED leaf as a radix-10 zoned digit image so the class collapses to Tier B — spec-wrong: it substitutes a character image for the mandated radix-2 / packed-BCD representation, collapsing USAGE BINARY/PACKED into USAGE DISPLAY (which the distinct per-usage radix mandates forbid), over-allocates the width (PIC S9(4) COMP would occupy 4 zoned characters, not 2 binary bytes), and yields the wrong bytes through every REDEFINES/RENAMES/group-move/file/SAME-RECORD-AREA view. A parallel "image profile" field beside `Pic` (two profiles for one storage — the byte[] canonical already carries the one true representation).

## C# mapping (the Roslyn backend's rendering)

> **Backend note (G4 dual backend, `ICodeGenBackend`):** codegen sits behind `ICodeGenBackend` over ONE
> backend-neutral bound tree (`--backend roslyn|cil`); the RoslynBackend (C# source) is primary/v1; a Cecil/CIL
> backend is future-additive with its OWN private structure→branch lowering — NO shared lowered IR; ALL semantics
> live in the binder/bound tree, emitters only RENDER (SSOT §18 #23). For this subsystem: the tier classification,
> `RedefinesClass`, and the one-canonical + computed-accessor-view semantics live in the backend-neutral
> binder/data model (`RedefinesModel.cs`) — "computed accessor" is the backend-neutral concept. This section shows
> how the PRIMARY Roslyn backend renders it as C# properties; a future CIL backend (`--backend cil`) renders the
> SAME model with its own private lowering (get/set method pairs over the same canonical). No REDEFINES semantics
> may exist only in the rendered C#; `BackingCsName` and friends are rendering policy, not semantics.

TIER A (alias): 01 WS-COUNTER PIC 9(4). / 01 WS-COUNT-ALIAS REDEFINES WS-COUNTER PIC 9(4). →
  private static long WS_COUNTER = 0L; private static readonly NumProfile _P_WS_COUNTER = ...;
  private static long WS_COUNT_ALIAS { get => WS_COUNTER; set => WS_COUNTER = value; }   // pass-through property, ONE stored field.
RENAMES no-THRU (GR1) → a property forwarding to the renamed item's field.

TIER B (string canonical — IC101A CCVS pattern): 03 COMPUTED-A PIC X(20) VALUE SPACE. / 03 COMPUTED-N REDEFINES COMPUTED-A PIC -9(9).9(9). / 03 CM-18V0 REDEFINES COMPUTED-A. / 04 COMPUTED-18V0 PIC -9(18). →
  // ONE stored backing, class width = max over views = 20; ONLY the original's VALUE inits it (rule 9).
  private static string _redef_COMPUTED_A = new string(' ', 20);
  // The ORIGINAL is itself a VIEW over the backing (so exactly one stored member):
  private static string COMPUTED_A { get => _redef_COMPUTED_A; set => _redef_COMPUTED_A = CobolString.Store(value, 20); }
  // Alphanumeric/edited view (off,len): get=backing.Substring(off,len); set=StorePunch(backing, off, len, CobolString.Store(value,len)).
  // DISPLAY-numeric view (off,len,scale,signed): get-as-number = CobolNum.ParseDisplay(backing.Substring(off,len), _P_view) → scaled long;
  //   set-from-number = StorePunch(backing, off, len, CobolNum.FormatDisplaySigned(value, scale, _P_view)).
  // IMPORTANT: a view SUPPRESSES only its stored VALUE field; a NUMERIC view STILL EMITS its NumProfile (_P_<view>) —
  //   the accessor needs it, and EmitArithAssign already references _P_<CsName>. Each view's C# surface type = its PICTURE's natural type
  //   (numeric→long, Int128 above 18 digits, alphanumeric/edited→string) so the existing FieldNum/ReadAsString/Store/Compare paths work unchanged.
  private static readonly NumProfile _P_COMPUTED_18V0 = new NumProfile { Digits=18, FractionDigits=0, Signed=true, ... };

RENAMES THRU (GR2 — NC252A: 66 RENAME1 RENAMES NAME1 THRU NAME3): composed accessor over EXISTING fields, NOT a new backing.
  Express composition via each leaf's existing read/write (so HETEROGENEOUS spans work):
  private static string RENAME1 {
    get => ReadImage(NAME1A) + ReadImage(NAME1B) + ReadImage(NAME2) + ReadImage(NAME3A) + ReadImage(NAME3B); // each leaf's DISPLAY image
    set { /* distribute value left-to-right back into the spanned fields by width via each leaf's MOVE-into path */ } }
  (all-PIC X span → plain string concat; a numeric leaf in the span → CobolNum.FormatDisplay on get, the numeric MOVE path on set.)
  ⛔ THE THROUGH ALIAS IS AN ALPHANUMERIC **GROUP ITEM**, not one composed elementary item, and statements whose rules turn on
  that distinction must read it that way. §13.18.45.4 GR2: "When the THROUGH phrase is specified, data-name-1 defines an
  alphanumeric group item that includes all elementary items starting with data-name-2 …" — so in a MOVE it is NOT an
  elementary operand on EITHER side (§14.9.25.4 GR4), which means no receiver-category editing when it sends and no GR6 a)
  operational-sign drop when it receives; `MoveClassifier.IsGroupPlace` is the one reader of that designation and
  `PlaceRenderer.WriteGroupImage` routes a group-image store into one through `WriteRenames` (kb/Work PB430). The
  WITHOUT-THROUGH alias is the opposite by GR1 ("all of the data attributes of data-name-2 become the data attributes of
  data-name-1"), which is why `ReferenceResolver` forwards it to the renamed item's own place and builds no `RenamesPlace`
  at all — the Tier-A/Tier-B split above is exactly this rule split, and the two must stay in step.
  Both forms are built by `ReferenceResolver.PlaceForItem` (its `PlaceForRenames` arm) — the ONE item→place builder — so a
  level-66 reference takes the same tail as any other identifier: a reference-modified alias `RN(2:3)` is the §8.4.3.3.4 GR5
  slice (a `RefModPlace` over the `RenamesPlace`, or over data-name-2's forwarded place), screened by §8.4.3.3.3 SR1 against the
  PLACE's item (the THROUGH alias itself, an alphanumeric group; data-name-2 for the no-THROUGH form), and a zero-length slice
  raises EC-BOUND-REF-MOD unless REF-MOD-ZERO-LENGTH is on (§7.3.23.3 GR1). The by-item entries (`ResolveItem`,
  `ResolveItemRefMod`, `ResolveByName`) reach it the same way (kb/Work PB1380; `RenamesPlaceBuilderDriftTests`).

TIER C (scoped byte[] — mixed-USAGE pun): 01 PUN. / 05 AS-TEXT PIC X(4). / 05 AS-NUM REDEFINES AS-TEXT PIC 9(8) COMP-5. →
  private static byte[] _redef_AS_TEXT = new byte[4];   // ONE stored backing, class width 4 (persistent, NOT materialize-on-demand).
  private static string AS_TEXT { get => RedefCodec.GetText(_redef_AS_TEXT,0,4); set => RedefCodec.PutText(_redef_AS_TEXT,0,4,value); }
  private static long AS_NUM { get => RedefCodec.GetBinary(_redef_AS_TEXT,0,4,signed:false); set => RedefCodec.PutBinary(_redef_AS_TEXT,0,4,value,signed:false); }
  RedefCodec (the ONLY byte surface this subsystem adds): GetText/PutText (Latin-1 lossless), GetBinary/PutBinary (COMP/COMP-5 width+endian+wrap),
  GetPacked/PutPacked (COMP-3 nibbles+sign), GetDisplay/PutDisplay (DISPLAY digits incl. overpunch) — mine legacy PicRuntime/PicDescriptor/PackedDecimal.

MODEL (IMPLEMENTED — `src/Cobol.Net.Compiler/Binding/Model/RedefinesModel.cs` + `src/Cobol.Net.Compiler/Binding/Model/DataItem.cs`): DataItem carries string? RedefinesTargetName; DataItem? RedefinesTarget; RenamesInfo? Renames; RedefinesClass? Class; bool IsCanonical; and List<DataItem> Renames66 on the owning record.
  class RenamesInfo { required string FromName; string? ThruName; DataItem? From; DataItem? Thru; List<DataItem> SpanLeaves; bool IsAlias => ThruName is null; }
  enum RedefinesTier { Alias, StringCanonical, Rejected }   // ByteCanonical DELETED — Step D, kb/Work PB164
  class RedefinesClass { required DataItem Canonical; List<DataItem> Members; RedefinesTier Tier; int Width; string BackingCsName => "_redef_"+Canonical.CsName; string? RejectReason; }
  DataBinder: binds the REDEFINES clause to RedefinesTargetName (resolved after the forest is built) through RedefinesDataName2, which
  screens the WRITTEN operand first — §13.18.44.3 SR6 no qualifier (COBOLNET2915), SR5 no subscript and §8.4.3.3.3 no reference-modifier
  (the one data-name-n screen, COBOLNET2024) — and keeps only the base data-name, never the reference's glued text (kb/Work PB1281);
  binds level 66 via BindRenames —
  attached to the owning record's Renames66 list, NOT the storage tree; a post-build pass resolves REDEFINES/RENAMES targets, groups overlaid
  entries into RedefinesClasses, and runs ComputeTier (cascade D > B > A; a mixed-USAGE pun is an ordinary Tier-B byte-window class since Step D,
  kb/Work PB164 — the remaining Tier-D rejects are the 2-byte national overlay, the §13.18.44.3 SR5 sentence-1
  OCCURS-bearing OBJECT (COBOLNET1701, every OCCURS format), and the dynamic-table SUBJECT that §13.18.44.4 GR1 /
  §8.5.1.9.1 decide against (COBOLNET1525)).

## Hard problems

### Two differently-typed C# reps over one storage must stay coherent with NO shared byte[].

They do not both exist: ONE canonical stored backing per redefines class; every other view is a computed accessor (C# property) over it. Tier picks the canonical (typed field / string / class-scoped byte[]).

### Init rule (REDEFINES SR9): no view (redefiner) may carry VALUE; only the original's VALUE inits the canonical.

Emitter checks item.IsCanonical: a view emits NO stored value field and NO initializer (only the canonical original initializes the backing). Prevents the naive per-field init from clobbering the original.

### A level-01 non-EXTERNAL original may be redefined by something LARGER (SR8 exception) — sizing from the original loses bytes.

Class width = MAX storage width across all views (RedefinesClass.Width); the canonical backing (string/byte[]) is sized to the max, not the original.

### Partial cross-field overlap — a view can span field-1 + half of field-2 (offsets need not align to leaf boundaries).

Model every leaf as a window (offset,length[,usage]) over the concatenated image — the Tier B/C accessor model. A naive field↔field map cannot express it; offset/length accessors can.

### Mixed-USAGE pun (PIC X over COMP/COMP-3/COMP-5/float) — no character string can represent a binary or packed field's bytes.

Tier C: ONE class-scoped byte[] canonical; each leaf is a typed accessor over (offset,length,usage) via RedefCodec (Latin-1 text, binary endian/width, packed nibbles, DISPLAY+overpunch) — mined from legacy PicRuntime/PackedDecimal. Confined to the class, never persisted further.

### Numeric view as an arithmetic target needs its NumProfile, but a view's stored field is suppressed.

Suppress only the stored VALUE field; STILL emit the view's NumProfile (_P_<view>) and give the view its PICTURE's natural surface type so EmitArithAssign / FieldNum / Store work unchanged on a view.

### RENAMES THRU over a heterogeneous span (a numeric leaf inside an alphanumeric group view).

Compose via each leaf's EXISTING read/write: get = concat each leaf's DISPLAY image (CobolNum.FormatDisplay for a numeric leaf, the field for an alphanumeric one); set = distribute the value left-to-right back into each leaf by width via its MOVE-into path. Not raw `+` on a long.

### Signed-DISPLAY overpunch: the byte image of S9(n) carries the sign as an overpunch on a digit, so a numeric view's encode/decode is not plain ASCII digits.

A numeric view's encode/decode is not plain ASCII digits — the sign rides as an overpunch on a digit. `CobolNum` provides `FormatDisplaySigned` (overpunch + leading/trailing separate + binary-minus) and `ParseDisplay` (its full inverse incl. overpunch decode), so Tier-B numeric-view accessors are exact. (A Tier-B numeric view rides the `StoreAsImage` path: its `Read()` is the character window, decoded by `CobolNum.ParseDisplay` in the numeric pipeline.)

### Lossless byte↔char carrier for alphanumeric content that holds binary (0x00–0xFF).

Latin-1 / ISO-8859-1 (Encoding.Latin1): byte b ↔ (char)b, lossless round-trip. CROSS-CUTTING: identical convention needed by whole-group-alphanumeric and file I/O — settle ONCE as a CobolNet.Runtime codepage constant (feedback_one_mechanism_per_job), not a REDEFINES-local convention.

### SYNCHRONIZED inserts alignment slack that shifts offsets in the shared image.

SYNC inside a class can break char-boundary alignment of views → classifier raises the class to Tier C (any doubt → bytes), where a ported StorageLayoutComputer supplies SYNC-aware offsets; SR15 alignment-mismatch → COBOLNET_REDEF_ALIGN diagnostic, not silent.

### REDEFINES inside an OCCURS table element (SR5 forbids OCCURS on the subject/target but allows a redefine inside an element).

The overlay is PER ELEMENT: the array element type (the record struct for the OCCURS group) carries the canonical+accessors, indexed by subscript. ODO anywhere in the class → Tier D.

## Edge cases

- Init only from the original (SR9): suppress initializers + stored value field on every non-canonical class member.
- Larger level-01 redefiner (SR8 exception): class width = max across views, not the original's width.
- Partial cross-field overlap: leaves are (offset,length) windows over the concatenated image.
- REDEFINES inside OCCURS (SR5): overlay is per array element; element type carries canonical+accessors.
- Signed-DISPLAY overpunch (SR-numeric): numeric view encode/decode is not plain ASCII digits; handled by `CobolNum.FormatDisplaySigned`/`ParseDisplay`.
- Alphanumeric view over binary content: needs a lossless 8-bit (Latin-1) carrier — shared with whole-group-alphanumeric + file I/O.
- Multiple redefinitions of one area (SR7): all redefiners name the original → one class, one canonical, anchored via RedefinesTarget closure.
- Group redefiner over an elementary target and vice-versa: width/offsets over leaves; tier decided by the union of all leaves' usages.
- Nested REDEFINES (SR11: data-name-2 may be subordinate to an entry that contains a REDEFINES clause; SR7: every redefinition names the original definer): follow RedefinesTarget transitively to the true non-redefining anchor; one canonical.
- RENAMES single-group (NC252A RENAME3 RENAMES NAME2) → Tier A forwarding to the group's composite read.
- RENAMES sub-span (RENAME2 RENAMES NAME1A THRU NAME1B) → Tier B composition over just the spanned leaves; data-name-3 not subordinate to / not before data-name-2 (SR11) — binder validates a forward sibling range.
- RENAMES must immediately follow the record's last entry (SR2), qualified only by 01/FD/SD (SR3): attach 66 entries to the owning record, not into the storage tree.
- Group MOVE/compare of a redefines original: read = the canonical (field/string/materialized byte image), write normalizes through the canonical's set; a group comparison/CORR lowers field-wise and never RAISES the tier.
- EXTERNAL/GLOBAL with REDEFINES (GLOBAL GR3: only the subject is global): cross-program identity deferred to the LINKAGE/EXTERNAL subsystem; the redefines canonical = the externalized member.
- Numeric view wider than 18 digits: parses to Int128 (the numeric subsystem's escape hatch), not long.
- Reference modification of a view: composes with the accessor's own offset (Tier B = substring of substring; Tier C numeric = ref-mod over the materialized image, shared with whole-group-alphanumeric).
- Tier D loud-reject classes, one screen per rule (⛔ this line used to read "OCCURS DEPENDING ON / variable-length / dynamic-length (SR5/SR17)", which cited a FOUR-SENTENCE rule by number and left it unknowable which sentence was meant — and sentence 1's population was in fact screened nowhere):
  - object/pointer/message-tag/strongly-typed — §13.18.44.3 SR12/SR14, per written entry (COBOLNET1697). ⛔ SR14's "an item subordinate to a strongly-typed group item" has NO same-root exemption (kb/Work PB1282): a subject and its data-name-2 are siblings and so share one strong root, which made the disjunct vacuous, and the one way to meet it is a REDEFINES written inside a STRONG typedef template — §13.18.58.4 GR3 makes it part of every TYPE subject, so it is rejected once per instance (COBOLNET1697; the RENAMES twin, §13.18.57.3 SR3, was always unexempted). A REDEFINES inside a WEAK template stays legal. §13.18.57.3 SR4 (COBOLNET1532) keeps an EXTERNAL redefinition of a strong item.
  - data-name-2's own entry carries an OCCURS clause, ANY format — §13.18.44.3 SR5 **sentence 1** (COBOLNET1701). Sentence 2 expressly permits data-name-2 to be SUBORDINATE to such an item, so the test is on its own entry.
  - an occurs-depending table in either definition — §13.18.44.3 SR5 **sentence 4** (COBOLNET0855), whose population is wider than sentence 1's and owns the overlap.
  - either side a variable-length group (§8.5.1.12.1) or a dynamic-length elementary item — §13.18.44.3 SR17 (COBOLNET1698).
  - the SUBJECT that IS a dynamic-capacity table — no syntax rule names it; §13.18.44.4 GR1's fixed association area against §8.5.1.9.1 (COBOLNET1525).
  These are already illegal, so a diagnostic is conformant.
- **The NESTED pointer/object leaf and its backstop (kb/Work PB179 → PB183).** `ComputeTier` keeps a Tier-D arm for a pointer/object-class LEAF nested inside a member — which is NOT §13.18.44.3 SR12/SR14's letter (those name the ENTRY-level items, per the drafting contrast with SR9's "nor any entry subordinate to it"). That leaf's real defect is in its own DECLARATION: **§13.18.60.3 SR14** confines the five pointer/object usage phrases to an elementary item at level 1 (or 77) or one subordinate to a STRONG type declaration, so a nested pointer leaf under an ordinary group is nonconforming before REDEFINES is reached. That screen now exists — `CheckUsageDeclarations` (`DataBinder.UsageDeclaration.cs`, COBOLNET1724), a declared bind pass running well ahead of `ClassifyRedefinesClasses` — and the backstop is therefore unreachable-by-construction on conforming source. ⛔ **It is KEPT, not deleted:** it remains the loud guard for the recovery paths, and without it such a leaf reaches the byte-window machinery as a silent zero-width Tier-B alias, which is the exact outcome the tier model exists to prevent. `PointerObjectClass` is now THE ONE class predicate shared by both screens (pinned by `UsageDeclarationPlacementDriftTests`), so the REDEFINES rule's class list ("class object, message-tag, or pointer") and the USAGE rule's phrase list cannot drift apart as MESSAGE-TAG and FUNCTION-POINTER gain models — and it is the STORAGE predicate too, since kb/Work PB231 defined it as `SlotWindow.CarriedBySlot` ("does this member ride the area's managed slots rather than its bytes?"), which is the same population for the same reason.
  ⛔ **THE ARM'S REASON CHANGED WITH kb/Work PB231's pointer third (2026-09-05), and the change is worth stating precisely.** A pointer-class leaf is no longer refused by the shared byte-window carriage gate `DataBinder.ByteWindowResidueOf` — it RIDES a shared storage area, on that area's MANAGED SLOTS (D-SLOT in `COBOLNET_DATA_MODEL_DESIGN.md`). What a REDEFINES class lacks is the CARRIER: the slots live on a `StorageCell`, which the three cell surfaces (EXTERNAL, ADDRESS OF, BASED) have and a REDEFINES class's plain stored `string` backing does not. That is a property of the carrier, not of the leaf, so it is asked HERE, once, rather than smuggled back into the shared gate — which would have re-created exactly the two-arm divergence PB231's collapse removed. `RedefinesClass.IsCellBacked`, set by the ONE forcer `ForceStringCanonical`, is the test.
- **SR14 decides what a group with a pointer leaf can even BE.** A consequence worth stating here because it bounds this subsystem's input: on conforming source the ONLY way to write a group containing a pointer/object-class leaf is inside a `TYPEDEF STRONG` template. Every other spelling is now rejected at the declaration.

## ISO citations

- ISO/IEC 1989:2023 §13.18.44 REDEFINES — SR4 (no lower level-number between target and subject)
- §13.18.44 SR5 (no OCCURS on subject/target; no occurs-depending table either side)
- §13.18.44 SR7 (multiple redefinitions of one area each name the original)
- §13.18.44 SR8 (subject size ≤ target, except a level-01 non-EXTERNAL target may be redefined larger)
- §13.18.44 SR9 (no VALUE on the subject or any subordinate, except level-88)
- §13.18.44 SR10 (the new descriptions follow the target's area without intervening new areas)
- §13.18.44 SR11 (data-name-2 may be subordinate to an entry that contains a REDEFINES clause)
- §13.18.44 SR12 (REDEFINES not for class object/message-tag/pointer or a strongly-typed group)
- §13.18.44 SR14 (data-name-2 not object/message-tag/pointer/strongly-typed or a member thereof)
- §13.18.44 SR15 (required alignment of the subject must match the target's)
- §13.18.44 SR17 (neither side a variable-length group or dynamic-length elementary item)
- §13.18.44 GR1 (storage starts at the target's first bit; the larger of the two sizes wins; the target's own reference size is unchanged)
- §13.18.44 GR2 (any of the names may reference the shared area)
- §13.18.44 GR3 (under VALIDATE each redefinition is checked independently; PRESENT WHEN selects)
- §13.18.45 RENAMES — SR2 (entries immediately follow the record's last data description entry)
- §13.18.45 SR3 (data-name-1 qualification rules; none subject to OCCURS)
- §13.18.45 SR4 (data-name-2/3 are elementary or groups in the same record, not the same name)
- §13.18.45 SR5 (data-name-2/3 not level 1/66/77/88)
- §13.18.45 SR7 (data-name-2/3 not subscripted)
- §13.18.45 SR8 (range excludes object/message-tag/pointer/strongly-typed/variable-length/occurs-depending items)
- §13.18.45 SR10 (the renamed area defines an integral number of bytes)
- §13.18.45 SR11 (data-name-3 begins at/after data-name-2 and ends after it; data-name-3 not subordinate to data-name-2)
- §13.18.45 GR1 (no-THRU: all attributes + storage of data-name-2 become data-name-1's)
- §13.18.45 GR2 (THRU: data-name-1 is an alphanumeric group view spanning data-name-2's first elementary through data-name-3's last)
- §13.18.27 GLOBAL GR3 (with REDEFINES, only the subject possesses the global attribute)
- §14.9.25 MOVE rules (GR4 byte preservation for same-representation group moves)
- §13.18.55 SYNCHRONIZED (alignment slack shifts offsets in the shared image)
- §8.5.3 Types (strongly-typed item rules referenced by REDEFINES SR12/14)

## Per-edition behavior (G1 — four compilers in one executable)

Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. REDEFINES (§13.18.44) and level-66 RENAMES (§13.18.45) exist in ALL four supported editions (ISO COBOL 1985 / 2002 / 2014 / 2023); the storage-overlay core and the 4-tier model are edition-stable, so this design applies unchanged at every `--std`. The edition-VARYING surface enters only through item categories the 2023 syntax rules reference that earlier editions lack: SR12/SR14 (class object / pointer / strongly-typed — 2002+; message-tag — later, verify intro edition against the spec's substantive-changes annex), SR17 (dynamic-length elementary items — 2023), GR3 of §13.18.44 (VALIDATE / PRESENT WHEN — 2002+). Under a `--std` lacking the category, the item is rejected at its DECLARATION by the owning subsystem's edition gate (per `VERSION_CHANGE_REFERENCE.md`, the 130-row edition-change checklist — 2002→2023 deltas ONLY; it has NO 85→2002 rows, so derive 85↔2002 gating from the 2002 standard), so this subsystem's Tier-D reject never fires there; where it does fire, the diagnostic must cite the rule as it exists in the TARGET edition, not blindly the 2023 SR number. MATRIX WORK (TODO): `VERSION_CHANGE_REFERENCE.md` carries no REDEFINES/RENAMES rows today — verify per edition that the core rules really are edition-stable (in particular whether SR8's larger-level-01-redefiner exception holds in COBOL-85) and either record the verified "edition-stable" verdict or add the changed rows; add (construct × edition) cases for REDEFINES/RENAMES plus the Tier-D categories to the version test matrix (`VERSION_TEST_MATRIX_DESIGN.md` — the (construct × edition) matrix; Phase 0 done).

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- OWNER FORK — RESOLVED (`COBOLNET_DESIGN.md` §18 #1): Tier C's PERSISTENT class-scoped byte[] canonical IS the accepted realization of "bytes only at a boundary" (Tiers A/B stay 100% typed incl. the entire near-term DISPLAY-homogeneous NIST path). The alternative (collapse C into D for zero-byte purity) remains owner-VETOABLE per §18 #1 but is not the plan. Implementation status: the RedefCodec is not yet built — `ComputeTier` interim-rejects would-be Tier-C classes loudly.
- Latin-1 lossless 8-bit carrier: **SETTLED** (`COBOLNET_DESIGN.md` §14.9 + §18 #13) — `Encoding.Latin1` is the ONE byte↔char boundary codepage constant in `CobolNet.Runtime`, shared by file serialization, REDEFINES Tier-C, and the whole-group image.
- Signed-DISPLAY overpunch: **SETTLED** — `CobolNum.FormatDisplaySigned`/`ParseDisplay` (overpunch + leading/trailing separate sign) implement the exact encode/decode; Tier-B numeric-view accessors are exact.
- EXTERNAL/GLOBAL redefines identity: defer cross-program canonical-member selection to the LINKAGE/EXTERNAL subsystem.
- Int128 views (>18 digits): align numeric-view parsing with the numeric subsystem's Int128 escape hatch.
- Tier C distinction from materialize-on-demand: **SETTLED** (`COBOLNET_DESIGN.md` §14.4) — they ARE separate mechanisms: Tier C's byte[] is PERSISTENT (the only storage for a mixed-usage class); the whole-group `AsImage()`/`FromImage()` image is transient, built on demand, never persisted.
