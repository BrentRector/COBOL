## PB2690 closed: the occurrences a BY REFERENCE formal's fixed table creates take the table's INITIALIZED default

Wave 1072 group C finished kb/Work PB2690's last half. Wave 1069 C had made a store through a BY REFERENCE formal's
FIXED table, past the capacity of the argument's dynamic-capacity table, create the occurrences (ISO 8.5.1.9.3), and its
branch review (R1-1) found that every item of a created occurrence the callee did not store, and every intermediate
occurrence, came back space filled. 8.5.1.9.5 says otherwise for an INITIALIZED table: "any elementary items not
referenced as receiving operands in a statement that creates new elements in that table are first initialized as though
they had been the subject of a statement of the form INITIALIZE ... WITH FILLER ALL TO VALUE THEN TO DEFAULT".

**Re-probe.** `E:\COBOL-coord\scratch\w1069c\r3.cob` on a fresh build of 2da05a28a printed `CALL=[ab|  |r ] 3` beside
`DIRECT=[ab|ab|rb] 3`; after the fix both read `[ab|ab|rb] 3`.

**The design came out simpler than the note's sketch.** The note proposed carrying the default on the argument's carrier
and merging it item by item wherever the callee's image was all spaces. That needs item boundaries, and it is wrong for a
callee that stores SPACES into an item: the default would come back. The fix carries the table's CREATION instead, and
lets the view show it. `CobolVarGroup.Created` holds, per component, the element its dynamic-capacity table creates at an
occurrence number (`CobolDynTable.CreatedAs`, the same seed a receiving subscript grows the table with, keyed by
occurrence so a Format 2 VALUE is honoured). Only a BY REFERENCE crossing's builder states it: the emitted
`AsVarImage(__byReference: true)` and `StorageCell.VarGroupAt(byReference: true)`, reached through
`PlaceRenderer.VarGroupBoundaryImage(..., byReference: true)` from the CALL's `RefCarrier`, the typed INVOKE's write-back
argument and copy-out, and the universal box. A carrier that states a creation is therefore storage, and the one signal is
on the data. `Reshape` shows a fixed table's occurrences past a storage table's capacity as created; the 8.5.1.9.5 NOTE
licenses that ("functionally identical to one where the intermediate occurrences are physically allocated and, if
specified, initialized"). `Overlay` keeps the highest occurrence whose image differs from what the view showed, and builds
each created occurrence as the created element with the formal's stores over it. Every item the callee did not store
already holds the default in the view it stored over, so the result is exact, a stored SPACE included. The one residue is
the effect test's: a callee that stores exactly the created image into an occurrence leaves the capacity unchanged. The
content is the same either way.

**The trap.** A BY CONTENT CALL argument is a detached cell, and the callee reads it through the same `CobolArgAdapt.Text`
view as a BY REFERENCE one. The first cut, which stated the creation on every carrier, made a BY CONTENT callee see the
INITIALIZED default where 14.2.3 GR9's record (one that does "not occupy the same storage area as the argument") holds the
MOVE's space fill (14.6.9.2 2)). That is why only a BY REFERENCE builder states the creation. A MOVE's sender then
allocates nothing for it either.

**Arms swept.** Both builders (record struct, cell); the view and the write-back; elements of fixed length
(`OverlaidTable`) and variable-length ones (`GrownCount`, `OverlaidOccurrences`, where a created element carries its own
inner creations); CALL with fixed and variable-length formals, the typed INVOKE, the universal INVOKE (14.9.23.3 SR6: always
BY REFERENCE), and BY CONTENT on both as the counter-arm.

**Witnesses.** Goldens `2014/pb2690_initialized_created_occurrences_call` and `_invoke`, negative
`pb2690-initialized-created-occurrences-below-2014` (COBOLNET0900), and three unit tests in
`BoundaryGroupCorrespondenceTests`. D-VGB in `docs/CONFORMANCE.md` section 3 and the interprogram design's storage-carrier
paragraph record the determination. Three older goldens' comments that said "space filled" were re-derived; their outputs
did not change.

**The runtime major is 3.** `CobolVarGroup`'s positional record gained `Created`, and `StorageCell.VarGroupAt` gained its
`byReference` flag, so two shipped signatures left `PublicAPI.Shipped.txt`. DESIGN-external-repository 15.4 raises the
major exactly then, and the first gate's one red was `RuntimeAbiPinDriftTests` saying so. `Directory.Build.props` is now
3.0.0 and the surface hash is re-pinned (B075C3F8AAC0C7F6). The call-ABI fixture's hash did not move.

**Self-review.** Architecture: one signal, on the data, and one place per rule (`Reshape` shows, `Overlay` creates). A first
cut added `View`/`ViewImage` beside a per-site choice; it was folded back into the data. Full code: both element arms and
every lane agree, and a division by a zero element width was guarded. Performance: a MOVE or a BY CONTENT copy allocates
nothing new; a BY REFERENCE view seeds one element per occurrence it shows past the capacity. Duplication: one helper
renders an element's image for both the current image and the creation (`GroupImageCodec.DynElementImage`), and one
builds the cell's element carrier (`StorageCell.ElementCarrier`). `semgrep/verify.py` PASS, no count moved.

**Arch oracle.** `compare_oracle.py` against a6376c64ff94: DIFFERENT, 3456 of 7807 cases, every one classified by
script (`E:\COBOL-coord\scratch\w1072c\classify.py`): 3342 differ only by the runtime version string the module records
("2.0.0.0" to "3.0.0.0", the major above); 111 differ by that and by the storage-carrier emission (`AsVarImage`'s
`__byReference` parameter, the `Created` list, `VarGroupAt(..., byReference: true)`); 3 are ADDED (this branch's goldens).
None is unexplained. The baseline is re-recorded at 42d6471e33f7.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261014T012409Z-986e91; Conformance
5,903/11,546, Unit 32,552/33,499, Characterization 7/36 ran in leg 1). The Linux gate is the landing's (batched gating).
