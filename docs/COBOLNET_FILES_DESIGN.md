# WiseOwl COBOL — File I/O (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the WiseOwl COBOL rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §8; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Deep, decision-complete design for the WiseOwl COBOL FILES subsystem (typed records, clean byte boundary). Core architecture: the FD or SD record IS a .NET record struct (the record area is a typed field, not a byte buffer) — including for a file description entry written with NO record description entries, which §13.4.5.3 SR3 permits and which gets §14.9.30.4 GR6's implied entry synthesized at bind time (D18); the only bytes are at the on-disk edge, produced by a compiler-GENERATED per-layout codec (Serialize and Deserialize) running only at READ and WRITE. CODE-SET is one Encoding parameter threaded into that codec. The proven 364-NIST legacy handlers are ported VERBATIM for control logic (open-mode tables, ISO-cited status codes, the file-position-indicator plus key-of-reference plus duplicate-ordering state machines) but re-substrated from a byte array to a generic FileConnector plus an IRecordCodec. Covers all organizations (SEQUENTIAL, LINE SEQUENTIAL, RELATIVE, INDEXED), all access modes, OPEN CLOSE READ WRITE REWRITE DELETE START (CLOSE dispatching on the §14.9.6.4 GR2 physical-file category through Table 14 — D11), FILE STATUS as a two-char string item, variable-length records, prime and composite ALTERNATE keys, SAME RECORD AREA, SORT and MERGE, and LINAGE. Ordering and lookup keys are a typed-derived CobolKey comparable (numeric by decoded value, alphanumeric by image plus collating, composite component-wise), decoupled from the stored payload; one comparison policy shared by indexed files and SORT.

## Decisions

### D1. The FD or SD record is a typed record struct (the record area); there is no per-file byte buffer. Bytes exist only transiently in a compiler-generated codec at the disk edge.

**Rationale.** Owner lock: a COBOL record is a .NET record struct and file I/O is the only legitimate bytes. A generated per-layout codec keeps the model native and the serialization fast and inspectable in the generated source.

**Rejected alternatives.** Keep a byte-array record area (the banned legacy substrate); interpret PIC at runtime (slow, untyped).

### D2. Port the three proven legacy handlers control logic verbatim but re-substrate to a generic FileConnector plus IRecordCodec.

**Rationale.** That logic encodes hundreds of ISO 9.1.13 and 14.9.30-51 edge cases proven by 364 NIST tests; re-deriving it would re-discover every bug the legacy already fixed (stale alt-index, START inclusive-FPI, 43 and 46 read-position, ascending-WRITE 21, duplicate-key 02 and the GR26 duplicate order). It is orthogonal to the byte-vs-typed substrate; one connector per organization sharing one codec is the singular pattern.

**Rejected alternatives.** Re-derive file semantics from scratch (loses 364 tests of edge-case knowledge); a single mega-connector with org branches (less cohesive).

### D3. Ordering and lookup keys are a typed-derived CobolKey comparable, decoupled from the stored payload: numeric by decoded value, alphanumeric by character image (ordinal or collating-weight, shorter-operand space-extension), composite component-wise.

**Rationale.** One comparison policy shared by indexed files and SORT. Fixes the legacy Latin-1 byte-string sorted-dictionary trick that silently mis-ordered COMP, COMP-3, and signed keys (their on-disk image is not order-preserving). Matches the spec rule to compare the key data item (14.9.30 and 41).

**Rejected alternatives.** Order by serialized key bytes (concedes the binary-key ordering bug and forks policy from SORT); a key projector by field offset (reintroduces an offset model); a non-generic connector over object (boxing per field access).

### D4. The relative and indexed connector in-memory model stores the serialized byte image per record as the payload (keyed and ordered by the typed CobolKey), deserialized to the typed record only on hand-back.

**Rationale.** The image is exactly the form persisted on CLOSE (one representation), bounded memory regardless of record-graph richness, and ordering is still typed; bytes never escape the connector except via the codec. Surfaced explicitly as owner-visible because an in-memory byte array is the exact thing the owner objected to for program data, but here it is legitimate confined file bytes.

**Rejected alternatives.** An all-typed sorted-dictionary-of-typed-records store (viable, more native, but holds the full object graph and gains nothing semantically; a one-line flip if the owner prefers it).

### D5. Multiple 01s under one FD (and SAME RECORD AREA) are a discriminated record-area wrapper: READ deserializes the raw bytes into EVERY 01 view; WRITE serializes the named view.

**Rationale.** ISO 9.1.2 NOTE and 13.18.33 GR3: all 01s under an FD implicitly redefine the same storage area. Round-tripping every view off the same raw bytes reproduces the COBOL byte-overlay (read alphanumeric then inspect as numeric) without a persistent byte substrate. It is the file-edge analogue of REDEFINES.

**Rejected alternatives.** A single shared byte-array record area for multi-01 FDs (the banned substrate); ignore the overlay (breaks reinterpretation-of-record NIST patterns).

> **Realization:** the shared area is realized as a synthesized REDEFINES class over the 01s (the
> REDEFINES deep-dive's tiers), not a separate wrapper type — the file edge and REDEFINES share ONE overlay
> mechanism (singular-pattern). With a fixed-point BINARY/PACKED leaf among the records (ST134A's SAME RECORD AREA
> pair) the class is Tier B: one string backing IS the record area (§12.4.6.4.4 GR2 — "an implicit redefinition of
> the area, with records aligned on the leftmost byte position"), each record/leaf a window accessor, binary leaves
> image-stored with the overpunch profile rewrite (REDEFINES deep-dive D10).
>
> **One storage fact, two constructs (kb/Work PB836).** The class is shared, the RULES are not. Every member
> records WHICH construct joined it — `DataItem.RedefinesKind`: `Clause` (a written REDEFINES, §13.18.44),
> `ImplicitFileRecord` (§13.18.33.4 GR3) or `SameRecordArea` (§12.4.6.4.4 GR2), written only together with
> `RedefinesTarget` through `DataItem.SetRedefinition`. The §13.18.44.3 REDEFINES-clause syntax rules (SR5, SR8,
> SR12, SR14, SR16, SR17) screen `Clause` members only — an implicit redefinition has no clause and no
> data-name-2. What DOES reach the implicit case is §13.18.57.3 SR4 ("shall not be implicitly or explicitly
> redefined"): a strongly-typed item anywhere in a record that shares its area is COBOLNET1532, tested on BOTH
> sides of the pair because the sharing is symmetric. A dynamic-length, variable-length-group or pointer-class
> record never joins the class at all — it is an OUT-OF-LINE record (D27). What can still be staged loud as
> COBOLNET0899 (`implicit-record-area-shape`) is a character-window record with a byte-window residue — never
> borrowed from a REDEFINES rule. A report
> description's level-1 entries are NOT implicit redefinitions (§13.18.33.4 GR3's second sentence) and never join a
> class.

### D27. A record with no character window is an OUT-OF-LINE record: it is never linked into the area's class, and it shares the area at the transfer boundary (kb/Work PB981; determination D-FRA, docs/CONFORMANCE.md §3).

**The rule and the latitude.** §13.18.33.4 GR3 makes every level-1 entry of an FD/SD an implicit redefinition of
one area, and nothing forbids such a record from being a dynamic-length item, a variable-length group
(§8.5.1.12.1) or a pointer-class item (§13.18.60.3 SR14 admits a pointer "only for an elementary data item at level
1"). The standard also leaves where such an item LIVES to the implementor: *"Dynamic-length elementary items may be
physically located in memory within the record they are subordinate to, or they may be located elsewhere in the
computer's memory"* (§8.5.1.10.3), its internal structure is implementor-defined (§8.5.1.10.2), and a pointer here
has no character image (A.1 items 210/216). What it does NOT leave open is the procedural view: a variable-length
item *"behaves in all respects as though it were in fact contiguous with its neighbors whenever a procedural
operation is applied to a group containing it"* (§8.5.1.11.2).

**The decision — one predicate, two halves of one area.**
- `FileModel.IsOutOfLineRecord` is THE predicate (a dynamic-length elementary record, a variable-length group, a
  pointer-class elementary record). `DataBinder.LinkImplicitRecordArea` — the ONE linker for the FD and SD arms —
  and the SAME RECORD AREA arm link only character-window records, anchored on `FileModel.CharacterAnchor`; each
  file of a record-area SAME clause learns its `SameRecordAreaPeers`.
- **The SAME clause is ONE membership model, and its syntax rules are one screen** (`DataBinder.IoControl.cs`;
  kb/Work PB1087, PB1242). `FileModel.SameClauses` records every clause as a `SameClause` — a `SameClauseKind` of
  `Area` / `RecordArea` / `SortMergeArea` (SORT and SORT-MERGE are ONE format, §12.4.6.4.3 SR1; `Written` keeps the
  spelling for messages), its resolved members and its words. `ScreenSameOperands` asks a clause its own questions
  (two or more distinct files, §12.4.6.4.4 GR1/GR2; SR2 each is a file of THIS source element; SR3 none EXTERNAL);
  `ScreenSameClauses` asks the paragraph's clauses together, from one occurrence table (file kind × format → how many
  clauses, `SameOccurrenceRule`: SR5 report, SR6 sort or merge, SR7 any other file), SR8 (a sort-merge-area clause
  names a sort or merge file), SR9 / SR10 (a file-area clause is wholly inside the record-area / sort-merge-area
  clauses it shares a file with) and §13.18.27.3 SR2 (no GLOBAL on the FD or records of a record-area member). One
  code, `COBOLNET2738`, names the rule in its message. The SORT and MERGE statements read the same model
  (`SortBinder.ScreenSameClauses`, §14.9.40.3 SR10 / §14.9.24.3 SR11).
- `FileModel.AreaRecord` is the largest CHARACTER-window record (the first record only when every record is out of
  line); `FileModel.OutOfLineRecords` is every other out-of-line record of the file and of its SAME peers.
- **READ / RETURN** go through ONE store, `SequentialIoEmitter.EmitRecordAreaStore`: the character half takes the
  area image (a group record distributes it through `FromImage`; an ELEMENTARY record — a record description entry
  may be one elementary item of any usage — decodes its own representation from the bytes through
  `ReceivingStore.StorageArea`, the receiving inverse of `OperandText.AsStorageImage` that GR4's group MOVE into an
  elementary receiver also rides, kb/Work PB1556); each out-of-line record takes the CURRENT RECORD at its own length
  (`FileConnector.CurrentRecord`, written only through `NoteRecordRead` beside `LastReadLength`; a RETURN's
  returned image is already that). A variable-length group decomposes it through its generated
  `FromContiguousImage(record, extents)` → `CobolContiguousLayout.Decompose` → the ONE split rule
  `CobolVarGroup.FromContiguous` — by the record's own EXTENT TABLE (below) when it describes the record, otherwise
  by the take step (each dynamic component, left to right,
  takes as many whole units as the record holds beyond the fixed material still to come, up to its maximum; in a
  file record the components are dynamic-length items only, since a dynamic-capacity table "may be defined in any
  place, other than the file section", §8.5.1.9.1 3), and the rule's element-unit arm serves the generic
  carrier — the
  exact inverse of the composer for one variable-length member, the earlier component taking the excess for
  several when the record carries no extent table); a dynamic-length record takes the record as its content
  (§8.5.1.10.4); a pointer record is not reached.
- **WRITE / REWRITE / RELEASE** send `OperandText.RecordSendImage`, which is `OperandText.RecordAreaImage` — whose
  out-of-line arms are the contiguous `CurrentImage()` of a variable-length group
  (`PlaceRenderer.VarGroupCurrentImage`), a dynamic-length record's content, and the zero-length image of a pointer
  record — except that a record holding a member that names a DYNAMIC LENGTH STRUCTURE is sent in its MEDIUM form
  (below).
- **THE EXTENT TABLE** (kb/Work PB1053; CONFORMANCE.md §3 D-FRA (v)). With two or more variable-length components
  the contiguous image cannot be inverted, so a variable-length group record is sent beside
  `OperandText.RecordAreaExtents` — the generated `CurrentExtents()` (`CobolContiguousLayout.ExtentsOf` over
  `AsVarImage()`), or `StorageCell.ContiguousExtentsAt` for a cell-backed group, arm for arm the twin of the image
  (`PlaceRenderer.VarGroupCurrentExtents`). It is a `RecordExtents` (each component's fixed-run offset and current
  length) passed as the named `areaExtents` argument of `CobolFile.WriteShared` / `RewriteShared` /
  `ReadKeyedShared` / `StartIndexed` / `DeleteShared` and of `CobolSort.Release` / `ReleaseStatement`. It is framing,
  never record data (§12.4.5.11.4 GR1): `RecordFraming` writes it after a frame's length word, flagged by bit 31 of
  that word, in both the stream shape (a RECORD VARYING record-sequential file) and the store shape (relative and
  indexed — `StoredFrame`); the sort store and `KeyedRec` hold it beside the record; a READ / RETURN hands it back
  as `FileConnector.CurrentRecordExtents` / `CobolSort.LastReturnedExtents`, written only through `NoteRecordRead`.
  `CobolContiguousLayout` honours it only when `RecordExtents.Describes` accepts it — the receiving group's
  components at the same fixed-run offsets (§8.5.1.12.2) and the record exactly the characters it describes — so a
  record written through another description, cut to another length, or held where no frame exists (a LINE
  SEQUENTIAL line, a fixed-length block) is split by the take step. `Position` reads the SAME two sources, so
  `IndexedConnector.KeyOf` / `AreaKey` and `CobolSort.Key.At` find a key where the decomposition puts it. A
  sequential in-place REWRITE keeps the frame's size (§14.9.35.4 GR16): it replaces a table of the same component
  count and otherwise writes `RecordFraming.VoidExtents` (offsets -1, corresponding to no layout).
- **THE MEDIUM FORM — a DYNAMIC LENGTH STRUCTURE** (kb/Work PB1094; CONFORMANCE.md §3 D-DL3). A dynamic-length member
  whose DYNAMIC LENGTH clause NAMES a structure (§13.18.19.3 SR2) occupies, in the record a file holds, its data
  prefixed by the length field and followed by the delimiter (§12.3.7.4 GR18, GR19): `[length field][data][delimiter]`.
  The record therefore has TWO FORMS — the *contiguous* image (the data alone: what `CurrentImage()`, a MOVE, a
  comparison and READ … INTO see, §8.5.1.11.2) and the *medium* image (what a connector or the sort store holds, each
  structured member framed). `CobolContiguousLayout` is the one place both are made: `MediumImage` + `MediumExtents`
  turn a sent record's `(CurrentImage(), CurrentExtents())` into the medium pair, and `Decompose` reads the medium image
  back — the frame's extent table keeps describing the STORED image, so `Describes`, `Position` (a key behind a
  structured member), the sort store, `ToFixedForm` (each member at its maximum EXTENT, length field and delimiter
  included) and the READ landing need no second rule. The layout of each member is a `CobolDynStructure`
  (`FileModel.StructureOf` is the one reader that turns a declaration into it), carried by the record type's layout as
  `Structure` codes. **The send channel** is `OperandText.RecordSendImage` / `RecordSendExtents` (WRITE, REWRITE, RELEASE,
  and the keyed READ / START / DELETE whose key is read out of the same image) — `RecordAreaImage` stays the procedural
  contiguous view and is what READ INTO sends; a record with no structured member (`FileModel.CarriesStructure`)
  answers the same strings as before, so nothing else renders differently. A dynamic-length ELEMENTARY record has no
  extent table (its image is its one component), so its structure alone says where the data ends on a READ
  (`CobolDynStructure.UnframeWith`, in `EmitOutOfLineInto`). **Sizes include the structure:** `FileModel.MaxDynamicExtent`
  and `MinExtent` add each structured member's length field and delimiter (the record on the medium physically
  contains them, §13.18.43.4 GR8), so GR9 / GR10, `RecordMax`, `VaryMin` / `VaryMax`, the key windows
  (`RecordLayout.KeyWindowOf`) and the §13.18.43.3 SR3 / SR4 comparisons all count them from that one place; the
  DEPENDING ON item holds the medium length. **LINE SEQUENTIAL** cannot hold the structure at all (DOC-A.1-115: the
  characters below U+0020 are outside its character set, and the length field's first byte and the delimiter are
  such characters), so a WRITE of a structured record to a LINE SEQUENTIAL file is the '71' of §14.9.51.4 GR23.
  Reading with NO table (a file another program wrote, or a Format 1 file) takes the structured members apart by their
  own structure (`CobolVarGroup.ComponentTake` is the one step of the walk, read by `FromContiguous` and `Position`);
  an UNSTRUCTURED member then takes what is left beyond the fixed material and the later structured members' minimum.
- **The implied RECORD clause** (§13.18.43.4 GR5, implementor-defined) is Format 2 exactly when a record is
  variable-length (`FileModel.ImpliesVariableFormat`; `RecordSizeVaries` is the one question every registration
  and `SortBinder.SortVaryingOf` ask). `MaxRecordSize` counts a dynamic-length item at its maximum size (GR8 b) —
  the only variable-length member a record can hold, since COBOLNET1526 refuses a dynamic-capacity table under any
  FD / SD record (§8.5.1.9.1 3); kb/Work PB1604); `FileModel.RecordMax` reaches the connector as `RecordMax`, the
  §14.9.30.4 GR14/GR15 truncation bound, separately from the area width the image is padded to — an unlimited
  dynamic-length record must not size every READ's area buffer.
- **SD records.** `SortBinder.SortRecordOf` admits a variable-length record whose extent composes.
- **A key that FOLLOWS a variable-length member** (kb/Work PB1025; CONFORMANCE.md §3 D-KWV) — a SORT/MERGE key or an
  indexed RECORD KEY / ALTERNATE RECORD KEY — has no fixed position in the contiguous image, so it is located PER
  RECORD. `RecordLayout.KeyWindowOf` is the ONE bind-time answer: the key's FIXED-run offset and its furthest reach
  (every preceding member at its maximum), read by `SortBinder.SortAddFileKeys` (§14.9.40.3 SR6 g) /
  §14.9.24.3 SR4 g), COBOLNET0874), the `FileControlKeyRules` rows for §12.4.5.12.3 SR4 and §12.4.5.6.3 SR5
  (COBOLNET0863) and `KeyedIoEmitter.KeyWindow`. At run time the record type's ONE `CobolContiguousLayout`
  (emitted beside `FromContiguousImage` as the static `__contiguous`, exposed through `__Contiguous`) serves both
  the decomposition and `Position` — the record's extent table, else the same take step
  (`CobolVarGroup.ContiguousTake`) — so `CobolSort.Key` and
  `IndexedConnector.KeyOf` find a key exactly where a READ / RETURN puts it. An indexed connector with such a key
  (`_layoutKeys`) takes every key from the record AS WRITTEN (`AreaKey`; the WRITE / REWRITE frame), never from an
  image fitted to the area width, because truncation would move the key.
- **EXTERNAL** (kb/Work PB1026). §13.18.22.4 GR4 b) makes "the data contained in all record description entries"
  of an EXTERNAL file external, so BOTH halves of the area are run-unit storage (`CallBindExternalAndGlobal`): the
  character half is ONE cell keyed `FD::<externalized name>`, re-based through `FileModel.CharacterAnchor`, and
  each out-of-line record gets a cell of its OWN, keyed `FD::<name>#<k>` by its ordinal k among the file's
  out-of-line records in declaration order — the key every describer computes from its own FD, since GR5
  externalizes the FILE name and never a record name. Each such cell is the ordinary cell-backed storage shape
  (`ForceStringCanonical`): a dynamic-length record's content and a variable-length group's dynamic members ride
  the cell's DYNAMIC SLOTS (`Place.DynSlotWindow` / `VarGroupWindow`, COBOLNET_DATA_MODEL_DESIGN D-SLOT), a
  pointer record its managed slot. The READ / WRITE halves above are unchanged: they already go through
  `PlaceRenderer`, whose cell arms compose and decompose the same contiguous image. It used to be staged loud
  (COBOLNET0899) — the character half alone would have split GR4 b)'s one area in two.

**Rejected alternatives.** Stage the shapes loud (the PB836 posture — rejects legal source); make the record area a
cell with managed slots for a pointer record (the pointer still has no character image to send, and a dynamic
member still has no fixed window); give an UNSTRUCTURED dynamic-length member a length prefix in the record (the
prefix is the DYNAMIC LENGTH STRUCTURE clause's representation, §8.5.1.10.2, and a member whose clause names no
structure has not asked for it; the extent table carries the same lengths in the FRAME instead, outside the record.
A member that NAMES a structure is given exactly that form in the medium image — the bullet "THE MEDIUM FORM" above —
while the contiguous procedural view of §8.5.1.11.2 stays the data alone); refuse at bind a record whose layout the take step cannot invert (legal
source); the take step alone for every record (not invertible with two or more variable-length components — kb/Work
PB1053).

### D6. SORT and MERGE: the SD record is a typed struct; the sort store holds serialized images ordered by the same CobolKey policy; SORT key offsets are computed into the deterministic serialized image at compile time. Format-2 in-place table SORT operates on the typed array directly.

**Rationale.** Reuses the proven stable sort plus numeric-value-versus-collated-image key rule (14.9.40 SORT / 8.8.4.2.4 numeric compare; DUPLICATES IN ORDER is a stable sort). Table SORT data is already typed in memory so it diverges to a typed comparer, the one documented place the two SORT forms differ.

**Rejected alternatives.** A true k-way priority-queue merge (a perf optimization, deferred); a byte comparer for table sort (the data is already typed).

**Correction (kb/Work PB1175).** "Operates on the typed array directly" holds for a table with an array of its own. A table whose storage is a SHARED byte area (a REDEFINES class, a record area shared by several 01s of one FD, a BASED/EXTERNAL record) has no element array, so `BoundTableSort.Storage` says which form the statement takes (`TypedArray` / `SharedArea`) and the shared form sorts the element order by the same typed key comparer — each key read through its own window — then places the element images back through the class's windows. The comparison is one policy; only element access differs.

### D7. The serialized record image of a MIXED-USAGE record (fixed-point BINARY/PACKED leaves beside string-stored leaves) is the generated `AsImage()`/`FromImage()` character image — each such leaf contributes its fixed-width ZONED DIGIT image (`Pic.Digits` chars, trailing-overpunch sign), per `PicInfo.ImageSignKind`.

**Rationale.** ISO §13.18.60 USAGE GR4 makes a binary item's representation — including the algebraic sign — implementor-defined; `COBOLNET_DESIGN.md` §14.4's total digit-image rule defines it ONCE for every character context, and the record codec rides the same definition (singular-pattern: `IRecordCodec` for these records IS the generated pair — WRITE/RELEASE send `AsImage()`, READ/RETURN distribute via `FromImage()`; `SequentialIoEmitter.EmitImageInto`, `SortBinder.SortRecordOf`). The SORT/MERGE consequence is load-bearing: a signed COMP/COMP-3 key's compile-time descriptor (`BoundSortMergeKey.SignKind`) carries the IMAGE sign (`ImageSignKind` — trailing overpunch), never the leaf's stored `BinaryMinus`, so `CobolSort.NumericKey` decodes the zoned window algebraically (§14.9.40 GR8 / §8.8.4.2.4 — negatives before positives; the failure mode of decoding with `BinaryMinus` is SILENT: everything sorts, negatives order positive). Key offsets/lengths are already computed in digit-image coordinates (`SortOffsetInRecord`/`SortPhysicalWidth` sum `ImageWidth`) — no width changes anywhere. The on-disk record width for such records legitimately DIFFERS from the legacy's raw-byte form (e.g. ST133A: 80 chars where the legacy wrote 72 + 4 binary bytes) — chains are same-engine; cross-engine file compatibility is explicitly not required. Float (COMP-1/2), COMP-5 and INDEX leaves are INSIDE the codec too (kb/Work PB164, R40): each has its storage image, and a record of any of them — group or elementary — round-trips through the record area by the one decode `ReceivingStore.StorageArea`, the inverse of `OperandText.AsStorageImage` (kb/Work PB1556). Proven by ST108A/ST127A/ST133A/ST134A + `MixedUsageRecordImageDifferentialTests`.

**Rejected alternatives.** Raw big-endian bytes as Latin-1 characters (the legacy on-disk form): a SECOND representation for the same concept — compare/DISPLAY contexts already use digit images, so `RETURN`-then-compare would disagree with `IF group = group` about what a record "looks like"; also needs a new Binary decode kind in `CobolSort.Key` and control characters through every string seam, buying only a cross-engine compatibility that is not required. Leading-separate-sign images (width = Digits+1 — would change `ImageWidth` and every offset computation).

### D8. The RETRY phrase and the conflict-status class rule: an exhausted retry lands the CONFLICT'S OWN §9.1.13 status, decided in ONE place, and the timeout period's maximum meaningful value is defined as ZERO.

**The rule.** ISO §14.7.9.3 GR4 scopes the whole RETRY discipline: it engages only "if the I/O operation is
unsuccessful on the first attempt **because of a file sharing conflict condition or a record operation conflict
condition**". Every other outcome — success, and every other unsuccessful status — is the statement's own answer
and the phrase does not touch it. When the discipline does engage, GR4a (no phrase, or an arithmetic-expression
evaluating negative or zero) and the clause's closing paragraph give the *same* landing: "the appropriate value is
placed in the I-O status associated with the file connector according to the rules for 9.1.13". The status is
therefore **a function of the conflict's own class**, never a literal chosen at a call site.

**The class asymmetry is deliberate and load-bearing.** §9.1.13.9 (file sharing conflict) defines exactly two
values — `61` for OPEN and `62` for DELETE FILE — and **no deadlock value**, so a file-sharing conflict has no
conforming landing but its own; §14.9.10.4 GR15b is imperative there ("*The* value … is placed") where its
record-conflict twin GR6b says only "*A* value". §9.1.13.8 item 2's `52` is a **record**-conflict value whose
detection conditions the implementor defines. WiseOwl COBOL detects a deadlock in exactly one circumstance: a
`RETRY FOREVER` waiting on a record locked by another file connector (§9.1.13.8 item 1). That holder is inside the
executing run unit and cannot release while this statement runs, so GR3's "until the input-output operation has
been completed" would never terminate — which is what makes it a deadlock rather than a timeout. Harmonizing the
two classes breaks conformance in one direction (a `52` for a file conflict) or two green goldens in the other.

**Why the class matters beyond the digits.** §9.1.13.1 keys the exception-name on the status's first digit: `5` →
`EC-I-O-RECORD-OPERATION`, `6` → `EC-I-O-FILE-SHARING`. Answering `52` for a file-sharing conflict raises the
*wrong* condition, so a USE declarative or exception-checking PERFORM keyed on `EC-I-O-FILE-SHARING` silently does
not fire. That is why this is a wrong-answer defect and not two cosmetic digits (kb/Work PB142).

**The holder of the locked resource decides whether waiting can help (kb/Work PB1163).** Two kinds of holder
exist. A file connector *of the executing run unit* cannot close a file or release a record while another statement
of the same run unit executes, so no wait can change the answer against it. A holder *outside* the run unit —
another run unit, or any process holding the host file, which the runtime sees only as the host refusing its handle
(`HostFile.IsSharingRefusal`, kb/Work PB860) — can let go at any moment. Only the attempt knows which holder refused
it: the Table 19 arbiter and the record-lock table see this run unit's connectors, and the host refusal is the only
sign of another run unit. So every attempt returns a `RetryAttempt` (its status plus `HolderOutsideRunUnit`), and
`RetryLoop` reads the holder from it and never infers it from the status digits. The record-lock table belongs to
the run unit, so a record conflict is always held inside it. A file-sharing conflict is inside when another connector
of this run unit has the file open (Table 19's '61', `OpenByAnotherConnector`'s '62') and outside when the host
refused (`FileConnector.Open`'s '61', `HostFile.IsHeldByAnother`'s '62'). The rules that follow:
- **GR3, FOREVER**, re-attempts while the holder is outside, waiting `FileRegistry.RetryInterval` (100 ms) before
  each re-attempt, until the operation completes. Against an inside holder it makes one re-check and stops, because
  that wait could never end. This is the deadlock item 109 names for a record, and the conflict's own status for a
  file. This premise used to be the only arm. The loop assumed "no releaser exists", so `DELETE FILE … RETRY
  FOREVER` answered '62' in 72 ms while another program held the file for 4 more seconds.
- **GR1, n TIMES**, waits the same interval before a re-attempt against an outside holder and none against an
  inside one (Annex A.1 item 165).
- **GR2, FOR n SECONDS**, is the determination below.

**The GR2 determination (Annex A.1 item 166).** §14.7.9.3 GR2 requires the implementor to specify the timeout
temporary's picture `9(n)V9(m)` and the **maximum meaningful value** of arithmetic-expression-2. WiseOwl COBOL defines
**n = 1, m = 0, maximum meaningful value = 0**. GR2 therefore clamps every SECONDS amount into a zero-length timeout
period, GR4b's "attempts as specified in General rule 2" performs none, and the closing paragraph lands the
conflict's own status — the same answer GR4a gives for a zero or negative expression. `RETRY FOR 0 SECONDS` and
`RETRY FOR 30 SECONDS` are thus correct **by one rule** rather than by a special case, which is the observable form
of the determination and is pinned as such in `2023/delete_file_sharing` (`DELSC0` / `DELSC30`). ⚠ The value's
original ground was that no holder can release during a wait. That holds for the run unit's own connectors only, as
the paragraph above shows. The value itself is letter-legal and is kept, and revising it is the owner's decision
(kb/Work PB1163). Until then SECONDS is the one form that does not wait for an outside holder. The determinations are
recorded in `docs/CONFORMANCE.md` §7 under A.1 items 165/166; the deadlock detection conditions are item 109.

**The two roundings are different rules and must stay separate in code.** GR1 rounds the TIMES count **up to the
next whole number** (`RETRY 1.5 TIMES` is two re-attempts), which is one of only two clauses in the standard that
say so — the other is ALLOCATE's §14.9.3.4 GR1 — and both render through the single
`NumericRenderer.AlignRoundedUp` (see `COBOLNET_NUMERIC_DESIGN.md`). GR2 instead stores the SECONDS period through
an implicit COMPUTE *without* the ROUNDED phrase, i.e. truncation at the implementor's `m`. One clause, two arms,
two roundings.

**No compile-time screen exists, and none may be added.** §14.7.9 has only 14.7.9.1/.2/.3 — General, General
format, General rules — and **no Syntax rules clause at all**. A non-integer or out-of-range arithmetic-expression
is LEGAL SOURCE that GR1/GR2/GR4a define at run time; rejecting it would be a `rejects_legal_source` regression.
`StatementBinder.BindRetry` binds the three forms with no screen and must stay that way.

