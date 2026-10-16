## PB694: a file with no record description entries is read only with INTO (§13.4.5.3 SR3 c), COBOLNET3983), and SR3 b) holds because the record-name form cannot reach it

Wave 1097 group F, kb/Work PB694. §13.4.5.3 SR3 (`cite.py --check 13.4.5.3` OK, rule 3) has three sub-rules for a
file description entry with no record description entries: a) a RECORD clause is required, b) every WRITE and
REWRITE carries `FILE file-name-1` and FROM, and c) every READ carries INTO. PB345 implemented a) (COBOLNET1836)
and left the row GAP because b) and c) were unscreened.

**Re-probe** on main `32f372674`, own Debug build. A sequential `FD F RECORD CONTAINS 10 CHARACTERS.` and a relative
`FD R RECORD CONTAINS 5 CHARACTERS.`, neither with a 01. `READ F AT END …`, `READ R INVALID KEY …` and
`READ R NEXT AT END …`, none with INTO, compiled clean and ran, so c) reproduced. `WRITE FILE F FROM W` and
`REWRITE FILE F FROM W` drew COBOLNET1706, as the note said.

**c), the fix.** One statement screen: `StatementValidation.CheckReadIntoOnRecordLessFile` reports
**COBOLNET3983** (Error at every edition; the READ the standard gives no meaning has no lenient corpus to protect).
`SequentialIoBinder.BindRead` calls it before its `!file.IsSequential` reroute, so the relative arm
(`KeyedIoBinder.BindRead`) is covered by the same call and has no second copy. The antecedent is a new
`FileModel.HasNoRecordDescriptionEntries`: an FD that is not a report file and not an SD, with no WRITTEN record
description entry. It is asked of `WrittenRecords`, so it holds even when `MaterializeImpliedRecord` could not
build the implied record (COBOLNET1836, COBOLNET1837).

**b) needed no owner decision and has no screen.** The note asked for A.4.13 to be settled first. It already was:
A.4.13 is Not claimed and refused (PB282), and PB1255 closed the CODE-SET row that depends on the same declined
phrase. Read as a rule on source, b) is enforced by the structure. The record-name-1 alternative names "a logical
record in the file section of the data division" (`cite.py --check 14.9.51.3` OK rule 5; `14.9.35.3` OK rule 1),
and a record-less entry has none, so no WRITE or REWRITE without FILE can be associated with the file. The FILE
form is refused COBOLNET1706. The consequence, that such a file is input-only in this implementation, is now
written in the CONFORMANCE.md §5 A.4.13 row (the §4.2.7 user documentation) and in COBOLNET_FILES_DESIGN D18.

**Goldens.** `negative/pb694-read-no-into-sequential` covers the sequential arm; its READ INTO line draws nothing.
`negative/pb694-read-no-into-relative` covers the relative arm. Both reject at 85, 2002, 2014 and 2023, with
exactly one COBOLNET3983 each. The positive goldens `2023/` and `85/pb345_record_less_fd_area` (READ INTO on both
organizations) still compile and print their expected output.

**Row.** `SR-13.4.5.3-3` DIVERGES → CONFORMS (batch `w1097f-PB694`); GAP 74 → 73. The note is landed and the row is
in its `closes_rows`.

**Sweep.** No other verb has a record-less-file rule: SR3 names only READ, WRITE and REWRITE, and an SD cannot be
record-less (COBOLNET1837). A scan of `tests/` found 424 FDs with no record entry. Only the two new negatives READ
one without INTO.

**Self-review.** Architecture: one rule, one site, asked above the organization split. Code: the diagnostic quotes
SR3 c) and GR6. Performance: one boolean test per READ, plus `WrittenRecords.Any()` only for an FD. Duplication: the
antecedent is a single `FileModel` property.
