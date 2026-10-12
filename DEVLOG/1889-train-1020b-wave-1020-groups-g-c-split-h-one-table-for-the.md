## Entry 1889 — 2026-10-05 07:22 PDT — Train 1020b: wave 1020 groups G, C (split), H: one table for the file-control formats, SELF and SUPER as expression-tier identifiers, REDEFINES data-name-2 screened, the non-ISO linkage USING entry deleted

Train 1020b carried three clusters from wave 1020, in order G, C, H, onto train 1020's head `ad7257631`. All three were
cut at `7546af101`, so no code hunk conflicted. The only conflicts were whole-element additions to `docs/DIAGNOSTICS.md`,
`DiagnosticCatalog.cs` (two adjacent descriptors, COBOLNET2897 from train 1020 and COBOLNET2900 from C) and the 85,
2002, 2014 and negative conformance manifests. Both sides were kept, the comma between list elements restored, the
DIAGNOSTICS rows re-sorted by code, and every JSON file re-parsed. The inventory hunks applied cleanly. A dry run of each
cluster's `record_verdicts` batch against the merged tree confirmed it was already applied (each retirement reported
"not on the row"), and the per-checkpoint GAP is 208, 203 (G), 201 (C), 197 (H).

**G: PB773 and PB1073, which §12.4.5.1 format a file-control clause specifies.** §12.4.5.2 SR8, SR9, SR11 and SR13 each
ask which of the four general formats a clause belongs to. The answer was two hand-written marker lists (Format 1's
clauses and Format 2's), so SR11 and SR13 had no site at all: an SD could write FILE STATUS, ACCESS MODE, LOCK MODE,
RESERVE, SHARING or RECORD DELIMITER, and an indexed file could write RECORD DELIMITER, and each compiled clean.
`FileControlKeyRules.EntryClauses` is now ONE table, one row per clause that is not in every format, carrying the set of
formats whose printed diagram shows it. A drift test (`EveryEntryClause_IsCarriedByExactlyTheFormatsThatPrintIt`)
re-derives every row from the four diagrams. A clause printed in one format specifies it (SR8, SR9, SR11). A clause
printed in several, none of them Format 4, is refused on an SD by SR13. So every clause beyond Format 4 is reported by
exactly one row, under COBOLNET1900, and RECORD DELIMITER on an indexed or relative file draws the new COBOLNET2912
(§12.4.5.2 11), report files exempt). `FileModel.AccessModeWritten` records the fact the SEQUENTIAL default had hidden.
The ALTERNATE RECORD KEY gained §12.4.5.6.3 SR3 (no dynamic-length elementary key) and SR4 (no key at the prime key's
or another alternate's leftmost byte, compared by the clause ordinal so a clause never collides with itself), and the
RECORD KEY gained §12.4.5.12.3 SR3. All key rules now read one window, `RecordLayout.KeyWindowInFile`. The implementer's
re-probe found that the note's `if (file.IsSortMerge) return;` had already gone with PB742. It also found that PB781's
premise ("RESERVE has no carrier") is stale; PB781 already says so. Seven negative goldens and two positive goldens.
Rows SR-12.4.5.2-11, SR-12.4.5.2-13, SR-12.4.5.6.3-3, SR-12.4.5.6.3-4 and SR-12.4.5.12.3-3 closed (GAP -5).

**C: PB1425 (split), SELF, SUPER and the object-view as identifiers of the expression tier.** §8.4.3.1.3 SR1 makes every
identifier slot "any of the formats for an identifier", but Formats 5 and 6 were reachable only through
`objectReference` (INVOKE, SET, RAISE), so `IF O = SELF`, `IF P AS UNIVERSAL = O` and an object-view argument were parse
errors. `primaryExpression` now carries `objectView` and `selfAndSuper`. Every consumer of a sole identifier reaches one
door, `OoBinder.OoBindOoIdentifier`: relations, EVALUATE, INVOKE and inline arguments, SET senders, and the RAISING
phrase, which now takes `objectReference`. SELF binds to an ACTIVE-CLASS temporary filled by the new pre-op
`BoundSelfReference`. SET and the INVOKE argument keep SELF by name because their rules name it. SUPER anywhere but an
invocation's or a property's object is §8.4.3.8.3 SR3, the new COBOLNET2900. `GOBACK RAISING NULL` is refused
COBOLNET0849 (NULL references no object, §14.9.18.4 GR1 b) 2.). Under `--permissive`, digit-decoding no longer
reaches an object or pointer operand: `COMPUTE N = O` used to end in Roslyn CS1503. The split remainder is Format 7's
general identifier-3 (`BAL OF SELF`, `BAL OF A OF T(2)`), which needs the PB1783 redesign. PB1425 stays `half` and
carries it. Golden `2002/pb1425_predefined_object_operands`, negative `pb1425-super-operand`. FMT-8.4.3.8.2 and
SR-8.4.3.8.3-7 closed (GAP -2). The implementer's lead, that `MOVE SELF TO N` is COBOL0001 rather than a refusal quoting
§14.9.25.3 SR1, is filed as PB2039.