**Where it lives.** `FileRegistry.RetryLoop` gates on `IsConflict` (§9.1.13.1's own first-digit classification) and
routes every landing through the private `ExhaustionStatus` — the ONE place the rule is written. Its FIVE call
sites (`DeleteFile`, `OpenCore`, `ConflictOnLockedRecord`, `RewriteShared`, `DeleteShared`) inherit it rather than
each judging for themselves, and that is every RETRY-bearing verb: the two READ formats do NOT call it
separately, they both reach `ConflictOnLockedRecord`, which is the one §14.9.30.4 GR9 check (kb/Work PB340).
`WriteShared` deliberately takes the phrase and discards it, and the ground is NOT §14.9.51.4 GR16 alone: GR16
says only that RETRY controls WRITE "for the case where resources needed to write a record are locked by another
run unit", which cannot arise in a single-run-unit model. What rules WRITE out of §9.1.13.8 item 1 is that its '51'
is "an attempt to ACCESS a record" and the WRITE general rules define no '51' leg (GR33/GR42 state the invalid-key
checks ignore record locks), so the first attempt decides. ⚠ The §12.4.5.9 GR7 lock-CEILING statuses '53'/'54' a
WRITE WITH LOCK can raise are first-digit '5' and therefore inside `IsConflict`, but no re-attempt within one run
unit can clear a ceiling and `ExhaustionStatus` returns them unchanged under every form, so routing them through
`RetryLoop` would be behaviour-identical; they stay outside it deliberately.

**No site upstream of `RetryLoop` may restate its outcome.** Splitting the SECONDS arm off the FOREVER arm was a
one-line change to the code and a change to NOTHING ELSE, and five descriptions of the old arm survived it — in
`StatementBinder.BindRetry`, `FileLockBinder.BindVerbRetry`, `BoundTree.RetryKind`, `BoundKeyedIo.Retry` and
`FileRegistry.ConflictOnLockedRecord` — plus `constructs.json`'s `retry-phrase-2002` row, which is the SOURCE of
a generated registry. Each of those now POINTS here instead of naming a status, so the next change to the retry
rules cannot leave a description behind it (kb/Work PB346).

**The drift tests.** `CobolFileLockTests.RetryLoop_LandsTheConflictsOwnStatus_ByClass` asserts every retry-form
× conflict-class cell, including the negative-SECONDS rows GR4a names explicitly and the not-a-conflict row;
`RetryLoop_AttemptCount_FollowsGR1AndGR4` asserts the same rules on the axis a status cannot witness, the ATTEMPT
COUNT; and `ReadUnderEveryRetryForm_BindsGR9ToTheRetryRules_OnBothFormats` covers §14.9.30.4 GR9's own
delegation to §14.7.9 through BOTH read formats — until it existed, no test passed a RETRY phrase to a READ
at all. The corpus witness is `conformance:2023/pb346_read_retry_record_conflict` with its 2014 twin. The
outside-holder arm is `CobolFileLockTests.RetryForever_AgainstAHolderOutsideTheRunUnit_WaitsUntilTheOperationCompletes`
and `RetryAgainstAnOutsideHolder_PausesBeforeEachReattempt_AndOnlyThen` (the pause is observed through
`FileRegistry.PauseObserver`, never timed), and end to end `SharingRefusalStatusTests.RetryForever_WaitsForAnotherRunUnitToRelease_DeleteFileAndOpen`,
which holds a real exclusive handle and releases it only once the statement has been refused. A corpus golden
cannot witness this arm, because a golden program has no second run unit to hold its file.

### D9. The L1–L3 phrase-placement leniency family is gated at ONE seam: an error under strict, a warning with an unchanged bind under `--permissive`.

**The rule shape.** Six rules across READ/WRITE/REWRITE/DELETE — five syntax rules and one general format —
close a phrase out of a particular organization or access mode:

| rule | forbids | when |
|---|---|---|
| §14.9.10.3 SR2 | INVALID KEY / NOT INVALID KEY | a DELETE RECORD referencing a file **in sequential access mode** |
| §14.9.35.3 SR2 | INVALID KEY / NOT INVALID KEY | a REWRITE referencing **a file with sequential organization**, *or* a file with **relative organization and sequential access mode** |
| §14.9.51.3 SR2 | INVALID KEY / NOT INVALID KEY | a WRITE referencing **a file with sequential organization** — stated as a format rule ("If the organization of the write file is sequential, format 1 shall be specified"), and Format 1 of §14.9.51.2 has no INVALID KEY bracket (⚠ that diagram is the whole prohibition, so it was **rendered from the PDF**, printed pages 785–786, not read off the OCR, whose known bias is toward falsely-restrictive syntax: Format 1 carries ADVANCING + END-OF-PAGE and no INVALID KEY, Format 2 the reverse) |
| §14.9.30.3 SR6 | ADVANCING / AT END / NEXT / NOT AT END / PREVIOUS | a READ whose file control entry specifies **ACCESS MODE RANDOM** |
| §14.9.30.3 SR7 | PREVIOUS | a READ referencing a file with **LINE SEQUENTIAL organization** |
| §14.9.30.2 Format 1 | INVALID KEY / NOT INVALID KEY | a READ referencing a file with **sequential organization** — every such READ is a Format-1 read (§12.4.5.5.2 SR2 → §14.9.30.3 SR8 → §14.9.30.4 GR19), and Format 1 has no INVALID KEY bracket |

**What was wrong.** All three were bound unconditionally with a "tolerated in the default (CCVS-lenient) mode"
comment and **no strict arm**, so at `--std 2023` strict the compiler accepted source the standard forbids and
emitted nothing — measured with CLI probes, not inferred. ⚠ §14.9.35.3 SR2 has **two arms** and only the second
was even commented: the sequential-*organization* arm binds through `SequentialIoBinder.BindRewrite`, which never
read `rewriteInvalidKeyPhrase()` at all — the phrase was parsed and dropped on the floor, a strictly worse shape
than its relative twin, which at least bound it as dead. Both arms are screened now, and both have a negative
fixture, because a fix landing only one of them reproduces the very shape that made the rule wrong.
⚠ **WRITE was the same drop a third time** (kb/Work PB691). `SequentialIoBinder.BindWrite` never called
`w.writeInvalidKey()` either, and unlike REWRITE it had no diagnostic at all: the program compiled clean at every
edition and on both severity axes, and neither imperative ran. It hid because the `writeInvalidKey` sub-rule of
the grammar was consumed by the **keyed** binder only — the two-arm dispatch with one arm fixed, which is this
repo's most reproducible defect shape. The mechanical sweep that finds it is *for every I-O statement rule, which
sub-rules does each binder arm consume?*, and it is now run whenever a phrase gains an organization-conditioned
arm.

**Where it lives.** `StatementValidation.ScreenForbiddenPhrase` is the one screen; the severity decision routes
through **`EditionContext.Removed`**, THE policy seam — which already carries documented-dialect-leniency gating
as well as removed-construct gating — so it is an ERROR under strict and a WARNING with an **unchanged bind**
under `--permissive`. Never a local `Permissive` test, never a parallel `Lenient()` method. ⛔ The legacy
engine's `DialectStrictnessChecks` (archived, not on `main`) must not be revived.

**Why the tolerated path is safe — and what "tolerated" obliges.** The bind is unchanged under `--permissive`
because the emitter's status-first branches make a phrase that cannot fire simply dead — a `'2x'` invalid-key
branch on a sequential-access DELETE, a `'1x'` at-end branch on a random READ — never silently rerouted. That is
what the CCVS-85 corpus depends on. ⛔ **But "dead" is a claim about the INVALID arm only, and it was over-read
into a licence to drop the whole phrase** (kb/Work PB691). Having accepted the program, the compiler owes the
phrase the meaning §9.1.14 gives it, and §9.1.14's final rule has **two** items: item 1 sends a *non*-invalid-key
unsuccessful completion to exception processing, and item 2 — "If the I-O status indicates a successful
completion, control is transferred to the end of the input-output statement **or to the imperative-statement
specified in the NOT INVALID KEY phrase if it is specified**" — makes the NOT INVALID arm **live** on every one
of these statements, because each of them succeeds. So on a sequential-organization WRITE or REWRITE the INVALID
arm is provably dead (§9.1.13.5's four invalid-key statuses `'21'`–`'24'` each name a relative or indexed file)
while the NOT INVALID arm must RUN, and both sequential arms bind the pair and render it through the one
§9.1.14 renderer rather than dropping it.

**Rows five and six are kb/Work PB334, and they arrived the same way SR2's sequential arm did.** SR7 pairs an
ORGANIZATION with a read DIRECTION, and `SequentialIoBinder.BindRead` never called `readDirection()` at all — so
SR7 had **no attachment point anywhere in the compiler**, and a line-sequential `READ … PREVIOUS` was accepted and
read FORWARD. §14.9.30.3 SR10 (the KEY phrase, a hard `COBOLNET0864`) and the Format-1 membership rule were the
same omission on the same arm. All three now read their phrase; §14.9.30.3 SR6 and SR10 live in
`StatementValidation` as ONE check each, called by BOTH READ binder arms, because a per-arm copy is precisely how
they came to be enforced on one organization only. ⚠ READ's NOT INVALID KEY is the ONE phrase on this seam that is
**not** dead under `--permissive`: §14.9.30.4 GR13c transfers control to it on a successful read, so
`BoundRead.InvalidKey` carries it and `SequentialIoEmitter` renders the NOT arm (never the INVALID arm — a
sequential READ raises no `'2x'` status).

**One §9.1.14 renderer, every arm.** `SequentialIoEmitter.EmitInvalid` renders the transfer contract — INVALID on
`'2'`, NOT INVALID on `'0'` — for the keyed READ/WRITE/REWRITE/DELETE/START **and** for the tolerated
sequential arms. It lives on the sequential emitter because that class is the declared home of the file-I/O
common services `KeyedIo` consumes (status store, USE hook, image splice, RETRY renders); a second copy on the
keyed side is exactly how the sequential WRITE came to have no branch at all.

**One code, six rules.** `COBOLNET1720` serves all six, on the `COBOLNET1694` precedent: the *shape* is one
rule ("a phrase is written where this statement's syntax rules forbid it") and each site's message quotes its own
§/SR. §14.9.10.3 **SR1** (DELETE RECORD on a sequential-organization file) is deliberately **not** on this seam —
it is a hard `COBOLNET0865` error at every edition and strictness, because it is not a documented leniency.

**The edition-gate sweep was RUN, not assumed** (gating a construct breaks everything compiling it below that
edition). The rules are edition-invariant — `Removed` keys on `Permissive`, never on edition — and the probe
emits an identical diagnostic count at 85/2002/2014/2023. The corpus risk was the real one: a static scan finds
**315** `REWRITE … INVALID KEY` and **28** `DELETE … INVALID KEY` sites in the NIST programs, and NIST runs
**strict** by default. Compiling all **459** at `--std 85` produces the **same 17 pre-existing failures** (every
one read individually: `CM*` COMMUNICATION SECTION parse errors and `DB*` `COBOLNET1571` debug-module rejections)
and **zero** new ones — every one of those 343 phrase sites is on an organization/access mode where the phrase is
LEGAL. The harness was proved able to surface `COBOLNET1720` as a failure before that zero was trusted.

### D10. A physical file's §9.1.6 FIXED FILE ATTRIBUTES are validated FROM THE FILE'S OWN FORMAT — there is no catalog and no sidecar (§14.9.27.4 GR10 → '39').

**⛔ OWNER DECISION, 2026-09-07 (question 24), verbatim: "Let's match GNUCobol's implementation in spirit. No
sidecar of any type."** It REPLACES the `.cbattr` catalog sidecar this decision used to carry (kb/Work PB193,
re-decided by kb/Work PB802); NTFS alternate data streams were considered and rejected with it. WiseOwl COBOL
writes and reads **nothing beside a data file**.

**The rule.** §9.1.6: a physical file's organization, key geometry, code set, logical record sizes, record type,
key collating sequence, physical record sizes and record delimiter "apply to the file at the time it is created
and cannot be changed throughout the lifetime of the file". §14.9.27.4 GR10 makes the OPEN statement compare the
connector's declared attributes with the file's and set I-O status **'39'** (§9.1.13.6 item 7 — "The OPEN or
DELETE FILE statement is unsuccessful because a conflict has been detected between the fixed file attributes and
the attributes specified for that file in the source unit") when they differ, and delegates WHICH attributes are
validated to the implementor — a **required, required-to-be-documented** determination (Annex A.1 item 129).

**The determination, in one sentence: the validated set is exactly what each organization's PHYSICAL FORMAT
ENCODES.** GR10's third and fourth sentences are the licence — "The implementor defines which of the fixed-file
attributes are validated during the execution of the OPEN statement. The validation of fixed-file attributes may
vary depending on the organization or storage medium of the file." — and *what the format encodes* is a RULE
that derives the per-organization answer instead of a hand-kept list of attributes: add an organization, or
change a format, and its validated set follows without anyone editing a table. Three organizations, three
answers, each stated in the code where that organization's format lives:

- **Record sequential with FIXED-length records — nothing, and line sequential — nothing.** §9.1.7.2: "In record
  sequential files the length of each record is determined by any information the implementor may add to the
  record on the physical storage medium (such as record length headers)" — WiseOwl COBOL adds none to a
  fixed-length one, which is plain bytes — and "In line sequential files the length of each record is determined
  by the number of characters between the preceding line delimiter and the following line delimiter or the end
  of file if no line delimiter is present", where the delimiters encode no attribute either. The standard then
  answers every disagreement such a re-read can produce with a SUCCESSFUL completion rather than a refused OPEN:
  §9.1.13.2 item 3's '04' ("A READ statement is successfully executed but the physical record from the file is
  shorter than or longer than the minimum or maximum length of records allowed for the fixed file attributes for
  that file"), item 5's '06' and item 7's '09'. Writing a print, report or extract file and reading it back under
  a different record description is exactly the idiom those three statuses exist for — measured on this corpus, a
  validated record size broke six conforming programs, one of them into an infinite READ loop.
  `RecordLayoutNotice` stays the stderr notice for the arithmetic case, and leaves the I-O status alone.
- **Record sequential with RECORD VARYING — the record-length header must PARSE, and that is the whole of it.**
  §9.1.7.2's "such as record length headers" is precisely what WiseOwl COBOL adds here: `RecordFraming`'s 4-byte
  little-endian length prefix per record, whose method §12.4.5.11.4 GR5 grants ("If the RECORD DELIMITER clause
  is not specified, the method used for determining the length of a variable-length record is specified by the
  implementor") and whose GR1 it obeys ("Any method used shall not be reflected in the record area or the record
  size used within the function, method, or program"). A first prefix naming more bytes than the file holds is
  not a framed file at all, so the connector's declared VARIABLE **record type** — a §9.1.6 fixed file attribute —
  contradicts the medium: **'39'**. A prefix that parses but falls outside the RECORD clause's bounds is NOT a
  conflict; that is §9.1.13.2 item 3's '04' on the READ, which a '39' at the OPEN would make unreachable. A file
  shorter than one prefix (fewer than four bytes) states nothing and is not a conflict — the same rule
  `RecordFraming` already applies to a torn tail.
  ⚖ Behaviourally GnuCOBOL's `variable-length SEQUENTIAL data integrity` case: an FD of
  `RECORD VARYING FROM 5 TO 500` over a plain XML text file answers `OPEN ERROR: 39`.
- **RELATIVE and INDEXED — whatever the framed store's HEADER carries**: the **organization**, the **record
  type**, the **minimum and maximum logical record size**, and the **key table** — each key's byte window,
  DUPLICATES phrase, SUPPRESS WHEN value and collating-sequence fingerprint. Those two organizations live in an
  implementor-defined store whose STRUCTURE *is* those attributes, and the header is where that structure is now
  written down: **in the file**, exactly as GnuCOBOL's ISAM key definitions are in the file. A store whose header
  is absent or unreadable is not a store this build can interpret, and answering '39' for it is LOUD where
  reading it as frames would be a silent misread. A ZERO-BYTE file is the one tolerated exception: it holds no
  records, so it can misread nothing, and it states no attribute to contradict.
  ⚖ Behaviourally GnuCOBOL's `INDEXED undeclared keys` case: three FDs on one physical file declaring three, two
  and one keys; the one whose key table disagrees with the file's answers '39' on OPEN.

**The key check has a runtime opt-out — `COBOLNET_KEYCHECK=OFF`**, GnuCOBOL's `COB_KEYCHECK=OFF` under this
repository's own naming (`COBOLNET_`, not `COBOL_`, because `COBOL_` is the external-switch family's prefix —
`SwitchStore.Prefix`). It is read **once per run unit**, in `FileRegistry`'s constructor, and registered in
`RuntimeConfig` like every other variable the runtime honours. It is itself part of the A.1 item 129
determination rather than an escape from it: with it OFF the key table leaves the validated set for that run,
and the organization, record type and record sizes stay in it. A connector that opens I-O under the opt-out and
persists re-stamps the header with ITS OWN key table — the file follows the description that wrote it, which is
what an opt-out from the key check means.

**⚠ THIS IS A FORMAT CHANGE TO RELATIVE AND INDEXED DATA FILES, and it is owner-visible.** The framed store now
begins with a header (magic + version, organization, record type, the two record sizes, the key table) and the
frames follow it; `RecordFraming.ComposeStore` stamps it from the writing connector's `DeclaredAttributes` on every
persist, so a store always describes itself. A relative or indexed file written by an earlier build carries no
header and is therefore **refused with '39'** rather than silently misread — the loud failure is the point, and
it is the reason the header is required rather than optional. The SEQUENTIAL formats are untouched: a
fixed-length record sequential file is still plain bytes and a line sequential file is still plain text, which is
the interchange property those shapes exist for, and a varying record-sequential file still carries only the
per-record prefix it always carried.

**The header is VERSION 2 (kb/Work PB1602).** Each key's SUPPRESS WHEN value is stored as its exact UTF-16 code
units behind an Int32 count biased by one (0 = no phrase). The header records what the program DECLARED, and the
OPEN comparison asks it back, so the encoding must be lossless over the whole repertoire, lone surrogates in a
national literal included. Version 1 wrote the value through Latin-1: `SUPPRESS WHEN "€€"` was recorded as `??`,
and the program could not reopen a file it had just written ('39'). A version-1 store is Foreign to this build and
refused with '39' like any other store it cannot read. No version-1 reader is kept (zero back-compat baggage).

**⚠ WHAT THE DECISION GIVES UP, stated plainly.** kb/Work PB193's original reproduction — a RELATIVE file
reopened INPUT through a SEQUENTIAL or LINE SEQUENTIAL FD — no longer answers '39'. The sequential
organization's format encodes nothing, so nothing in it contradicts a sequential description, and the OPEN
succeeds ('00') exactly as GnuCOBOL's does. What survives is the direction the format CAN see: a plain
sequential or line sequential file opened through a RELATIVE or INDEXED FD has no store header and is still
'39', and `RecordLayoutNotice` still reports on stderr that the byte count and the record description disagree.
That asymmetry is not an oversight — it is what "validate what the file itself records" means when one of the
two formats records nothing.

**For indexed files the key half is not latitude at all.** §12.4.5.12.4 GR3 requires a prime key's data
description and its relative location within the record to "be the same as that used when the file was created",
and §12.4.5.6.4 GR3 says the same of every alternate key and of the number of alternate keys ("shall be the same
as that used when the physical file was created" — the two rules are worded differently and the difference is
quoted here rather than smoothed over). Neither states a consequence where it is written; GR10's conflict
condition is the mechanism that detects a violation, so carrying the key table in the store header is what gives
those two rules an effect at all. The recorded descriptor is the key's byte window plus its collating sequence,
which is the whole of "the data description … as well as their relative location" this processor can act on:
§12.4.5.12.3 SR2 confines a record key to category alphanumeric or category national, §12.4.5.12.4 GR1 makes key
equality a relation condition under the file's collating sequence, and both native sequences are one code-unit
ordinal over the UTF-16 substrate — so two descriptions sharing a window and a sequence order every key value
identically whatever their category.

**The three §9.1.6 attributes outside the set, for every organization.** The **code set** is a DETERMINATION and
not an omission: §13.18.13.4 GR7 makes it the native character set when no CODE-SET clause is written and GR2/GR6
the named alphabet's when one is, and since kb/Work PB793 those two really can differ (the `EBCDIC` code-name is a
genuine alternate device code set). It is not validated for two reasons GR10's latitude exists to accommodate —
the whole purpose of a CODE-SET clause is to read a physical file some OTHER system wrote, which carries no
WiseOwl COBOL header at all, so the check would fire only where it is not wanted; and reading a converted file
through a description with NO CODE-SET clause is GR7's own default and the raw-medium idiom a dump or transcode
utility is written in, so a '39' there would reject legal source. The **minimum and maximum physical record
size** is outside the set because the managed I-O model does no blocking and BLOCK CONTAINS is accepted inert
under A.3 item 5, so no physical record size exists. The **record delimiter** is outside it because the RECORD
DELIMITER clause has no compiler surface and the delimiter distinction this processor does realize (§9.1.7.2's
record-sequential vs line-sequential) is a SEQUENTIAL-organization attribute, whose format records nothing.

**§9.1.6's own last-sentence obligation** — "The implementor shall specify whether the ability to share a
physical file is a fixed file attribute" — is answered **NO**: a sharing mode is established per FILE CONNECTOR
by the SHARING clause or the OPEN's SHARING phrase (§14.9.27.4 GR21–GR23) and arbitrated live against the
connectors currently open (§9.1.15 Table 19), so it is not a property the physical file carries between run
units.

**Where each half lives, each in ONE place.** `FileConnector.Open` performs GR10's comparison — before
`OpenCore`, because GR25 leaves an unsuccessful OPEN's file unaffected ("If the execution of the OPEN statement
is unsuccessful, the file is not affected") and the OUTPUT/creation arms truncate — and both OPEN dispatch arms
reach it (the plain `FileRegistry.Open` and the sharing-active `SharedOpenAttempt`). It asks ONE virtual,
`FileConnector.FixedAttributeConflict`, whose overrides are the three answers above: the base is "this format
encodes nothing", `SequentialConnector`'s is the varying record-length header's parse, and `KeyedConnector`'s is
the store header compared through `FixedFileAttributes.Conflicts` — the one place the comparison itself is
written, and the twin of `FileRegistry.ValidateFixedFileAttributes` for §14.9.10.4 GR19 (whose set is empty; A.1
item 50). `FileConnector.DeclaredAttributes` assembles a connector's declared set ONCE for every organization
(record type and record-size bounds from the RECORD clause); the organizations supply only `CatalogOrganization`
and, for indexed, `CatalogKeys`. `RecordFraming` owns the store header's bytes, beside the frames it prefixes,
because the header IS the format.

**When the attributes are established** is exactly the two cases the OPEN statement CREATES the file: GR18 ("If
the OUTPUT phrase is specified, the successful execution of the OPEN statement creates the file") and GR17 (an
absent OPTIONAL file opened I-O or EXTEND is created as if OPEN OUTPUT / CLOSE). So an **OPEN OUTPUT is never
judged against the previous file's attributes — it replaces them**, which is what §9.1.6's "at the time it is
created" means and what the surveyed implementations do. An absent OPTIONAL file opened INPUT is *not* created
(Table 18) and records nothing. Because the store's header is written by the store's own persist, no separate
"record the attributes" step exists any more, and neither does the failure mode that step had (kb/Work PB684: a
sidecar this process could not write left the PREVIOUS file's attributes in force).

### D11. A physical file's §14.9.6.4 GR2 CATEGORY is a named property of the connector, and CLOSE executes Table 14's cell rather than a per-form transcription.

**The rule.** §14.9.6.4 GR2 partitions physical files into (a) Non-unit, (b) Sequential single-unit,
(c) Sequential multi-unit and (d) Non-sequential single/multi-unit, and GR3's **Table 14** is indexed by
those four names crossed with the four written CLOSE forms (plain, WITH NO REWIND, UNIT, UNIT FOR REMOVAL).
Each cell is a set of the symbols a–g GR3 defines. A conforming implementation must place every physical
file it supports in exactly one category, and §14.9.6.4 GR3 symbol c (“closing operations specified by the
implementor”) is a **required, required-to-be-documented** determination (Annex A.1 item 24).

**The decision.** `PhysicalFileCategory` is an enum with GR2's four members and `FileConnector.Category` is
an ABSTRACT property, so the placement is total by construction; `Table14.Cell(format, category)` returns
the printed symbol set and `FileRegistry.CloseByFormat` is the ONE dispatch that executes it. The written
forms (`Close`, `CloseNoRewind`, `CloseReelUnit`, `CloseReelUnitForRemoval`) are thin entries over it, and
`CLOSE … WITH LOCK` layers the reopen prohibition over the plain form — it is not a Table 14 row.

**The placement, and why it is forced.** `SequentialConnector` is (a) Non-unit; `RelativeConnector` and
`IndexedConnector` are (d) by organization alone. (a) is not a free choice: §9.1.13.2 item 6 defines the
`'07'` this compiler reports for the NO REWIND, REEL/UNIT and FOR REMOVAL phrases as the status of a CLOSE
that “references a physical file on a non-reel/unit medium”, and Table 14 prints symbol g (which is what
makes `CLOSE … WITH NO REWIND` answer `'07'`) only in the Non-unit column. The determination is documented
for users in `docs/CONFORMANCE.md` §7 (Annex A.1 item 24) and §2 rows 28–30 / 33–34.

**⛔ THE SAME DETERMINATION GOVERNS THE OPEN STATEMENT'S NO REWIND PHRASE, AND ONE SITE OWNS IT.** §9.1.13.2
item 6 names *“an OPEN statement with the NO REWIND phrase”* in the same sentence as the three CLOSE phrases,
so the OPEN half of the phrase is the same medium question, not a second one. §14.9.27.4 **GR11** — *“The NO
REWIND phrase will be ignored if it does not apply to the storage medium on which the file resides. If the NO
REWIND phrase is ignored, the OPEN statement is successful and the I-O status associated with file-name-1 is
set to '07'.”* — and **GR12**, its complement *“If the storage medium for the file permits rewinding …”*,
partition the media between them, so choosing category (a) chooses GR11 and makes GR12 vacuous. The phrase
rides `BoundOpenFile.Tape` (per FILE-NAME, as §14.9.27.4 GR20 requires — the mode, SHARING and RETRY are
the GROUP's, the tape phrase is the file-name's own), both emitter arms carry it (the plain `FileOpen` and
the sharing-aware `FileOpenShared`, since the phrases are independent), and `FileRegistry.NoRewindPhraseEffect`
is the ONE place the rule is written: it refuses any category but (a) LOUDLY, and overlays '07' on a status
whose first digit is '0' — GR25 a) reserves an unsuccessful open's own diagnosis. §14.9.27.3 SR5 and SR6 —
the phrase is for sequential files, under INPUT or OUTPUT only — are screened at bind (COBOLNET1802/1803),
which is what keeps categories (b), (c) and (d) off the runtime path in the first place; SR5 is the exact twin
of the CLOSE rule §14.9.6.3 SR1, and until kb/Work PB317 only the CLOSE spelling had a screen while the OPEN
phrase parsed and was silently dropped. Both screens are now rows of the ONE tape-phrase rule set — **D26**,
which is also where REVERSED, the other alternative of the same printed bracket, is answered.

**What it buys.** Categories (b) and (c) are unreachable today, so symbols a, b, d and f — previous units,
no rewind of the current reel, unit removal, rewind — and symbol e's two unit-media branches are vacuous
rather than missing, which is what `docs/CONFORMANCE.md` §8 `DRV-GR-14.9.6.4-L2.1` records. Adding a
unit-structured medium is then a bounded change: give its connector a category, fill the cells
`CloseByFormat`'s loud arm names, and implement §14.9.27.4 GR12 b) where `NoRewindPhraseEffect` refuses.
`CloseTable14Tests` is the drift test that keeps that true — it pins the
16 cells against the printed table, asserts both enums still have exactly four members (the `_` arm C#
requires for out-of-range casts would otherwise swallow a new one), and fails the moment any connector
answers (b) or (c), on the OPEN side (`OpenNoRewind_IsTheCloseArmsTwinOnTheSameMedium`) as well as the CLOSE
side. Before this, each written form carried a hand-copied transcription of its own Non-unit
cell, REEL/UNIT and REEL/UNIT FOR REMOVAL shared one bound kind so `FOR REMOVAL` had no consumer at all,
and the category placement existed only as a sentence in a doc comment that `CONFORMANCE.md` contradicted
(kb/Work PB235).

**GR1 is hoisted into the dispatch.** “If the file connector is not open, the CLOSE statement is
unsuccessful and the I-O status indicator … is set to '42'” now guards every symbol, so an unsuccessful
CLOSE performs none of the closing actions — including §14.9.6.4 GR9's lock release, which used to run
ahead of the connector's own `Close()` on every path. **Symbol e does not release locks either**: its
non-unit branch is “the file remains in the open mode … and no other action takes place”, and GR9's release
rides symbol c (“Close file”), so `CLOSE … UNIT` keeps the file lock and every record lock — pinned by
`conformance:2002/pb235_close_unit_locks`.

### D26. The per-file-name TAPE PHRASE is ONE enum end to end, with a ROW per phrase — never a bool per phrase.

**The rule.** §14.9.27.2 prints ONE optional bracket beside each file-name. In COBOL-2023 it holds a single
alternative, `[ WITH NO REWIND ]`; at COBOL-85 it held two, `[ REVERSED | WITH NO REWIND ]`, and
`VERSION_CHANGE_REFERENCE` row 7.12 records REVERSED's deletion by ISO 2002. The superset grammar still parses
both — `openFileSpec : dataReference (REVERSED | WITH? NO REWIND)?` — so the two are **mutually exclusive by
construction** and a COBOL-85 compiler shall implement the phrase it admits (`feedback_four_editions_one_compiler`).
REVERSED positions the file at its END and makes every subsequent READ retrieve the preceding record; the
repository holds no 1985 text, so the semantics are row 7.12's plus the surveyed implementations (GnuCOBOL
implements REVERSED for sequential files), under the standing implementor-latitude decision.

**What went wrong.** kb/Work PB317 gave NO REWIND a `bool NoRewind` of its own on `BoundOpenFile`, a `noRewind`
parameter on two emitter renderers, two runtime entry points and `OpenCore` — and REVERSED, the alternative
beside it in the SAME grammar production, was read by NOBODY. `VersionConformancePass` gated it out at 2002+
(COBOLNET0902, correct) and at `--std 85` it parsed, bound to nothing and ran FORWARD with no diagnostic:
`OPEN INPUT F REVERSED. READ F.` answered the FIRST record where COBOL-85 requires the last (kb/Work PB668).
A phrase's own boolean is a phrase's own opportunity to be forgotten — the two-arm dispatch one layer up.

**The shape.** ONE enum, three members, four hops: `BoundOpenTapePhrase` on `BoundOpenFile.Tape` →
`RuntimeApi.OpenTapePhraseExpr` (the ONE seam between the backend-neutral bound tree and the runtime enum) →
`OpenTapePhrase` on `CobolFile.OpenTape` / `CobolFile.OpenShared` → `FileRegistry.OpenCore`'s ONE effect
switch, one arm per phrase (`NoRewindPhraseEffect`, `ReversedPhraseEffect`). The syntax rules are DATA:
`OpenTapePhraseRule` carries a NAMED row per phrase — its organization predicate and its admitted open modes,
each with the clause and the diagnostic that state it — and `OpenTapePhraseRule.For` is a switch over the enum
that `CheckOpenTapePhrase` is a lookup into and nothing else. ⛔ NAMED rows behind a switch, not an array
indexed by the enum's ordinal: an inserted member cannot silently mis-key, and a member with no row throws at
the first OPEN that writes the phrase. That is the drift guard the structure CARRIES, instead of a test
asserting an array's order. **A third tape phrase is a member on each enum, an arm in the effect switch and a
named row with an arm in `For`.**

**REVERSED adds NO retrieval code.** §14.9.30.4 GR21 c) already defines retrieval in the decreasing direction
and `SequentialConnector` has implemented it since kb/Work PB334 for `READ … PREVIOUS`. `PositionReversed` sets
the file position indicator to one past the last record — `ExistingRecordCount`, the same ONE measurement the
§14.9.51.4 GR19 EXTEND ordinal base uses — and raises a standing `Reversed` flag; `MovesBackward(previous)` is
`previous ^ Reversed`, the ONE conversion from a statement's direction phrase to the connector's direction of
travel, which `Read` and the §14.9.30.4 GR9 pre-read peek both ask. A second backward walk would be the same
rule written twice and would drift on the RECORD VARYING framing and at-end boundaries the existing one has
goldens for. The flag is cleared by every `OpenCore`, so the phrase cannot outlive the OPEN that wrote it —
the 85 golden's final plain re-open is that witness. Rejected: a `ReversedSequentialConnector` subclass (a
second retrieval path for a direction bit); reading REVERSED as a status overlay like NO REWIND's '07' (GR11
is about a medium the phrase does not APPLY to, and REVERSED applies — it is a positioning).

**The syntax rules are NARROWER than NO REWIND's on both axes**, which is precisely why the rules are a row per
phrase rather than a shared predicate: REVERSED admits RECORD sequential organization only (COBOLNET2210 —
§14.9.30.3 SR7 denies the backward walk to LINE SEQUENTIAL, and a relative or indexed file has no record
*number* for GR21 c) to compare) and the INPUT group only (COBOLNET2211 — the phrase's whole effect is a
retrieval direction), where §14.9.27.3 SR5/SR6 give NO REWIND both sequential kinds (§9.1.7.2) and both INPUT
and OUTPUT. ⚠ The LINE SEQUENTIAL cell is unreachable from conforming source at EVERY edition — LINE
SEQUENTIAL arrived in 2023 and REVERSED left in 2002 — so it is defence in depth under `--permissive`; the
negative golden witnesses the reachable half of the same rule with a RELATIVE file.

### D12. The OPEN statement's REPEATED GROUP is the bound tree's shape: one `BoundOpenFile` per file-name, carrying its OWN group's mode and phrases. The statement node holds no phrase state.

**The rule.** §14.9.27.2's general format has two nested brace pairs, and the OUTER pair — verified by
rendering the printed page, not by reading the transcription — encloses the whole group
`{open-mode} [sharing-phrase] [retry-phrase] {file-name-1 [WITH NO REWIND]} …` and carries its own trailing
ellipsis. Both phrases therefore sit INSIDE the repeated group, beside the open mode and the file-names they
govern. §14.9.27.4 GR20 settles what that means: *"If more than one file-name is specified in an OPEN
statement, the result of executing this OPEN statement is the same as if a separate OPEN statement had been
written for each file-name in the same order as specified in the OPEN statement. These separate OPEN
statements would each have the same open mode specification, the sharing-phrase, retry-phrase, and REWIND
phrase as specified in the OPEN statement."* The open mode is in that list and is unarguably per group — a
multi-group OPEN exists to open files in differing modes — so the phrases listed beside it are per group too.
§14.9.27.4 GR23 bounds the other side: *"If there is no SHARING phrase on the OPEN statement, then file
sharing is completely specified in the file control entry."*

**The design.** `BoundOpenFile(FileModel File, BoundOpenMode Mode, SharingMode? Sharing, RetrySpec? Retry,
string? Unsupported)` is GR20's normal form written down: `BindOpen` flattens the groups into exactly the
separate OPENs the rule names, in source order, and emits ONE `BoundOpen` per file-name inside a
`BoundImplicitSeries` — GR20's *"the same as if a separate OPEN statement had been written for each
file-name"* taken literally, so that its next sentence (*"processing resumes at the next implicit OPEN
statement, if any"*) has a statement boundary to resume at; see COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D11,
"The MULTI-OPERAND arm". The statement node
carries **no** phrase property, so a consumer cannot re-broaden a phrase's scope — the previous shape put
`SharingOverride` and `Retry` on the statement, and both the binder (a `sharing` local hoisted out of the
`openClause` loop) and the emitter (one `bool shared` computed before the per-file loop) then leaked one
group's phrase over every file. The two leaks had different fingerprints, which is the signature of a scalar
standing in for a per-group value: the bind leak was forward-only (a later group inherited an earlier
group's phrase, so `OPEN OUTPUT SHARING WITH ALL OTHER F1 OUTPUT F2` rejected legal source at §14.9.27.3
SR8 while its reverse ordering compiled), the emit leak order-independent (a file written BEFORE the phrase
was routed through the sharing facade too, and `FileRegistry.OpenShared` then made it a sharing participant
for the rest of the run). Pinned by `conformance:2002/pb316_open_group_scope`, which writes every arbitration
twice — once as separate OPEN statements, once as one statement with the same groups — and requires the two
to agree, so the test asserts GR20 itself rather than any implementor-defined default.

**This is the carrier the rest of the OPEN work needs.** The NO REWIND phrase (§14.9.27.4 GR11/GR12) and the
per-file syntax rules §14.9.27.3 SR1/SR2/SR5/SR6 are all written per file-name-1, and each adds a field or a
screen to `BoundOpenFile` rather than a second statement-level property.

### D13. A syntax rule of the OPEN statement is enforced ONLY in the OPEN binder, with its FULL antecedent — never a second time at the file control entry.

**The rule.** §14.9.27.3 SR8: *"When file-name-1 is not subject to an APPLY COMMIT clause, then if the sharing
phrase is omitted from the OPEN statement and the ALL phrase is specified in the SHARING clause of the file
control entry for file-name-1 or if the ALL phrase is specified on the OPEN statement, the LOCK MODE clause
shall be specified in the file control entry for file-name-1."* It is a rule about **file-name-1**, which
§14.9.27.2's general format defines as the OPEN statement's operand, and every branch of its antecedent
presupposes an OPEN statement. The SHARING clause's own clause imposes nothing of the kind: §12.4.5.15.3 has
exactly ONE general rule — *"The SHARING clause specifies the sharing mode to be used for the file unless it is
overridden by the SHARING phrase of the OPEN statement"* — and **no syntax rules**, so §14.9.27.3 SR8 is the
standard's only source of a LOCK MODE requirement anywhere.

**The design.** `StatementValidation.CheckOpenSharingAllOther(file, sharing ?? file.Sharing)`, called from
`BindOpen` once per file-name, is SR8's only site. Its two disjuncts collapse onto the EFFECTIVE sharing mode —
the group's phrase when one is written, the file control entry's clause when none is (§14.9.27.4 GR23) — which
is exactly *[ALL on the OPEN]* OR *[phrase omitted AND the SELECT says ALL]*; the leading conjunct reads
`FileModel.SubjectToApplyCommit`, set by `DataBinder.BindIoControl` from the I-O-CONTROL APPLY COMMIT clause
(§12.4.6.3). The exemption is load-bearing rather than decorative: §12.4.5.9.3 SR1 forbids writing a LOCK MODE
clause *"for a file that is the subject of an APPLY COMMIT clause"*, so without it SR8 would demand the one
clause another rule forbids and such a program could not be written at all. It is reachable today — COBOLNET1709
declines the clause but is `PermissiveInert`, so `--permissive` compiles the program.

**Why the file control entry cannot host it.** A second copy lived in `DataBinder.BindFileControl` and had to
drop the antecedent, because a SELECT sees no OPEN statement and the I-O-CONTROL paragraph is not bound until
after the file control entries are. It cost both harms at once: legal source rejected (a file whose OPEN
supplies its own non-ALL sharing phrase; a file declared `SHARING WITH ALL OTHER` and never opened) and, for a
program that really does violate SR8, the same rule reported TWICE in two different spellings. The general
shape: **a syntax rule listed under a statement's clause belongs to that statement's binder; a declaration-time
copy of it can only be a paraphrase, because a declaration cannot see the statement the rule quantifies over.**

`conformance:2002/pb319_sr8_antecedent` pins the antecedent falsified in both compilable ways;
`conformance:negative/sharing-all-no-lockmode` (disjunct 1) and
`conformance:negative/pb316-open-sharing-all-no-lockmode` (disjunct 2) pin it holding.
`conformance:OpenSharingLockModeTests` carries the two assertions no `.cob` corpus can make — that ONE violation
draws exactly ONE diagnostic (a substring-matching `.err` is blind to a duplicate, which is how the second copy
survived), and the APPLY COMMIT exemption, which needs the permissive axis.

