## Entry 1837 — 2026-10-01 20:14 PDT — Train 1002: PB1907's undefined-result behaviors (INSPECT, WRITE/REWRITE, overlapping MOVE, UNSTRING) documented, GnuCOBOL-aligned and pinned (GAP 682 → 675)

Wave 1002 carried the owner's PB1907 decision (DEVLOG 1836) into code: for each Annex A.2 undefined case, document
what WiseOwl COBOL does in `docs/CONFORMANCE.md` §3.1 "Implementor behavior in Annex A.2 undefined cases", follow
GnuCOBOL where it measurably differs (except WRITE's record length, which ISO defines), and pin it with a golden.
Four Sonnet implementers ran in parallel from one base (5b856e338), one per emitter; this train lands all four. No
diagnostic codes were claimed (COBOLNET2692-2699 returned).

**Group A — INSPECT (rows GR-14.9.22.4-13, -18, -21).** Re-probe reproduced both measured differences: format 3 with
the counter inside identifier-1 gave `0BBB` (GnuCOBOL and §14.9.22.4 GR19 give `3BBB`), and a TALLYING counter inside
identifier-1 gave `21A1A1`/3/4/`2AA` where GnuCOBOL gives `31A1A1`/4/7/`3AA`. Fix: `InspectEmitter.EmitBody` re-reads
identifier-1 and re-runs the shared comparison cycle before a later TALLYING operand when the previous counter may
share storage with what the cycle reads, and re-reads identifier-1 between the two halves of a format 3 (GR19);
the new `StorageOverlap.MayShareStorage` is a conservative declaration-level disjointness proof, so a statement
whose counters overlap nothing stays GR8's single shared cycle. REPLACING and CONVERTING already matched GnuCOBOL
(documented, pinned, no code change). D-INS1..4; goldens `conformance:85/pb1907_inspect_{tally,format3_counter,
replacing,converting}_overlap`. The survey's draft tally golden had pinned the OLD values and was corrected to the
measured ones. One GnuCOBOL deviation from ISO surfaced and is NOT followed: GnuCOBOL runs each TALLYING phrase as its
own pass, so `ALL "BC" ALL "AB"` over `ABCABC` counts BC=2 AB=0 where §14.9.22.4 GR8 a), b) (and ours) give BC=0
AB=2; the standard controls (control line C1). The GnuCOBOL differential may flag it.

**Group C — WRITE and REWRITE past record-name-1 (rows GR-14.9.51.4-13 and GR-14.9.35.4-15).** Re-probe: the bytes
past a short record-name-1 were spaces. Determination D-WRT1: a byte past record-name-1 is the record area's content
at its position (§13.18.33.4 GR3, every 01 implicitly redefines the area); a position no description occupies is a
space; the NUMBER of bytes stays ISO's (§13.18.43.4 GR13 a), GR6), so GnuCOBOL's DEPENDING-length clamp is not
adopted (owner). One predicate `FileModel.TransfersPastRecord` and one image helper
`SequentialIoEmitter.SentRecordImage`, used by both arms of the dispatch (sequential and keyed WRITE and REWRITE).
Exempt because nothing is undefined there: LINE SEQUENTIAL (GR21, GR22, GR17 c)), a variable file without DEPENDING,
the record sequential '44' REWRITE, and out-of-line records. Goldens `conformance:85/pb1907_write_past_record`,
`conformance:85/pb1907_keyed_rewrite_past_record`, `conformance:2023/pb1907_past_record_whole_record` (LINE
SEQUENTIAL is a 2023 phrase; its L3 line did not turn red under mutation, so it pins GR17 c) without guarding the
predicate). NIST RL106A's INFO-ONLY REL-TEST-10 line now reads `FIXED LENGTH RECORDS`: a no-RECORD-clause multi-01
FD is the implied Format 1 of D-FRA; no pass/fail line moved. NIST SQ107A is the same case and was missed (its
corpus row is not asserted by the Conformance assembly, only by CI's NIST guard): its FD SQ-VS7 has 01s of 120 and
151 characters and no RECORD clause, so its INFO section SEQ-INFO-004..006, which reports whether "THE MAXIMUM SIZE
RECORD IS ALWAYS WRITTEN", now reads `MAXIMUM RECORD SIZE WRITTEN` with the record numbers it found instead of
`NO DEFINITE CONCLUSION POSSIBLE`; its golden is re-baselined at the landing, licensed exactly as RL106A's (an
informational line, §13.18.43.4 GR5 a)). RELEASE past the record is the SD twin and needs PB322's
determination F first; the lead is now in PB322.

