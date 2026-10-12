## PB2502 - a level-66 RENAMES item is a legal RECORD KEY, ALTERNATE RECORD KEY and SORT key

**The defect (re-probed on this build).** `RECORD KEY IS KR` with `66 KR RENAMES K1 THRU K2.` compiled clean and then aborted the run unit at file registration with
`NotImplementedCobolFeatureException: ... RECORD KEY missing or not locatable in the record image (ISO 12.4.5.12)`, for the THRU form and the no-THRU form alike (a no-THRU
alias of an EDITED item was already refused, correctly, as COBOLNET0863). The note left the legality open; the spec settles it. 13.18.45.4 GR2 makes the THROUGH alias "an
alphanumeric group item", which is 12.4.5.12.3 SR2's "category alphanumeric or category national within a record description entry" (the RENAMES entries belong to the record
description entry: 13.18.45.3 SR2); GR1 gives the no-THROUGH alias all of data-name-2's attributes and its storage area, so it is a legal key exactly when data-name-2 is.
Legal source, so the fix implements it. Every citation was run through `cite.py --check`.

**Root cause.** An alias rides `DataItem.Renames66`, not `Children`, and every position, width and kind reader walks `Children`. `RecordLayout.FirstOccurrenceOffsetOf` found
no place for it and returned null; `KeyedIoEmitter.KeyWindow` read null as "not locatable" (the abort). Three more readers had the same blind spot: `DataItem.ByteWidth` of a
no-THROUGH alias of a GROUP was 0 (no PICTURE, no children), `ItemCategory.GroupKindsOf` answered None for it (a comment claimed it was "never asked here"; a key screen asks it),
and `RecordLayout.ContiguousPlaceOf`, the variable-length key walk, would not have found it either.

**The sibling arm.** A SORT/MERGE key naming an alias was refused as COBOLNET1757 "subject to any OCCURS clause" (14.9.40.3 SR6 b, f): the SORT screen reads the same null as its
OCCURS bail. That was rejects-legal-source on a second statement, with the same root, so it is fixed by the same change.

**The fix, at the readers.** `FirstOccurrenceOffsetOf`, `OffsetInRecord` and `ContiguousPlaceOf` forward an alias to `Renames.From` (its storage begins where data-name-2's does; the
variable-length window and the REDEFINES-class placement of data-name-2 come along for free); `DataItem.ByteWidth` forwards a PICTURE-less no-THROUGH alias to the group it
renames; `GroupKindsOf` forwards to the renamed item. `docs/COBOLNET_DATA_MODEL_DESIGN.md` (the RENAMES bullet) states the positions/width/kind rule.

**Goldens.** `85/pb2502_renames_record_key` (prime THRU key, group-alias and elementary-alias alternate keys; WRITE, READ KEY IS, START, READ NEXT; statuses 22 and 10),
`85/pb2502_renames_sort_key` (major THRU key ascending, minor no-THRU key descending), `negative/pb2502-record-key-alias-of-numeric` (the fix places an alias; it does not admit
every alias). RENAMES keys exist since COBOL-85, so the positives are 85; there is no earlier edition for a negative. Probed on the fix and not pinned: an alias key in the secondary
01 of a multi-record FD, and one after a DYNAMIC LENGTH member (the PB1025 contiguous window); both read the right record.

**Rows.** None: no inventory row claims that an alias is positioned in the record (`closes_rows: []` with its reason).