**SR8's antecedent family is enforced in its own right (kb/Work PB666).** The same fact, `SubjectToApplyCommit`,
is the antecedent of six syntax rules of CLAIMED clauses and statements: §12.4.5.9.3 SR1 (LOCK MODE), §12.4.6.4.3
SR11 (a SAME clause mixing subject and non-subject files), §14.9.27.3 SR7 (OPEN's sharing phrase), §14.9.30.3 SR5 and
§14.9.35.3 SR5 (READ's and REWRITE's lock phrases) and §14.9.47.3 SR2 (UNLOCK). kb/Work PB371 recorded them CONFORMS
"witnessed by the refusal of the antecedent", but that refusal (COBOLNET1709) is `PermissiveInert`, so under
`--permissive` every forbidden shape compiled clean. They now report through ONE helper,
`DataBinder.ScreenApplyCommitSubject` (COBOLNET2974), each caller quoting its own rule: the LOCK MODE arm as the
APPLY COMMIT clause records the fact (the file control entries are bound first), SR11 after the whole I-O-CONTROL
paragraph (an APPLY COMMIT clause may follow the SAME clause), SR7 in `BindOpen` beside SR8, the lock phrases in
`FileLockBinder.ScreenLockPhraseAgainstAutomatic` (the one screen every lock phrase passes) and UNLOCK in
`BindUnlock`. §12.4.6.4.4 GR3, the family's seventh member, is a general rule about an ACTIVE clause's sharing at run
time, and no APPLY COMMIT clause is ever active here in either lane. Witness: `ApplyCommitSubjectRuleTests`, which
compiles each `negative/apply-commit-*` fixture under `--permissive`.
### D14. ISO §14.9.27.4 Table 19 is a STRUCTURE, and it arbitrates EVERY OPEN — not only the connectors that declared a SHARING clause.

**The rule.** §9.1.15 puts the gate on the physical file, not on the connectors that opted in: *"Before access
to a shared physical file is allowed through an OPEN statement, the sharing mode and the open mode of that OPEN
statement shall be allowed by all other file connectors that are currently associated with the physical file,
as described in 9.1.13, I-O status; 14.9.27, OPEN statement; and Table 19"*. **Table 19** is that decision
table: 7 *Open request* rows × 5 *Most restrictive existing sharing mode and open mode* columns = 35 cells,
each *Normal open* or *Unsuccessful open*, and §9.1.13.9 item 1 supplies the status the unsuccessful cell
reports (`'61'`) while §14.9.27.4 GR25 supplies its effect (*"the file is not affected"*).

**The decision.** `OpenRequestRow` and `ExistingSharingColumn` are enums with the printed row and column groups
as their members, `Table19.Cell(row, column)` is an exhaustive switch over all 35 printed cells, and
`FileRegistry.Conflicts` is a lookup into it and nothing else. `FileRegistry.OpenCore` is the ONE OPEN
dispatch — the plain `Open` and the phrase-bearing `OpenShared` are entries over it — so every connector is
arbitrated whether or not it wrote a SHARING or LOCK MODE clause. That also covers the opens §14.9.27.4 names
without a statement of their own: Table 19 shows the results of opening files currently open by another
connector *"including those implicitly opened by the SORT and MERGE statements"*, and those route through the
SAME `OpenCore` — but **with the SHARING PHRASE their own general rules give them**, which is a fact of the
emitter and not of this dispatch (**D22**). Being arbitrated is only half of what those four rules require, and
the sentence that used to stand here — *"those route through `CobolFile.OpenInput`/`OpenOutput` → `Open` →
`OpenCore` with no arm of their own"* — recorded the half that was true as though it were the whole, which is
how §14.9.40.4 GR12 a)'s READ ONLY and GR15 a)'s NO OTHER went unrendered for as long as they did
(kb/Work PB714). Because every successful OPEN registers in `PhysicalFileTable`, every close
path releases: `SharedClose` (from the Table-14 CLOSE dispatch), `CloseDisplaced`, `CloseAll` and
`CloseAndDrop`. The registration is gated on the OPEN's STATUS, not on `IsOpen`, so a re-OPEN of an already-open
connector — `'41'`, which leaves the connector in its ORIGINAL mode — no longer re-registers it under the failed
request's mode.

**⛔ The PRINTED TABLE is the arbiter, not §9.1.13.9's five sub-cases, and the difference is four cells.**
§9.1.13.9 item 1 enumerates five *"possible violations"* (a)–(e). Sub-cases (c) and (d) are written over
*"I-O or extend"*, but Table 19's existing-side column groups are `extend I-O output` — so a literal reading of
the five sub-cases answers *Normal open* in four combinations the table marks *Unsuccessful open* (an incoming
SHARING WITH READ ONLY request, in the EXTEND/I-O or INPUT mode, against a connector open in the OUTPUT mode).
§9.1.15 rule 2 resolves it in the table's favour in so many words — *"unsuccessful if the physical file is
associated with another file connector whose open mode is other than input"* and *"subsequent requests to open
the physical file through other file connectors in a mode other than input … will be unsuccessful"* — and
OUTPUT is a mode other than input. `OpenTable19Tests` pins both readings: the 35 cells one by one against the
**rendered PDF page**, and the §9.1.15 prose over all 144 (sharing, mode)² connector pairs, asserting that the
sub-case reading disagrees in exactly four. The finding is an assertion, not a comment, so it cannot rot.

**⛔ The implementor default is a function of the open mode, and the arbitration is a plain table lookup.**
§9.1.15: *"If no specification is made in either location, the implementor defines the sharing mode in which the
file is opened"*, and §14.9.27.4 GR23 sends an OPEN with neither a SHARING phrase nor a SHARING clause to the same
implementor choice. `FileRegistry.ImplementorDefaultSharing(mode)` is the ONE place that default is named
(Annex A.1 items 77 and 131; `docs/CONFORMANCE.md` DOC-A.1-77 / DOC-A.1-131; kb/Work **PB322**): `OPEN INPUT`
establishes `SHARING WITH READ ONLY`, and `OPEN OUTPUT`, `I-O` and `EXTEND` establish `SHARING WITH NO OTHER` —
always one of §9.1.15's own three modes, and GnuCOBOL's posture (rule 1: a file opened for input takes a shared
lock, every other mode an exclusive one). It is read at ONE site, `FileRegistry.SharedOpenAttempt`
(`sharingOverride ?? declared clause ?? ImplementorDefaultSharing(mode)`), where the OPEN statement's mode is
known; `ConnectorShare.Sharing` stays nullable because it records whether the file control entry WROTE a SHARING
clause, but `PhysicalFileTable.State.Open`, `Table19.Conflicts` and `FileLockPosture` hold a non-null
`FileSharing` everywhere. Two clause-less connectors can therefore be open together only when both are INPUT; a
clause-less updater or writer has the file to itself. A LOCK MODE clause is not a sharing specification, so a
LOCK MODE-only SELECT and a RETRY-phrase-only OPEN take the same default (and a connector that is NO OTHER has
its record locks ignored, §9.1.15 1)). Programs that need two connectors on one file say `SHARING WITH ALL
OTHER`, which §14.9.27.3 SR8 couples with a LOCK MODE clause.

**What it buys, and what it cost before.** `Conflicts` used to be a four-test predicate chain whose final
`return false;` carried the comment `// (e) ALL OTHER` — the letter of a sub-case it had never implemented —
so every incoming OPEN OUTPUT was permitted and truncated a file another connector held open; and
`FileRegistry.Open` consulted the table only `if (IsSharingActive(name))`, while `RegisterSharing` set
`FileConnector.SharedStreams` (an OS handle opened `FileShare.ReadWrite`). Together those made a file declared
`SHARING WITH NO OTHER` strictly LESS protected than one declaring nothing: the clause dropped the OS exclusion
and handed arbitration to a table that could not see the plain connector coming. PB321 fixed the arbitration
half by making Table 19 arbitrate every connector; the OS half survived it and was measured across processes
three landings later — a `SHARING WITH NO OTHER` connector still admitted a foreign append while a clause-less
one refused it, and two clause-less connectors Table 19 permitted to share were refused by the host with '30'.
`SharedStreams` is now gone: the host share mode is §9.1.15's **file lock**, derived by `FileLockPosture` from
the connector's sharing MODE alone, handed down in one place, `FileRegistry.SharedOpenAttempt`, before the handle
exists (kb/Work PB740; see `DESIGN-runtime-library.md`). Since every connector holds one of the three modes, every
pair Table 19 admits is already mutually admissible at the host and no handle is rebuilt to admit a sibling
(kb/Work PB322 deleted the widening that served the undetermined default).

**⛔ AND A SHARE MODE IS NOT THE WHOLE OF THE LOCK AGAINST OTHER RUN UNITS** (kb/Work PB833). A host share mode
refuses an open by its ACCESS, so on Unix (one advisory `flock`, two states) rule 2's *"restricts concurrent access
… to input mode"* — admit a reader, refuse a writer — cannot be said at all, and on EVERY host it cannot refuse the
truncation of an `OPEN OUTPUT` a sibling's posture admitted (measured: a second run unit's `OPEN OUTPUT` answered
`00` against a file the first held `SHARING WITH ALL OTHER`, and emptied it). A connector therefore also takes
`RunUnitFileLock` at the OPEN — its Table 19 column published as a shared byte-range lock on the physical file, then
the columns its request row refuses tested for another holder — and, on a host whose share modes are mandatory
(Windows), an OUTPUT open on an existing file first asks the host for exclusive access, before anything is truncated. Both are taken ONCE, in `FileConnector.Open`, above the
organizations, and released by the CLOSE or an unsuccessful OPEN; the verdict is `Table19.Cell`, so the cross-run-unit
and in-run-unit answers cannot drift (`RunUnitFileLockDriftTests`, 768 pairs through two registries). The mechanism,
the host primitive (Linux open-file-description locks) and the hosts that cannot carry it are in
`DESIGN-runtime-library.md`.

**⛔ AND THE LOCK IS OWED BY EVERY ORGANIZATION, WHICH IS A SEPARATE FACT FROM THE DERIVATION BEING RIGHT**
(kb/Work PB771). §9.1.15 3) names none — *"The successful opening of a file establishes a file lock for the
applicable sharing rules, thereby preventing other run units from opening that file with incompatible sharing
rules"* — and a host share mode is the only thing a process can say to another run unit, so a connector that
holds no handle establishes no lock however correct the derivation above is. RELATIVE and INDEXED held none:
their whole record store was loaded at the OPEN and rewritten at the CLOSE through short-lived
`HostFile.OpenAuxiliary` handles, so `SHARING WITH NO OTHER` — §9.1.15 1)'s *"exclusive access to a physical
file"* — protected a keyed file from nothing outside the run unit, and the CLOSE then truncated it and rewrote
it from the OPEN's snapshot, discarding another run unit's records with '00' reported on both sides. Measured,
on both organizations, before the fix. `KeyedConnector` now takes and holds the handle for the life of the
OPEN and the store travels THROUGH it (`KeyedConnector.Load`/`Persist` read and write positionally through that handle; `RecordFraming` only composes and decodes the image),
which is also what keeps kb/Work PB713 closed: a second handle for the load would ask for access the
connector's own `FileShare.None` forbids. `FileLockPostureDriftTests.EveryOrganizationHoldsALiveFileLockWhileOpen`
measures the lock from a handle OUTSIDE the connector for every (organization × open mode), and
`EveryOrganisationsHandleAccessLiesInThatBand` turns a FOURTH organization red rather than letting it inherit
the omission. The in-run-unit half is certified from COBOL by `pb771_keyed_file_lock` at both editions. The guarding test was six
`InlineData` rows against 35 cells with no row whose incoming mode was OUTPUT (kb/Work PB321). Two silent
defaults went with it — the emitter registered a LOCK-MODE-only file as ALL OTHER, and a RETRY-phrase-only OPEN
registered ALL OTHER here — three arms of one determination, two of which had already answered it.

**Residue (reported, not fixed):** the runtime has no edition, so the arbitration also runs at `--std 85`. No
SHARING clause is accepted there, so the only conflict reachable is sub-case (e), and a second OPEN OUTPUT
answers `'61'` where the OS accident answered `'30'`. Table 19 rides Annex A.4.7, a COBOL-2002 introduction, so
whether a pre-2002 edition may report a value from the 2002 5x/6x family is a determination in A.1 item 77's
family; nothing in the corpus exercises it.

### D15. The connector-to-physical-file ASSOCIATION is a per-statement act with ONE entry point, `FileConnector.Associate`, and the connector's `HostPath` is its only answer — nothing caches a resolved path.

**The rule.** §14.9.27.4 GR26 routes the OPEN statement to §12.4.5.3 GR3/GR4, and GR3 is written as a timing rule
first and a value rule second: *"The ASSIGN clause specifies the association of the file connector referenced by
file-name-1 to a physical file identified by device-name-1, literal-1, or the content of the data item referenced by
data-name-1. **The association occurs at the time of execution of an OPEN, SORT, or MERGE statement that referenced
file-name-1**, according to the following rules: a) When the TO phrase … is specified and the USING phrase is
omitted, … identified by the specification of device-name-1 or the value of literal-1 …; b) **When the USING phrase
of the ASSIGN clause is specified, the file connector … is associated with a physical file identified by the content
of the data item referenced by data-name-1 in the runtime element that executes the OPEN, SORT, or MERGE
statement.**"* §9.1.21 (Dynamic file assignment) is the concepts clause that names the whole facility, and Annex
D.19.9.2's NOTE states the consequence outright: *"The MOVE statements only have an effect on the dynamic assignment
when a subsequent OPEN statement for the file connector is executed."*

**The shape — THE OPERANDS TRAVEL WITH THE STATEMENT.** `FileConnector.HostPath` is a settable property whose ONE
writer is `FileConnector.Associate(spec, dynamic)`, and `FileRegistry.OpenCore` — the one OPEN dispatch that the plain
`Open`, `OpenNoRewind`, `OpenShared` and the emitted SORT/MERGE implicit opens (§14.9.40.4 GR12a/GR15a, §14.9.24.4
GR7a) all funnel through — is its only caller. `spec` is the EXECUTING element's own ASSIGN specification, rendered at
the statement by `SequentialIoEmitter.ExecutingElementArgs(file)` and passed as an argument of the OPEN call; `dynamic`
selects GR3 b)'s content rules over GR3 a)'s plain literal, so both arms of GR3 run through one entry point rather than
a register-time mechanism for TO and an open-time mechanism for USING.

**⛔ Why the source is an ARGUMENT and never state on the connector (kb/Work PB673).** Both arms of GR3 name the
element that runs the statement — a) *"in the source unit that specifies the OPEN, SORT, or MERGE statement"*, b)
*"in the runtime element that executes the OPEN, SORT, or MERGE statement"* — and a file connector is neither
per-element nor per-activation. An EXTERNAL file connector is ONE object per run unit shared by every describing
element (§13.18.22.4 GR4 a), whose entries §12.4.5.3 GR1 b) requires only to be CONSISTENT — unlike GR1 i), which
makes FILE STATUS *the same* external item — so two programs may legally hold separate storage for data-name-1
(WiseOwl COBOL's GR1 b) consistency rule compares the ASSIGN operand text — device class dropped, a literal by its value,
case ignored, no host-path mapping — and the USING data-name-1 by spelling: `docs/CONFORMANCE.md` §7, `DOC-A.1-72`). A
RECURSIVE non-INITIAL unit's internal connector is unit-scoped last-used state across activations (§8.6.4,
§14.6.2.3.3) while its LOCAL-STORAGE is per-activation. An installed closure therefore answers with whichever
element/activation installed it LAST, which is the executing one only by accident: the earlier
`CobolFile.SetAssignUsing(key, () => <data-name-1>)` install (unguarded, for the kb/Work PB168 reason) opened the
wrong physical file with status '00'. Rendering the operand at the statement makes the right answer structural —
the emitting unit IS the executing element and the emitted expression reads the LIVE activation's storage — and the
runtime entry points take the operands with NO DEFAULT, so an emitter path that forgets them is a compile error.
Both emit paths get it from the same helper: the statement emitters for a program, and the same ones inside
`OoEmitter`'s class bodies for an object — Annex D.19.9.2's own worked example is an instance file.

**Why the sharing registry stopped caching the host path.** `ConnectorShare` used to hold a `Host` copy taken at
`RegisterSharing` time, and every physical-file-table lookup (`ReadLockGovern`, `ReadShared`, `WriteShared`,
`RewriteShared`, `DeleteShared`, `SharedClose`) read that copy. It was only ever right because the path could not
change; a mutable association makes it a stale key that would arbitrate §9.1.15 sharing over the file the connector
used to be associated with. The field is gone and the lookups read `c.HostPath` (or `HostPathOf(name)`).

**The failure status is the standard's own.** GR3's closing sentence — *"If the association cannot be made because
the content of the data item referenced by data-name-1 is not consistent with the specification for device-name-1 or
literal-1, the OPEN, SORT, or MERGE statement is unsuccessful"* — has a dedicated I-O status, §9.1.13.6 item 2's
**'31'**, and `Associate` returns it. The CONTENT rules it applies are the implementor's (§12.4.5.3 GR4, Annex A.1
items 10 and 73) and are stated in `docs/CONFORMANCE.md` §7 under `DOC-A.1-73`.

**An unassociated connector is a real state, not an error.** A bare `ASSIGN USING data-name-1` names no
device-name-1/literal-1 at all, so nothing identifies a physical file until the first OPEN/SORT/MERGE. It registers
with an EMPTY `HostPath`, and `DELETE FILE` on one takes §14.9.10.4 GR14 — *"If the file associated with file-name-1
is not present, the execution of the DELETE FILE statement is successful and the I-O status value … is set to
'05'"* — rather than re-resolving data-name-1, which would be an implementor extension to GR3's closed list of
associating statements on the one verb where guessing wrong destroys data.

**§12.4.5.2 SR7 is enforced at bind time**, once the data forest is indexed (`DataBinder.ResolveAssignUsing`):
data-name-1 shall reference an alphanumeric data item (`COBOLNET1810`) and shall not be subordinate to the file
description entry for file-name-1 (`COBOLNET1811`). The second half is the dangerous one — an operand inside the
file's own record area is overwritten by every READ of that file.

**Rejected alternatives.** Emitting a `CobolFile.Assign(key, expr)` statement before every OPEN (spreads one rule
across every OPEN site, and misses SORT/MERGE unless each is remembered); re-resolving inside
`FileConnector.Open` (too late — `SharedOpenAttempt` consults the physical-file table on the OLD path before
calling it); keeping the registration-time resolve and adding a second dynamic path (two mechanisms for one job).
### D16. The READ statement's lock options are TWO INDEPENDENT PRINTED BRACKETS, and they are two facts from the grammar through to the runtime — contention and retention — never one enum.

ISO §14.9.30.2 prints the READ lock options as two plain brackets, in this order, and then the KEY phrase:

    [ ADVANCING ON LOCK | IGNORING LOCK | retry-phrase ]      (Format 2 drops ADVANCING ON LOCK)
    [ WITH LOCK | WITH NO LOCK ]
    [ KEY IS { data-name-1 | record-key-name-1 } ]            (Format 2 only)

Measured, not inherited: `clause_page.py 14.9.30.2` -> PDF page 722, and `figure_geometry.py 722` reports the
Format-1 brackets at y=280.90 (h=48.79, three stacked alternatives) and y=339.26 (h=31.98, two), the Format-2
brackets at y=507.57 and y=547.48 and the KEY bracket at y=589.03, all with PLAIN stems — contrast the AT END
group at y=377.08 and the INVALID KEY group at y=634.55, which the same tool flags
`<-- CHOICE INDICATORS (5.2.6.4)`. Three rules follow and all three are load-bearing:

- **§5.2.6.2** — a bracket admits "the syntax element contained within the brackets or one of the alternatives
  contained within the brackets", so AT MOST ONE alternative comes out of each bracket.
- **§5.2.6.1** — an option may also be selected "by specifying a unique combination of possibilities from a
  series of brackets", so the two brackets are INDEPENDENT: `IGNORING LOCK WITH NO LOCK` is one legal READ.
- **§5.2.1** — the phrases "shall be written … in the sequence given in the general format", so the KEY phrase
  FOLLOWS both brackets and the reverse spelling is not conforming.

**The shape.** `CobolIO.g4#readLockContentionPhrase` is bracket 1 (`readAdvancingOnLock | readIgnoringLock |
retryPhrase`) and `#recordLockPhrase` is bracket 2 (`WITH? NO LOCK | WITH? LOCK`) — one grammar object per
printed bracket, which is what writes "at most one of these" down exactly once. `BoundRead` / `BoundKeyedRead`
therefore carry FOUR fields, not one: `Lock` (the retention bracket), and `AdvancingOnLock` / `IgnoringLock` /
`Retry` (the three alternatives of the contention bracket, at most one of which the grammar can deliver). The
runtime mirrors it: `FileRecordLock` is the retention bracket alone, `CobolFile.ReadShared` takes
`advancingOnLock` and `ignoringLock` as their own arguments, and the Format-2 `ReadLockGovern` takes
`ignoringLock` (ADVANCING ON LOCK is not in the Format-2 general format — D17). WRITE and REWRITE reference
`recordLockPhrase` only, because §14.9.51.2 and §14.9.35.2 print `[ retry-phrase ]` and
`[ WITH LOCK | WITH NO LOCK ]` and no IGNORING LOCK at all.

**Why it is written as a decision (kb/Work PB331).** One rule, `recordLockPhrase : IGNORING LOCK | WITH? NO
LOCK | WITH? LOCK`, reached through three free optionals, inverted BOTH cardinalities at once: what the printed
brackets make mutually exclusive was free (`ADVANCING ON LOCK RETRY 3 TIMES IGNORING LOCK` compiled), and what
they make independent was exclusive (`IGNORING LOCK WITH NO LOCK` was `no viable alternative at input 'WITH'`).
The same merge put IGNORING LOCK on WRITE and REWRITE, and — because a single enum cannot say two things — it
kept the legal contention+retention pair out of `ApplyReadLockDiscipline`'s GR11 b) release. It also SILENTLY
SUPPLIED §14.9.30.3 SR3: `FileLockBinder.CheckRecordLockPhrase`'s summary claimed to enforce "IGNORING LOCK and
WITH LOCK are mutually exclusive" while its body tested only SR4's automatic-locking condition, so the
forbidden pair was rejected by the collapse and by nothing else. Splitting the brackets without landing a real
SR3 check in the same change set would have opened that hole; `CheckIgnoringLock` -> COBOLNET1818 is the check,
and `negative/pb331-read-ignoring-with-lock` is its witness, paired with the permitted-pair positive
`2002/pb331_read_lock_brackets`. ⚠ SR3's "the LOCK phrase" is WITH LOCK and NOT WITH NO LOCK: §14.9.30.4 GR11 b)
names "the NO LOCK phrase" and GR11 d) "the LOCK phrase" as different phrases in the same statement.

**One optional word came with the measurement.** Page 722 underlines ADVANCING and LOCK and NOT ON
(`figure_extract.py 722` -> `_ADVANCING_ ON _LOCK_`), so by §5.2.3 `READ … ADVANCING LOCK` is conforming; it was
`error COBOL0001: missing token before 'LOCK'`. The grammar now spells the phrase `ADVANCING ON? LOCK`.

**And one optional word written down TWICE.** The statement's single printed `RECORD` — one word, after the
`{NEXT | PREVIOUS}` braces — lived in BOTH `readDirection : (NEXT | PREVIOUS) RECORD?` and `readStatement`'s own
`RECORD?`, so `READ SQF NEXT RECORD RECORD … END-READ` compiled clean. §5.2.3 makes a non-underlined word one
that may be OMITTED, not one that may be repeated. `readDirection` is now `NEXT | PREVIOUS`, which is also what
§5.2.6.3 describes: the NEXT alternative "contains only optional words", so it is the default and the whole rule
is optional at the call site. Neither spelling of the word was wrong on its own; having the same optional word in
two rules was, and no test could see it because both single-`RECORD` spellings still parsed
(`feedback_one_rule_one_place`).

### D17. Record-lock governance on a READ is split by FORMAT, not by ORGANIZATION: one governed Format-1 read serves sequential, relative and indexed, and it owns the GR22 skip-scan.

**The rule shape.** ISO §14.9.30.4 GR7–GR12 are ALL-FORMATS rules; GR22 is not. ADVANCING ON LOCK appears only
in the **Format-1** general format (§14.9.30.2) and §14.9.30.3 SR6 bars it under ACCESS MODE RANDOM, so it is a
phrase of the sequential-ACCESS read on **every organization** — sequential, relative and indexed alike. The
compiler had split the governance the other way, by organization: `FileRegistry.ReadShared` (which carried the
skip-scan) began `if (… c is not SequentialConnector) return false;`, and the keyed emitter called the post-read
`ReadLockGovern`, which has no advancing-on-lock parameter at all. `BoundKeyedRead.AdvancingOnLock` was set by
the binder, edition-gated by `VersionConformancePass`, and read by nothing: a relative or indexed
`READ … ADVANCING ON LOCK` answered `'51'`, the one status GR22 rules out (kb/Work PB340).

**GR22 needs no pre-read peek, on any organization.** GR22 states the conflict condition **does not exist**,
and its model is explicitly post-read: *"as if the locked record were read and then the same READ statement were
executed"*. The locked record IS read and the position DOES advance; that is the rule, not a compromise. So the
skip is one `continue` after the physical step, shared by all three organizations, and it is the ONE arm of the
governed read that takes no peek.

**⛔ EVERY OTHER ARM DOES TAKE ONE, AND EVERY ORGANIZATION CAN ANSWER IT (corrected 2026-09-06, kb/Work
PB338).** This decision used to argue that "only the sequential walk can name the record it is about to deliver
— a relative or indexed walk selects *the first existing record … greater than the file position indicator*
(GR21) and learns which that is by reading it." **That premise is false for these connectors.** A relative or
indexed walk learns the record by SELECTING it out of an ordered in-memory store; the physical read is the
COMMIT, not the discovery. `RelativeConnector.SelectSequentialSlot`, `IndexedConnector.SelectSequentialRecord`
and `IndexedConnector.FindRandom` are that selection, extracted so the peek and the read share ONE copy of
GR21/GR32 rather than the peek carrying a second. Because the premise was believed, the keyed READ ran the GR9
conflict check AFTER the connector had already set `_fpi`/`_fpiKey`/`_readOrdinal`/`_refKey`, so a `'51'`
answered with the position ALREADY ADVANCED — the next sequential READ silently skipped the record the failed
one had been refused, and on an indexed random READ `_refKey` had been reassigned against GR10 d).

Three general rules require the decision to precede the commit, and they are the same requirement three ways:
GR10 a) *"The file position indicator is unchanged"*, GR10 d) *"The key of reference for indexed files is
unchanged"*, and GR13 a) *"The I-O status associated with file-name-1 is updated and, **if the record operation
conflict condition did not occur**, the file position indicator is set."* GR13 a) had been read as its
unconditional half.

**What '51' does NOT arm.** §14.9.30.4 GR18 — *"**Unless otherwise specified**, at the completion of any
unsuccessful execution of a READ statement … the file position indicator is set to indicate that no valid record
position has been established"* — is the DEFAULT, and GR10 a)/d) are its "otherwise specified". So a `'51'` does
not arm §9.1.13.7 item 6's `'46'`: that status needs BOTH *"no valid next record has been established"* and one
of its two causes, and GR10 a) leaves a valid next record established. Read the other way GR10 a) and GR10 d)
would have no observable consequence anywhere, and a `'51'` on a sequential-ORGANIZATION file — which has no
START to reposition with — would be unrecoverable.

**What '53'/'54' DOES arm.** A §12.4.5.9.4 GR7 record-lock-ceiling denial is unsuccessful but is NOT the record
operation conflict condition: GR9 defines that condition as *"the record identified for access is locked by
ANOTHER file connector"*, where '53'/'54' are this connector's own lock COUNT. GR10 does not reach it, GR18
applies in full, and the next sequential READ is `'46'`. That is why the ceiling is a PRE-FLIGHT
(`FileRegistry.PreflightReadLock`, the shape WRITE and REWRITE already used) and why the denial calls
`FileConnector.ApplyUnsuccessfulReadPosition()`: refusing after the physical step left a successfully advanced
position wearing a failure status.

**GR11 a) is tied to EXECUTION, not to success.** *"If single record locking is specified … any prior record
lock associated with that file connector is released by the execution of the READ statement"* — and
§12.4.5.9.4 GR6 says it for every verb: *"Execution of any I-O statement except START releases any previously
locked record in that file for that file connector."* The standard qualifies deliberately where it means to, in
the very same rule (GR11 b) *"at the completion of the successful execution"*, GR11 c)/d) *"a successfully
accessed record"*). The release therefore fires at the TOP of both governed READ entries, before anything can
make the statement unsuccessful — and §12.4.5.9.4 GR6 now lives in ONE place, `ReleasePriorRecordLocks`, which
all four governed record verbs call; it had been written out four times and the READ copy alone sat on the
success side of its caller's guard. START calls it from nowhere, which is §14.9.41.4 GR3: *"The execution of the
START statement does not detect, acquire, or release record locks."*

**The shape.** `FileRegistry.ReadShared(name, previous, phrase, advancingOnLock, ignoringLock, retry…, out image)`
is the ONE governed Format-1 read and returns the **I-O status** (a record was made available iff it begins
`'0'`), which is the more general of the two contracts the emitters need. Three collaborators, each the single
place its rule is written:

| member | rule |
|---|---|
| `ReadFormat1Step` | ONE physical NEXT/PREVIOUS retrieval on any organization — the step GR22 repeats, and the COMMIT half of the split |
| `FileConnector.PeekSequentialRecordId` | GR9's pre-read conflict target, overridden by ALL THREE organizations; `""` only when no record is identified |
| `FileConnector.PeekRandomReadRecordId` | the same for Format 2 — GR29's RRN, GR32's key of reference — without establishing the key of reference (GR10 d) |
| `ConflictOnLockedRecord` | GR9 + §14.7.9's RETRY — also §14.9.35.4 GR11 and §14.9.10.4 GR6, one rule worded three times |
| `PreflightReadLock` | §12.4.5.9.4 GR7's ceiling, tested before the retrieval |
| `ApplyPostReadLockActions` | GR11 b)/c)/d), the only lock actions that ARE conditioned on success |
| `ReleasePriorRecordLocks` | §12.4.5.9.4 GR6 / §14.9.30.4 GR11 a), for all four governed verbs |

`SequentialIoEmitter` renders it through `RuntimeApi.FileReadSharedOk`, which wraps the call in `[0] == '0'` so
its bool contract is unchanged; `KeyedIoEmitter` renders `ReadKind.Next`/`Previous` through the same entry.
**Format 2 has its own governed entry, `FileRegistry.ReadKeyedShared`**, built to the same shape and for the
same reason: it OWNS the physical retrieval so the GR9 check and the GR7 ceiling precede it. It replaced a
post-read `ReadLockGovern` patch, and the emitter's two-call `FileReadKeyed` + `FileReadLockGovern` pair is now
one call — nothing may render a bare keyed read and then adjust its status, because GR10 a)/d) cannot be
delivered by an adjustment. ADVANCING ON LOCK is absent from Format 2 (§14.9.30.2, and §14.9.30.3 SR6 bars it
under ACCESS MODE RANDOM), so that entry carries no advancing-on-lock argument.

**The binder half came with it.** §14.9.30.3 SR9 implies NEXT under ACCESS DYNAMIC "if any of the following
phrases is specified: **ADVANCING**, AT END, or NOT AT END", and `KeyedIoBinder.BindRead` tested two of the
three, so `READ f ADVANCING ON LOCK` bound as a Format-2 random read — the GR22 loop was unreachable for the one
spelling SR9 exists to name (kb/Work PB335).

**The drift test that generalises it.** `BoundIoPhraseConsumptionDriftTests` asserts that every phrase property
declared by a bound I-O node is READ by its emitter, over all eight READ/WRITE/REWRITE/DELETE/START nodes. Three
inventory rows were open for that one shape at once — this one, OPEN … WITH NO REWIND (PB317) and the sequential
READ direction (PB334). It cannot see a phrase the BINDER never stored, which was PB334's remaining half — now
landed: `BoundRead` carries `ReadKind` and `InvalidKey`, so the drift test covers that arm too, and
`previous` reaches `ReadFormat1Step` / `PeekFormat1RecordId` from BOTH emitters.
### D18. A file description entry with NO record description entries still has a record area, and it is SYNTHESIZED AT BIND TIME — ISO §14.9.30.4 GR6's implied entry, made real — never worked around at the consumers.

ISO §13.4.5.3 SR3 expressly contemplates the shape: *"When no record description entries are specified: a) a
RECORD clause shall be specified in the file description entry, b) a FILE phrase specifying file-name-1 and the
FROM phrase shall be specified on all WRITE and REWRITE statements associated with the file, and c) an INTO
phrase shall be specified on all READ statements associated with the file."* SR7 confines the *one or more record
description entries* requirement to INDEXED files, so `FD FIN RECORD CONTAINS 20 CHARACTERS.` with no level-01 is
legal on both the sequential and the relative organizations. §14.9.30.4 GR6 then says exactly what that file's
record area IS: a READ INTO *"proceeds as though there were one record description entry describing an
alphanumeric group item of the maximum size established by the RECORD clause"*.

**The decision:** `DataBinder.MaterializeImpliedRecord` builds that entry — an unnamed level-01 GROUP over one
FILLER `PIC X(n)` — as the last act of binding each FD, and everything downstream sees an ordinary record.
`FileModel.AreaRecord` is consequently non-null for every FD that can be opened with data, and is null only for a
REPORT file (§13.4.5.3 SR8 forbids it record descriptions). A SELECT with no FD or SD at all never reaches code
generation: §12.4.5.2 SR3 rejects it (COBOLNET2635, `DataBinder.ScreenSelectsHaveDescriptions`), and an FD or SD
that no SELECT names is rejected by §13.4.5.3 SR1 / §13.4.6.3 SR1 (COBOLNET2636) (kb/Work PB1077, PB1237, PB1290).

- **A GROUP, not an elementary `PIC X(n)`,** because the distinction is observable: §14.9.25.4 makes a MOVE whose
  sender is a group item a group (alphanumeric) move, so `READ … INTO` a numeric receiver copies bytes where an
  elementary alphanumeric sender would convert. GR6 says "group item".
- **Unnamed and off `ByName`,** because the entry is implied: the program cannot reference it, which is precisely
  why SR3 b) and c) require the `FILE … FROM` and `INTO` phrases on the verbs.
- **The size is "the maximum size established by the RECORD clause"** — format 1's integer-1, format 3's
  integer-5, format 2's integer-3. A format-2 clause with no `TO` phrase establishes none (§13.18.43.4 GR10
  defers to "the greatest number of bytes described for a record in that file", and none is described), and
  neither does an absent clause, which §13.4.5.3 SR3 a) and §13.18.43.3 SR1 forbid outright: **COBOLNET1836**.
- **The two entry kinds that take SR3's permission back are SCREENS, not fallbacks: COBOLNET1837.** §13.4.5.3 SR7
  (INDEXED) and §13.4.6.3 SR2 (sort-merge) both require record description entries, because both are keyed and a
  key is located IN a record (§12.4.5.12.3 SR2; §14.9.40.3 SR6 a).

**Rejected alternatives.** *Register the connector and leave `AreaRecord` null* — the shape this replaced: the
record-less arm was asked TWICE and answered differently (`SequentialIoEmitter.EmitFileRegistration` registered
only a REPORT file; `KeyedIoEmitter.EmitRegistration` returned outright), so both organizations produced NO file
connector and the first I-O verb aborted the run unit — and repairing registration ALONE would only have turned
the crash into a silent no-op, because the READ record-area store and the implicit INTO move were separately
guarded on the same null. Five null arms for one absent fact is four too many (`feedback_one_rule_one_place`).
*Synthesize at emit time instead* — the record has to exist for the binder's own record-area resolution
(`ReferenceResolver.ResolveItem`), not just for the registration call.

### D19. A syntax rule of a FILE CONTROL ENTRY clause is enforced ON THE ENTRY — in `DataBinder.BindFileControl` when the clauses alone decide it, in `DataBinder.ResolveFiles` when it needs a resolved data item — never on a verb. It is the converse of D13, and the two together decide where every I-O syntax rule goes.

**The rule.** §12.4.5.5.2 SR2: *"The DYNAMIC and RANDOM phrases shall not be specified for a sequential file."*
The general format says the same thing structurally — Format 3, the sequential file control entry, admits only
`[ ACCESS MODE IS SEQUENTIAL ]` — but a format is not a check, and nothing screened it: `SELECT F ASSIGN TO "f"
ORGANIZATION IS SEQUENTIAL ACCESS MODE IS RANDOM.` compiled and ran clean (kb/Work PB692).

**Where it lives.** `DataBinder.BindFileControl`, immediately after the clause loop, beside the §12.4.5.9 SR2
screen already there: `file.IsSequential && file.AccessMode is Random or Dynamic` — the predicate NAMES the
two phrases SR2 names rather than negating SEQUENTIAL → **COBOLNET1858**,
positioned at the ACCESS MODE clause itself. Both banned phrases go through one screen and one descriptor,
because SR2 is one prohibition with two spellings.