**Group D — overlapping MOVE and arithmetic operands (row GR-14.6.10-1).** Re-probe: `JUST=[A AB]`, `NUM-MOVE=[0234]`;
now `[A A ]` and `[0000]`, as libcob. Shape: the decision is made at bind time — `StorageExtent` (the compile-time
storage extent, null where not provable) and `MoveOverlap.Classify` give an `OverlapPrefill` carried on
`MoveStore.Prefill`, and `MoveEmitter.EmitOverlapPrefill` renders it before the store. Corners (D-OVL2): a JUSTIFIED
RIGHT receiver's leading pad is space-filled first; a numeric DISPLAY receiver is zero-filled first (identical numeric
descriptions keep the snapshot, as cobc's memcpy does). Every other MOVE carries `None` and emits nothing new (62
corpus programs' emitted C# compared). Golden `conformance:85/pb1907_overlap_move` (21 lines, identical to a built
cobc's output); `MoveOverlapDriftTests`. Not adopted, documented: a numeric sender into a shorter JUSTIFIED RIGHT item
(cobc keeps the leftmost digits, §13.18.32.4 right-aligns), and libcob's sign re-punch (a negative zero). PACKED and
numeric-edited receivers are not prepared; with the sign question that is PB1910.

**Group B — UNSTRING (row GR-14.9.48.4-18).** Re-probe reproduced all four survey repros. Shape (D-UNS1..4):
`StringEmitter.EmitUnstring` re-reads the sender and the identifier delimiters before each receiving area after the
first wherever an INTO, DELIMITER IN or COUNT IN store may reach them (`StorageSharing.MayShare`); DELIMITER IN takes
the live delimiter content (`CobolStringOps.MatchedDelimiterIndex`); TALLYING receives its content at the end plus
the count, stored before POINTER. A disjoint UNSTRING emits the old code. Goldens
`conformance:2023/pb1907_unstring_overlap_{sender,delimiter,tallying,pointer,linkage}`, drift test
`UnstringLiveReadDriftTests`; re-measured byte-equal to GnuCOBOL 3.2.0 except one case kept as ours and pinned (leg
B2): a `DELIMITED BY ALL` run the same statement overwrites is consumed before the INTO store here and after it in
libcob, which needs the kernel's examination split around the store (PB1909). COBOLNET_DESIGN §7's UNSTRING bullet
updated.

**The train.** Clusters brought in A, C, D, B from their branches with inventory and DEVLOG hunks excluded; the four
`record_verdicts` batches re-applied on the merged tree (GAP 682 → 679 → 677 → 676 → 675; 7 rows CONFORMS with
test-refs). Hand-merged: the §3.1 heading each group created (one heading, A's intro plus the IBM and Micro Focus
survey sentence from B's, then the D-INS, D-WRT, D-OVL and D-UNS rows), kb/Work PB1907 (frontmatter rows and four
landing paragraphs; C's paragraph named the 2023 golden as 85, corrected) and the 2023 manifest (element count 766 →
771 checked; 85 manifest 325 → 332). `docs/DRIFT_RULES.md` regenerated (252 drift tests). Gate: `=== BUILD-LOCAL
GATE: GREEN — Conformance 9,740/9,740 · Unit 30,209/30,209 · Characterization 35/35` (lander mode, one leg, run
20261002T030919Z-eae329; the GnuCOBOL corpus fetched), legacy integration 503 passed + 1 skipped, the WSL Linux gate
GREEN, semgrep unchanged (BigInteger 46, decimal 2, raw diagnostic literal 294, bound rendered text 3), the CI audits
(code and doc citations, evidence supersession, witness loss, drift index, `work.py check`) all clean. Review (full
code pass over the merged diff, six citations re-run through `cite.py --check`, all OK): no correctness finding, no
cluster dropped; one duplication finding: two classes each claim to be THE ONE may-share predicate
(`StorageOverlap` for INSPECT, `StorageSharing` for UNSTRING), written in parallel from one base, filed as PB1911.
PB1907 → `landed` (all six claimed rows closed, plus REWRITE's GR-14.9.35.4-15). New notes: PB1908 (STRING overlap,
row GR-14.9.43.4-10, the same decision not yet applied), PB1909, PB1910, PB1911; PB322 carries the RELEASE lead.
PB1912-PB1915 returned unused.

**The first CI run was red** (run 36959750923 on e4338d770, main untouched): job `Guard (WiseOwl COBOL NIST + legacy
unit + integration, parallel)`, step `scripts/guard-fast.sh`, `SQ107A: DIFF — REGRESSION!` (363 MATCH, 1
regression; legacy unit 1,199 and integration 503 green). Attributed to group C: reproduced locally on the train
build (compile with `--nist SQ107A`, run, diff against `tests/nist/valid/SQ107A.txt`), and the only difference is the
three INFO lines above, the D-WRT1 effect C's own report re-baselined for RL106A. Re-baselined inside group C's
commit rather than dropping the cluster; the lander's local gates never run the NIST guard, so the miss was invisible
before CI.

