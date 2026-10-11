## Entry 1308 — 2026-08-18 06:11 PDT — PB80 + PB89 land: a BASED occurs-depending group is an ODO operand; SET ADDRESS OF … TO NULL parses

**PB80.** `MOVE BODO TO OUT` over a BASED occurs-depending group copied the MAXIMUM image (`ABcccdddeee`), the
WORKING-STORAGE twin the GR8a slice (`ABcccddd`); `FUNCTION LENGTH(BODO)` was the named PB61 loud stage. A BASED root
is forced to a string-canonical class (`ForceStringCanonical`), and the resolver's class-tier branch returned its
`RedefViewPlace` BEFORE the step that wraps an occurs-depending group as an `OdoGroupPlace` — so MoveEmitter's GR8
slice, `SendingImage`, the LENGTH r4b term, none of it saw the ODO. §13.18.38.4 GR8 does not care how a group is
stored.

- **One wrap rule.** `ReferenceResolver.WrapIfOdoGroup` applies to the class-tier `RedefViewPlace` and the struct
  `MemberPlace` alike (`OdoModel.WrapGroup` takes any `Place`). A REDEFINES-class ODO group cannot be declared
  (§13.18.44 forbids redefining a variable-size item), so BASED is the only class-tier shape the rule reaches — but
  the rule is written for the SHAPE, not the case.
- **One image reader.** `PlaceRenderer.GroupImage` — a record struct's `AsImage()`, a window's `Read`, a wrapper's
  inner — used by `SendingImage`, `ReceiveInto` (which now stores through `WriteGroupImage`), `WriteGroupImage`'s
  ODO arm, the group ref-mod channel and CALL's boundary read. A consumer that spelled `.AsImage()` itself was
  wrong for the window shape.
- **One storage-boundary store.** The RL210A NIST program (an FD record holding an ODO table) broke the first gate:
  its record area is now an `OdoGroupPlace` over a class-tier window, and four emitters spelled
  `Read(record).FromImage(…)` themselves — CS1061 on the string. `PlaceRenderer.WriteFullGroupImage` (the WHOLE
  image — a READ into a record area, a FILE STATUS group, CALL's copy-in/out — never GR8's slice; unwraps the ODO
  wrapper) replaces those spellings; UNSTRING's group receiver takes the ONE `WriteGroupImage`. No PLACE-based
  emitter spells `.FromImage(` any more (the codec generator and OoEmitter's fresh method-LINKAGE root call the
  record struct's own member).
- **LENGTH r4a.** `BoundOdoExtent.BasedAddress` carries the entry's data-address field so an UNASSOCIATED based
  entry answers §15.50.4 r4a / §15.14.4 r2a's MAXIMUM (GR8b) through the runtime's null test — BEFORE data-name-1 is
  read (a null data address traps in `CobolPtr.Deref`). The first draft's `__addr_X is null` guard was ALWAYS
  false: the null pointer is the `ManagedPointer.Null` singleton, never a C# null. `RuntimeApi.PtrIsNull` is now the
  ONE spelling of that test.
- **Measured** (golden `pb80_based_odo_group`, the WORKING-STORAGE twin as the control on every row): LENGTH 17
  unassociated / 8 associated / 17 again after `SET ADDRESS OF BODO TO NULL`; MOVE BODO TO OUT = ABcccddd; a MOVE
  into BODO writes only the current part (BT(3) keeps "eee"); `IF BODO = "XYZ12345"` compares the current part;
  BODO(2:4) = "YZ12"; NN = 3 sends 11 characters. Verdicts RV-15.50.4-4, RV-15.14.4-2 (PARTIAL → CONFORMS),
  GR-13.18.38.4-8, SR-14.9.39.3-19 → CONFORMS (GAP 4019 → 4015).

**PB89** (found writing the golden). `SET ADDRESS OF BODO TO NULL` was `COBOL0001: no viable alternative at input
'NULL'` — §14.9.39.3 SR19: identifier-6 "shall be the predefined address NULL or shall reference a data-pointer".
The `setAddressStatement` receiver arm is `TO (dataReference | NULL_)`; `PtrBinder` binds the NULL form as
`BoundSetAddressOfBased(based, Source: null)`; `PtrEmitter` assigns `ManagedPointer.Null` (§13.18.5 GR2's initial
state — the item is disassociated). `FREE` stays pointer-only, as the standard has it.

**Gates (wave-local, after the RL210A repair).** Solution build · Characterization 33/33 · Conformance
`Corpus|Negative|Odo|Occurs|Based|Pointer|Ptr|Length|Move|Group|SpecTraceability|Nist|Call|Set|Allocate|Redef|
Unstring|String|File|Sequential|Relative|Indexed` **1600/1600** · Unit
`SpecTraceabilityInventory|Odo|Place|Registry|Manifest|Guard|Ptr|Pointer|Image|V59|Grammar|Parser` **400/400** ·
CLI probes (RL210A byte-equal to its golden; the PB80 golden). Docs: COBOLNET_DATA_MODEL_DESIGN.md (the Place
catalog — OdoGroupPlace over both storage shapes, the storage-boundary store), kb/Work PB80 + PB89 → landed, plan
§0. **The comprehensive battery for the PB88 + PB80/PB89 batch runs next.**