**Which files are "sequential".** `FileModel.IsSequential` — the ONE spelling of sequential organization, now
shared by both entry screens rather than each open-coding the enum pair. It answers three source shapes:
an explicit `ORGANIZATION IS SEQUENTIAL`; `LINE SEQUENTIAL` (§12.4.5.10.3 GR2 puts the phrase in the ORGANIZATION
clause, that clause is written only in the Format-3 entry, and §12.4.5.2 SR11 says *"Format 3 shall be specified
only for a sequential file or a report file"*); and the **omitted** clause, which §12.4.5.10.3 GR6 makes
*"sequential organization with the RECORD SEQUENTIAL phrase"*. The omitted clause is the shape a screen keyed on
a written clause would miss, and it is the shape a user is most likely to write.

**Why a verb cannot host it.** D13's converse, and for the mirror-image reason. §12.4.5.5.2 SR2 quantifies over
the ENTRY, not over a statement: a program that declares the combination and never opens the file has already
violated it. Screening it at READ (or WRITE, or START) would leave every unread file unchecked, would report the
one entry error once per statement in programs that do use the file, and would state a declaration rule in the
vocabulary of a statement that does not appear in it. **A syntax rule listed under a file control entry clause
belongs to the entry binder; a syntax rule listed under a statement belongs to that statement's binder (D13).
Neither is a place to put the other.**

**What this makes unreachable, deliberately.** From here on `IsSequential && AccessMode != Sequential` cannot
hold, so every statement-level screen that tests `AccessMode == Random` on a **sequential-organization** file is
defence-in-depth — kept (it is the statement's own rule, and it costs one comparison), never the only guard.
§14.9.30.3 SR6's screen in `KeyedIoBinder` is unaffected: RANDOM is legal for relative and indexed files, which
is the only path that reaches it.

**Edition-invariance was derived, not assumed.** No `docs/VERSION_CHANGE_REFERENCE.md` row touches §12.4.5.5, and
ANSI X3.23-1985's sequential-I-O file control entry likewise offered no ACCESS MODE but SEQUENTIAL, so the screen
is unconditional and three of the four negatives assert `COBOLNET1858` at 85/2002/2014/2023. The LINE SEQUENTIAL
negative names 2002/2014/2023 only — 1985 has no line sequential organization at all, so its 1985 verdict belongs
to that organization's edition gate (kb/Work PB688), not to this rule.

**The complement was measured, not assumed.** A screen is evidence about what it REJECTED, never about what it
let through. A static scan of all 1483 SELECT entries under `tests/` reports every cell this screen rejects
EMPTY — no corpus program declares the combination, so the change cannot over-reject anything the suite already
compiles — and every legal (organization × access) cell populated **except** LINE SEQUENTIAL with an explicit
`ACCESS MODE IS SEQUENTIAL`, which had zero witnesses. `conformance:2023/pb692_line_sequential_access_sequential`
is that missing positive; `conformance:2023/l1_open_mixed_org_access` already pins RELATIVE/RANDOM and
INDEXED/DYNAMIC.

**Two sites, one decision, and the criterion that picks between them.** The entry's syntax rules do not all
become decidable at the same moment, so "on the entry" is two places, and which one a rule goes to is settled by
what the rule needs rather than by taste:

- **Decidable from the CLAUSES ALONE** → `DataBinder.BindFileControl`, inline after the clause loop, reporting at
  once. §12.4.5.5.2 SR2 above, and the §12.4.5.9 SR2 screen beside it, are of this kind: organization against
  access mode, a clause against another clause.
- **Needs a RESOLVED DATA ITEM** → `DataBinder.ResolveFiles`, the post-build pass, through the one table below.
  A key clause names a data-name whose OCCURS ancestry, PICTURE and owning record description are not known until
  the data forest is indexed, so a screen in `BindFileControl` could only guess at them.

Both run once per declared file, before any statement is bound; neither is reachable from a verb. A new entry
rule is placed by asking which list it falls in, and nothing else.

**The key clauses' rule set — the second consumer.** The key clauses of §12.4.5 state overlapping rule
sets, and the overlap is the point:
§12.4.5.12.3 SR1 (RECORD KEY) and §12.4.5.6.3 SR1 (ALTERNATE RECORD KEY) are the *same sentence* —
*"Data-name-1 and data-name-2 shall not be subject to any OCCURS clauses"* — and §12.4.5.13.3 SR1 states it a
third time for the relative key. Alongside them sit §12.4.5.12.3 SR2 / §12.4.5.6.3 SR2 (data-name-1 is within a
record description entry associated with the file-name), §12.4.5.13.3 SR2 (an unsigned integer without `P`) and
SR3 (*not* in a record of the file), §12.4.5.1 Format 1's unbracketed RECORD KEY clause, and §12.4.5.2 SR10
(*"The RELATIVE clause shall be specified if the DYNAMIC or RANDOM phrase of the ACCESS clause is specified"*).
Every one of them is stated about the ENTRY. The entry violates it whether or not a statement references the
file, and §4.2.2's mechanism has to be able to indicate it.

**What was wrong.** They were enforced by `KeyedIoBinder.KeyedValidateFile`, called from the five keyed verbs
behind a `_keyedCheckedFiles` memo whose only job was to make the report once-per-file. So the rules fired on the
FIRST KEYED VERB naming the file — and a program whose procedure division only OPENs and CLOSEs the file has no
such verb. Measured on the pre-fix tree: an entry with `RECORD KEY IS IX-KEY` and `ALTERNATE RECORD KEY IS
IX-ALT` both under `OCCURS` compiled with **zero diagnostics**, and adding one `READ … NEXT` grew two
COBOLNET0863s. Two citations were wrong in the same site — the required-clause rule was filed under §12.4.5.13,
a clause that has no syntax rules at all, and SR2/SR3 were cited as "§12.4.5.13 SR2/SR3" — and one rule was
missing entirely in a way the resolution path hid: an ALTERNATE RECORD KEY whose data-name-1 resolved to nothing
was *dropped silently* from `FileModel.AlternateKeys`, leaving the file with fewer access paths than the entry
declared and every index-aligned list downstream out of step with the written clauses.

**The design.** `FileControlKeyRules` (`src/Cobol.Net.Compiler/Binding/FileControlKeyRules.cs`) is ONE table —
one row per rule, carrying its inventory rule-id, its clause, a verbatim span of the printed rule, the SET of
file KINDS it is screened over (`FileKinds` — one flag per §12.4.5.10.3 organization plus `SortMerge`), the entry
CLAUSE it is stated about (`FileKeyRole`, whose `Entry` member is the rule stated about the entry itself), three
predicates (precondition · violation · message) and an optional diagnostic code. `DataBinder.ResolveFiles` calls `Screen(file, Edition)` once per declared
file, after the keys resolve. One-report-per-file is then a consequence of *where* the screen runs, not a set it
has to carry, and the diagnostic is positioned at the offending CLAUSE (`FileModel.RecordKeyAt` /
`AlternateKeyClause.At` / `RelativeKeyAt`) or, for an absent clause, at the entry (`FileModel.EntryAt`).
An absent clause is modelled as an operand with a null name, so a "clause required" rule and an "operand breaks
a rule" rule are the same kind of row and there is no second arm to dispatch on.

**Why a table and not seven `if`s.** A rule set with one member written down is where the missing members hide:
PB354 found two of these three OCCURS bans absent, PB699 found the third fired only under a verb. The table makes
the next key rule a ROW, and `FileControlKeyRuleDriftTests` keeps that true — it re-derives every row's rule text
from `specs/ISO_COBOL.md` (clause region *and* printed ordinal), asserts each row's shipped message names the
row's own citation, asserts each row is screened either on EXACTLY the kind whose format carries its clause or on
a set DISJOINT from it (never a partial overlap, and only §12.4.5.2 may hold the disjoint half), asserts every
rule the standard states as TWO obligations has a row per arm — re-deriving the split from the printed sentence —
and asserts the table and the traceability inventory name the same set of rules, so a rule screened here without a
row, or a row claiming an id the inventory files elsewhere, is red.

**The organization column is a SET, and that is what let §12.4.5.2 SR8/SR9 in** (kb/Work PB742). It began as one
`FileOrganization` meaning two things at once — the format that CARRIES a clause, and the files a rule is IN FORCE
over. Those coincide for every rule stated *inside* a format and are OPPOSITE for SR8 and SR9, whose whole subject
is a clause written where its format does not apply, so a scalar column could not hold them and neither rule had
any site at all: `SELECT F ASSIGN … RECORD KEY IS K.` — no ORGANIZATION clause, hence record sequential by
§12.4.5.10.3 GR6 — compiled clean and bound a key nothing read. Both rules also carry a SECOND sentence over the
file DESCRIPTION entry (*"The associated file description entry shall not be a sort-merge file description
entry"*), which no per-clause row can reach because the format may be specified by the ORGANIZATION clause alone;
that is the `Entry` role, and it reports under its own code (COBOLNET1900) because its subject is the file
description entry and its remedy is a different edit.

**A rule with two obligations gets a row per arm, sharing one rule-id** (kb/Work PB743). §12.4.5.12.3 SR2 and
§12.4.5.6.3 SR2 each join a CATEGORY and a LOCATION with the word "within"; PB699 screened the location half
alone, so a `PIC 9(5)` prime key built an index on an operand the standard forbids, silently. The category arm
reads `ItemCategory.IsAlphanumericOrNational` — the ONE §8.5.2 category reader, shared with §12.4.5.2 SR7's
ASSIGN … USING screen — which admits an alphanumeric group item (§13.18.29.4 GR3) and a national group and
refuses alphabetic, numeric, edited, boolean and the PICTURE-less usages. The inventory row earns CONFORMS only
when both rows are live, which is why PB699 recorded no verdict for either.

**WHICH FORMAT A CLAUSE SPECIFIES IS ONE TABLE TOO** (kb/Work PB773). §12.4.5.2 SR8, SR9, SR11 and SR13 tie a
format to a file (Format 1 to an indexed file, Format 2 to a relative one, Format 3 to a sequential or report file,
Format 4 to a sort-merge file), and every one of them has to know which format an entry SPECIFIES. The only source
is the four §12.4.5.1 diagrams, and the compiler read them twice — as two hand-written marker lists, one per
format — before the third and fourth rule arrived. `FileControlKeyRules.EntryClauses` is the one list: a row per
clause that is not in all four formats (`EntryClause(Face, Carried, Written)`, `Carried` the SET of `FileFormats`
that print it), and `FileControlKeyRuleDriftTests.EveryEntryClause_IsCarriedByExactlyTheFormatsThatPrintIt`
re-reads the printed diagrams on every run and fails a row whose set disagrees. A clause printed in exactly one
format SPECIFIES it (`ORGANIZATION IS INDEXED`, RECORD KEY, ALTERNATE RECORD KEY, COLLATING SEQUENCE → Format 1;
`RELATIVE`, RELATIVE KEY → Format 2; `LINE SEQUENTIAL`, RECORD DELIMITER → Format 3); a clause several formats
print is not in Format 4 whichever it was meant for (ACCESS MODE, FILE STATUS, LOCK MODE, RESERVE, SHARING).
**Every clause beyond Format 4 on an SD is therefore reported by exactly one row** — SR8, SR9 or SR11 when the
clause names a format, SR13's own row when it names none — and a clause a later edition adds is one `EntryClauses`
row plus one oracle line in the drift test. `ORGANIZATION IS RECORD SEQUENTIAL` and the bare `SEQUENTIAL` are one
phrase (RECORD is an optional word, §12.4.5.10.2), which Format 3 and Format 4 both print, so an SD may write
either. **SR11 is two rows, one per sentence, SR13 one row** — Format 4 is a SUBSET of Format 3, so an entry with
only SELECT, ASSIGN and SEQUENTIAL is a legal Format 3 entry for an FD and no entry can break either SR13 sentence
from the FD side; the obligation SR13 states is the SD's (its entry shall be Format 4), and the drift test pins the
subset relation so that statement is checked rather than asserted. SR11's first sentence is the one row on the
`Entry` role that is NOT about an SD: it is screened over the indexed and relative kinds (a report file is exempt:
Format 3 is stated for it), under its own code, COBOLNET2912, because its subject is the file's organization and
not a key clause (SR8's and SR9's first sentences are COBOLNET0863 rows over the key clause that names them). The
note PB773 was dispatched as *"one table row each"*; measured against the table that was wrong — the table's rows
were about one key operand and these two rules are about the whole clause SET of an entry — so the table gained
the formats column instead.

**Boundaries.** The screen holds only what the standard states about the ENTRY. It carries every syntax rule of the
three key clauses except the two DESCRIPTIVE ones, §12.4.5.12.3 SR5 and §12.4.5.6.3 SR6 (*"Record-key-name-1 has
the class and category of data-name-2"*), which state a property of the SOURCE phrase's operand and have nothing
to screen while that phrase is declined (just below). §12.4.5.12.3 SR3 and §12.4.5.6.3 SR3 (kb/Work PB1073) are
one row each over the operand's own DYNAMIC LENGTH attribute, which is the ELEMENTARY half of §8.5.1.11.1's
*"dynamic-capacity table or … dynamic-length elementary item"* — the table half is a key under OCCURS, which SR1
already reports, and a GROUP that merely contains a dynamic-length item is a variable-length GROUP, which the
standard names separately where it means to forbid one (§12.4.5.8.3 SR3, §13.10.3 SR12) and does not name for a
key. §12.4.5.6.3 SR4 is one row that compares each alternate clause's `RecordLayout.KeyWindowInFile` offset (and
whether a variable-length member precedes it) with the prime key's and with every OTHER alternate clause's; the
operand carries its clause ordinal (`FileKeyOperand.Ordinal`), so two clauses naming one item each violate it. Its
SOURCE-phrase exemption exempts the declined form, so no operand reaches the row through it. §12.4.5.2 SR8's third operand, the file-level COLLATING SEQUENCE clause, is refused where it
is RESOLVED (`DataBinder.ResolveFileCollating`, COBOLNET1582), because the same test is also the guard that stops
the rest of that method and splitting a guard from its report would leave the resolution running on an entry it
has already refused; its citation was §12.4.5.7.1, the clause's descriptive General paragraph, and is now SR8.
The traceability row `SR-12.4.5.2-8` names both sites.

`conformance:negative/pb699-*`, `pb742-*` and `pb743-*` pin each rule on an OPEN/CLOSE-only program at all four
editions; `conformance:85/pb699_legal_key_clauses` and `pb742_legal_entry_formats_85` are the over-rejection twins
(the second adds a plain sequential file with no key clause, and a LEGAL Format-4 sort-merge entry, which the
deleted `if (file.IsSortMerge) return;` used to protect by construction) (a GROUP prime key reached through an `IN`
qualifier, an alternate key `WITH DUPLICATES`, a relative key in WORKING-STORAGE);
`FileControlKeyRuleSpecTests` carries the assertion no `.cob` corpus can make — that the SAME entry, with and
without a keyed verb, draws the SAME diagnostics, exactly once.

**THE SECOND KEY FORM IS DECLINED, AND IT IS DECLINED BY NAME** (kb/Work PB358, 2026-09-09). §12.4.5.12.2 and
§12.4.5.6.2 each print a required choice of two key forms — `{ data-name-1 | record-key-name-1 SOURCE IS
{ data-name-2 } … }` (rendered from printed pages 359 and 350; `RECORD`, `ALTERNATE` and `SOURCE` carry underline
rules, `KEY` and both occurrences of `IS` do not) — and §12.4.5.12.4 GR2 / §12.4.5.6.4 GR2 give the second one
its meaning: *"Record-key-name-1 defines a record key consisting of the concatenation of all occurrences of
data-name-2 in the order specified."* A WiseOwl COBOL record key is **one contiguous byte window** at every layer
that carries it — `FileModel`, `RecordLayout.KeyIndexOfKeyItem`, `RuntimeApi.FileRegisterIndexed`,
`IndexedConnector`'s `(Off, Len)` slice, and `FixedFileAttributes.KeyDescriptor` in D10's store header — so the
concatenated form is **not provided**, which Annex A.3 item 40 expressly permits (*"The capability of specifying
the SOURCE phrase … is dependent on the capabilities of the processor"*; `docs/CONFORMANCE.md` §2 row 40).
⛔ **The decline is a REFUSAL, not the accept-inert warning its A.3 sibling the RECORD DELIMITER clause gets**
(D-item: COBOLNET1778). The axis is not the annex — it is whether an inert reading exists. A declined RECORD
DELIMITER changes no program-visible value (§12.4.5.11.4 GR1); a declined RECORD KEY names no data item at all
(record-key-name-1 is its own name class, §8.3.2.2.24, scoped by §8.4.6.2.4), so an inert compile would leave the
file **keyless** — a wrong answer. §4.2.6 ¶3 licenses refusing outright: *"The implementor is not required to
produce executable code when unsupported processor-dependent language elements are used."*
Both clauses PARSE the phrase (`CobolIO.g4#recordKeySourcePhrase`, one shared rule) so that
`DataBinder.DeclineRecordKeySource` can name it — **ONE diagnostic, COBOLNET1954, for both clauses**, because
A.3 item 40 names both in one sentence and the two general formats print the same brace group. Until this
landed the phrase matched no alternative anywhere and a user saw `COBOL0312: unexpected 'SOURCE' … A period
may be missing at the end of the previous sentence`, so §4.2.6 ¶3's mandatory compile-time indication was
unbuilt here. ⚠ The §12.4.5.1 Format-1 requiredness rule reads `FileKeyOperand.Written`, not a null data-name,
for a measured reason: the first build of the surface told the same program both that the form is not provided
and that its indexed file *"has no RECORD KEY clause"*. **What providing it would cost** is recorded in
kb/Work PB293 and is not proposed here: a key would become a LIST of windows end-to-end, including a second
owner-visible break of D10's store-header format.

**Still homeless, and NOT closed by either half** (measured over the entry-rule family while here):
§12.4.5.5.2 **SR1** (RANDOM banned on a file named in a SORT/MERGE USING or GIVING phrase — a statement-context
rule, so D13 puts it in the SORT/MERGE binder, not here); §12.4.5.2 **SR12** (LINE SEQUENTIAL excludes RESERVE;
kb/Work PB781 — the RESERVE clause is now carried, `FileModel.ReserveAreas`, so it is one clause-pair check beside
the §12.4.5.5.2 SR2 screen). PB742 closed §12.4.5.2 **SR8/SR9**, PB773 **SR11/SR13** and PB743 the CATEGORY
arm of both SR2s; the over-rejection sweep behind them measured **zero** hits across `tests/` (1611 file control
entries — 230 RECORD KEY, 29 ALTERNATE RECORD KEY, 94 RELATIVE KEY clauses, 263 ORGANIZATION INDEXED, and no
numeric indexed key), and the external GnuCOBOL corpus is absent from a worktree and was not swept there. PB699
closed the key rules (§12.4.5.6.3 SR1, §12.4.5.12.3 SR1, §12.4.5.13.3 SR1/SR2/SR3 and the LOCATION arm of both
SR2s) and §12.4.5.2 **SR10**, which PB692 left
here as *"its substance IS enforced by KeyedValidateFile, but cited there as §12.4.5.13, which carries no such
requirement"* — the citation is repaired and the screen is now a row of the table.


### D20. Record-lock governance is a RUN-TIME fact, so the emitter renders ONE entry per record verb — never a choice between a governed and an ungoverned one, and never a second entry for a verb's SHAPE.

**The rule.** ISO §9.1.15: *"The SHARING phrase on an OPEN statement overrides the SHARING clause in the file
control entry for establishing the sharing mode."* Whether a connector is open FOR FILE SHARING — and therefore
whether §9.1.16's record locks bind its READ/WRITE/REWRITE/DELETE — is decided by the OPEN **statement** that
ran, not by anything a compiler can read. §14.9.27.3 SR8 hides how sharp this is: it forces a LOCK MODE clause
whenever the ALL phrase is used, so the `ALL OTHER` spelling always leaves a compile-time trace. The `READ ONLY`
and `NO OTHER` spellings leave none.

**What went wrong.** `SequentialIoEmitter.LockGoverned(file, lockPhrase, retry, ignoringLock)` returned
`file.Sharing != None || file.LockMode is not null || lockPhrase != None || retry is not null || ignoringLock`,
and every record verb — both READ emitters, WRITE, REWRITE, DELETE — chose its runtime entry with it. Every term
is a compile-time property of the SELECT or of the statement. So a connector made a sharing participant by
`OPEN INPUT SHARING WITH READ ONLY F` ran every unphrased verb on the UNGOVERNED entry and read a record another
connector had locked with `'00'`, where §14.9.30.4 GR9/GR10 b) require the record operation conflict status
`'51'`. Adding `RETRY 0 TIMES` — a behavioural no-op by §14.7.9.3 GR4 a) — changed the answer, because the
predicate could see the RETRY phrase and could not see the OPEN (kb/Work PB683).

**The shape.** There is no compile-time governance predicate and there is no ungoverned renderer. `RuntimeApi`
exposes exactly one renderer per record verb — `FileReadShared`/`FileReadSharedOk`, `FileWriteShared`,
`FileRewriteShared`, `FileDeleteShared`, and `FileReadKeyedShared` for the Format-2 random read (kb/Work PB338
collapsed its two halves — an ungoverned `FileReadKeyed` plus a post-read `FileReadLockGovern` patch — into that
ONE governed entry, because §14.9.30.4 GR10 a)/d) require the file position indicator and the key of reference to
be UNCHANGED on a conflict, which a post-read adjustment cannot deliver) — and every emitter renders it
unconditionally. Governance is then decided ONE layer down, in
`FileRegistry`, where the OPEN's own phrase has already been recorded. The cost of the change is one
dictionary probe per record verb on the non-sharing path, replacing a compile-time branch that produced the same
answer only when the SELECT happened to mention sharing.

**⛔ AND ONE LAYER DOWN AGAIN, THE SAME SHAPE WAS STILL THERE** (kb/Work PB669). Each governed body used to
open with a `_connectorShares` probe and **fall through to the identical plain body on a miss**, so a connector
that never opted in never consulted the physical file's lock table at all. `SELECT F-B …` with neither a
SHARING clause nor a LOCK MODE clause could READ, REWRITE and **DELETE** a record another connector held locked,
answering `'00'` every time — and the DELETE removed it. §9.1.16 states the rule over the RECORD and the OTHER
connector, with no qualification on the reading one: *"While locked by a given file connector, a record is not
accessible to another file connector in the same or a different run unit, except by the execution of a READ
statement with the IGNORING LOCK phrase."* §12.4.5.9.4 GR1 a) and b) 1. are worded narrowly and deliberately —
*"no record locks are **set** by the execution of I-O statements through the associated file connector"* — and
GR2 is the clause that disables the CHECK, for a processor that does not support record locking at all.

**The shape.** `FileRegistry.ShareOf` is the ONE reader of the opt-in map: a miss yields
`ImplementorDefaultShare`, the §12.4.5.9.4 GR1 b) 2. posture (`FileLockMode.None` — the *"specify that the
default is no record locking"* option the rule itself offers), and the governed body runs unchanged. Setting is
still gated on `LocksEffective`, so a clause-less connector acquires nothing even from an explicit LOCK phrase;
checking is gated on nothing. **Two questions, two gates, one body** — where before one gate answered both.

**⚠ DETERMINATION, recorded here and in `LocksEffective`'s doc comment.** §14.9.30.4 GR9, §14.9.35.4 GR11 and
§14.9.10.4 GR6 each open *"If record locking is enabled for the … file connector"*, and §14.9.30.4 GR7 sends
that to §12.4.5.9. **Read as a property of the ENVIRONMENT** — the processor supports record locking
(§12.4.5.9.4 GR2) and the physical file is not open in the sharing-with-no-other mode (GR3). *Rejected:* "this
connector's own clause causes it to acquire locks", which would make §9.1.16's unqualified sentence false for
any program that omits LOCK MODE — record locking unenforceable against exactly the programs it constrains —
and would give GR1 a)'s "no record locks are set" a scope its words do not have.

**The hot path pays a `Count` test.** `RecordLocksGovern(meta, st, name)` — *some connector holds a lock on this
physical file, or this one may acquire one* — is asked before any record IDENTITY is computed, so the plain
REWRITE/DELETE path no longer allocates a key or RRN string per statement and the plain READ tail no longer
reads `LastReadRecordId`; `ReleasePriorRecordLocks` returns on an empty table before its LINQ walk. When the
gate is false every governed action below it is provably a no-op, which is what makes skipping them a
refactoring rather than a second posture.

**A verb's SHAPE rides as DATA, for the same reason.** WRITE had three renderers — plain, `WriteAdvancing` and
a COBOL-2023 `WriteBeforeAndAfter` — and only the plain one was governed. §14.9.51.4 GR10 and GR11 are **ALL
FILES** rules, and §14.9.51.2 Format 1 prints the ADVANCING phrase, the `retry-phrase` and the
`WITH LOCK / WITH NO LOCK` bracket together, so `WRITE R AFTER ADVANCING 1 LINE WITH LOCK RETRY 5 TIMES` is one
legal statement — whose lock and RETRY phrases the two print-control entries, having no such parameters, dropped
on the floor. The ADVANCING phrase now travels INTO `WriteShared` as a `WriteAdvance` descriptor
(`WriteAdvanceKind` × lines) and `WriteAnyOrg` dispatches the physical shape inside the governed body.
`SequentialConnector.WriteAdvancing` counts the record it releases (§14.9.51.4 GR12) so GR11 has an identity to
lock, exactly as the plain arm does.

**⛔ THE PRINT-CONTROL SHAPES ARE TWO, NOT THREE, AND THE REASON IS THE PRINTED FORMAT** (kb/Work PB712).
`WriteBeforeAndAfter` — a third entry taking a BEFORE amount AND an AFTER amount — existed because the grammar
spelled §14.9.51.2 Format 1's print-control bracket as the whole phrase REPEATED. It is not: measured off the
PDF (page 815, printed folio 785), the bracket's choice indicators (`|` bars, enclosed by braces) enclose ONLY
the words `BEFORE` and `AFTER`; `ADVANCING` and the operand group sit outside them, ONCE. §5.2.6.4 then reads
"one or more … each at most once … in any order", so the legal spellings are `BEFORE …`, `AFTER …`,
`BEFORE AFTER …` and `AFTER BEFORE …` — always one operand. §14.9.51.4 GR25 a)–d) define ONE advance from that
operand and GR25 e)–h) only PLACE it: e) presents the line first when BEFORE is used, f)'s second sentence puts
the PAIR on e)'s side ("the printed page is advanced … after the line was presented as specified in General rule
25e"), and g)/h) place the PAGE operand "depending on the phrase used" — which the pair leaves unanswered, and
which is why SR17 forbids exactly that combination. **So the binder decides the placement once, from the words,
and the runtime has exactly the two presentation shapes the standard has.** The end-of-page condition the second
`PositionOnLogicalPage` call used to erase (the kb/Work PB686 observation) cannot arise: there is one call.

**Why this is the shape that makes the next case automatic.** Adding a WRITE shape now means adding a
`WriteAdvanceKind` member and one arm to `WriteAnyOrg`'s sequential dispatch inside the governed body (two
presentation shapes today, so that dispatch is a conditional rather than a switch); it cannot produce a new
ungoverned path,
because there is no ungoverned entry to reach. `BoundIoPhraseConsumptionDriftTests` keeps the other half honest:
every phrase a bound I-O node declares must be READ by its emitter.

**The evidence.** `tests/conformance/2023/pb683_open_sharing_read_only` (the `READ ONLY` spelling on all three
organizations, with the `RETRY 0 TIMES` discriminator that used to disagree),
`pb683_open_sharing_no_other` (the `NO OTHER` spelling, both open orders, and the §9.1.15 item 1 `'61'`), and
`pb683_write_advancing_governed` (the same WRITE in all three printed shapes, all `'51'`).
### D21. The sending operand of a `READ … INTO` / `RETURN … INTO` implicit MOVE is THE CURRENT RECORD, and it is built in ONE place for all three arms.

ISO §14.9.30.4 GR4 b) and §14.9.34.4 GR5 b) are the same sentence twice: *"The current record is moved from the
record area to the area specified by identifier-1 according to the rules for the MOVE statement without the
CORRESPONDING phrase. The size of the current record is determined by rules specified in the RECORD clause. If
the file description entry contains a RECORD IS VARYING clause, the implied move is an alphanumeric group
move."* §13.18.43.4 GR16 supplies that size for a FORMAT 2 RECORD clause: *"the number of bytes in the current
record that participate as the sending operands in the implicit MOVE statement"* is a) data-name-1's content when
the DEPENDING ON phrase is written, b) otherwise *"the value that would have been moved into the data item
referenced by data-name-1 had data-name-1 been specified"* — the just-read length GR15 stores. Its closing
sentence makes ZERO a legal count: *"If the number of bytes determined as above is zero, the record is a
zero-length item."*

**The decision:** a variable-length RECORD clause makes the sender a `BoundCurrentRecord` operand — the record
area place, the file, the resolved DEPENDING place, and the GR4 b) group-move designation, all STRUCTURAL — and
ONE method builds one. ⛔ **That method is now `MoveBinder.BindIntoPhrase` and the three INTO arms are the three
BINDERS** (`SequentialIoBinder.BindRead`, `KeyedIoBinder.BindRead`, `SortBinder.BindReturn`) — D23 moved the
implicit move of every INTO phrase to bind time, and an operand is a bind-time object, so the sender came with
it; the invariant is unchanged and only its address is. A FIXED-length
file keeps the plain record-area operand, because §13.18.43.4 GR6 makes its current record the whole area, the
short-final-record '04' case of §14.9.30.4 GR14 included (there the area right of the last valid character is
*undefined*, not short). `OperandText.CurrentRecordImage` renders it once, as
`CobolString.RefMod(<the ONE record-area image channel>, 1, <the GR16 count>, allowZeroLength: true)`.

- **Why the length could not stay implicit.** The old shape sent the whole space-padded area and argued in a
  comment that the two are *observationally identical* — true for a LEFT-justified receiver, since the area's
  padding and the receiver's fill are both spaces on the right. The discriminator is the receiving item's
  JUSTIFIED clause: §13.18.32.4 GR1 truncates the LEFTMOST characters of an over-long sender, so a five-byte
  record read into `PIC X(10) JUSTIFIED RIGHT` arrived as ten spaces (kb/Work PB339).
- **Why the GROUP-MOVE designation lives on the operand, not at the call sites.** GR4 b)'s third sentence makes
  the implied move an alphanumeric group move for a `RECORD IS VARYING` FD *whatever the record area's own level
  structure*, so `MoveClassifier.IsGroupSender` answers true for the designation and falls through to the
  ordinary structural test without it. Before this, a FORMAT 2 FD with an elementary `01` EDITED into an
  alphanumeric-edited receiver and CONVERTED into a numeric-edited one, where §14.9.25.4 GR4 requires the
  receiving area be *"filled without consideration for the individual elementary or group items"*.
- **Why `VaryingRecordInfo` had to learn which RECORD format was written.** GR4 b) conditions the designation on
  the WORD VARYING and GR16 is stated under the FORMAT 2 heading, so a FORMAT 3 `RECORD CONTAINS m TO n` file
  inherits neither (§13.18.43.4 GR18 puts its record size in the record description entry instead). The two
  formats were indistinguishable in the model, so a rule keyed on that word had nothing to key on.
- **The `… FROM` arms are NOT the mirror of this, and deliberately keep the plain SENDER.** §14.9.51.4 GR5,
  §14.9.35.4 GR7 and §14.9.32.4 GR4 each expand the FROM phrase to `MOVE identifier-1 TO record-name-1`
  *"according to the rules specified for the MOVE statement"* — an ordinary MOVE, sender identifier-1 at its own
  length, receiver the record; the byte count actually written is the separate §13.18.43.4 GR13 rule that
  `SequentialIoEmitter.VaryingLengthArg` already renders. ⛔ This is a statement about the SENDING OPERAND only.
  The MOVE itself is bound exactly as an INTO phrase's is (D23): "according to the rules specified for the MOVE
  statement" is the same clause on both sides, and it was applied on neither until PB348.

**Rejected alternatives.** *A reference-modified sender (`area(1:n)`)* — §8.4.3.3.4 GR6 makes a
reference-modified operand an ELEMENTARY alphanumeric item, which is exactly the classification GR4 b)'s third
sentence overrides, so the slice would have bought the length at the cost of the group move. *A sender-length
argument threaded through `MoveEmitter`'s four per-kind renderers* — four places to keep in step for one fact,
where the operand model already has a seam that every renderer reads through.

### D22. An "as if an OPEN statement …" initiation renders THE WHOLE OPEN IT IS AS-IF OF — the SHARING phrase its own general rule names, and the I-O status store that precedes the USE procedure — through ONE emitter helper serving all four such rules.

**The rule.** The standard writes exactly FOUR implicit opens, and every one of them names a phrase:
§14.9.40.4 GR12 a) and §14.9.24.4 GR7 a) (SORT / MERGE `USING`) — *"If the file-control entry for the file has a
SHARING clause with the ALL phrase, the initiation is performed as if an OPEN statement with the INPUT phrase
and the SHARING WITH READ ONLY phrase had been executed; otherwise, the initiation is performed as if an OPEN
statement with the INPUT phrase and without a SHARING phrase is executed"* — and §14.9.40.4 GR15 a) /
§14.9.24.4 GR12 a) (`GIVING`) — *"The initiation is performed as if an OPEN statement with the OUTPUT and
SHARING WITH NO OTHER phrases had been executed"*. §9.1.15 says what the phrases buy: read only *"restricts
concurrent access to a physical file through file connectors other than this one, to input mode"*, no other
*"specifies exclusive access to a physical file"*. Searching the standard for *"as if an OPEN statement"*
returns those four and nothing else, and *"as if a CLOSE statement"* returns their four terminations.

**The decision.** `SortEmitter.EmitImplicitOpen(file, mode, sharing, ruleComment)` is the ONE renderer for all
four, and both directions of both verbs reach it (`EmitInputFile` and `EmitGivingFile` each serve SORT and
MERGE, so the verb axis needs no arm). A non-null sharing mode renders `CobolFile.OpenShared` — the same entry
an explicit `OPEN … SHARING` renders, through the same `OpenCore` and the same Table 19 (**D14**); a null one
renders the plain mode-specific entry, because GR12 a)'s *otherwise* arm is a real distinction and not a
formality: *"The absence of the SHARING phrase means that the sharing mode is completely determined by the
SHARING clause, if any, in the file control entry"*, and the phrase-bearing entry additionally REGISTERS an
unregistered connector's record-locking posture, which for a phrase-less open would contradict §14.9.27.4 GR23
(the two arms **D12** already keeps apart for the explicit statement). The USING arm's condition — *has a
SHARING clause with the ALL phrase* — is a compile-time fact of the file control entry, so it is decided in the
emitter and never re-derived at runtime. The FILE STATUS store rides the same helper: §9.1.13.1 sets the I-O
status *"during the execution of a CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK or WRITE statement and
prior to the execution of … any applicable exception processing statements"* and §12.4.5.8.4 GR1 updates the
FILE STATUS item with it, so the USE procedure these same rules invoke on the next emitted line sees the status
THIS open produced — the `'61'` among them.

**Drift is compile-enforced, not test-enforced.** `RuntimeApi.FileOpenInput` and `FileOpenOutput` — renderers
whose whole defect was the parameter they did not take — are **deleted**, and a ⛔ block stands where they were.
There is no phrase-less implicit-open renderer left for a fifth implicit open to reach by accident; a new one
has exactly one landing place, and it has to say which sharing mode its rule names.

**Rejected alternatives.** *Pass the override through the plain `Open` entry* — the plain entry's whole contract
is that it carries no phrase; adding one there would re-merge the two arms §14.9.27.4 GR23 separates. *Decide
the USING condition in `FileRegistry`* — the condition reads the file control entry, which the emitter holds and
the registry would have to re-derive from a second copy of the same fact. *Render `OpenShared` with
`hasSharingOverride: false` on the otherwise arm* — it would register the connector as a record-locking
participant that the standard never made one. *A drift TEST asserting the old renderers are unused* — a test
that can go stale where a deletion cannot (kb/Work PB714).