**H: PB1281 and PB1252; PB1309, PB734 and PB1949 discharged.** The REDEFINES operand reached the binder as
`dataReference().GetText()`, so `REDEFINES A OF G` looked up the one word AOFG and bound silently to an unrelated item of
that name when one existed. `DataDescriptionCst.RedefinesTarget` now hands over the written reference, and
`DataBinder.RedefinesDataName2` refuses a qualifier (§13.18.44.3 SR6, new COBOLNET2915), sends the subscript,
reference-modifier and special-register shapes to the one data-name-n screen (COBOLNET2024, SR5), and resolves only the
base data-name, so nothing cascades. The screen now takes the caller's own subscript sentence, because its fixed one
("shall not be subject to any OCCURS clauses") was false for REDEFINES and the SORT table key. PB1252: the grammar's
`linkageProcedureParameter` (a linkage entry carrying a USING phrase) is defined by no ISO edition, and every binder
filtered it out, so `01 L-X USING BY VALUE W-A PIC 9(4).` compiled and the entry vanished. It is deleted, with
`linkageEntry`, `parameterDescriptionBody` and `parameterPassingClause`, and every reader changed: the linkage section is
`dataDescriptionEntry*` like working-storage. PB1309 and PB734 were already closed by PB1246 and PB415 and gained only
an admission golden. PB1949 was a probe whose period stood in column 73. Rows FMT-13.7.2, FMT-13.5.2, SR-13.18.44.3-5 and
-6 closed (GAP -4). The implementer's lead, that `OPEN OUTPUT A OF B` opens a file named AOFB by the same glued-text
root, is filed as PB2040 (wrong answer, silent; every `ResolveFile` caller named).

**The train.** Whole-population lander gate GREEN: `Conformance 10,635/10,635 · Unit 31,607/31,607 · Characterization
35/35 cases ran (skipped 0)`, run `20261005T141541Z-9927db`, with the external corpus fetched. Legacy
`CobolSharp.Tests.Integration` passed 503, skipped 1. semgrep verify PASS, every count unchanged. Linux gate GREEN on
the train's head. The review pass over the train's diff found two things. The first was a missing space in G's
`RecordLayout` doc comment, fixed in G's commit. The second was a sibling H's sweep missed: `ClauseDataName` still
passes the fixed OCCURS sentence for every other operand, which is false for `OCCURS … KEY IS` (a key lies under the
table's OCCURS) and is not the rule the OCCURS DEPENDING, CAPACITY and RENAMES operands state (§13.18.38.3 2) and 31),
§13.18.45.3 7)). That is diagnostic text only, across about fifteen call sites that each need their rule re-derived,
and it is filed as PB2041. A probe confirmed that C's new `selfAndSuper` primary does not capture SELF or SUPER as
COBOL-85 user words (`COMPUTE N = SELF + 1` at `--std 85` gives 42). No cluster was dropped. Codes claimed:
COBOLNET2900, 2912, 2915; returned 2901-2902, 2913-2914, 2916-2917. GAP 208 → 197.

