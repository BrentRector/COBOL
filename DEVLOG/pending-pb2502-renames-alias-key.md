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

**Self-review.** Architecture: one rule in one place per reader (position, width, kind), no wrapper. Code: the forwards implement 13.18.45.4 GR1/GR2 (the alias's place is data-name-2's); the
sibling arms (RECORD KEY, ALTERNATE KEY, SORT key, READ/START key of reference, variable-length window) were swept and probed. Performance: one null test on a cold path. Duplication: none added.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261012T191542Z-2cb312`: Conformance 5,088 · Unit 32,721 · Characterization 36 ran; the three new
corpus cases Passed in `leg-1-Conformance.trx`). `scripts/semgrep/verify.py` PASS, no count rose.

**Rows.** None: no inventory row claims that an alias is positioned in the record (`closes_rows: []` with its reason).

**Review round 1 (R1-1).** The reviewer found the alias forwards covered `GroupKindsOf` and `ByteWidth` but not the D20 operand-category reader: a no-THROUGH alias of a GROUP-USAGE NATIONAL group had
`OperandPic` null (its own `GroupUsage` is None and `Pic` is only copied for an elementary data-name-2), so `ItemCategory.IsAlphanumericOrNational` refused it as COBOLNET0863 "a national group
item" while `RECORD KEY IS G` on the group itself compiled and ran: legal source rejected (12.4.5.12.3 SR2, 13.18.45.4 GR1, 13.18.29.4 GR2 b). Fixed at the reader: `DataItem.AsIfPic` and
`IsAsIfElementary` forward a no-THROUGH alias to the group it renames (the same rule as `ByteWidth` and `GroupKindsOf`), so every operand-category question about the alias is answered as
data-name-2. Golden `2002/pb2502_renames_record_key_national_group` (WRITE, duplicate 22, READ KEY IS, START, READ NEXT, end 10 over a national-group alias key; at the introducing edition, GROUP-USAGE being a 2002
clause, `constructs.json` group-usage-clause-2002; the rule is the same at 2014 and 2023, and below 2002 the GROUP-USAGE clause itself is refused by `negative/pb1653-national-group-view-below-2002`).
Sibling sweep: the other readers of an alias DataItem (`Pic`, `ByteWidth`, `GroupKindsOf`, `AsIfPic`, `IsAsIfElementary`) now all forward; ReferenceResolver already forwarded place references.

**Review round 2 (R2-1).** The round-1 golden sat in the 2023 corpus on the false premise that GROUP-USAGE is 2023-only; it was introduced in 2002, so a 2002 or 2014 regression of the national-group alias
path was invisible to the per-edition compilers. The golden moved to the introducing edition's corpus (`2002/`, both manifests updated); the reviewer verified the same output at 2014 and 2023.