**What is NOT observable, and where it is pinned instead.** `SHARING WITH NO OTHER`'s exclusive half is
unreachable from a single-threaded COBOL program: no statement of the program's runs between the implicit OPEN
and the implicit CLOSE, and every Table 19 *Open request* row whose mode is OUTPUT is *Unsuccessful open* in all
five columns anyway (§9.1.13.9 1) e)), so the override cannot change an INCOMING arbitration either. It is
pinned at the registry, on the exact call the emitter renders — `CobolFileLockTests`
`SortGivingImplicitOpen_TakesNoOther_AndLocksOutEveryOtherConnector_GR15a`. The USING override IS observable and
rides three conformance goldens (`pb714_sort_using_sharing_read_only`,
`pb714_merge_using_sharing_read_only`, `pb714_sort_giving_open_status`).

**The same holds for every as-if statement AFTER the open: one status, one hook, per statement (kb/Work
PB837).** GR12's closing paragraph — *"These implicit functions are performed such that any applicable USE
procedures are executed"* — covers the as-if READ (GR12 b) / GR7 b)), the as-if WRITE (GR15 b) / MERGE GR12 b))
and the as-if CLOSE (GR12 c) / GR15 c)) exactly as it covers the OPEN, and §14.9.49.4 GR6 runs the procedure
*"upon the unsuccessful execution of an input-output operation unless an AT END or INVALID KEY phrase takes
precedence"*. So `EmitInputFile` stores and hooks the retrieval that ended its loop (with `atEndHandled`, the
as-if READ's own AT END phrase) BEFORE the CLOSE, and the CLOSE gets its own hook; `EmitGivingFile` hooks each
unsuccessful WRITE inside its loop and ends the file's processing on the boundary condition alone
(`IoStatusClass.WriteBoundary` — '34' §14.9.51.4 GR20, '24' GR33 b)), which is the one write failure the GR15 /
MERGE GR12 paragraph names (*"On the first attempt to write outside the externally defined boundaries of the
file … the processing of the file is terminated"*). One hook after the CLOSE, the former shape, read the
CLOSE's status for all three and ran no declarative for a failed retrieval whose CLOSE succeeded. Witness:
`2023/pb837_sort_implicit_io_status` ('61' → '47' for SORT and MERGE USING, where the fatal '47' ends the
statement; one '71' for a LINE SEQUENTIAL GIVING file, which ends the SORT too).

**Each hook is followed by its DISPOSITION — one table, `SortEmitter.RuleFor` (kb/Work PB993).** A hook alone
only runs the procedure; what the status then does to the statement is the verb's rule, and before PB993 no site
had one — a fatal GIVING OPEN ('30') ran the declarative, then again for every record's WRITE ('48') against the
unopened file, then for the CLOSE ('42'), and the SORT "completed". Now every as-if statement goes
`EmitTransferHook` → the hook (`EmitTransferUse`) → `EmitDisposition`, which renders the table's cell for a fatal
status (`IoStatusClass.Fatal`, §9.1.13.1's class) and for any other unsuccessful one as `goto` the statement's end
label (TERMINATE), `goto` the GIVING file's bypass label after its CLOSE (BYPASS, MERGE GR12 a) only), or nothing
(CONTINUE). The default cell is §9.1.13.1's — a fatal status transfers "to the end of the statement that produced
the fatal exception condition", the SORT/MERGE — and the MERGE cells that turn on "an applicable USE procedure that
completes normally" (GR7 a), GR12 a), GR12 b)) read that fact from the hook itself: `__IoCheck` / `__IoCheckEc`
answer -1 when a procedure ran and completed normally and -3 when none applied (the EC-free `__IoCheck` answers a
bool), and the transfer passes `__verbRule` so a fatal status under EC-I-O checking is the verb's to dispose of
(§14.6.13.1.3 2) precedes 5)/7)). A terminated statement performs none of its remaining implicit functions, so a
GIVING file whose WRITE terminated it is still open afterwards (determination in `docs/CONFORMANCE.md` §3).
`SortTransferRuleDriftTests` pins every cell and that the hook is reached from ONE place in the emitter. Witness:
`2002/pb993_sort_merge_transfer_termination` (nine legs, one per cell family).

**Before the table, the question every explicit I-O statement asks (kb/Work PB2700; DOC-A.1-103).** The table is what
a CONTINUED run unit does with the SORT/MERGE; whether the run unit continues is §9.1.13.1's implementor choice, and
the as-if statement is performed "as if" the statement "had been executed", so it gets the explicit statement's
answer: a fatal status on a file with no FILE STATUS clause, no USE procedure applied and no checking enabled for its
condition ends the run unit. `SequentialIoEmitter.EmitUseHook` computes that condition for every hook; an explicit
statement renders it at once, while a SORT/MERGE hook (`verbDisposes`) returns it as `UseHookResult.Uncovered` and
`SortEmitter.EmitDisposition` renders it through the same `SequentialIoEmitter.EmitUncoveredFatalTermination`,
first, ahead of the table's cell. It sits in the disposition rather than the hook because the GIVING write's
boundary paragraph — "if no such USE procedure is specified, the processing of the file is terminated" — is the one
rule that continues past a status nothing covers, and its test runs between the hook and the disposition. Under
EC-I-O checking the `__IoCheckEc` hook's `__verbRule` keeps the enabled-and-unhandled case the verb's
(§14.6.13.1.3 2)), so the returned condition also requires the raised condition's mask bit to be clear. Before
PB2700 the hook suppressed the question outright for every as-if statement, so `SORT … USING` a missing file
skipped its output procedure and the program carried on. Witnesses: `DocA1Item103WitnessTests` (SORT USING, MERGE
USING, SORT GIVING; the checked control) and `85/pb2700_sort_merge_missing_using_covered` (the covered arms).

**A SUCCESSFUL as-if READ or WRITE is a READ or WRITE too: it stores its status and offers EC-I-O-WARNING (kb/Work
PB749).** §9.1.13.1 sets the I-O status *"during the execution of a CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK
or WRITE statement"* and says *"Any I-O status associated with an unsuccessful completion or a nonzero successful
completion is associated with an exception condition"*; §12.4.5.8.4 GR1 puts the status in the FILE STATUS item. The
contract is therefore the same for all four as-if statements, OPEN, READ, WRITE and CLOSE, and it is the contract of
the explicit verbs: the status is stored into the FILE STATUS item after the statement whatever it was, and a hook
runs when something can observe it. `SortEmitter.EmitSuccessfulTransfer` / `EmitSuccessfulTransferHook` are the two
halves for the retrieval and the write: the store is emitted per record only for a file with a FILE STATUS clause
(`EmitStoreFileStatus` emits nothing without one), and the hook only where an EC-I-O name is enabled for the file
(`EcEmitter.IoMaskFor`, the predicate `EmitUseHook`'s own `successArm` asks, so the guard and the hook cannot
disagree), behind one `IoStatusClass.Warning` test, so a clean transfer of a file with no EC-I-O checking pays one
status assignment per record at most and no hook call. The plain `USE AFTER EXCEPTION` declarative still runs only
on an unsuccessful status (§14.9.49.4 GR6). Witness: `2023/pb749_sort_implicit_statement_status` (a LINE SEQUENTIAL
USING file whose as-if READ is '06': the declarative runs during the SORT, once, and sees `06` in the FILE STATUS
item). The GIVING WRITE arm emits the same two halves but no runtime in this compiler produces a nonzero successful
WRITE status for a sequential file; the keyed '02' case waits on kb/Work PB994 (keyed USING/GIVING is refused today).

**A short record's fill is decided by the file it moves to, once, at bind (kb/Work PB1140).** §14.9.40.4 GR7 (USING)
and GR16 (GIVING), and their MERGE twins §14.9.24.4 GR2 and GR13, space-fill a record with fewer character positions than the fixed
length of the receiving file: with the NATIONAL space when there is one record description and it is a national data
item or an elementary item of usage national and category numeric, numeric-edited or boolean (a), and with the
alphanumeric space otherwise (c); arm b) needs SELECT WHEN, which Annex A.4.8 declines. `FileModel.ShortRecordFillNational`
is that one test, over the file's own record descriptions. It is NOT the connector's `NationalRecordArea`, which is
§14.9.30.4 GR15's per-record-area test for the READ's own fill (any national record description of the FD, so a
`PIC 9(3) USAGE NATIONAL` record got the alphanumeric space and a two-record FD with one national record the
national one). `RecordFill.Fit` is the one right-fill both use; the USING release calls `CobolSort.FillTo` with the
USING file's answer, and the GIVING loop calls it before the governed write with the GIVING file's, unless the file's
record size varies (nothing fixed to fill to), it is LINE SEQUENTIAL (§14.9.51.4 GR21 strips trailing spaces on write,
so the connector's own fit-and-trim owns its shape), or the returned record carries an extent table (a
variable-length group, whose D-FRA (vi) fixed form pads each member itself). The printed SORT GR16 a) / b) say
file-name-2 where the rule's lead-in names the GIVING file (file-name-3; the MERGE twin names file-name-4): a slip of
the standard, read against the GIVING file. Witness: `2002/pb1140_sort_merge_short_record_fill`.

### D23. The implicit MOVE of a `… FROM` / `… INTO` phrase is BOUND by the MOVE binder, as a sub-statement of the I-O node — never synthesized in the emitter.

A bound node built AFTER binding is never checked by a bind pass. Every `… FROM` phrase (RELEASE §14.9.32,
WRITE §14.9.51, REWRITE §14.9.35 — sequential and keyed alike) and every `… INTO` phrase (READ §14.9.30,
RETURN §14.9.34) used to construct its implicit `BoundMove` in the EMITTER —
`move.Emit(new BoundMove(from, [rl.Record]))` — downstream of every bind-time MOVE screen AND of the storage
facts the emitter's own output consumes. §14.9.32.4 GR4 makes `RELEASE record-name-1 FROM x` **exactly**
`MOVE x TO record-name-1` followed by the same RELEASE without FROM (§14.9.51.4 GR5 a) and §14.9.35.4 GR7 a) say
it for WRITE and REWRITE; §14.9.30.4 GR4 b) and §14.9.34.4 GR5 b) are the same sentence for INTO), so *exactly*
has to include the bind. Measured, before the fix: `RELEASE SRT-REC FROM WS-NUM` (a `PIC 9(3)` sender into a
`PIC A(8)` record) compiled clean where the identical explicit MOVE drew COBOLNET0819; and
`RELEASE SRT-NUM FROM QUOTE` **aborted the run** with an unhandled `NotImplementedCobolFeatureException` before
the record was released, because `MarkImageForced` — a fact `StorageFormPass` consumes — is collected by the
binder and the emitter-built move never passed through it (kb/Work PB348).

**The decision:** `MoveBinder.BindMoveOf(source, targets, implicitOf)` is THE one application of the MOVE
statement's own rules to a sender/receiver pair. The explicit MOVE statement calls it; so do
`MoveBinder.BindFromPhrase` and `MoveBinder.BindIntoPhrase`, which every I-O binder uses. The resulting
`BoundMove` rides its I-O node as a sub-statement — `BoundWrite.FromMove`, `BoundRewrite.FromMove`,
`BoundRead.IntoMove`, `BoundKeyedWrite.FromMove`, `BoundKeyedRewrite.FromMove`, `BoundKeyedRead.IntoMove`,
`BoundRelease.FromMove`, `BoundReturn.IntoMove` — and the emitter *renders* it, deciding nothing.
`BoundMove.ImplicitOf` carries the phrase, so a screen written for the explicit statement reports the statement
the programmer actually wrote (`the implicit MOVE of RELEASE … FROM to SRT-REC (ISO §14.9.32.4 GR4 a);
§14.9.32.3 SR3)`) without an arm of its own.

- **The per-verb phrase rules are a ROW WITH A SET, not a flag.** Each verb states its own rules for the phrase
  and the sets genuinely DIFFER: §14.9.32.3 SR2 (RELEASE) and §14.9.51.3 SR4 (WRITE) admit *"an alphanumeric or
  national function"*, §14.9.35.3 SR9 (REWRITE, the FILE phrase not specified) admits *"an alphanumeric, boolean,
  or national function"*, and only §14.9.32.3 SR4 forbids a zero-length literal-1. `FromPhraseRules` is one
  record per verb carrying the admitted CATEGORIES and the nullable zero-length clause; a scalar "must be
  alphanumeric" column would have REJECTED LEGAL SOURCE on the REWRITE arm. The FILE-phrase readings
  (§14.9.51.3 SR10, §14.9.35.3 SR8) are unreachable — the FILE arm is Annex A.4.13 item 2, declined
  COBOLNET1706 earlier. Diagnostics: COBOLNET1914, COBOLNET1915.
- **The phrase is ONE grammar rule, and its edition edge is ONE gate** (kb/Work PB2650). §14.9.51.2, §14.9.35.2
  and §14.9.32.2 print the same `FROM { identifier-1 | literal-1 }`, so the three statements share
  `recordFromPhrase : FROM sendingOperand`, and `VersionConformancePass.ParseArm.VisitRecordFromPhrase` refuses
  literal-1 (a figurative constant included) below COBOL-2002 (construct `from-literal-2002`, COBOLNET0871;
  VERSION_CHANGE_REFERENCE row 7.34 derives the 1985 edge). It used to be three copies of the rule with the gate
  on RELEASE's alone, so WRITE and REWRITE took a literal at `--std 85`. The per-verb rules above stay rows of
  `FromPhraseRules`; the edition edge is the same for all three, so it is not a column.
- **The function's class is read off the BOUND operand, through the Table-16 sender position.**
  `MoveBinder.SenderPosition` is a named reader rather than an inline switch precisely because the phrase rules
  ask the same question of the same operand: `FUNCTION LENGTH` constant-folds to a plain numeric literal, and a
  second reading of "what category is this sender" would have answered *unknown* and waved it through.
- **`SortBinder` no longer takes a `SequentialIoBinder`.** That constructor edge existed only for the former
  `SequentialIoBinder.WriteSource` — a three-way operand hand-off shared by WRITE, REWRITE and RELEASE that
  INSPECTED NOTHING, so each verb's own phrase rules were applied by no one. The sort verbs and the sequential
  I-O verbs are independent collaborators again.
- **The non-phrase implicit moves are bound too (kb/Work PB880).** INITIALIZE's §14.9.20.4 GR4 stores
  (`InitializeStore.Move`), MOVE CORRESPONDING's §14.9.25.4 GR11 per-pair moves (`CorrespondingPair.Move`) and
  GOBACK RETURNING's move into the procedure-division RETURNING item (`BoundGoback.ReturningMove`) are built by
  `BindMoveOf` with a static `ImplicitMovePhrase` (`Initialize` · `MoveCorresponding` · `GobackReturning`). What
  an emitter-built `new BoundMove(...)` skipped, and why each mattered: **(1)** the §14.9.25.3 syntax screens
  (SR1 class, SR2/SR6–SR10 through `MoveTable16.Validity`); **(2)** the §14.9.25.4 GR1 sender freeze
  (`SendingValueTemp.Materialize`); **(3)** `MarkFillImageStorage` — the `StoreAsImage` fact `StorageFormPass`
  consumes for a figurative / GR2-substituted fill into a numeric-DISPLAY receiver (INITIALIZE REPLACING NUMERIC BY
  SPACE, a zero-length dynamic-length CORRESPONDING sender and the same GOBACK RETURNING sender each aborted the run
  unit without it); **(4)** `MarkRefModStoreImage`; **(5)** visibility to the post-bind passes that walk
  `StatementChildren` (the CORRESPONDING and GOBACK moves are child statements). INITIALIZE's phrase carries
  `ValidityAskedByStatement`: §14.9.20.3 SR4 asks the validity question once per REPLACING CATEGORY
  (`InitializeBinder.CheckReplacingMoveValidity`) and the GR6a/GR6c senders are valid by construction, so the
  per-receiver screens stay silent while (2)–(4) still run. Its edition half, §14.9.25.3 SR5, is asked by
  `VersionConformancePass.GateInitialize` over `BoundInitialize.Replacing` through the same `GateSr5` rows
  `GateMove` asks (kb/Work PB879).
- **A statement whose rule says "according to the rules for the MOVE statement" binds a MOVE, even when its
  sender is conceptual (kb/Work PB887).** ACCEPT format 2 (§14.9.1.4 GR6) transfers a clock reading that GR7–GR12
  describe as "an unsigned elementary integer data item of usage display" of 6/8/5/7/8/1 digits
  (`AcceptKinds.ConceptualDigits`). `AcceptDisplayBinder` materializes exactly that item as a compiler temp
  (`BoundAccept.Conceptual`), asks `MoveTable16.Validity` with it (framed COBOLNET0818 under §14.9.1.3 SR3, so the
  phrase `ImplicitMovePhrase.AcceptTemporal` carries `ValidityAskedByStatement`), and binds the transfer with
  `BindMoveOf` onto `BoundAccept.Store`; the emitter fills the temp from the clock and renders the store with
  `MoveEmitter`. The emitter's former per-category arms were a second copy of `MoveEmitter.ConvertSource` whose
  float arm went missing; `StaleDeferralDriftTests.AcceptTemporal_RoutesThroughTheOneMoveSeam` forbids the
  MOVE-store primitives there. ACCEPT format 1 (§14.9.1.4 GR1–GR4) is explicitly NOT a MOVE and keeps its own
  device conversion, documented in `CONFORMANCE.md` §7 DOC-A.1-1 / DOC-A.1-5.
- **The drift test is on the SHAPE.** `ImplicitMoveConstructionDriftTests` asserts no `new BoundMove` survives in
  the three phrase emitters, and that NO construction survives anywhere in `CodeGen` (it used to enumerate the
  three non-phrase moves as "known", which held the hole open rather than closing it). Adding a statement whose
  implicit move is synthesized at emission fails there rather than silently in a program nobody has written yet.
- **The validity question has ONE entry (kb/Work PB878).** `MoveTable16.Validity` (a bound operand against a
  place, or against a Table-16 position for §14.9.20.3 SR4's "item of the specified category") and
  `MoveTable16.DataItemRefusal` (§14.7.6 rule 2's two data items) run one chain in SR order — SR2, SR6/SR7/SR8,
  SR9, SR10 — and return the refusing RULE with its reason; the per-rule readers are private.
  `MoveBinder` frames each rule under its own code (COBOLNET1533 · COBOLNET1931 · COBOLNET0819 with SR10's
  `--permissive` re-reading); INITIALIZE (COBOLNET2031), INVOKE/CALL BY CONTENT (§14.8.2.3.3 rule 2d,
  COBOLNET0828) and CORRESPONDING (a silent non-pairing) take the reason. `MoveTable16AskerDriftTests` pins the
  private readers and the three Table-16-alone askers whose sender has no data item.

**Rejected alternatives.** *Fix the RELEASE call site* — the smallest diff and the wrong shape: eight call sites
carry the same rule, and the next FROM phrase would re-derive it a ninth time. *Re-run the MOVE screens over the
emitter-built node* — the screens COLLECT (`MarkImageForced`, `MarkRefModStoreImage`) as well as diagnose, and
`StorageFormPass` has already run by then; a screen that runs after its consumer is not a screen. *Give each verb
its own copy of its phrase rules at its binder* — three copies of one rule, which is what `WriteSource` was, and
the reason none of them existed.

### D24. An `… INTO` phrase's RECEIVER ADMISSIBILITY is ONE screen inside `MoveBinder.BindIntoPhrase`, driven by a per-verb rules row — never two verb-local `if`s.

READ and RETURN each state, for themselves, when the INTO phrase may be written at all. §14.9.30.3 SR1 admits it
*"a) If no record description entry or only one record description is subordinate to the file description entry,
or b) If the data item referenced by identifier-1 and all record-names associated with file-name-1 describe an
alphanumeric group item or an elementary item of category alphanumeric or category national"*; §14.9.34.3 SR2 is
the same rule for RETURN with arm a) reading *"If only one record description is subordinate to the sort-merge
file description entry"*. §14.9.30.3 SR2 adds, for a strongly-typed identifier-1, *"there shall be at most one
record area subordinate to the FD for file-name-1. This record area, if specified, shall be a strongly-typed group
item of the same type as identifier-1"*, and §14.9.34.3 SR3 is its twin with *"exactly one record area subordinate
to the SD"*.

**Four syntax rules, two verbs, and before kb/Work PB337 none of them had an implementation.** Both READ binders
and `SortBinder.BindReturn` resolved the INTO operand and inspected nothing; no caller screened it. Measured on
the pre-fix tree: an FD with `01 REC-A / 05 A-1 PIC X(4)` and `01 REC-B / 05 B-1 PIC 9(4) COMP` read `INTO` a
`PIC 9(4) COMP` item compiled and printed `GOT=0000`, and the RETURN twin over an SD holding an alphanumeric
group and an elementary `PIC 9(6)` ran clean. The one descriptor that would have said so — `CBL1704` — is
declared in `Cobol.Net.Frontend` and reported only from the FROZEN legacy engine's `BoundTreeValidator`.

**The decision:** `StatementValidation.CheckIntoReceiver(file, receiver, rules)` is the one screen, and it is
called from **`MoveBinder.BindIntoPhrase`** — the single place D23 already routes all three INTO arms through —
rather than from the three binders. That is the whole architectural point: a verb added tomorrow with an INTO
phrase gets the rule by calling the phrase binder, which it must do anyway to get its implicit move. `IntoPhraseRules`
(beside `ImplicitMovePhrase`, and the INTO twin of `FromPhraseRules`) carries each verb's wording and absorbs the
former `ImplicitMovePhrase.ReadInto` / `ReturnInto` statics as its `Phrase` property — one object per INTO verb,
not two. Diagnostics: COBOLNET1994 (admissibility), COBOLNET1995 (the strong-receiver count).

- **The two verbs differ in ONE fact, and it is a property of the DESCRIPTION ENTRY, not of the rule.** Both of
  READ's rules admit a record-less entry (*"no record description entry or only one"*, *"at most one"*) and both
  of RETURN's require a record (*"only one"*, *"exactly one"*), because §13.4.5.3 SR3 permits an FD with no record
  description entries — the permission D18's `MaterializeImpliedRecord` implements — while §13.4.6.3 SR2 requires
  them under an SD. So `AdmitsRecordLessEntry` is one field read by both rules, and `AdmitsRecordCount` is one
  predicate. Two booleans that can never disagree would have been two chances to make them.
- **The SAME-TYPE half of SR2/SR3 is deliberately NOT checked here.** Each rule's second sentence — *"shall be a
  strongly-typed group item of the same type as identifier-1"* — is the same predicate over the same pair that
  §14.9.25.3 SR2 applies to this move's SENDER, which *is* the record area; `CheckStrongMove` reports it as
  COBOLNET1533 naming the phrase (D23), and a second copy here would be one rule in two places. What that screen
  **cannot** express is the COUNT, because it inspects only `FileModel.AreaRecord`: a file whose LARGEST record
  happened to be a strongly-typed group of the right type passed everything, with a second record area no
  diagnostic could see. The count is therefore exactly what COBOLNET1995 owns — the division is by what each
  screen can decide, not by which rule number it belongs to.
- **A strongly-typed receiver draws ONE diagnostic, not two, and that is derived.** §13.18.29.4 GR3 excludes a
  strongly-typed group from the alphanumeric group items, so arm b) of the admissibility rule can never admit
  one — leaving arm a), which is the same count SR2/SR3 tests. For that receiver the two rules impose the same
  condition, so the specific one speaks and the general one is silent.
- **The root cause was one rule written down twice, and the fix was to write it once.** §13.18.29.4 GR3 — what an
  *alphanumeric group item* IS — lived in `ItemCategory.Admits` as `item.IsGroup` (the structural conjunct alone)
  and, separately and completely, in a private `DataBinder.GroupValue.IsAlphanumericGroup`. The incomplete copy is
  the one three existing rules read, so §12.4.5.2 SR7 (`ASSIGN … USING`) and the record keys of §12.4.5.12.3 SR2 /
  §12.4.5.6.3 SR2 each admitted a strongly-typed or variable-length group. `ItemCategory.IsAlphanumericGroup` is
  now the one spelling, GR3's three conjuncts, and `ItemCategory.Face` names the two exclusions out loud — it used
  to answer *"not an elementary or group data item"* for every plain group, false but unreachable while no group
  could fail the predicate. The sweep is witnessed by `conformance:negative/pb337-assign-using-strong-group`.
  `ItemCategory` has since become the ONE §8.5.2 CLASS reader as well as the category one — the index /
  message-tag / object / pointer population that three rules name with identical words lives there too
  (`IsIndexMessageTagObjectOrPointer`, kb/Work PB391); see `docs/COBOLNET_DATA_MODEL_DESIGN.md`.
- **The class carries the USAGE half of the same question too — `ItemCategory.UsageOf`** (kb/Work PB411). §8.5.2.1
  states a group's usage in one sentence — *"An alphanumeric group item is treated as though it had a usage of
  display"* — and every rule worded *"a data item with usage display or usage national"* (§14.9.18.3 SR6 /
  §14.9.42.3 SR2's termination-status operand; §8.4.3.3.4 GR6's unique data item; §14.9.41.3 SR6 b) 2.'s key
  comparison) asks it. The reader is `OperandPic?.Usage ?? (IsAlphanumericGroup(item) ? Usage.Display : null)`, so
  it rides the SAME §3.11 definition-by-exclusion as the category half and a strongly-typed or variable-length
  group — for which the standard states no usage at all — answers `null` rather than being given an invented one.
  Every site that instead asked `DataItem.OperandPic` alone read its null for an alphanumeric group as *"no usage"*
  and rejected legal source: measured at the status operand of both STOP RUN and GOBACK, and as a wrong usage on
  a reference-modified national group's GR6 intermediate. ⚠ `IntrinsicArgumentRules.StaticUsageOf` deliberately
  answers differently and says so in its own comment: it asks whether an argument has a statically fixed
  REPRESENTATION for the §15.19.3 keyword screens, which a group's leaves-only storage does not.
- **No edition gate, and the question was asked rather than skipped.** The admissibility rule is an all-editions
  rule. The two shapes that make the 2023 wording narrower than its '85 ancestor — *alphanumeric* group item
  rather than any group item, and category *national* — cannot be DECLARED below the edition that introduces them
  (TYPE/TYPEDEF and USAGE NATIONAL at COBOL-2002, DYNAMIC LENGTH at COBOL-2014), so one predicate is
  per-edition-correct by construction rather than by a predicate that could drift. §13.4.5.3 SR3's record-less FD
  is likewise unobservable, because D18 materializes the implied record area at every edition. The positive golden
  `pb337_into_receiver_admissible` ships in all four edition legs; the strong-receiver negatives list 2002 and up,
  the admissibility negatives all four.
- **The over-reject guard is the point of the positive golden.** Arm a) admits a receiver arm b) would refuse — a
  `PIC 9(4)` identifier-1 over a single-record FD is legal — so a screen that tested only the category set would
  reject conforming source. Case A of the golden is exactly that program.

**Rejected alternatives.** *Screen at the three binders* — the note's own first sketch, and the shape D23 had
already made obsolete: three copies, and the fourth INTO verb inherits none of them. *Fold the count test into
`CheckStrongMove`* — that screen is about a MOVE's sender/receiver pair and knows nothing of a file's record
list; giving it a `FileModel` would make the MOVE rule depend on the I-O model to serve one caller. *Re-derive
"alphanumeric group item" at the INTO screen* — a third copy of §13.18.29.4 GR3, when two had already drifted.
### D25. The FILE DESCRIPTION entry's own syntax rules are enforced ON THE ENTRY too, and the RECORD clause's are a SECOND TABLE beside the file-control one — `RecordClauseRules`, run from the same `DataBinder.ResolveFiles` pass. D19's criterion generalizes; the FD is simply the other entry.

**The rules.** ISO §13.18.43.3 states nine syntax rules for the RECORD clause, cut on the three general formats of
§13.18.43.2. Four of them bound the clause's integers against each other or against the file's record description
entries: **SR3** (Format 1) *"No record description entry for the file may specify a number of bytes greater than
integer-1"*; **SR4** (Format 2) *"Record descriptions for the file shall describe neither records that contain a
lesser number of bytes than that specified by integer-2 nor records that contain a greater number of bytes than
that specified by integer-3"*; **SR5** (Format 2) *"Integer-3 shall be greater than integer-2"*; **SR9**
(Format 3) *"Integer-5 shall be greater than integer-4"*.

**What was wrong.** `DataBinder.BindRecordClause` parsed the integers and stored them, and nothing checked any of
it. `FD F RECORD IS VARYING IN SIZE FROM 20 TO 5 DEPENDING ON L.` — an SR5 violation written in two adjacent
literals — compiled clean at 85, 2002, 2014 and 2023, and the program learned about it only as an I-O status
`'44'` on the first WRITE, which is §13.18.43.4 GR14 a)'s runtime CONSEQUENCE and not the clause's diagnosis. An
inverted range makes EVERY write unsuccessful, so the runtime report is also unusably late (kb/Work PB721).

**Where it lives, and why not at the clause.** `src/Cobol.Net.Compiler/Binding/RecordClauseRules.cs`, called from
`DataBinder.ResolveFiles` immediately after `FileControlKeyRules.Screen`. D19's two-site criterion decides it:
SR3 and SR4 need a RESOLVED quantity — §13.18.43.4 GR8's byte count of a record description, whose summation
reads occurrence counts, redefinition targets, group usage and the §8.5.1.6.3 bit layout — and none of that is
settled while the file description entry is being read. SR5 and SR9 are decidable from the clause alone, but they
travel with their formats' other rules in one table rather than being split across two sites for the sake of a
few nanoseconds. The clause's source position is captured at bind time on `FileModel.RecordClauseAt` — the
`RecordKeyAt` pattern — so a post-build report still points at the clause.

**The quantities are the standard's own, each is written down once, and every one is in BYTES.** GR3 sizes a
record by *"the number of bytes required to store the logical record, regardless of the types of characters
used"*, so the unit is `DataItem.ByteWidth` (a national position is two bytes, D-N1) and never the carrier's
character positions (`ImageWidth`), which counted a national record at half its size (kb/Work PB1277).
`FileModel.MaxRecordSize` is GR8 b) and delegates to `ByteWidth` rather than re-summing: `ByteWidth` already skips
redefining children (GR8's *"excluding redefinitions and renamings"*), already takes an ODO table's maximum
occurrences, and already lays a bit-bearing subtree out by the §8.5.1.6.3 walk (GR4's *"the entire byte in which
that data item ends"*). `FileModel.MinRecordSize` is GR8 a): the same `ByteWidth` wherever no table varies beneath
an item, and otherwise each occurs-depending table at `DataItem.MinimumOccurrences` (its integer-1; a record holds
no dynamic-capacity table — §8.5.1.9.1 3), COBOLNET1526), a bit-bearing ODO subtree placed by the bit walk. A
parallel leaf re-summation would be the same rule twice — and it was: the one this replaced rounded each bit leaf to
a byte of its own, so two `PIC 1(3) USAGE BIT` items had minimum 2 over maximum 1.

**The file is sized by the same model (kb/Work PB1276).** `FileModel.RecordWidth` — the record AREA, §13.18.43.4
GR2, which every connector registration, the SORT/MERGE record length and the fixed-file attributes read — is the
explicit Format 1 integer-1 (GR6: *"Integer-1 specifies the number of bytes contained in each record in the
file"*; SR3 lets every description be smaller) or, with none, the descriptions' physical width (GR5 a)'s implied
integer-1). Positions of an integer-1 record that no record description covers are written as spaces (the
national space in a national record area) — `FileConnector.FitRecord`. `FileModel.VaryMin` / `VaryMax` are GR9 /
GR10 with ONE formula for the explicit clause that omits an integer and the implied Format 2 clause alike: the
least / greatest `MinRecordSize` / `MaxRecordSize` over the records.

**The format is a COLUMN, and `FileModel.RecordClause` is its one reader.** That property projects the existing
`Varying` / `RecordContains` pair into a `RecordClauseFacts` — format, lower operand, upper operand, position —
so no site re-derives "which general format is this". It is a projection and never stored: Format 1 is
`Varying is null` with a stated `RecordContains`, Format 2 is `Varying.VaryingClause`, Format 3 is the remaining
variable-length shape, and NO clause at all answers null, which is what keeps the screen silent where
§13.18.43.4 GR5 implies a clause from the record descriptions (an implied clause cannot contradict what it was
derived from).

**Two codes, because the subjects differ.** **COBOLNET2009** for the clause's own integer pair (SR5/SR9 — the
remedy is editing the clause); **COBOLNET2010** for a record description outside it (SR3/SR4 — the remedy may be
at either end). SR4 is ONE printed sentence with TWO obligations and therefore TWO rows sharing one rule-id, with
a negative golden per arm: the PB743 clamp applied before the defect rather than after it.

**Format 3 gets no SR4-analogue, deliberately.** §13.18.43.4 GR18 says that for that format *"the size of each
record is completely defined in the record description entry"*, so the standard states no rule bounding the
descriptions by integer-4 and integer-5. Inventing one would reject legal source, and
`conformance:negative/pb721-record-contains-inverted-range` asserts the record draws nothing while the clause is
reported.

**SR7 and SR8 need no row, and the absence is DERIVED.** §5.5 1) — *"When the term 'integer-n' (n = 1, 2, …) is
used in a general format and associated rules, it refers to a fixed-point integer literal that shall be unsigned
and nonzero unless otherwise specified in the associated rules"* — makes every `integer-n` unsigned, and the
general format spells integer-2 and integer-4 with the grammar's unsigned `integerLiteral`, so a negative
operand cannot be written at all. SR7 and SR8 (*"shall be greater than or equal to zero"*) are precisely the
*"unless otherwise specified"* override, and they lift the NONZERO half only: what they require of this compiler
is that `FROM 0` be ACCEPTED. That is a positive golden, not a screen — and a table row asserting them would be a
dead lookup. `RecordClauseRuleDriftTests.TheReasonSr7AndSr8NeedNoRow_IsStillWhatTheStandardPrints` fails if that
sentence ever changes.

**SR2 is discharged by the GRAMMAR, not by a screen — and its spelling is the subclause's ONE edition gate.**
*"The words BYTES and CHARACTERS are synonymous and may be used interchangeably"*, and all three printed
general formats carry the same `{ BYTES | CHARACTERS }` brace group, so `recordClause` writes
`(CHARACTERS | BYTES)` in both of its alternatives. BYTES had no lexer token at all and
`RECORD CONTAINS 20 BYTES` was refused COBOLNET1970 as an unrecognized FD clause — legal COBOL-2023,
rejected.

⛔ **BUT THE SPELLING IS A 2023 ADDITION, AND THAT WAS DERIVED RATHER THAN ASSUMED.** Annex E.3.3 item 13
lists BYTES among the words that *"have either been added to the list of context-sensitive words or the
context in which they are reserved has been expanded"*. That sentence is an OR, so the word alone does not
settle it — but §8.10 gives BYTES exactly ONE construct, the *"RECORD clause"*, so there is no earlier
context for 2023 to have EXPANDED and the first arm is the one that applies. `VisitRecordClause` in the
version-conformance pass therefore refuses the word below 2023 (**COBOLNET0900**, constructs.json row
`record-clause-bytes-2023`), on the CONTINUE AFTER … SECONDS precedent — SECONDS is in the same item-13 list.
The gate is a PARSE ARM and not a `{is2023()}?` predicate because the word sits mid-alternative, where a
semantic predicate does not steer prediction and would throw a token error instead of naming the edition.

⚖ **Only the word is gated, on both counts.** CHARACTERS is 1985-continuous and ungated, so the old spelling
of the same clause is untouched everywhere; and BYTES stays a `nameSlot` row in
`tests/version-matrix/cobol-words.json`, so `01 BYTES PIC X.` is still legal COBOL-85 — §8.10's own sentence
says a context-sensitive word outside its format *"is treated as a user-defined word"*, and a hard keyword
token is exactly how that right gets lost silently. Three programs say which is which:
`conformance:negative/pb721-record-bytes-below-2023` (the spelling, refused at 85/2002/2014),
`conformance:85/pb721_zero_length_record_85` (the CHARACTERS spelling, accepted at 85) and
`conformance:85/pb721_bytes_user_word_85` (the word as a data-name, a table and a paragraph-name at 85).

**SR1 stays where it is** (`DataBinder.MaterializeImpliedRecord`, COBOLNET1836): it is about the clause's
PRESENCE, decidable while the entry is read — D19's first list, not its second.

**SR6 is the one rule of this subclause still without a site, and that is a decision.** *"Data-name-1 shall
describe an elementary unsigned integer in the working-storage, local-storage, or linkage section"* states THREE
obligations, and the third needs a fact the data model does not carry: which SECTION an item was described in.
`EntrySection` is passed to `DataBinder.BindEntries` and discarded; `DataItem` has no section member. Screening
two arms of three is exactly the defect SR4's split exists to prevent, so SR6 waits for that fact and then
becomes three rows of this table.

**Edition-invariance was derived, not assumed.** The RECORD clause and its syntax rules predate COBOL-85, Annex E
— the complete 2014→2023 delta — carries no entry for this subclause, and `docs/VERSION_CHANGE_REFERENCE.md`
records that this repository's spec cannot speak for the 85↔2002 delta at all, so a gate below 2014 would be
INVENTED rather than derived. The screen is therefore unconditional and every negative rejects at all four
editions; `conformance:85/pb721_zero_length_record_85` is the over-rejection twin that proves no gate leaked
downward.

**Why a table, and the drift guard that keeps it one.** `RecordClauseRuleDriftTests` re-derives every row from
`specs/ISO_COBOL.md` on every run — the clause region, the printed ordinal, the two arms of SR4, and the FORMAT
column, which it reads off the standard's own bare `ALL FORMATS` / `FORMAT 1` / `FORMAT 2` / `FORMAT 3` division
lines. That last one matters more than it looks: a row screened on the wrong format either fires on source the
standard permits or is silent on source it forbids, and neither shows up as a compile error or a golden failure.
The three spec-reading helpers both drift guards need now live in `tests/_shared/SpecClauseText.cs` rather than
as a second paste.

### D25. A file clause's `data-name-n` operand is a QUALIFIED-DATA-NAME: ONE capture that refuses every other written shape, and ONE ISO §8.4.2.2 resolver whose answer is the NUMBER of survivors — never a base word plus a first-match lookup.

**The rule.** §8.4.2.2.2 Format 1 defines a qualified-data-name as `data-name-1 [ data-qualifier ] …
[ file-report-qualifier ]`, and §8.4.2.2.1 makes uniqueness the operand's obligation: *"uniqueness shall be
established through qualification for each user-defined name explicitly referenced"* (§8.4.2.2.3 SR1 repeats it;
SR2 permits qualification even where it is unnecessary). So every clause that prints `data-name-n` — LINAGE
(§13.18.34.2), FILE STATUS (§12.4.5.8), RECORD KEY, ALTERNATE RECORD KEY, RELATIVE KEY, ASSIGN … USING,
RECORD … DEPENDING ON — means exactly that reference, and exactly that resolution.

**What it replaced.** Each of those clauses captured its operand as `d.cobolWord()?.GetText() ?? d.GetText()` —
the FIRST word of the written reference, every IN/OF qualifier discarded — and then resolved it with
`ByName.TryGetValue(n, out var l) && l.Count > 0 ? l[0] : null`, the first declaration of that name in source
order. Both halves are silent. On the LINAGE clause the pair was a measured wrong answer: `LINAGE IS SZ OF GRP-B
LINES` over `01 GRP-A. 05 SZ VALUE 3.` and `01 GRP-B. 05 SZ VALUE 9.` built the whole logical page on GRP-A's
value, with empty compile output, and moving the two VALUE clauses between the groups changed the program's
behaviour with no other edit (kb/Work PB489). For the special-register alternative of the shared `dataReference`
nonterminal the first word is the FILE-NAME QUALIFIER, so `LINAGE IS LINAGE-COUNTER OF LPF LINES` recorded the
file name as the clause's data-name and died at OPEN naming a word the programmer never wrote as a data item.

**Where it lives.** Three members of `DataBinder`, and nothing else:

- **`QualifiedCandidates(name, quals, scope)`** — THE §8.4.2.2 matcher: every in-scope declaration whose ancestor
  chain carries the qualifiers inner → outer with gaps allowed (§8.4.2.2.3 SR4), the outermost qualifier
  optionally the owning FILE (Format 1's `file-report-qualifier`). It returns the SET, because the count is the
  standard's answer. `ReferenceResolver.ResolveQualified` — which used to own the only complete copy, while the
  binder carried a weaker private twin that knew neither the file-name qualifier nor uniqueness — now calls it.
  **The set is a `DataNameCandidates` and has no indexer (kb/Work PB978):** it can be counted, narrowed
  (`Where`) and read only as `Single`, so no caller can take "the first". Its record-confined twin
  `SubtreeCandidates(root, name, quals)` serves RENAMES (§13.18.45.3 SR4) and a TYPEDEF clone's own OCCURS DEPENDING
  ON counter. Several survivors go to **`UniqueOrReportAmbiguous`**, the ONE data-division ambiguity verdict
  (§8.4.2.2.3 SR1, COBOLNET1639; under `--permissive` a warning and the first declared — the procedure division's
  own disposition). Every data-division operand reads it: FILE STATUS, RELATIVE KEY, RECORD … DEPENDING ON and LINAGE
  (through `ResolveClauseOperand`), RECORD KEY / ALTERNATE RECORD KEY (SR2 narrows to this file's records first,
  then counts), ASSIGN … USING, OCCURS … DEPENDING ON, SAME AS, RENAMES, and the report CONTROL / SOURCE / SUM
  operands. `DataNameResolutionDriftTests` (Unit) pins the type's shape and forbids `[0]` off a raw candidate list
  in the data binder.
- **`ClauseDataName(dref, clauseFace)`** — the capture, screened by `ScreenClauseOperandShape` against the three
  shapes a `data-name-n` position does not admit → **COBOLNET2024**: a SPECIAL REGISTER (LINAGE-COUNTER /
  LINE-COUNTER / PAGE-COUNTER are §8.4.3.1 Format 10 / Format 11 identifiers, and §8.4.3.14.3 SR1 / §8.4.3.15.3
  SR1 confine them to the procedure division), a SUBSCRIPT (§8.4.2.3's *with-subscripts* form is an identifier,
  and each of these clauses independently bans an operand subject to an OCCURS clause), and a
  REFERENCE-MODIFIER (§8.4.3.3.3's NOTE). A refused operand is recorded AS WRITTEN and its later resolution stays
  silent — one fault, one verdict, including the RECORD KEY / ALTERNATE RECORD KEY selection and the
  `FileControlKeyRules` SR2 row, which take the refused names with the ambiguous ones (kb/Work PB481). The screen
  reads the operand's suffixes through `ReferenceResolver.ReadOperandSuffixes`, the one reader of a reference's
  subscript lists and reference modifiers; while a ref-mod written after a name was a SUBSCRIPT-mode capture
  (before kb/Work PB2113), counting every `subscriptPart` as a subscript called `IX-KEY(1:3)` "a subscript".
- **`ScreenDataNameShape(dref, face, edition)`** — the same screen, static, for a PROCEDURE-DIVISION operand
  where a general format prints `data-name-n`: the SORT KEY phrase of both formats (§14.9.40.2; §14.9.40.3
  SR14 b) *"Key data names shall not be subscripted"*) and the MERGE KEY phrase (§14.9.24.2). Those keys used to
  keep the base item and drop a reference-modifier, so a table SORT on `K(4:3)` sorted on all of K (kb/Work
  PB481). READ's KEY operand (§14.9.30.3 SR11) and START's (§14.9.41.3 SR5/SR6) are identity rules and ask
  `Place.DenotedItem`, which a reference-modified slice never is (kb/Work PB602, PB481).
- **`ScreenReferenceModifier(dref, face, edition)`** — the NOTE's reference-modifier arm alone, for the one
  `data-name-n` operand that must carry a subscript: SEARCH ALL Format 2's `data-name-1` and every `data-name-2`
  (§14.9.37.2; §14.9.37.3 SR8 requires the first index-name subscript). `SearchAllFormat2Rules.CheckKeySide` asks it,
  so `WHEN TK (IX) (1:2) = "AB"` is COBOLNET2024 rather than a prefix-key search (kb/Work PB1457). The sentence is
  worded once, in `ReportReferenceModified`, shared with `ScreenDataNameShape`.
- **`ResolveClauseOperand(name, quals, face, at)`** — the resolution, reporting a zero- or many-survivor outcome
  under **COBOLNET1639**, the same descriptor the procedure division's own unidentified reference uses: the rule
  broken is §8.4.2.1/§8.4.2.2, not a rule of the clause.

**The exceptions, and why they are principled.** The REPORT SECTION's SOURCE (§13.18.53), SUM (§13.18.54) and
CONTROL (§13.18.16) operands capture with the unscreened `KeyReference` and apply their own rules, because
§13.18.16.3 SR4 and its twins EXPRESSLY permit a reference-modifier there — the generic screen would reject
source those clauses allow. The KEY clauses keep their own SELECTION among the candidates (§12.4.5.12.3 SR2 puts
the key inside a record description of the file), but take the candidates from the one matcher.

**The LINAGE clause's own syntax rules** live in `DataBinder.ResolveLinage`, the FD-clause twin of
`ResolveFileCollating` and D19's `ResolveFiles` arm: §13.18.34.3 **SR1** (*"Data-name-1, data-name-2,
data-name-3, and data-name-4 shall not be subject to any OCCURS clauses"*) → **COBOLNET2025**. It had no site at
all — a LINAGE operand naming a table element compiled clean and killed the process at OPEN OUTPUT. **SR2**
(*"shall reference elementary unsigned numeric integer data items"*) and **SR3** (*"Integer-2 shall not be greater
than integer-1"*) are the other two tests of the same method, under the same code (kb/Work PB524). SR2 names the
first conjunct the item breaks — group, not numeric (an index item is class index), not an integer
(`PicInfo.IsIntegerDescription`: fixed-point, no fraction — a floating-point item is refused), signed; SR3 compares
the two LITERALS only, because with a data-name on either side the relation is a run-time value and §13.18.34.4 GR3
governs it. Before, the emitter ASSUMED SR2 and its "defensive rescale" laundered an illegal operand: an
alphanumeric "007" became page size 7 and a PIC 9V9 5.5 was truncated to 5. A zero integer-1 / integer-2 is §5.5
1)'s, refused by `IntegerOperandPass` (SR4's permission covers only integer-3 / integer-4). §8.4.3.14.3 **SR2**, *"The LINAGE-COUNTER identifier shall not be referenced as a
receiving operand"*, is the third arm of `ExpressionBinder.ResolveReceiving`'s register dispatch →
**COBOLNET2026**; LINE-COUNTER and PAGE-COUNTER had arms and LINAGE-COUNTER did not, so permanently illegal
source was refused as a not-yet-implemented receiver shape.

**Pinned by** `ClauseOperandQualificationSpecTests` (behaviour, all four editions: the qualified operand of each
clause binds the qualified item, the ambiguous one is refused) and `ClauseOperandCaptureDriftTests` (shape: the
first-word reduction and the §8.4.2.2 matcher are each written down ONCE, and `ResolveFiles` holds no first-match
lookup — every assertion was red on the pre-change tree).

**Rejected alternatives.** *A row in `FileControlKeyRules`* — that table's organizing axis is §12.4.5.1's four
FORMATS (its `FileKinds` set and its format-vs-disjoint invariant exist for §12.4.5.2 SR8/SR9), and the LINAGE
clause is an FD clause with no format axis at all; widening it would have meant a class rename rippling through
every inventory `code-location` that names it. *Leave the key clauses on their record-subtree walk* — that walk
was the second copy of the qualification rule, and it could not see the file-name qualifier. *Report the
resolution failure for an already-refused shape* — a second verdict for one fault, and the shape refusal is the
cause.

### D28. The MEDIUM'S BOUNDARIES are answered ONCE each, at the WRITE or REWRITE, before anything reaches the medium — the file coded character set, the store's capacity, the host medium, and the organization the file records (kb/Work PB1192, PB690, PB1098).

**Decision.** Four questions decide whether a record may reach a physical file, and each has ONE executable
answer that every organization's WRITE and REWRITE asks, in the connector, before the record is released:

1. **Can the record be written in the file coded character set?** The medium's encoding is an attribute of the file (`FileConnector.MediumEncoding`, derived — never stored: CODE-SET present -> `CodeSet`; else LINE SEQUENTIAL -> `Utf8`; else `SingleByte`; D29). A `SingleByte` file (Annex A.1 item 31, owner decisions kb/Work R47 and R51) is ISO/IEC 8859-1, one byte per character position, through the storage-byte law (`FileCharacterSet.ToChannel/FromChannel`: U+FFFF, the HIGH-VALUE character, is byte 0xFF): `FileConnector.RecordHasCharacterWithoutByteImage` refuses a record holding any other character above U+00FF; a file with a CODE-SET is refused a character the alphabet does not represent (`CodeSetConversion.HasCharacterWithoutImage`, §13.18.13.4 GR6 b; kb/Work PB1150, PB1542) and writes HIGH-VALUE as the set's highest code unit (`CodeSetConversion.HighUnit`). The refusal is '91' (§9.1.13.11; DOC-A.1-110) on every fixed-record organization and on the report writer's line. A LINE SEQUENTIAL file with no CODE-SET is UTF-8 text: its character set (`LineSequentialCharacterSet`, item 115) is the Unicode scalar values from U+0020 in both record-area classes, so only C0 controls and an unpaired surrogate are refused, with '71'. Every record write encodes through the medium's codec — `FileCharacterSet.Medium` (a STRICT Latin-1 encoding of the channel) or `LineSequentialEncoding` — so a path that skipped the refusal fails loudly: there is no replacement fallback and no print-specific character map anywhere.
2. **Does the store hold it?** A relative or indexed file is held in memory while open and written whole at
   CLOSE, so its externally-defined boundary (§9.1.13.5 item 4; Annex A.1 item 107) is the store image's
   capacity, `RecordFraming.MaxStoreBytes`. `RecordFraming.FrameBytes` is the ONE size formula — the stores keep a
   running total through their only mutators (`RelativeStore.FramedBytesAfterPut`, `IndexedStore.RecordBytes`)
   and `ComposeStore` sizes its composition with it — so `KeyedConnector.StoreHolds` answers §14.9.51.4 GR33 b) /
   GR42 d)'s '24' in O(1), at the WRITE. A relative store is persisted from its sparse slots LAZILY
   (`RelativeStore.Ordinal`), never through an array sized by the highest RRN. GR29 b)'s highest permitted RRN
   is `RelativeConnector.HighestRelativeRecordNumber`; above it is '34'.
3. **Does the host medium take it?** A sequential file's boundary is the host medium's capacity (§9.1.13.6
   item 3; Annex A.1 item 108). `HostFile.IsMediumBoundary` is the one classification of the host's refusal and
   `FileStatusCode.ForWriteFailure` maps it ('34', else '30'); `SequentialConnector.Write`, `WriteAdvancing` and
   `Rewrite` are the only three entry points a record reaches the medium through, and each wraps its body in
   that one catch. A keyed store meets the host only at the CLOSE, whose failure is '30'. The READ side has the
   twin catch (kb/Work PB1512): `SequentialConnector.Read` and `StartFirstLast` are the only entry points that
   read the medium, and a host failure there is §9.1.13.6 item 1's '30' with the file position invalidated
   (`ReadFailed`), never an escaping exception. An OPEN whose body failed releases its handles
   (`AbandonOpen`) without letting a refused final flush replace the OPEN's own status.
4. **Does the file's recorded organization allow this connector to write?** A keyed store's header records its
   organization (D10's no-sidecar format), and §12.4.5.10.3 GR1 makes it permanent, so a SEQUENTIAL connector's
   OPEN I-O or EXTEND over one is the '39' conflict (`SequentialConnector.FixedAttributeConflict`; DOC-A.1-129).

**Rationale.** Each of these used to be answered late, twice, or not at all: Latin-1's replacement fallback
wrote '?' for an unrepresentable character with '00', and a private print map wrote '?' for everything above
U+007F; a relative WRITE past the store's capacity was '00' and the CLOSE then crashed (a dense frame array
sized by the key) or failed; a host `IOException` on a sequential WRITE escaped the statement; a sequential I-O
connector overwrote a keyed store's header. Answering each question once, at the connector, before the release
is what makes the refusal an I-O status the program can observe and what keeps the medium unchanged (§14.9.51.4
GR15).

**Rejected alternatives.** *An exception fallback in the writer as THE mechanism* (R47's first sketch): the
sequential writer buffers, so the exception would surface on a LATER write — the wrong statement — after the
refused record was already in the buffer; the fallback is kept only as the guard behind the pre-check.
*Reserving host space at each keyed WRITE* (`SetLength` growth): it is not a reservation on a sparse Unix file
and it leaves zero-padding a crashed run unit would read back as records. *Flushing every sequential record* to
detect a full medium on its own WRITE: a syscall per record for every program, where the buffer-filling WRITE
already reports the boundary.

### D29. TEXT, BYTES AND ENCODING are three layers: characters in memory, ONE storage-byte law wherever a byte becomes a character, and the file encoding as an attribute of the file. (kb/Work R51, R52; PB1759, PB1760.)

**Decision (owner, 2026-09-29; the mechanism corrected by the implementer's measurement, kb/Work PB1759 step 0, under
the owner's addendum of the same day).** Text in memory is characters (UTF-16 code units). A record on a byte medium is
BYTES. The two meet at exactly one law and one per-file encoding:

| layer | rule | where it lives |
|---|---|---|
| in memory | alphanumeric / national items hold characters; a byte-form leaf (binary, packed, IEEE, pointer, bit, a national item's UTF-16BE pairs in a record image) holds BYTES, imaged one character per byte | the record structs and REDEFINES backings |
| the storage-byte law | **byte 0xFF is the character U+FFFF (the native HIGH-VALUE, R52); every other byte b is U+00bb; a character's byte is its low-order byte**, so U+00FF and U+FFFF share 0xFF | `CobolNet.Runtime.StorageByte` — every byte-form encoder (`CobolNum.Image`, `PointerImage`, `CobolBits`) and decoder, the item-209 byte reduction (`CobolIntrinsics.RawBytes`) and the fixed-record medium |
| the file encoding | fixed-record organizations (sequential, relative, indexed, report): one byte per position through the law, a character with no byte (above U+00FF, not U+FFFF) refused `'91'` (R47); LINE SEQUENTIAL: UTF-8 (PB1760); a CODE-SET clause converts the native one-byte channel (§13.18.13.4 GR6) | `FileCharacterSet.ToChannel` / `FromChannel` (the two medium sites: `FileConnector.ToMedium`/`FromMedium`, `RecordFraming.WriteStore`/`ReadStore`) |

**Why one law and no per-leaf medium codec (the correction).** The first version of this decision gave every record
struct a second generated image (`AsMedium`/`FromMedium`) whose alphanumeric leaves mapped U+FFFF <-> 0xFF, on the
reasoning that a connector-wide map cannot tell a text 0xFF from a binary 0xFF. Measurement showed three things
(kb/Work PB1759, step 0): (1) every in-memory byte-form DECODER already truncates a character to its low byte, so
the direction "0xFF -> U+FFFF" costs a numeric view nothing; (2) the record areas that matter most have no leaves to
dispatch on — a multi-01 FD's shared area and every Tier-B REDEFINES backing are string backings, and an elementary
record has no struct; (3) the real collision is between two CHARACTERS and one BYTE, and it bites IN MEMORY first:
with HIGH-VALUE = U+FFFF and binary bytes imaged as U+00bb, `MOVE HIGH-VALUES TO REC` then `IF REC = HIGH-VALUES`
turns FALSE for any record with a COMP field, because storing the group through the binary leaf re-images FF FF as
U+00FF U+00FF — content changed by a mere store, where §14.9.25.4 GR4 moves a group "without conversion of data from
one form of internal representation to another". Making byte 0xFF the character U+FFFF
everywhere fixes that and makes the medium a pure channel: a byte-form leaf's image already holds U+FFFF for 0xFF,
a text leaf holds U+FFFF for HIGH-VALUE, and both leave as 0xFF and come back as U+FFFF — no layout knowledge, one
place. The per-leaf design would also have needed a second image composer arm for every leaf kind (fixed, variable,
bit runs, OCCURS, national), which is two mechanisms for one job.

**Accepted cost (owner R52, generalized; DOC-A.1-31).** U+00FF and U+FFFF share the byte 0xFF, so a real `ÿ` that
passes through a byte — the record medium of a fixed-record file, or a byte-form storage position (a group MOVE of
`ÿ` over a COMP field) — returns as HIGH-VALUE. In an alphanumeric leaf in memory it stays U+00FF, the literal
`X"FF"` stays U+00FF, and a LINE SEQUENTIAL file (UTF-8, PB1760) carries it exactly.

**A CODE-SET file.** No alphabet's correspondence names U+FFFF, so HIGH-VALUE crosses a CODE-SET medium as that coded
character set's HIGHEST code unit — 0xFF under `EBCDIC`, 0x7F under STANDARD-1 / STANDARD-2 / `ASCII` — and that unit
reads back as HIGH-VALUE (owner decision R51 item 4: "cross a byte medium as that medium's extreme byte";
`CodeSetConversion.HighUnit`). The cost mirrors the native medium's: the character the alphabet itself assigns to that
unit (U+009F, DEL) reads back as HIGH-VALUE (DOC-A.1-187).

**The encoding is an attribute of the FILE.** Fixed-record organizations default to one byte per alphanumeric
position, because a variable-width encoding makes a record's byte length vary and breaks record-number positioning
and the fixed-attribute check (§9.1.6); LINE SEQUENTIAL defaults to UTF-8, because a text line has no fixed width and
full Unicode is required (owner). The FD CODE-SET clause selects any other encoding (`CodeSetConversion`), and Unicode
inside a fixed-record file is NATIONAL. R47's "refuse a character above U+00FF" survives for fixed-record files with no
CODE-SET (HIGH-VALUE excepted); it is superseded for LINE SEQUENTIAL (PB1760).

**The collating fingerprint** (§12.4.5.7.4 GR1, a fixed file attribute) hashes the weight of every one of the 65,536
native characters (`FixedFileAttributes.Fingerprint`), so two sequences that differ only above U+00FF — where
HIGH-VALUE now sits — are different attributes.

**Rejected alternatives.** *UTF-8 as the default for every file*: binary record content (packed, binary, float) is
raw bytes, and a multi-byte encoding would corrupt any byte above 0x7F. *A per-leaf medium codec* (`AsMedium` /
`FromMedium`): unreachable for leafless record areas and wrong in memory (above). *Imaging a binary 0xFF as U+00FF*:
breaks `IF REC = HIGH-VALUES` after `MOVE HIGH-VALUES TO REC` (above). *Keeping HIGH-VALUE at U+00FF*: leaves
U+0100..U+FFFF ordered above "the highest character" in a UTF-16 repertoire (§8.3.3.6.4 GR6; PB1093).
*Byte-position alphanumeric items* (GnuCOBOL, IBM DISPLAY): safe and interoperable, but `PIC X(10) VALUE "héllo"`
would hold four characters' worth of positions and truncation would split code points.

### D30. Seven statement-level rules the connectors used to leave to chance, each written in ONE place (kb/Work PB1541, PB1513, PB1195, PB1097, PB1562, PB679, PB677).

Each of the seven was a rule the standard states once and the runtime or emitter honored on one arm only.

**A permanent error remains in effect (PB1541; §9.1.13.1, Annex A.1 item 105, `docs/CONFORMANCE.md` DOC-A.1-105).**
`FileConnector.Status`'s setter — the one I-O status assignment path — starts the condition when a status beginning
'3' is assigned to a connector that is OPEN (a failed OPEN leaves the connector closed, which already is the corrected
state), and a completed CLOSE ends it: CLOSE is the implementor's correction technique. Every other statement
asks `PermanentErrorReplay` once, in the precondition its verb family already shares — `ReadOpenModeGuard`,
`StartOpenModeGuard`, `MutationOpenModeGuard`, the first line of each WRITE entry, `Open`, the registry's UNLOCK and
DELETE FILE — and answers the same status without touching the file; the replay outranks '41', '43', '46', '47',
'48' and '49'. No producer starts it by hand, so the next producer of a '3x' (a host failure, a boundary, the ODO
READ below) is covered. `PermanentErrorInEffectTests` is the organization x verb matrix and the drift guard.
*Rejected:* a flag each '3x' producer sets (the producers are the open-ended set); replaying only the verbs that
already had a '4x' guard (WRITE had none shared, which is why it asks first on its own line).

**A READ of a record made variable by OCCURS DEPENDING ON is judged after the record lands (PB1513; §9.1.13.6 item 4 b),
D-ODO2).** The connector cannot see the record layout, so the generated READ evaluates the predicate where the record
description is known — one comparison of the DEPENDING ON item against a compile-time limit
(`SequentialIoEmitter.OdoRecordExceedsMaximum`, which sizes the record with the arithmetic `OdoModel.WrapGroup`
already uses) — and reports the outcome through `FileRegistry.ReadExceedsRecordMaximum`, which assigns '34' through the
one status path. The sequential READ, the keyed READ and the SORT/MERGE USING transfer's implicit READ share that
predicate; a file with no such record emits nothing.

**A released record is also available in the other records of its SAME RECORD AREA (PB1195; §14.9.51.4 GR4,
§14.9.35.4 GR6, §14.9.32.4 GR3).** Character-window records share one backing and get it for free; an OUT-OF-LINE
record (D27) does not, so WRITE, REWRITE and RELEASE run `EmitReleasedRecordAlsoAvailable` after a SUCCESSFUL statement:
the shared character area takes the released out-of-line record's image, and each other out-of-line record of the area
(`FileModel.OutOfLineRecordsOfArea` — the same enumeration a READ's `OutOfLineRecords` is now defined over) takes the
record at its GR13 length with its extent table. The READ twin was completed in the same change
(`EmitRecordAreaStore` reaches a clause peer's character window when the file has none of its own).

**A sort-merge file's own ASSIGN USING is associated at its SORT or MERGE (PB1097; §12.4.5.3 GR3, DOC-A.1-73).**
`FileConnector.AssociationFailure` is the one rule — a pure function of the specification; `Associate` records the
path of an OPEN with it and `CobolSort.AssociationMade` asks it for the sort-merge file, which is never a connector.
`SortEmitter.EmitAssociation` leaves for the statement's end label before any phase when it fails (§3.176: an
unsuccessful execution performs not all the operations of the statement).

**The RECORD clause's size rules screen variable-length records, and an explicit Format 1 file carries them in the FIXED
FORM (PB1562; §13.18.43.3 SR3/SR4, §13.18.43.4 GR6, §9.1.6, D-FRA (iv)/(vi)).** `RecordClauseRules` no longer skips a record
holding a dynamic-length item: GR8 b)'s maximum is the item's maximum size and its minimum is zero. An explicit
`RECORD CONTAINS integer-1` keeps the file FIXED (`FileModel.ImpliesVariableFormat` again requires an absent clause):
its record type and size are attributes every program using the file shares (§9.1.6), so a second program describing
it as `PIC X(20)` reads the same bytes, and no frame or extent table can be added by one program's description. A
record that holds variable-length members is therefore carried in its fixed form (`FileModel.FixedFormRecords`;
`CobolContiguousLayout.ToFixedForm`): every member at the position it has at its MAXIMUM size, space padded. ONE
place decides it on the way out — `FileConnector.FixedForm`, asked by the registry's governed WRITE and REWRITE
entries and by `IndexedConnector.AreaKey` — and ONE on the way in — the READ landing's `fixedForm` flag
(`SequentialIoEmitter.EmitRecordAreaStore`), which makes `CobolContiguousLayout.Decompose` take each member at its
maximum width and drop its trailing padding. An indexed key a dynamic member precedes registers as a plain window at
its fixed-form offset (`KeyedIoEmitter.KeyWindow`), the window the second program names.

**One pad, one trim, keyed on the record area's category (PB679; §14.9.30.4 GR15).** `FileConnector.FitRecord` is the only
short-record pad; the indexed key slice, the varying-record store and the blank area an unsuccessful READ returns
(`BlankRecordArea`) all use it, so a national record area is never padded with a byte-level space.

**A report file has no record description (PB677; §13.4.5.3 SR8, §9.1.22).** The emitter's one record-less arm registers
it through `CobolFile.RegisterReport`; its connector is told `recordDescribed: false`, and `NoticeIfLayoutDisagrees` — the
one rule that compares the file's bytes with a record length — asks that first. A line width is not a record length.

## C# mapping

> Backend neutrality (G4; SSOT §18 #23): everything semantic in this section — FILE STATUS capture, the AT END /
> INVALID KEY branch, READ INTO / WRITE FROM expansion, the prologue registrations — is a structured BOUND-TREE form;
> this section shows the primary RoslynBackend rendering. The future CilBackend renders the SAME bound nodes behind
> `ICodeGenBackend` with its own private lowering; no bound node carries pre-rendered C# text.

An FD record CUST-REC with CUST-ID PIC 9(5), CUST-NAME PIC X(20), CUST-BAL PIC S9(7)V99 COMP-3 maps to a public record struct CUST_REC holding public long CUST_ID, public string CUST_NAME, public long CUST_BAL, where CUST_BAL is the UNSCALED long (scale 2 is compile-time metadata per the owner numeric lock; no decimal). The record area is a single field of that type in the program class. The connector is a public sealed generic FileConnector of TRec exposing Open, Close, Read, ReadPrevious, ReadByKey, Write, Rewrite, Delete, Start, SetKey, plus properties CurrentSlot, LastRecordLength, EndOfPage, and LastStatus. There is NO program-supplied key parameter; the key is the current value of the typed RECORD KEY or RELATIVE KEY field. The codec is a generated IRecordCodec exposing Serialize, Deserialize, PrimeKey, AlternateKey, FixedLength, MinLength, MaxLength, and CodeSet. Stores: the sequential connector uses a StreamReader or StreamWriter for line-sequential and a FileStream for record-sequential with a 4-byte little-endian length prefix for varying; the relative connector uses a sorted dictionary from int slot to byte image with 0xFF gaps; the indexed connector uses a sorted dictionary from CobolKey to byte image as the sole source of truth, with alternates derived on demand and a PER-KEY release ordinal for duplicate ordering. The run-unit prologue emits one registration per SELECT plus AddAlternateKey calls. After each I/O verb the compiler stores the connector LastStatus into the FILE STATUS item then branches AT END or INVALID KEY on the first char (1 at-end, 2 invalid-key, 3 4 7 9 fatal to a USE declarative). READ INTO lowers to Read plus a typed MOVE OF THE CURRENT RECORD (D21 - the record area sliced to the 13.18.43.4 GR16 byte count, and an alphanumeric group move when the FD carries a RECORD IS VARYING clause); WRITE FROM lowers to an ordinary typed MOVE plus Write; both moves are BOUND, not synthesized at emission (D23), so the emitter renders a BoundMove the MOVE binder already screened; a sequential RELATIVE WRITE or READ NEXT MOVEs CurrentSlot back into the RELATIVE KEY field.

## Hard problems

### Key ordering must match COBOL semantics for binary, signed, and composite keys; the legacy Latin-1 byte-string sorted-dictionary trick silently mis-orders COMP keys.

Order by the typed CobolKey comparable: numeric by decoded value, alphanumeric by image plus collating, composite component-wise. One policy across indexed files and SORT; the on-disk image is payload only and never drives ordering.

### Multi-01 record-area overlay: a program writes through one 01 then reads another 01 reinterpretation of the same bytes, with no persistent byte buffer to overlay.

READ deserializes the raw record bytes into every 01 view of the FD via a discriminated wrapper; the file edge has the bytes anyway. Cost is order of number of views, and views are few.

### REDEFINES and RENAMES inside an FD record cannot be deferred (the codec must emit the redefining layout bytes).

The codec serializes the base (first) definition; a REDEFINES sub-view is materialized by re-deserializing the emitted bytes under the redefining layout on demand. Designed once and shared with the general working-storage REDEFINES solution.

### REWRITE record-length rules are organization-dependent: a RECORD SEQUENTIAL REWRITE must equal the replaced record's length (14.9.35 GR16) or it is status 44; a RELATIVE or INDEXED REWRITE may change length (14.9.35 GR18) but must stay within the RECORD IS VARYING bounds (14.9.35 GR20) or it is status 44. On any 44 no logical updating takes place and the record area is unchanged (14.9.35 GR14).

`SequentialConnector.RewriteRecord` dispatches on the ORGANIZATION — the axis §14.9.35.4 states the rules on — never on fixed-versus-varying (kb/Work PB1168). For a RECORD sequential file, fixed-length or variable, the connector remembers the last-read frame start and length and returns 44 unless record-name-1's byte count EQUALS it (GR16 — the in-place frame cannot change size); the count is §13.18.43.4 GR13's, which the emitter passes as a constant for a fixed-length file (`SequentialIoEmitter.RewriteLengthArg`), because the runtime's default on a fixed file is the WRITE's whole-record-area rule. For a LINE sequential file GR17 a)–d) apply in order, comparing that same untrimmed byte count with the replaced line: more bytes is 44, fewer are space-filled to the line's length (GR21's trailing-space rule is WRITE's, not REWRITE's). A WRITE makes its length decision once for both of its arms (`WrittenLength` / `OutsideVaryingBounds`), so a print-control WRITE is held to the RECORD IS VARYING bounds exactly as a plain one (kb/Work PB1190). For a relative or indexed file the record length is allowed to differ (GR18), so REWRITE checks only that the serialized length lies within the file's RECORD IS VARYING minimum/maximum, returning 44 otherwise (GR20).

### Read-position state machine: a sequential READ (NEXT or PREVIOUS) issued after a prior unsuccessful sequential READ is 46; a sequential REWRITE or DELETE without a preceding successful READ is 43; START establishes an inclusive file-position indicator.

Port the proven per-connector last-read-unsuccessful, past-end, prev-op-was-successful-read, and read-next-inclusive flags. An at-end READ is itself an unsuccessful READ (14.9.30 GR24 — "when the at end condition exists, execution of the READ statement is unsuccessful"), so once the last-read-unsuccessful flag is set the NEXT sequential READ — whether READ NEXT or READ PREVIOUS — is unsuccessful with status 46 (14.9.30 GR21); it does NOT re-expose the last record. A sequential REWRITE or DELETE with no immediately preceding successful READ is status 43 (14.9.35 GR5).

### The READ DIRECTION is one fact on both bound READ nodes, and a backward sequential read is a REPOSITION feeding the same physical read body.

§14.9.30.4 GR19 — "An implicit or explicit NEXT phrase or a PREVIOUS phrase results in a sequential read:
otherwise, the read is a random read and the rules for format 2 apply" — makes the direction phrase and the
FORMAT one fact, so ONE enum carries it: `ReadKind { Next, Previous, Random }`, on `BoundRead` and
`BoundKeyedRead` alike, reached through `IBoundRead`. `VersionConformancePass.GateStatement` has ONE arm over
that interface. (It was `KeyedReadKind`, reachable only from the keyed node; the sequential node had no direction
member, so `READ … PREVIOUS` on a SEQUENTIAL file bound as a forward read AND skipped its COBOLNET0900 2002 gate
— kb/Work PB334.)

`SequentialConnector.Read(previous, out image)` implements GR21's "When the file is a sequential file" block
directly: rule b) — the file position indicator established by a prior OPEN selects "the first existing record …
regardless of whether NEXT or PREVIOUS is specified", so both directions target ordinal 1; rule c) — after a
successful READ the target is one greater (NEXT) or one less (PREVIOUS); rule e) — no such record is the at end
condition ('10' + the AT END imperative, GR24). `TargetReadOrdinal(previous)` is that arithmetic in one place, and
it is also the §14.9.30.4 GR9 pre-read conflict target the sharing registry checks, so `ADVANCING ON LOCK`
skip-scans in the statement's own direction — GR22 ends the scan at "the end of the file … if NEXT is specified or
implied, or the beginning of file … if PREVIOUS is specified".

A backward read is a **reposition**, not a second reader: `SeekToOrdinal` positions the stream at the record's
first character and the ordinary read body then delivers it, which is what keeps `_lastReadBlockStart` (the
in-place REWRITE anchor, §14.9.35.4 GR5) correct after a PREVIOUS. The offset is arithmetic on a fixed-width
record-sequential file; on a RECORD VARYING file it comes from `RecordFraming.FrameStarts`, a prefix-only scan
built **lazily on the first backward read**, so a forward READ walk pays nothing for a facility it never uses.
LINE SEQUENTIAL has no backward walk and needs none — §14.9.30.3 SR7 forbids the phrase there — and a stream that
cannot seek is not §14.9.30.4 GR20's "single reel/unit mass storage file", which the connector reports as the '30'
permanent error rather than silently reading something else.

### EXTERNAL files shared across programs plus GLOBAL FD inheritance in nested programs, with matching layouts.

A process-wide registry keyed by external name (with an Area discriminator for record sharing, porting IC227A); nested programs resolve the parent connector via a global registry (porting IC233A and 234A); the codec and layout must match across declarations.

## Edge cases

- OPTIONAL files: OPEN INPUT missing gives 05 and positions at EOF (first READ is 10); OPEN I-O or EXTEND missing creates and gives 05; non-optional missing gives 35 (not silent-create, the legacy RelativeFileHandler bug fix).
- Sequential RELATIVE WRITE assigns the next slot and MOVEs it into the typed RELATIVE KEY field; READ NEXT exposes CurrentSlot for the same MOVE-back (14.9.51 sequential).
- Random RELATIVE: key below 1 gives 34, an occupied slot gives 22 (INVALID KEY), an absent slot gives 23; sequential digit overflow gives 24, sequential READ digit overflow gives 14.
- Indexed: ascending-order WRITE in ACCESS SEQUENTIAL gives 21 on a non-increasing key; a duplicate prime or alt-without-duplicates gives 22; alt-with-duplicates gives 02; START supports a generic partial or prefix key compare (14.9.41) and positions inclusively so the next READ NEXT returns the matched record.
- **START is a THREE-organization verb** (kb/Work PB352). 14.9.41 writes its general rules three times, once
  per organization heading — GR11/GR12 for RELATIVE, GR18/GR19 for INDEXED, GR20/GR21 for SEQUENTIAL — and
  14.9.41.3 SR2 makes FIRST or LAST the REQUIRED phrase on a sequential-organization file, so the sequential
  arm is not an extension but the only shape a conforming START on one can have. `SequentialConnector`
  answers it over the ONE framing walk (`NextFrame`, extracted out of `Read` so START's record scan cannot
  become a second copy of the line/varying/fixed framing rules): FIRST rewinds to record 1, LAST scans to the
  last frame, each repositions the reader through `SeekToRecord` (which discards the buffered data and resets
  the derived read state — the byte anchors, the GR15 unread remainder, the 9.1.16 record ordinal), and
  positioning is INCLUSIVE per 14.9.30.4 GR21's sequential arm b). Every failure arm — an empty file, a
  non-seekable stream, an absent OPTIONAL file — is '23' (9.1.13.5 item 3 b)/d)), never an abort; a wrong open
  mode is '47' (9.1.13.7 item 7, Table 20's blank Output/Extend cells); an unsuccessful START arms 9.1.13.7
  item 6 a)'s '46' on the next sequential READ. Goldens: `conformance:2002/pb352_start_sequential_first_last`
  (record sequential, at the oldest edition START FIRST/LAST exists at),
  `conformance:2023/pb352_start_sequential_first_last_line` (the LINE-sequential framing, 2023 because the
  organization is — kb/Work PB688) and the Sequential-row START cells of
  `conformance:2023/l1_table20_seq_relative`.
- **EVERY START FORMAT DECIDES THE KEY OF REFERENCE, AND THE EPILOGUE MAKES IT SAY SO** (kb/Work PB356).
  14.9.41.4 states the decision once per format and never as a default: GR16 for the KEY phrase ("The key
  specified in the KEY phrase, or that shares a leftmost character with the data item specified in the KEY
  phrase, becomes the key of reference"), GR18/GR19 for FIRST/LAST ("the file position indicator is set to the
  value of the primary key of the first [last] existing logical record in the physical file **and the key of
  reference is set to the primary key**"). So `IndexedConnector` completes every successful START through ONE
  epilogue, `StartSucceeded(int keyOfReference, KeyedRec found)`, **whose key of reference is a REQUIRED
  ARGUMENT** — a fourth START format cannot be written without answering the question, and an epilogue that read
  `_refKey` would have no correct reading at all. `StartFirstLast` passes `PrimeKey` and orders its search with
  `Ordered(PrimeKey)`: GR18/GR19 name the primary key TWICE, once as the ordering ("in the physical file") and
  once as the outcome. ⛔ Reading the inherited key of reference instead — justified by 14.9.27.4 GR14, a real
  clause about what OPEN establishes and not about what START FIRST/LAST does — made START LAST answer the
  ALTERNATE ordering's last record (with the two orderings inverted, the FIRST record), and left every following
  sequential READ walking that alternate (14.9.30.4 GR21 b)'s "Otherwise, the key of reference is set to the last
  key of reference in the file position indicator"); where the alternate carried SUPPRESS WHEN it additionally
  HID records that 12.4.5.6.4 GR6 withholds from the ALTERNATE path only and §14.9.41.4 GR18/GR19's primary-key view must
  include. The relative and sequential arms have no key of reference and pass no such argument. Goldens:
  `conformance:2002/pb356_start_first_last_indexed` (the inverted orderings, the four following prime-order
  READs, and GR18/GR19's empty-file '23') and `conformance:2023/pb356_start_first_last_suppressed` (the
  SUPPRESS WHEN records FIRST/LAST must still see); the SHAPE is held by
  `unit:StartKeyOfReferenceDriftTests`.
- **The START key operand is screened by TWO rules with one home each** (kb/Work PB354).
  `RecordLayout.KeyIndexOfKeyItem` answers "this IS a record key of the file" — by reference identity, or by
  12.4.5.12.4 GR4's identical BYTE POSITIONS in another record description entry of the SAME file (hence
  equal widths, never a prefix) — and it is what 14.9.30.3 SR11 (`READ … KEY`, which has no generic arm at
  all) and 14.9.41.3 SR6 a) ask. `RecordLayout.GenericKeyIndex` answers SR6 b) alone, and enforces all three
  of its conditions: leftmost-coincident *within a record of the file* (b 1.), the same class, category and
  usage as that key (b 2. — via the ONE 8.5.2.1 Table-2 classifier), and no longer than it (b 3.). SR4's
  "shall not be subject to any OCCURS clauses" is its OWN named check on BOTH organization arms, through
  `RecordLayout.IsSubjectToOccurs`, ahead of the position walk — it used to be a side effect of the offset
  walk bailing out, reported under SR6's message, and the relative arm had no such check at all. The same
  predicate supplies the three key clauses' identical bans (12.4.5.12.3 SR1 RECORD KEY, 12.4.5.6.3 SR1
  ALTERNATE RECORD KEY, 12.4.5.13.3 SR1 RELATIVE KEY) in `KeyedIoBinder.KeyedValidateFile`.
- **⛔ START'S SEARCH KEY IS CUT OUT OF THE RECORD AREA, AND THE THREE KEY-VALUED VERBS CUT IT THE SAME WAY**
  (kb/Work PB355). 14.9.41.4 GR17 a) — *"The specified key is set up by moving the relevant parts of the record
  area into a temporary data area"* — names the SOURCE (the record area) and, through GR16 (*"The key specified
  in the KEY phrase, or that shares a leftmost character with the data item specified in the KEY phrase, becomes
  the key of reference"*), the KEY. data-name-1's whole role is to name that key and, through GR17 b) (*"the
  length specified in the LENGTH clause, if specified, or else the length of record-key-name-1, if specified, or
  else the length of data-name-1"*), to supply a default length; it is never itself the source. So
  `KeyedIoEmitter.EmitStart` sends `OperandText.RecordSendImage` of `ReferenceResolver.RecordArea(file)` and
  `IndexedConnector.Start` slices the key of reference out of it with `KeyOf(Fit(image), keyIndex)` truncated to
  the GR17 b) count — **the same extraction the random READ (14.9.30.4 GR32) and DELETE (14.9.10.4 GR3) already
  make from the same image**, which is what makes a SOURCE-phrase key's several *parts* (12.4.5.12.4 GR2) fall
  out with no second mechanism. Nothing is padded: the only fill the model admits is the record area's own
  (`FitRecord`, per 14.9.30.4 GR15).
  **What the old shape cost.** The emitter sent `OperandText.AsStorageImage(sta.Operand)` and the connector
  space-padded it to the count. The two readings agree for every temporary area no longer than data-name-1 —
  13.18.33.4 GR3 makes the FD's level-1 entries *"implicit redefinitions of the same area"*, so a generic key's
  own content IS the area's content at those positions — so only `WITH LENGTH` counting PAST the operand told
  them apart, and there the padded reading searched for characters no record area holds. Under `KEY IS >` it did
  not even fail loudly: a two-character generic key with `WITH LENGTH 4` compared `"AB  "`, and `"AB01" > "AB  "`
  is true, so the statement positioned on the WRONG record and the following READ NEXT delivered it. On a
  NATIONAL key the pad was not even a character — one 0x20 byte where 14.9.30.4 GR15's national space is the
  pair 0x00 0x20. Goldens: `conformance:2002/pb355_start_length_past_operand` (the alphanumeric arms, including
  the silent-wrong-record one and the complement arms that pin the short case) and
  `conformance:2002/pb355_start_length_national_past_operand`; structure `unit:StartTemporaryKeyAreaDriftTests`.
  Below COBOL-2002 there is no LENGTH phrase, so the temporary area can never be longer than data-name-1 and the
  two readings coincide — the 85 regression net is `conformance:85/pb358_start_no_key_phrase`.
- **The GR17 b) LENGTH travels to the connector UNNARROWED** (kb/Work PB357). 14.9.41.4 GR14 is a predicate on
  arithmetic-expression-1 itself — *"If arithmetic-expression-1 does not evaluate to a positive nonzero integer that
  is less than or equal to the length of the associated key, the I-O status value … is set to '23'"* — and 14.9.41.3
  has no rule confining the expression to an integer, so `WITH LENGTH 2.5` is legal source whose result is '23'.
  `KeyedIoEmitter.EmitStart` therefore renders a `StartKeyLength` (runtime `IO/StartKeyLength.cs`) from the exact
  lane `SetEmitter.LandAmount` uses — the scaled `Int128` with its scale, or the native `double` — and
  `IndexedConnector.Start` asks `TryCompareLength(keyLength, …)` exactly once: integrality through THE ONE
  carrier-neutral landing `SetAmount.Land`, then 1 ≤ count ≤ key length, then the GR13 national-position scaling on
  the bounded integer. The former int narrowing of `Align(expr, 0)` truncated 2.5 to 2 and wrapped 4294967297 to 1,
  so the connector's bound test ran on a value already forced into range. Golden
  `conformance:2002/pb357_start_length_not_integer`; structure + failure proof `unit:StartKeyLengthTests`.
- **The record area is resolved in ONE place** — `ReferenceResolver.RecordArea(FileModel)`, the
  `FileModel.AreaRecord` lookup plus the `ResolveItem` step that five consumers each used to write out (the two
  keyed emitter sites, the sequential READ emitter, the two READ/RETURN `INTO` binders). 13.18.33.4 GR3 makes the
  FD's level-1 entries redefinitions of one area and 13.18.43.4 GR5 a) sizes it as *"the record size of the
  largest record description entry in this file description entry"*, which is why the LARGEST description is the
  view (a shorter `Records[0]` window truncates the splice — RL106A's 56/102 pair, ST111A's 50/75/100 SD). START
  built its comparand out of data-name-1 partly because the area was not something a verb could simply ask for.
- READ INTO lowers to the verb plus a typed MOVE whose SENDER is the current record, WRITE FROM to an ordinary typed MOVE plus the verb (D21; receiving uses the MAX length for ODO records, the ST146A lesson).
- Record length mismatch on READ (a fixed file whose physical record differs from the FD size) gives status 04; add for conformance since the legacy pads silently.
- LINE SEQUENTIAL: newline-framed, TrimEnd on WRITE, pad or truncate on READ, LastRecordLength is the line length; status **06 and 09 are both implemented** — 06 is the GR15 over-length truncation (the file position indicator keeps the unread remainder, NOTE 3), 09 the GR16 character-set warning below. LINE SEQUENTIAL itself is a COBOL-2023 introduction; see Per-edition gating.
- **The LINE SEQUENTIAL CHARACTER SET is ONE set behind FOUR rules** (`LineSequentialCharacterSet`, kb/Work PB329). Annex A.1 item 115 makes the set a REQUIRED, documented determination and the standard names it from four places: 14.9.30.4 GR16 / 9.1.13.2 item 7 (a SUCCESSFUL READ whose record area holds a non-member ⇒ '09', the record still delivered), 14.9.51.4 GR23 (WRITE ⇒ unsuccessful, '71'), 14.9.35.4 GR17 d) (REWRITE ⇒ unsuccessful, '71') and 9.1.13.10 item 1 (both write directions leave the record area — and the medium — unchanged). **The determination is: every character at code point U+0020 or above is a member; the C0 controls below it are not** (derivation and the GnuCOBOL survey at `docs/CONFORMANCE.md` DOC-A.1-115). Design consequences: (a) the set lives in ONE type and the connector reaches it through ONE predicate, `SequentialConnector.RecordAreaOutsideLineCharacterSet`, so the read arm and both write entry points (`Write`, `WriteAdvancing`) cannot diverge — before PB329 only REWRITE had an arm and it carried a private CR/LF test; (b) the subject is the RECORD AREA, tested CHARACTER-wise, so a national record area is read two bytes at a time as UTF-16BE exactly as `FitRecord`/`TrimRecordEnd` pad and trim it (a byte-wise test would refuse every national line sequential record); (c) GR16 is stated after GR15 and asks only that the read be successful, so '09' is the status that lands even on a truncated ('06') read.
- **The RECORD-AREA CATEGORY flag (`NationalRecordArea`) is set from ANY of the FD's record descriptions, not from the widest one.** 13.18.33.4 GR3 — "Multiple level 1 entries subordinate to a FD or SD entry represent implicit redefinitions of the same area" — makes them all descriptions of ONE area, and 14.9.51.4 GR21/GR22 key the trailing-space rule on *record-name-1*, so a WRITE naming the national record must shed national spaces whatever a sibling description says. Keying on the widest description alone (the original PB327 selector) silently answered "alphanumeric" for `01 R PIC N(4). 01 B PIC X(8).`, so that FD's WRITE shed one 0x20 with `string.TrimEnd` and left a seven-byte line ending in half a national position; the READ then re-padded alphanumerically to the same eight bytes, so the golden that was meant to pin the national fill never exercised it. ⛔ The flag is the AREA's category only (`FileModel.RecordAreaIsNational`, §14.9.30.4 GR15), never a WRITE's: GR21/GR22 (14.9.51.4) and GR17 c) (14.9.35.4) key on *record-name-1*, so the statement carries its own category (`FileModel.IsNationalRecord` of the record it names, rendered as `nationalRecord: true` on `CobolFile.WriteShared` / `RewriteShared`) and the connector's trailing-space strip (`TrimRecordEnd`, which GR22's DEPENDING phrase also disables), space fill, character-set screen and on-medium encoding of a written record take that parameter and never read the flag. An FD carrying both a national and an alphanumeric record description therefore writes each as what it is (golden `2023/pb1191_line_sequential_record_category`, kb/Work PB1191); `IsNationalRecord` is the one answer to "is this record description national?" behind the SORT fill, the area flag and the statement's category.
- CODE-SET translates only character (alphanumeric and DISPLAY-numeric digit) bytes, not COMP or COMP-3 binary fields (13.18.13); the default is the native ASCII set.
- LINAGE:
  - **⛔ A LOGICAL PAGE IS A PHYSICAL LAYOUT, NOT A COUNTER** (kb/Work PB523). 13.18.34.4 GR1 — "the logical page size is the sum of the values referenced by each phrase except the FOOTING phrase" — makes the logical page *top margin + page body + bottom margin*; GR4 and GR5 name those margins as lines OF that page; GR8 — "each logical page is contiguous to the next with no additional spacing provided" — lays the pages end to end on the medium. GR8 forbids spacing ADDITIONAL to the logical page; it does not delete the margins the page is defined to contain, and reading it that way (the design this bullet used to state) left GR4 and GR5 with no content at all, made GR1's "sum … except the FOOTING phrase" a sum of one term, and made 14.9.51.4 GR25 g) / GR26 a) — "the device is repositioned to the first line that may be written on the next logical page" — a bare `\f` or a one-line advance.
    **The model.** `SequentialConnector` tracks where on the current logical page the device sits, page-relative: physical page lines `1 … _top` are the top margin (GR4, never written on), `_top + c` is page-body line `c` (GR2) and `c` IS the LINAGE-COUNTER (GR7), `… + _pageBody + 1 … + _bottom` is the bottom margin (GR5). The device position is therefore exactly `_top + LinageCounter` and needs no second variable — except at the start of a page, where the counter already reads 1 while the top margin has not yet reached the stream; `_topMarginPending` is that one bit and `EmitTopMarginIfPending` materializes it, called FIRST by every travel (`PositionOnLogicalPage`) and every presentation (`Present`), so no write arm can reach a page body without it. **The lazy materialization is the one latitude, and 14.9.27.4 GR18 grants it** ("the positioning of the output medium with respect to physical page boundaries is implementor-defined following the successful execution of the OPEN statement, whether or not the LINAGE clause is specified"): an OPEN OUTPUT + CLOSE with no WRITE released nothing (14.9.51.4 GR12) and shall not acquire a page of blank lines; whenever a record IS presented the bytes equal the eager reading, and GnuCOBOL (`flag_needs_top`) defers it the same way.
    **One page transition, shared** — `BeginNextLogicalPage`, reached from 14.9.51.4 GR25 g)'s ADVANCING PAGE and GR26 a)'s overflow because the standard writes the same destination for both. It is split along 13.18.34.4 GR6 b)'s own sentence ("this occurs before the device is positioned and after all positioning on the current page"): (1) all remaining positioning on the CURRENT page — its unwritten body lines plus its bottom margin, against the OLD page model; (2) the operand re-evaluation, whose values "apply to the next logical page"; (3) exactly ONE line onto the next page, which is what GR8's contiguity makes it. **The travel is derived from the LOGICAL PAGE, never from the WRITE's own line count.** `Advance`'s form feed survives only for 14.9.51.4 GR25 h), the arm for a file with NO LINAGE clause. The page model dies at CLOSE (`EndLinagePage`): GR6 b) 1 and GR7 d) establish it at OPEN OUTPUT and at no other mode.
    An ADVANCING or LINAGE file is forced to line-oriented output, and **a WRITE with no ADVANCING phrase on a file that supports vertical positioning IS an explicit `AFTER ADVANCING 1 LINE`** (kb/Work PB1027; §14.9.51.4 GR25: "automatic advancing shall be provided by the implementor to act as if the user has specified AFTER ADVANCING 1 LINE"): `SequentialConnector.WriteRecord` reroutes it to `WriteAdvancingRecord(lines: 1, before: false)` in EVERY state of the device, for record sequential AND line sequential files, so there is one placement (GR25 f): advance, then present) and one counter step (GR7 c) 3)), with no second arm to disagree with it. This replaced kb/Work PB964's `ImplicitAdvanceIsBefore`, which placed the advance by the device's line state and took BEFORE's record-then-terminator shape when no line was open: that left a plain WRITE after a BEFORE write with no blank line where the spelled-out AFTER has one, put the first record of a LINAGE page one line high, and made a line sequential file travel twice around an AFTER write. **Which files support vertical positioning is the implementor's determination** (`SequentialConnector.SupportsVerticalPositioning`, docs/CONFORMANCE.md §4): a file with a LINAGE clause, and a file to which an ADVANCING phrase has been written since its OPEN (`_printControl`); any other file is unpositioned and its plain WRITE is the record and its own line end (a line sequential delimiter, a record sequential block). `PlainWriteAdvancingConformanceTests` holds the equivalence over every three-write mix. A BEFORE write after an AFTER write still presents on the open line (GR25 e): no advance precedes it), exactly as AFTER ADVANCING 0 LINES does (GR25 c)).
  - **One statement-supplied operand set for both forms** (13.18.34 GR6): every OPEN and every sequential WRITE entry takes a `LinagePage?` — `SequentialIoEmitter.LinageArg(file)` renders `new LinagePage(body, footing, top, bottom)` from the file model (literals fold to constants, GR6a; data-names render the EXECUTING element's field reads, GR6b) or `null` for an FD with no LINAGE clause. The runtime adopts the values at OPEN OUTPUT (GR6b1, with counter := 1 per GR7d — `FileRegistry.SharedOpenAttempt` → `SequentialConnector.BeginLinagePage`, at the COMPLETION of a successful open as GR6b1 says) and at the two page transitions — the ADVANCING-PAGE reset (GR6b2) and the overflow wrap (GR6b3), AFTER the overflow decision against the OLD body, because "the value applies to the next logical page". Evaluating data-names at these page transitions (not only at OPEN) is required by GR6b2/GR6b3 — evaluating solely at OPEN is a conformance hole (SQ208M/SQ210M). **⛔ The page model is connector state; the operand SOURCE is not** (kb/Work PB673): a connector-held evaluator closure belonged to whichever activation installed it last, which for a RECURSIVE unit with a LOCAL-STORAGE operand is a RETURNED activation's dead storage — and it was installed on the program path only, so a class's LINAGE FD (`OoEmitter`) had no page model at all. `HasLinage` is gone with it: "this FD has a LINAGE clause" is now exactly "the statement supplied a page".
  - **GR26 operational mapping** (`PositionOnLogicalPage` — the counter arithmetic ported from the legacy `AdvanceLinageCounter` and proven over the SQ goldens, now deciding the DEVICE with it because GR26 a) states both in one sentence): with post-advance counter c, body B, footing F — `c > B` ⇒ overflow end-of-page, counter := 1 (GR26a + GR7c4; the reposition lands on the next page's body line 1 through `BeginNextLogicalPage`, never a modulo carry); else `F > 0 ∧ c ≥ F` ⇒ footing end-of-page (GR26b). ADVANCING PAGE: the same transition, counter := 1, no observable EOP (SR18 bars PAGE+EOP). The counter advances in the CONNECTOR as part of EVERY write (EOP phrase or not), with the device and not separately from it, so an AT branch reads the post-advance counter whichever side of the presentation 14.9.51.4 GR25 e)/f) put the advance on.
    - ⚖ **`c == B` IS AN ADJUDICATED BOUNDARY, DECIDED AGAINST THE PRINTED ARM TEXT — do not "fix" either comparison to match GR26's words.** GR26 a) as printed fires at `c ≥ B` and GR26 b) is clamped to `c < B`; at `c == B` those two cannot both hold with §13.18.34.4 GR2 (all B lines may be written), GR3 (the footing area is [F, B] INCLUSIVE) and GR26's own lead sentence (the lines "do not fit within the current page body"). The strict `c > B` boundary above is the reading `docs/CONFORMANCE.md` §4 records as **DETERMINATION — the §14.9.51.4 GR26 a)/b) boundary at LINAGE-COUNTER = page size** (kb/Work PB686), with the survey (IBM, Micro Focus, GnuCOBOL-via-NIST) and the NIST SQ201M evidence. It is pinned at the boundary by `tests/conformance/2023/pb686_linage_gr26_boundary.cob` and its `85/` twin — **including the no-FOOTING arm**, because the literal reading would make the last body line unwritable for a file that never mentions FOOTING, and a FOOTING-only fixture cannot see that.
  - **LINAGE-COUNTER register** (8.4.3.14): runtime-sourced (`BoundLinageCounterRef` → `CobolFile.LinageCounter(name)`), never a synthesized storage item (only the IOCS modifies it, GR7b); qualified `OF/IN file-name` resolves via the grammar's dedicated alternative, unqualified requires exactly one LINAGE file (SR3/8.4.2.2, ambiguity is a bind-time diagnostic). `ReferenceResolver.Resolve` returns null for the register early (the qualified form's cobolWord is the FILE-name).
  - **END-OF-PAGE phrases** branch on `CobolFile.EndOfPage(name)` read in the `if` HEADER (a branch body may WRITE the same file — SQ208M); EOP is a SUCCESSFUL write (GR27a — status 00, no USE hook competition). Bind-time diagnostics: SR19 (EOP without LINAGE — the old silent-drop), SR18 (PAGE+EOP), SR13 (mnemonic ADVANCING on a LINAGE file).
  - **The GR6 b) VALUE RULES and EC-I-O-LINAGE** (kb/Work PB525 + PB526). `EvaluateLinage` is the ONE place the two value rules are applied — GR6 b) 1 `body > 0`, GR6 b) 2 `0 < footing ≤ body` **whenever the FOOTING PHRASE IS SPECIFIED** — and it is reached from all three GR6 b) evaluation points (the OPEN OUTPUT completion and both page transitions inside `BeginNextLogicalPage`).
    **The absent FOOTING phrase is ABSENT in the model, not zero.** `LinagePage.Footing` is `int?` and `SequentialIoEmitter.LinageOpExpr` renders `null` for an omitted phrase; TOP and BOTTOM stay `int` because GR1 says their values ARE zero when absent, so only FOOTING had two facts in one slot. With 0 encoding "absent", GR6 b) 2 could never be applied and a specified `WITH FOOTING AT` holding 0 silently took GR1's no-end-of-page reading — the program ran to completion printing NO-EOP where the footing area was written into. The `_footing > 0` guard on the GR26 b) arm is now `_footing is { } f`, a test of PRESENCE.
    **A violation is an exception condition, not a process kill.** GR6 b) 2 spells out three consequences and all three are realized: LINAGE-COUNTER is set to 0, the page model is discarded, and a `_linagePageBroken` latch holds until CLOSE so *"all subsequent WRITE statements referencing the file cause the EC-I-O-LINAGE exception condition to continue to exist"* — a latched WRITE returns before the record reaches the medium and before GR12's release. The latch is a SEPARATE bit from `HasLogicalPage`: read off the zeroed page model, every later write would take the no-LINAGE print-stream arm and SUCCEED. Both WRITE entries carry the check, because a plain WRITE on a LINE SEQUENTIAL LINAGE file does not route through `WriteAdvancing` (the two-arm dispatch this connector has been bitten by twice). The statement is ABANDONED where GR6 b) 2 abandons it — *"it continues with the statement following the WRITE statement"* — so an AFTER write never presents its record and a BEFORE write keeps the line it had already presented on the page whose values were still valid (GR25 e) puts the presentation first) but does not release it.
    **The raise itself is NOT made in the connector.** The connector reports an unsuccessful I-O status `'90'` (§9.1.13.11's implementor-defined value; docs/CONFORMANCE.md §7 **DOC-A.1-110**) together with the exception-NAME GR6 b) 2 gives it, through `FileConnector.SetIoCondition` — and the ONE EC-I-O channel every other I-O condition already uses, the emitted `__IoCheckEc` hook after the verb, owns the USE format-1 and format-3 tiers, the exception-checking PERFORM frames and the fatal default. **The name has to travel beside the status** because §9.1.13.1's status→EC correspondence has no entry for EC-I-O-LINAGE and would read a `'9x'` as EC-I-O-IMP; `FileConnector.IoConditionName` carries it, the one `Status` setter clears it so it cannot outlive its operation, and `FileRegistry.IoConditionName` is the one place the named condition and the correspondence are combined. `ExceptionCatalog.IoMaskNames` gained EC-I-O-LINAGE (append-only — the mask is a compile-time integer) so `>>TURN EC-I-O-LINAGE CHECKING ON` reaches a statement's mask.
    **The OPEN stays open.** A violation at the OPEN OUTPUT completion replaces the OPEN's STATUS only: GR6 b) 2's *"until the file is closed"* and *"all subsequent WRITE statements referencing the file"* both presuppose an open file connector, and the CLOSE is what ends the regime (`EndLinagePage` clears the latch with the page). Goldens `2023/pb526_linage_ec_condition` (+ the `85/` twin — the whole behaviour is reachable with no construct newer than 1985, since the status and the format-1 declarative carry it) and `2023/pb526_linage_ec_checking` (the `>>TURN` path, where FUNCTION EXCEPTION-STATUS proves the name is EC-I-O-LINAGE and not EC-I-O-IMP).
  - Conformance net: `LinageConformanceTests.cs` (per-GR: GR7c1–c4/GR7d, GR26a/b discrimination incl. c==B, GR6b1/2/3 timing, GR1 no-footing, qualified/ambiguous register, ADVANCING 0, SR13/18/19) — **plus the `Bytes_*` group, which reads the produced FILE** through `CobolNetCompiler.CompileRunAndReadFile` (margins + overflow, the zero-margin identity, AFTER and BEFORE ADVANCING PAGE, the BEFORE-phrase overflow ordering, and the LINE SEQUENTIAL arm). ⛔ That group exists because the rest of the class can only see stdout, and a rule about WHERE ON THE MEDIUM a record lands is invisible there — which is how kb/Work PB523 stayed green for the whole life of the connector. The corpus twins are `tests/conformance/2023/pb523_linage_margins_on_the_medium.cob` (writes two files, reads them back AS LINE SEQUENTIAL so each DISPLAYed index is a physical line number) and `tests/conformance/85/pb523_linage_margins_on_the_medium_85.cob` (LINE SEQUENTIAL is 2023-only, so the 85 twin reads the medium one character at a time and pins the file length and each record's byte offset).
- On-disk framing: a fixed record-sequential file is contiguous; a variable sequential, relative, or indexed file uses a 4-byte little-endian length prefix; a sparse relative file uses 0xFF gaps.
- DELETE FILE statement (14.9.10 Format 2): delete the host path and reset the in-memory map. A present file that is deleted gives 00 and an absent file gives 05 — BOTH successful (14.9.10 GR14/GR20); the error paths are 41 (the connector is still open, GR13), 62 (the physical file is open by another connector, GR15), and 37 (insufficient authority or the storage medium forbids deletion, GR16/GR17). A missing file is NEVER 35 — 35 is an OPEN-only status.
- Keyed record stores are PER PHYSICAL FILE, not per connector (kb/Work PB143; §14.9.10.4 GR5 — "removed from
  the physical file"): `KeyedStoreTable` (registry-owned, keyed by resolved host path — the same key
  `PhysicalFileTable` arbitrates sharing and locks by) holds ONE `RelativeStore` (RRN → image) or `IndexedStore`
  (the records plus the GLOBAL release-ordinal mint) per host. The FIRST opener loads from disk; later openers
  ATTACH to the live store (never reload — the in-memory store is the truth while any connector holds it); every
  DELETE/WRITE/REWRITE is instantly visible to every attached connector; any CLOSE persists the one shared state
  (close order cannot resurrect a deleted record or drop another connector's write); the LAST detach drops the
  entry so a later OPEN re-reads the disk; OPEN OUTPUT empties the shared view. Position/key state (FPI, key of
  reference, the §14.9.51 GR29a sequential-WRITE slot, the GR38 high-key) stays per-CONNECTOR. The store is
  unconditional — two SELECTs to one ASSIGN target reach it with no SHARING clause — and sequential connectors
  are out of its scope (their OS-backed streams are already the shared store). A connector constructed outside a
  registry (a focused unit test) keeps a private store.
- Indexed duplicate order is PER KEY OF REFERENCE, and the file position indicator is a KEY VALUE (kb/Work
  PB341, PB342). 14.9.30.4 GR26 scopes the release order to "an alternate record key that is the key of
  reference", and 14.9.35.4 GR24 a) freezes an untouched key's order across a REWRITE while b) moves the record
  last under a changed one - so the release ordinal is a VECTOR on `KeyedRec`, one slot per key (slot 0 the
  prime, assigned at release and never re-stamped, which makes it the record's physical release order too).
  WRITE stamps one fresh ordinal into every slot; REWRITE re-stamps only the keys whose value or SUPPRESS WHEN
  state changed, and "changed" is ONE predicate (KeyEq under the key's collating sequence, per GR24's closing
  sentence) that GR24 a), GR24 b), GR24's suppression sub-rules and the 9.1.13.2 2 c) '02' all read.
  14.9.41.4 GR17 e) 1. puts only a KEY VALUE in the file position indicator, so a START-seeded walk enters a
  duplicate set at the end GR26 names for ITS direction - first-released forward, last-released backward - and
  the duplicate-set position of a prior READ is separate connector state, which is what 14.9.30.4 GR21 rules
  e)/f) name instead of the indicator. CLOSE persists a TOPOLOGICAL order of the per-key duplicate orders
  (`IndexedConnector.PersistOrder`), so a reload - which can only give every key the file's own order -
  reproduces all of them; with no REWRITE repositioning that IS release order, byte-identical to before. RESIDUE:
  the per-key orders can be made mutually cyclic, and one sequence of record images cannot then carry them.
- I-O status discipline (kb/Work PB140): `FileConnector.Status`'s setter is the ONE assignment path — it records `EverAccessed` AND drops the §9.1.13.7 3) '43' gate (READ terminals re-arm through `ReadSucceeded`); openness is the ONE base `_openMode` bit, separate from the `OptionalAbsent` file-position state a CLOSE leaves unchanged (§14.9.6.4 GR6); `FileRegistry` throws on an unregistered or misrouted name (never a fail-open '00' — the SD/organization screens reject at bind time, COBOLNET1692/1693); `FileConnector.Close` maps OS failures to '30' (§9.1.13.6 item 1) with the sequential streams nulled either way; CLOSE WITH LOCK locks only on a successful close.
- Keyed-verb branch discipline (kb/Work PB325): the ACCESS MODE is the SOLE discriminator of a keyed verb's branch, and it lives in ONE place — `KeyedConnector.Access`, the abstract base `RelativeConnector` and `IndexedConnector` share. Every branch the standard draws inside a keyed verb is drawn on it (§14.9.51.4 GR29 a)/b), GR38/GR39; §14.9.35.4 GR5 vs. GR21/GR22/GR23; §14.9.10.4 GR2 vs. GR3/GR4), and the OPEN MODE enters only as the permission test that FOLLOWS — Table 20 (§14.9.27.4 GR8), whose unsuccessful cells §9.1.13.7 items 8 and 9 name. ⛔ An open mode in a branch PREDICATE inverts that dependency: `_access == Sequential || Mode == Extend` made the runtime's answer depend on an unenforced bind-time screen (§14.9.27.3 SR2) and turned Table 20's blank Random/Dynamic × WRITE × Extend cell into a successful append instead of item 8 b)'s '48'. The whole table is walked by conformance:2023/l1_table20_seq_relative + l1_table20_indexed and, for the cell conforming source cannot reach, unit:Table20WriteOpenModeTests.
- READ preconditions live in the BASE, once, in the standard's own order (kb/Work PB336). `FileConnector` owns
  the three of them and every organization's READ entry delegates: `ReadOpenModeGuard` (§14.9.30.4 GR2 /
  §9.1.13.7 item 7 → '47'), `SequentialReadGuard` (GR2, then §14.9.30.4 GR21's "if the previous READ or START statement for
  the file connector was unsuccessful … the I-O status is set to '46'", then §9.1.13.4 item 1 c)'s '10'), and
  `RandomReadAbsentOptionalGuard` (§9.1.13.5 item 3 b) → '23'). **The ORDER inside `SequentialReadGuard` is
  itself the rule**: the absent-OPTIONAL arm is an unsuccessful READ that arms the '46' poison, which is exactly
  what makes '10' apply only to a READ "attempted for the first time" — write the arms the other way and the
  '46' branch below is unreachable for the life of the open mode. Three private copies of this chain, all three
  in the wrong order, is what PB336 was; the goldens are `tests/conformance/{2023,85}/pb336_optional_absent_read_46*`
  and the structural gate is `ReadPreconditionOrderDriftTests`. The random-side guard is deliberately NOT the
  sequential one: GR21 opens "For a sequential READ statement" so a random READ never yields '46', and item 3 b)
  has no "first time" qualifier so every random READ on an absent optional file is '23' — but the failed random
  READ still arms the poison, because §9.1.13.7 item 6 b) ("The preceding READ statement …") is not restricted to
  sequential reads. START's own absent-optional rule (§14.9.41 GR5) is a different statement's rule and lives in
  each connector's own START body.
- **START's preconditions live in the same base, for the same reason** (kb/Work PB352). `FileConnector` owns
  `StartOpenModeGuard` — 14.9.41.4 GR1's "input or I-O" test, whose unsuccessful value is 9.1.13.7 item 7's
  '47' and whose blank cells are Table 20's — and the virtual `InvalidateFilePosition`, which is GR7 ("the file
  position indicator is set to indicate that no valid record position has been established"). The guard applies
  GR7 on the way out, because a refused START is an unsuccessful one: GR1's test had been written out FIVE
  times (twice in each keyed connector, once in the sequential arm) and every copy returned '47' leaving the
  indicator alone. A keyed connector overrides `InvalidateFilePosition` to clear its FPI VALIDITY BIT as well —
  its indicator is a key value plus a bit, where the sequential connector's is the stream position — and each
  connector's `StartFail` is that invalidation plus the invalid key condition's '23'.
- SAME AREA buffer-only and SAME SORT-MERGE AREA are no-ops in a managed runtime (pure memory-layout optimizations with no observable behavior).

## Per-edition gating (G1 — four compilers in one `cobol.exe`)

File I/O contains edition-varying constructs. Every edition-varying construct carries TWO co-equal obligations:
(1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every
edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. Rows cite
`docs/VERSION_CHANGE_REFERENCE.md`, the 130-row edition-change checklist (2002→2023 deltas ONLY — it has NO 85→2002
rows; derive 85↔2002 gating from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog); the
(construct × edition) cases land in the version test matrix (`docs/VERSION_TEST_MATRIX_DESIGN.md`, Phase 0 done).

- **DELETE FILE (14.9.10 Format 2) is NEW in 2023** (rows 58/78; E.3.3 items 15/35): rejected with a
  not-yet-introduced diagnostic under `--std` 85/2002/2014; its statuses 05/37/39/41/62 from DELETE FILE exist only
  at 2023. (Format-1 record DELETE is 85.)
- **READ PREVIOUS is not COBOL-85** (a 2002 introduction — derive from the 2002 standard): rejected at 85 on
  EVERY organization (the gate is one `IBoundRead` arm in the VersionConformancePass; it used to be two arms and
  the sequential one did not carry it — kb/Work PB334). FLAG-14 flags every READ PREVIOUS (row 108).
  ⚠ **The 2014→2023 after-OPEN change (row 29) is the INDEXED leg only.** Annex E.2 item 22 ("READ PREVIOUS
  statement following an OPEN statement. Ensure that an at end condition occurs.") is INFORMATIVE and names the
  rule it amended: §14.9.30.4 GR21's indexed sub-rule d.3, which in the 2023 text reads "If no such record is
  found or PREVIOUS is specified and the previous operation on the file was an OPEN statement, the at end
  condition exists." The RELATIVE and SEQUENTIAL sub-rule blocks print rule b) unamended — "the first existing
  record that is selected is made available, regardless of whether NEXT or PREVIOUS is specified" — so on those
  two organizations the after-OPEN behaviour is **identical at 2002, 2014 and 2023** and the connector takes no
  edition parameter for it. Both legs are now asserted at all three editions —
  `conformance:{2002,2014,2023}/pb334_read_previous_sequential` (sequential) and
  `conformance:{2002,2014,2023}/pb343_read_previous_relative` (relative, kb/Work PB343, which also removed the
  carve-out from the legacy engine's `RelativeFileHandler`). The relative golden's SPARSE-file phase is what
  makes rule b) falsifiable: with the lowest record at RRN 5, OPEN's indicator of 1 names an empty slot, so
  PREVIOUS and NEXT can only name the same record if the rule ignores the direction.
  ⚠ **The INDEXED leg IS gated, and the `<=2014` answer is the first existing record** (kb/Work PB344).
  `IndexedConnector.SelectSequentialRecord` asks `DialectBehaviors.IsActive(IndexedReadPreviousAfterOpenAtEnd,
  Edition)`; below 2023 the after-OPEN arm is DIRECTION-BLIND, so a READ PREVIOUS immediately after an OPEN
  makes the same record NEXT does available. Annex E.2 item 22's justification is what states the prior rule —
  *"the rule itself stated that the first record would be retrieved. The rule itself has been amended such that
  an at end condition would occur"* — and the conflicting prior NOTE it also mentions states no requirement
  (this standard is drafted to the ISO/IEC Directives Part 2), so rule-versus-note is not a normative
  contradiction and no owner decision was needed. Asserted by
  `conformance:{2002,2014,2023}/pb344_read_previous_indexed` (P1 differs by edition; P2/P3/P4 are the controls
  that do not).
- ⛔ **The OPEN-MODE USE-declarative tier is EDITION-INVARIANT — and that is a DETERMINATION, not an
  omission** (`VERSION_CHANGE_REFERENCE` rows 25 and 26; Annex E.2 item 19 a)/b); kb/Work PB344). §14.9.49.4
  GR6 b)–e) selects a declarative by the file's open mode, and Annex E.2 item 19 records that the previous
  standards did NOT run it (a) for an invalid key condition when no INVALID KEY phrase was written, nor (b) for
  a READ exception that is neither an invalid key nor an at end condition. **That reading was implemented as a
  `<=2014` guard and then WITHDRAWN**, because it does not survive its own sources: Annex E is INFORMATIVE;
  §E.1 scopes it to *"a list of the substantive changes between the previous COBOL standard and this Working
  Draft International Standard"* — ONE prior edition, so it says nothing about 1985 or 2002 at all; and item
  19's own justification classes the prior state as defective TEXT rather than a required behaviour — *"The
  previous COBOL Standard was not clear or missing processing of some I-O exceptions. This change clarifies or
  corrects that processing"*, each sub-item adding *"This appears to be an error in previous standards."* A
  silence is not a prohibition. For 1985 the behaviour is positively fixed the OTHER way by that edition's own
  validation suite: NIST CCVS `SQ122A`, `SQ136A`, `SQ137A` and `SQ138A` require the INPUT declarative to run for
  a '46' READ and `SQ148A` the OUTPUT one for a '47' READ — `SQ137A` fails with the literal remark
  "INPUT DECLARATIVE NOT EXECUTED" against ANSI X3.23-1985 VII-2 1.3.5 / VII-51 4.6.4(5) — and the guard made
  those five programs RED on CI (train 29, 2026-09-13) while every local gate was green. Both tiers therefore
  run at every edition, the generated selector carries no edition condition, and the USE hook takes no
  which-verb argument. Asserted by `conformance:{85,2002,2014,2023}/pb344_use_mode_invalid_key` and
  `…/pb344_use_mode_read_exception`, whose four copies are byte-identical BY DESIGN — that identity is the
  assertion — plus `conformance:2023/pb344_use_f3_perform_preempt` for GR6's leading exception-checking-PERFORM
  clause.

**The two mechanisms behind those rows.** (1) **`CobolNet.Runtime.DialectBehaviors` is the named register of
per-edition BEHAVIOUR changes** — the behaviour twin of `CobolNet.Editions.ConstructRegistry`. A construct row
answers *does this edition HAVE this construct?* and yields a DIAGNOSTIC; a `DialectBehavior` member answers
*which edition's RULE does this construct obey?* and yields an ANSWER, which is why it can never be a
`constructs.json` row: the source is legal at every edition. It lives in the RUNTIME because both sides read it
— the emitter folds a compile-time-decided behaviour, the connectors ask at run time over the edition the
compilation baked into them (`FileConnector.Edition`, carried per CONNECTOR from the registration's `edition:`
argument, since a run unit may link programs from separate compilations). Each member names the
`VERSION_CHANGE_REFERENCE` row that owns it, which carries a `<!-- behavior:id -->` anchor instead of
`<!-- todo -->`; `DialectBehaviorRegistryDriftTests` holds the two registers together in both directions.
⛔ **A member is ADMITTED only when the Annex E item says what the PRIOR RULE STATED.** Item 22 does (*"the
rule itself stated that the first record would be retrieved"*) and is the register's one member; item 19 a)/b)
says only that the prior text was missing or unclear, which is a silence, and rows 25/26 are `ref-only`. That
test is written on the `DialectBehavior` enum itself and in the `VERSION_CHANGE_REFERENCE` legend, because the
cost of getting it wrong was measured: five NIST programs red on CI and a whole landing dropped.
(2) **The GR3 a)/GR5 + GR6 b)–e) tier pair is rendered ONCE**, by `CodeGen/UseTierEmitter`, for all THREE
selectors that need it — `DispatchEmitter.__IoCheck`, `EcEmitter.__IoCheckEc` and `ProgramEmitter`'s GR4 b)
outward GLOBAL walk `__RunGlobalUse`. Both tiers are edition-invariant and the determination above is written
on that one emitter, so a fourth selector cannot acquire a per-edition guard by copy. That last clause is a TEST, not a hope:
`UseTierEditionInvarianceDriftTests` compiles one program — an outer with GLOBAL file-name and open-mode
declaratives, containing one with its own four-mode declaratives over an INDEXED file — at 85, 2002, 2014
and 2023 and asserts the EMITTED TEXT of `__IoCheck`, `__IoCheckEc` and `__RunGlobalUse` is byte-identical,
failing loudly if a selector is not emitted at all (a comparison of nothing is not evidence). Both of its
failure branches were fired once before it was trusted.
- **ORGANIZATION LINE SEQUENTIAL is a COBOL-2023 INTRODUCTION** (§12.4.5.10.3 GR2), so it is rejected at 85,
  2002 AND 2014 — `constructs.json` row `file-organization-line-sequential-2023`, gated on the clause's
  RECOGNITION by `VersionConformancePass.ParseArm.VisitOrganizationClause` (COBOLNET0900). **The edition IS
  derivable from the 2023 spec after all** (kb/Work PB688 corrected the earlier "not derivable — derive it from
  the 2002 standard" reading): the **Foreword's** list of the main changes this third edition makes over
  ISO/IEC 1989:2014 names “Line Sequential file organization” outright. Annex E happens to carry no item for
  it and `VERSION_CHANGE_REFERENCE.md` no row, which is what made it look underivable. §9.1.6 / §9.1.7.1 still
  name exactly THREE organizations, so LINE SEQUENTIAL is a *phrase* selecting the line-delimited type of the
  sequential organization (§9.1.7.2) — the `{ LINE | RECORD }` inner choice of the §12.4.5.10.2 general format.
  Two consequences the corpus had to absorb: every golden below 2023 that named the organization moved to
  `tests/conformance/2023/`, and **a report file WiseOwl COBOL writes cannot be read back at all below 2023** —
  the report writer frames a report file as host-newline-delimited text (CR LF on Windows, LF on Linux and macOS: DOC-A.1-159) whatever its ORGANIZATION, and only a
  line-sequential READ recovers those lines, so the 85/2002 report goldens that observed their report by
  re-reading it are 2023 programs. **`RECORD SEQUENTIAL` — the other half of the 2023 inner choice — gates from
  the SAME arm** (kb/Work PB706; `file-organization-record-sequential-2023`). The rendered §12.4.5.10.2 page
  (PDF p357) underlines LINE but NOT RECORD, so RECORD is an optional word (§5.2.3) and the grammar writes the
  arm `RECORD? SEQUENTIAL`: bare `SEQUENTIAL` is the same alternative and stays legal at every edition, and only
  the written-out RECORD token is the 2023 spelling. It binds to the one record-sequential value
  (`FileOrganization.Sequential`), never a fourth organization.
- **RECORD IS VARYING (13.18.43) is a 2002 introduction** (derive from the 2002 standard) — the 85 RECORD clause has
  only the CONTAINS forms; at 85 the VARYING phrase is rejected and `RECORD CONTAINS m TO n` drives
  `IsVaryingRecord`.
- **Format-2 in-place table SORT (D6) is a 2002 introduction** (derive from the 2002 standard): rejected at 85
  (file-format SORT is 85). Its KEY phrase is BRACKETED (Format 1 braces it — rendered, folio 746), so
  `SORT table-name` alone is conforming when the table's OCCURS clause has a KEY phrase (§14.9.40.3 SR15) and
  sorts on that phrase's keys in their written significance order and direction (§14.9.40.4 GR21), read from
  the ONE `OccursSpec.Keys` model SEARCH ALL also reads (resolved once, qualifiers and all, by
  `DataBinder.OccursKeyResolve` — kb/Work PB1018). A written table key and the table subject data-name-2 resolve
  through the §8.4.2.2 resolver too (`SubtreeCandidates` over data-name-2 / `ResolveTableOperand`): never the
  first same-named item. The grammar's key list is zero-or-more; SR15 and
  Format 1's at-least-one are screened in `SortBinder` on the resolved operand (kb/Work PB846). MERGE has no
  table format, so its key list stays one-or-more in the grammar. Note also row 27: MERGE newly prohibited in another MERGE's output procedure / a
  file-format SORT input-output procedure at 2023.
- **OPTIONAL reach differs by edition** (85 restricts which organizations/open modes admit OPTIONAL; 2002 widens
  it) — the edge-case table above states the 2023 behavior; derive and gate the 85 subset from the 1985 standard.
- Gating lives in the BINDER off the ONE `DialectMode` (SSOT §14.11): a construct the target edition lacks never
  reaches the bound tree (it is a `COBOLNET-` diagnostic); the connectors/codec implement the union of editions, and
  per-edition BEHAVIOR variants (e.g. READ PREVIOUS after OPEN) are bound-tree/runtime-parameterized by edition,
  never duplicated per backend. The NIST corpus compiles at `--std` 85 and is the 85 positive net; the
  rejected-construct negative cases are version-test-matrix work, not NIST work.

## Declined optional elements of the I-O statements and the file description entry

Two Annex A.4 modules of the file-handling surface are OPTIONAL language elements (§4.2.7) for which this
implementation claims NO support, per the owner's documented-non-support decision recorded in
`docs/CONFORMANCE.md` §5. They are **recognized and refused by name at bind**, never parse-errored and never
compiled inert:

| Module | Elements | Where it parses | Refusal |
| --- | --- | --- | --- |
| A.4.8 | FORMAT clause (§13.18.24) on an FD; SELECT WHEN clause (§13.18.51) on a record description entry | `formatClause` in `fileDescriptionClause`; `selectWhenClause` in `dataDescriptionClause` (`Core/CobolData.g4`) | `COBOLNET1705`, from `DataBinder` — the FD clause loop and `BindEntry` |
| A.4.13 | the `FILE file-name-1` alternative of WRITE (§14.9.51.2, **both** formats) and REWRITE (§14.9.35.2) | the `FILE fileName` alternative of `writeStatement` / `rewriteStatement` (`Core/CobolIO.g4`) | `COBOLNET1706`, from `SequentialIoBinder.DeclinedFilePhrase` — ONE site, both verbs |

**Why recognize-and-refuse, and why an ERROR rather than the accept-inert band.** Annex A.4.1: *"An
implementation shall accept the syntax and provide the functionality for an optional element only when
support for that language element is claimed by the implementor."* Unclaimed ⇒ the syntax is not accepted. A
parse error would satisfy that letter but not the product bar — it names nothing the user can act on, and it
is what the traceability inventory refuses to close a DOCUMENTED-NON-SUPPORT row against. So the grammar
carries the full printed general format (rendered from the PDF, not read off the OCR) purely so the binder
can name what it saw.

The severity is the point of difference from `COBOLNET1560/1578/1579/1580`, which ACCEPT their facility inert
and warn. Those facilities are **additive** — a program means what it says with the screen, message, commit
or validate behavior simply absent. These two are not: an inert FORMAT changes which bytes reach the medium
(§13.18.24.4 GR1 — external media format), an inert SELECT WHEN selects the wrong record description entry
(§13.18.51.4 GR1/GR2, with the §9.1.13.7 rule-5 status-45 path), and the FILE phrase carries its own
implicit-record model (§14.9.51.4 GR8, §14.9.35.4 GR9) that a whole-record-area write does not implement.
Compiling them inert is a **wrong answer**, not a missing facility.

**One mechanism.** Every A.3/A.4 decline — accept-inert and refuse alike — routes through
`EditionContext.Declined(descriptor, seen)`, and the DESCRIPTOR's severity chooses the disposition. There is
no local strictness test at any site. `--permissive` does **not** move an A.4 decline in either direction: it
is the migration seam for constructs an edition REMOVED (`EditionContext.Removed`), and a declined optional
element has no pre-removal semantics to preserve.

**Edition posture.** The decline is edition-INVARIANT — the module is unclaimed at 85, 2002, 2014 and 2023
alike, and the negative witnesses carry `*> reject-at: 85 2002 2014 2023`. One lexical consequence differs at
85: §8.9 reserves `FORMAT` only from 2002 (`ReservedWords.Table`, `r85=false`), so at `--std 85` the word is a
legal user-defined name. That is preserved by the `nameSlot` row in `tests/version-matrix/cobol-words.json`
— measured: `01 FORMAT PIC X(4).` plus a subscripted `FORMAT-E (2)` compiles and RUNS at `--std 85`, and
draws `COBOLNET0901` by name at 2002/2014/2023 through the existing §8.9 funnel. No `constructs.json` row is
owed: that register's contract is source that COMPILES CLEAN at its introducing edition, which a permanently
declined element never does — the same reason VALIDATE, the SCREEN SECTION, MCS and COMMIT/ROLLBACK have no
rows either.

**What is NOT declined.** A.4.1 NOTE 1: *"The higher-level constructs or cross-referenced topics are not
optional."* The WRITE and REWRITE statements themselves are mandatory, fully supported surface; only one
alternative of `{ record-name-1 | FILE file-name-1 }` is declined. `tests/conformance/85/`
`a413_declined_phrase_positive_control` is the run-and-compare witness for that half, and it also pins the
optional word `RECORD` of §14.9.35.2 (not underlined in the printed format, therefore optional) which this
grammar did not accept at all before 2026-09-02.

## The `record-name-1` operand — ONE rule for WRITE, REWRITE and RELEASE

Three statements print `record-name-1` in their general format, and all three say the same thing about it:

- WRITE §14.9.51.3 SR5 — *"Record-name-1 is the name of a logical record in the file section of the data
  division and may be qualified."*
- REWRITE §14.9.35.3 SR1 — the identical sentence.
- RELEASE §14.9.32.3 SR1 — *"Record-name-1 shall be the name of a logical record in a sort-merge file
  description entry and it may be qualified."*

**The rule has ONE home: `StatementValidation.ResolveRecordName`**, which returns the owning `FileModel` or
false having reported (COBOLNET1757), in `ResolveFile`'s shape. It enforces two things:

1. **Identity, not containment.** The reference shall BE one of the file's `Records`. Its predecessor,
   `SequentialIoBinder.FileOfRecord`, walked the reference's `DataItem` up to its top-level 01 and tested THAT
   root, so what it enforced was *the reference lies somewhere inside a record* — and every subordinate item
   passed, at all three verbs, with no diagnostic at any `--std`. The released/written width is taken from the
   REFERENCE, so the wrong operand produced a wrong-**width** record rather than anything loud: `RELEASE
   SR-DATA` on an 8-byte SD injected a 5-byte image, space-extended to 8, as a record the program never
   released. The upward walk survives as the message's explanation — naming the record the operand is
   subordinate to is the repair.
2. **No reference modification.** A record-name is a user-defined word (§8.3.2.2.25); §5.2.4's operand table
   gives that operand type *"User-defined word, including qualification and subscripting if needed"* and
   nothing further, and §8.4.3.3.3 SR5 permits reference modification only *"anywhere an identifier referencing
   a data item of class alphanumeric, boolean, or national is permitted"* (its NOTE: *"where data-name-n is
   used in a general format or syntax rule, then reference-modification is not permitted"*). The printed
   RELEASE format writes `record-name-1`, not `identifier-1`. A reference modifier rides on the `RefModPlace`
   DECORATOR and leaves `Place.Item` untouched, which is exactly why a containment test could not see it.
3. **A visible record whose FILE is not visible.** §13.18.27.3 SR1 b) lets a file-section level-1 entry carry
   its OWN GLOBAL clause, so under a non-GLOBAL FD the record-name is a global name in contained programs while
   the file is not. WRITE §14.9.51.3 SR21 and REWRITE §14.9.35.3 SR3 are written for exactly that reference
   (*"the file description entry for the file associated with record-name-1 shall contain a GLOBAL clause"*);
   each caller passes its own sentence as `containedRule`, and the owning FD is found in
   `DataBinder.ContainerLocalFiles` — the containers' non-global files, recorded beside the GLOBAL-FD merge
   (kb/Work PB1193). RELEASE passes §13.18.27.3 SR3, the general rule the other two specialise.

RELEASE's second half — *"in a **sort-merge** file description entry"* — is RELEASE's alone and stays in
`CheckReleaseRecord`, which is now asked only of a reference that already IS a logical record and so takes a
non-null `FileModel`.

**Grammar unchanged.** `releaseStatement : RELEASE dataReference` and `recordName : dataReference` stay
general. Both halves of one syntax rule belong in one place, and the subordinate-item half cannot be decided by
a grammar at all; narrowing `recordName` would also turn a citable diagnosis into a parse error.

**Staging.** ISO §4.2.2 ¶2 makes the compile-time indication mandatory for *"violations of the general formats
and the explicit syntax rules of standard COBOL"*. WRITE and REWRITE previously staged an unresolvable record
to `BoundUnsupported`, so `WRITE WS-REC` drew only the COBOLNET1756 **deferral warning** — the compiler
announcing ITS OWN gap for what is the source's error. The two arms are now separate: a name that resolves to
nothing already has COBOLNET1639 (§8.4.2.1) from the resolver and stays a deferral; a reference that resolves
but is not a logical record is refused at bind.

**Edition posture.** Edition-INVARIANT — the rule is written identically in 1985, 2002 and 2014, so there is no
gate and no `constructs.json` row. Witnesses: `tests/conformance/negative/pb347-*` (seven cases, three verbs ×
the subordinate / reference-modified / not-a-file-record faults, each `*> reject-at: 85 2002 2014 2023`) and
the acceptance twins `tests/conformance/2023/pb347_record_name_identity` and
`tests/conformance/85/pb347_record_name_identity_85`, which pin that the qualified `OF` form still binds at all
three verbs and that a SORT returns exactly the records RELEASE named. (kb/Work PB347)

## The `file-name` operand — ONE resolution that takes the PARSE NODE, and ONE shape rule

Every procedure-division statement that names a file resolves it through `StatementValidation.ResolveFile`
(§8.4.2.1, COBOLNET1639 for a word that names no file; the §13.4.5.3 SR9 report-file screen beside it). It
takes the operand's PARSE NODE, never a string, in two overloads for the two productions that reach it:

- `fileName` (a bare `cobolWord`) — CLOSE, READ, START, DELETE, DELETE FILE, UNLOCK, RETURN. The grammar already
  admits only the word.
- `dataReference` — OPEN (`openFileSpec`), the SORT and MERGE subjects, and the SORT/MERGE USING and GIVING
  lists. These positions share the production with a data-name (the SORT subject is a table in Format 2) or keep
  a trailing phrase apart with it, so the parse is a superset and the overload asks
  `StatementValidation.ScreenFileNameOperand` FIRST. FUNCTION EXCEPTION-FILE / EXCEPTION-FILE-N ask the same screen
  of a sole-reference argument.

**The shape rule (COBOLNET2972).** A file-name is a user-defined word naming a file connector and is written alone:
§8.4.2.2.2 prints no qualified format for it (it appears only as the `file-report-qualifier` of another name), and
subscripting (§8.4.2.3.1) and reference modification (§8.4.3.3.1) decorate identifiers. Until kb/Work PB2040 these
callers handed the resolver `dataReference().GetText()` — ANTLR's GLUED text — so `OPEN OUTPUT A OF B` silently
opened a file named `AOFB`, while SORT and MERGE took the base word and dropped the qualifier in silence. A position
that may hold either a file-name or a data-name decides by the BASE WORD first and asks the shape only of the file
reading: the SORT subject (a table's data-name-2 may be qualified), and the I-O-CONTROL APPLY COMMIT operand list
(§12.4.6.3.2's `[file-name-1][identifier-1]`, where a qualified operand is simply identifier-1). Witnesses:
`tests/conformance/negative/pb2040-*` (`*> reject-at: 85 2002 2014 2023`).

## An SD's records in an input-output statement — §13.4.6.3 SR3 and SR4, two rules, two screens

§13.4.6.3 SR3 keeps an SD **file-name** out of every input-output statement, and SR4 keeps its **record
description entries** out of them "other than following the word FROM or the word INTO". The input-output
statements are the eight §9.1.13.1 names as those whose execution sets the I-O status: CLOSE, DELETE (both
formats), OPEN, READ, REWRITE, START, UNLOCK and WRITE.

- SR3, and the WRITE / REWRITE record-name slot (where the record names its file): `StatementValidation.ScreenSortMergeFile`
  (COBOLNET1692), asked by each verb binder of the file it resolved.
- SR4 everywhere else: `StatementValidation.ScreenSortMergeRecordReferences` (COBOLNET2973), asked ONCE per
  input-output statement by `StatementBinder.BindStatement`, the funnel every statement passes through. It walks the
  statement's parse subtree, subscripts included, skipping the FROM / INTO phrases, the nested imperative statements
  (each screened as its own statement) and the record-name / file-name operand itself, and refuses any reference
  whose record is an SD record (the record description entry is the 01 and everything under it). A new operand
  position in any of the eight is therefore screened without a verb binder remembering to ask: until kb/Work PB1291
  only the record-name slot was, and `WRITE … AFTER ADVANCING SRT-N LINES`, a START KEY or a RETRY count naming an
  SD item compiled clean. Witnesses: `tests/conformance/negative/pb1291-sd-record-advancing` and the permitted arm
  `tests/conformance/85/pb1291_sd_record_from_into_85`.

## The COLLATING SEQUENCE clause — SR8 counts CLAUSES, not occurrences

§12.4.5.7.3 SR8 says *"Neither data-name-1 nor record-key-name-1 shall be specified in more than one COLLATING
SEQUENCE clause"*, and the unit it counts is the **clause**. §12.4.5.7.2's Format-2 figure prints
`COLLATING SEQUENCE OF { data-name-1 | record-key-name-1 } … IS alphabet-name-3` with the ellipsis immediately
right of the closing brace, so per §5.2.7 the repeated portion is the brace group and no rule adds a distinctness
requirement to the repetition: **one clause may list the same key twice, and one clause is never *more than
one*.** §12.4.5.7.4 GR6 is indifferent to the repeat — *"Alphabet-name-3 applies to record keys identified by
data-name-1 or record-key-name-1"* names the clause's own alphabet either way.

`DataBinder.ResolveFileCollating` therefore screens SR8 with the shared `ConstructOperandRegister<string>`
(`Binding/ConstructOperandRegister.cs`), whose construct here is the Format-2 CLAUSE, per file control entry —
never a `HashSet` hoisted out of the loop over `FileModel.KeyLevelCollating`, which is what the register exists to
make impossible: that shape screened per NAME across every clause at once and rejected
`COLLATING SEQUENCE OF IX-KEY IX-KEY IS REV` with a diagnostic (COBOLNET1582) whose own text — *named in more
than one COLLATING SEQUENCE clause* — the program falsified. The same register enforces §14.9.49.3 SR7/SR8/SR9/SR14
over the USE statement (`COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md`, kb/Work PB364); it is one mechanism for every
rule whose boundary is a construct and whose subject is an operand written inside it. Because the register
remembers the LAST clause that named a key rather than the first, ONE violation is ONE diagnostic even when the
offending clause also repeats the key internally.

**Edition posture.** The file-control COLLATING SEQUENCE clause is a COBOL-2002 addition (`constructs.json`
`file-collating-clause-2002`, COBOLNET0900 at `--std 85`), so SR8's behaviour lanes are 2002/2014/2023 and 85 is the
gate lane. Witnesses: `conformance:FileCollatingSequenceSpecTests` (both arms per edition, the
one-diagnostic count, and the 85 gate), the positive golden
`tests/conformance/2002/pb703_collating_key_named_twice` (a key named twice in ONE clause, whose output proves
GR6 still gives each key its own clause's alphabet) and the negative
`tests/conformance/negative/pb703-collating-key-in-two-clauses` (`*> reject-at: 2002 2014 2023`). (kb/Work PB703)

**The sequence follows the KEY'S CLASS, and the alphabet operands are screened as written** (kb/Work PB1074).
§12.4.5.7.4 GR2/GR3 give alphabet-name-1 to the keys of class alphanumeric and alphabet-name-2 to the keys of class
national; GR4/GR5 give each class its own native sequence otherwise. `DataBinder.ResolveKeyCollating` therefore
takes the resolved key ITEM and resolves `key-level ?? (national ? alphabet-name-2 : alphabet-name-1)`; it was
key-class-blind (`key-level ?? alphabet-name-1` for every key), so an alphanumeric alphabet re-ordered and
re-judged the uniqueness of a national key, and alphabet-name-2 was stored and never read. SR1, SR2 and SR7 are
syntax rules about the CLAUSE, so `ResolveFileCollating` screens them before any key is resolved, through ONE class
test (`CollatingAlphabetFault`) — never lazily inside the per-key resolution, where a shadowed alphabet-name-1
escaped. A NATIVE or UCS-4 national alphabet is the native national sequence and resolves to it; a national
alphabet defined by literals or LOCALE on a national key is Annex A.3 item 41, not provided, and is declined BY NAME
through `EditionContext.Declined` (COBOLNET1584, severity Error — an inert compile would order by a different
sequence than the one written). Witnesses: `2002/pb1074_file_collating_key_class`,
`negative/pb1074-file-collating-alphabet-class`, `negative/pb1074-file-collating-national-declined`.

## The SUPPRESS WHEN literal-1 — a literal POSITION, sized to the key at bind time

`SUPPRESS WHEN literal-1` (§12.4.5.6.2) is read like every other literal position — the VALUE clause's literal-n
and the PICTURE EDITING literals — through `DataBinder.RawValueOperandText`: the grammar takes `valueClauseOperand`,
so a constant-name (§13.10.3 SR2) and a symbolic-character (§8.3.3.6.2 Format 7) are accepted and a concatenation
expression folds (kb/Work PB1072). `DataBinder.ReadSuppressWhen` records the operand's SHAPE (`SuppressWhenOperand`),
which §12.4.5.6.3 SR7's three `FileControlKeyRules` rows screen; `DataBinder.KeySuppressionOf` builds the §12.4.5.6.4
GR6 value in the KEY'S OWN POSITIONS — a figurative in the key's category repeated to its size (§8.3.3.6.4 GR2), ALL
literal-1 repeated, a short literal space-extended and a long one cut back only over spaces (§8.8.4.2.7 2)) — and
the emitter encodes a national value through the one national serializer. The connector then compares two key
images; sizing the relation's operands is the compiler's job because only it knows the literal's form and the key's
category.

## ISO citations

- ISO/IEC 1989:2023 section 12.4.5.7 COLLATING SEQUENCE clause: 12.4.5.7.2's two general formats (file-level and key-level), 12.4.5.7.3 SR3 (at most one file-level clause per file control entry), SR4/SR5 (a Format-2 name shall be a declared RECORD KEY or ALTERNATE RECORD KEY), SR8 (a key in at most one clause) and 12.4.5.7.4 GR2–GR6 (which sequence applies to which key), with 5.2.7 for the ellipsis that makes the Format-2 brace group repeatable.
- ISO/IEC 1989:2023 section 9.1.13 I-O status: two-character codes; first digit 1 at-end, 2 invalid-key, 3 4 7 9 fatal or exception; subsections 9.1.13.2 through 9.1.13.11 enumerate every code (00 02 04 05 06 07 09 10 14 21 22 23 24 30 34 35 37 39 41 42 43 44 46 47 48 49).
- Section 9.1.2 record area plus its NOTE: all 01s under an FD or SD implicitly redefine the same storage area per 13.18.33 GR3.
- Section 9.1.1: CODE-SET or FORMAT translation occurs only when a logical record transfers to or from the physical unit; padding is added or deleted as necessary.
- Sections 13.18.13 CODE-SET, 13.18.34 LINAGE, 13.18.43 RECORD VARYING DEPENDING ON, 13.18.41 implicit default record.
- Section 9.1.6 fixed file attributes (the attributes fixed when a physical file is created), with 14.9.27.4 GR10 (the OPEN statement's comparison against them and its '39'), GR17 and GR18 (the two cases in which the OPEN statement CREATES the file), 9.1.13.6 item 7 (the '39' status itself) and Annex A.1 items 50 and 129 (the two required, required-to-be-documented validated-set determinations — DELETE FILE's and OPEN's, which are not the same answer). See D10.
- Sections 14.9.30 READ, 14.9.35 REWRITE, 14.9.41 START, 14.9.51 WRITE, 14.9.10 DELETE and DELETE FILE, 14.9.40 and 14.9.24 SORT and MERGE, 8.8.4.2.4 numeric-key value compare, 8.8.4.2.7 alphanumeric compare with shorter-operand space-extension, 14.9.49 USE statement (declarative).
- Section 4.2.7 optional language elements and Annex A.4 (A.4.1 general + NOTE 1, A.4.8 items 1–2, A.4.13 items 1–2) — the declined-element section above; §13.18.24 FORMAT clause and §13.18.51 SELECT WHEN clause are the two A.4.8 clauses, §14.9.51.2 / §14.9.35.2 carry the A.4.13 FILE phrase.

## Open questions — RESOLVED (`COBOLNET_DESIGN.md` §15 #3 + §18, owner-confirmed 2026-06-08)

> Status of each: **Q6** — settled, native `long`/`Int128` (see below). **Q7** — settled by §18 #1: a `byte[]` is
> sanctioned ONLY at the external-medium boundary, serialized via `IRecordCodec`; the connector's confined byte-image
> payload IS that boundary, and the all-typed store remains a one-line flip. **Q1/Q2** — stand as the owner-reviewed
> mechanical defaults (SSOT §15 #3 Q-file-1/-3): internal framed format + in-memory load/flush for v1, a pluggable
> file-format provider / on-disk backend later. **Q4/Q5** — see the edge-case notes above.

- Q6 cross-subsystem foundational — **RESOLVED** (`COBOLNET_DESIGN.md` §18 #2/#3/#19/#22, owner-confirmed 2026-06-08): the numeric substrate is native unscaled `long` (≤18 digits) / `Int128` (19–38 digits), never `decimal`. The files codec is designed to exactly that lock — no conflict remains.
- Q7: the relative and indexed connector holds the file on-disk image as a byte array per record in memory (payload only; deserialized to the typed record only on hand-back; ordering by the typed CobolKey). This is the exact spot the owner objected to for program data. Accept the legitimate-confined-file-bytes framing, or switch to an all-typed sorted-dictionary-of-typed-records store? Lean: byte-image payload; a one-line flip if the owner prefers all-typed.
- Q1: the on-disk format for relative, indexed, and variable-sequential files is an internal framed convention (4-byte little-endian prefix, 0xFF gaps), not a standard interchange format. Keep for v1, or does commercial quality demand an interoperable format such as a real ISAM or GnuCOBOL-compatible layout? Recommendation: keep plus add a pluggable file-format provider later.
- Q2: indexed and relative persistence loads the whole file into memory on OPEN and flushes on CLOSE, fine for batch and NIST but not for multi-gigabyte files. Scope v1 to in-memory plus a later pluggable on-disk B-plus-tree or SQLite-backed backend?
- Q4 — **RESOLVED: implemented, not deferred.** LINE SEQUENTIAL status 06 landed with the GR15 remainder machinery, and 09 landed with kb/Work PB329 together with the Annex A.1 item 115 determination it delegates to (`docs/CONFORMANCE.md` DOC-A.1-115) and the WRITE-side '71' arm (14.9.51.4 GR23) that had never existed. See the LINE SEQUENTIAL edge-case notes above.
- Q5 minor: confirm SAME AREA buffer-only and SAME SORT-MERGE AREA are acceptable no-ops in a managed runtime.
