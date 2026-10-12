## Entry 1460 — 2026-09-04 22:02 PDT — Landing train 1: PB203 · PB201 · PB470 · PB193 in ONE landing — REDEFINES bit windows, the position operand's carrier, the LOCALE substituted result, and OPEN's per-organization validated set

**PB203 — a REDEFINES bit member holds BITS of the shared area, and the byte window was wrong in four places.**
`01 A PIC X(2). 01 BV REDEFINES A GROUP-USAGE BIT. 05 BV1 PIC 1(8). 05 BV2 PIC 1(8).` failed BACKEND compilation
with four CS0103 on conforming source. Re-probing the shapes the note held fixed found the population was three
times larger and two of the shapes were not crashes at all but SILENT WRONG ANSWERS: an elementary
`01 B REDEFINES A PIC 1(8) USAGE BIT` displayed `A` instead of `01000001`, and two sub-byte bit members of one
view read the raw character of byte 0 and a space past the end of a one-byte backing. The note's own location for
the CS0103s ("the plain `DISPLAY BV` line") was wrong: all four sat inside a dead `record struct` the emitter
produced for a group the physical model had already collapsed into the class backing; its `wrong_answer` and
`silent` flags were both false and are now true. §13.18.44.4 GR1 states the storage association in BITS — "starts
at the first bit of the data item referenced by data-name-2 and continues over an area sufficient to contain the
number of bits required" — §13.18.29.4 GR1c routes a bit group's members through §8.5.1.6.3, and §14.6.8.6's NOTE
settles it outright: "When an item is of usage bit, the item is not necessarily aligned on a byte boundary and the
item need not occupy an integral number of bytes." So the alternative the queue entry offered — a Tier-D rejection
— would have refused conforming source. Four root causes, one mechanism: `RedefViewPlace` gained a BIT window
payload rendered through the new `CobolBits.ReadWindow`/`WriteWindow` pair (which touch only the member's own
bits, so two members SHARING a byte are independent receivers); `AssignClassOffsets` carries a §8.5.1.6.3 bit
cursor, gated on `HasBitDescendant` exactly as `ImageWidth` is, so every bit-free class is byte-identical;
`RecordStructEmitter` no longer emits a type for a collapsed group, whose `AsImage()`/`AsBits()` could only ever
have been `""`; and `ComputeTier`'s Tier-A arm now proves "one field serves both" from the storage UNIT rather
than from CLR type plus byte width, which is how a bit leaf came to alias a character field. The offset law lived
in THREE builders — the resolver, the INITIALIZE cursor and the MOVE CORRESPONDING cursor, one of whose comments
admitted it was "the same arithmetic as ReferenceResolver.PlaceForItem" — which is exactly how the bit unit came
to be missing from all three at once; they go through one `RedefViewPlace.For` factory now. The sibling sweep
found `OoEmitter`'s LINKAGE and LOCAL-STORAGE root loops both missing the Tier-B non-canonical skip
`ProgramEmitter` has carried since M2-OO-1h, splitting one §13.18.44 storage area in two for a method-scoped view,
and `ProgramEmitter.PrefixPlace`'s positional rebuild would have silently dropped the new payload. PB173's open
"RELATED" question is closed with a measurement rather than an argument: `BitImagePlace` needs no `RedefViewPlace`
arm, because a bit window already reads its boolean carrier and the ref-mod slices §8.4.3.3.4 GR5a's bit positions
structurally. D19's scope boundary claimed a sub-byte REDEFINES overlay "is refused rather than given a rounded
offset" — false in both halves and never measured — and now carries the as-built model. Golden
`pb203_redefines_bit_window` pins twenty-one legs with every bit leg PAIRED against its no-REDEFINES control, and
the gate was proven to read it by mutating the `.out` once; `char_redefines.85.g.cs.txt` was re-baselined by
exactly the eight lines of the dead struct — that snapshot WAS the characterization of this defect. No inventory
rows (the note carries none) and no diagnostic code.

**PB201 — a position operand's CARRIER, not its class: the bind-time renderer bet on C# overload resolution and
the bet had five losing sides.** `ReferenceResolver.PositionRead` emits `CobolTable.Occ(<field>)` /
`CobolString.RefModPosition(<field>, scale)` at bind time and lets C# supply the conversion — the deliberate device
that lets ONE text serve a `long` field and the `string` image a post-bind analysis may promote it to. The overload
sets were `long|string`, while `DataItem.ElementType` also yields `double`/`float`, `Int128`, `ulong`, `UInt128`,
`ManagedPointer`, `ProgramPointer`, an object reference and a group's per-program `record struct`. The filed defect
(a GROUP subscript under `--permissive`, CS1503/CS0103) turned out to be the MILDEST member of its family, and half
of it no longer held on today's tree — PB170's `1884ba1e` had already put a class screen on the strict fast path.
The sweep found four strict-lane, legal-source siblings that were worse: `E(W-FL)` with `USAGE FLOAT-LONG`,
`E(W-BIG)` with `PIC 9(20) COMP`, `E(W-U)` with `BINARY-DOUBLE UNSIGNED` and `W-W(W-FL:2)` each failed the backend
in the DEFAULT STRICT lane on source §8.8.1.1 and §8.4.2.3.4 GR1b make perfectly legal, so the note's
`rejects_legal_source: false` was wrong and is now true. The sweep then found a SECOND emitter with no escape:
`RuntimeApi.TableOcc` renders the OCCURS DEPENDING current-count read at CODEGEN time from a Place, and
§13.18.38.3 SR17 asks only that data-name-1 "shall describe an integer" — so a 20-digit COMP and an unsigned
BINARY-DOUBLE are both legal control items and both broke `MOVE <odo-group> TO X`. The fix is therefore BOTH halves
at their one place: `Occ` gains `(Int128)`, `(ulong)`, `(UInt128)` — the second emitter needs the CAPABILITY, not
an escape — all narrowing through **the ONE narrowing, `CobolNum.Position`, which SATURATES**, because §8.4.2.3.4
GR2 requires an out-of-range occurrence to be DETECTED and a plain `(long)` cast can WRAP one back inside `1..n`;
and `PositionRead` admits only a carrier the helper it is about to emit declares a parameter for, sending
everything else down the D18 route it already documents as the answer, where the §8.8.1.1 screen and
`NumericRenderer.FieldNum` — THE ONE numeric read — apply. That second emitter's carrier universe was MEASURED
rather than assumed: `OdoResolve`'s SR17 screen (Numeric, not float, scale 0) admits exactly `long`, `Int128`,
`ulong`, `UInt128` plus the promoted `string`, and `Occ` now declares a parameter for all five. The FLOAT carriers
are deliberately absent — a `double` can be fractional and GR1b's integrality test lives only in the scaled arity —
and `PositionCarrierOverloadDriftTests` pins the exclusion, the two carrier lists against the real overload sets in
BOTH directions, and the two-arm agreement of `Occ` and `RefModPosition`. The emit floor for an ordinary subscript
is byte-identical. Closing pass: a stale reachability comment claiming the fast path "commits only for a long or
string carrier" — false since the overloads widened — was corrected together with its sibling in
`COBOLNET_DATA_MODEL_DESIGN.md`, and SR17 got the negative golden (`pb201-odo-depending-not-integer`) it never had,
because SR17 is the only thing bounding the second emitter's carrier universe. Goldens:
`pb201_position_operand_carriers`, `pb201_odo_count_carriers`, four negatives,
`Permissive_GroupPositionOperands_DecodeTheirImageAndAgree` and the drift guard. Inventory: GR-8.4.2.3.4-1,
GR-8.4.2.3.4-2, SR-8.4.3.3.3-4 and SR-13.18.38.3-17 → CONFORMS, **GAP 3013 → 3009**. No diagnostic code.

**PB470 — the three LOCALE intrinsics answered a zero-length value where the determination says spaces, and the
substituted-result table had only one of its two text classes.** `FUNCTION LOCALE-DATE("20261399")` returned a
ZERO-LENGTH value. `docs/CONFORMANCE.md` row `DOC-A.1-90` puts a function in the zero-length class only when the
returned LENGTH is *derived from the rejected argument*, and §15.52.4 / §15.53.4 / §15.54.4 rule 3 each say the
length "depends on the format indicated in the locale" — the locale survives the rejection of argument-1
untouched, so the row's general clause governs and, the function type being alphanumeric, the answer is SPACES. A
wrong answer in a user's program, at four guards, and silent: §14.6.8.5 space-fills a receiver from a zero-length
sender, so `pb64t4_locale_functions`' existing `ARG=[]` leg (a MOVE plus TRIM) had been green under either answer
since PB64 T4 landed. ONE space, and the number is derived: DETERMINATION L10 renders `d_fmt`/`t_fmt` as culture
patterns whose width varies with the VALUE as well as the format, so once the value is rejected the format fixes no
width — the note's own proposal, "spaces of the length the locale format would have produced", names a length that
does not exist. §15.30.3 rule 1 is the standard's own answer for exactly that shape — EXCEPTION-LOCATION, an
alphanumeric function whose length is "based on its contents", returns "one alphanumeric space character" when the
contents are absent — and row DOC-A.1-90 adopts it, which also makes the answer locale-independent and therefore
testable. The root cause is that PB383 left the TEXT side of that row with ONE mechanism: `ArgumentErrorZeroLength`
existed, while the other class, spaces, was still spelled `return " ";` at four CHAR / CHAR-NATIONAL guards, so a
fifth member could be written wrong with nothing to contradict it — which means the note's proposed fix (route the
four sites through an existing helper) was not available, and the fix had to ADD the missing member of the table.
Both classes are now values in `ArgumentSubstitute` (`ZeroLength`, `Spaces(n)`) with `ArgumentErrorZeroLength` /
`ArgumentErrorSpaces(detail, positions)` as the raise-and-substitute expressions over them — `positions` with no
default, so the length stays the call site's cited decision (`CobolLocale.LocaleSubstitutePositions`,
`CobolIntrinsics.CharPositions`: two constants because they are two derivations that agree on 1). Twelve sites
moved onto the table — the four LOCALE guards, CHAR ×2, CHAR-NATIONAL ×2, CONVERT's §15.19.3 r1 arm, SUBSTITUTE's
three §15.87.2 shape arms, and the three post-predicate arms in `FormattedTime`/`FormattedDatetime` whose raise
lives in a `bool` screen — and only the LOCALE four changed value. `ArgumentSubstituteDriftTests` then bans the
shape structurally over `src/Cobol.Net.Runtime/Intrinsics` with no exemption list (a space-only returned literal is
always one of the two classes there), and its second fact feeds the detector the verbatim pre-fix lines so the
watchdog is shown to bite. Witnesses that can tell the answers apart:
`2014/pb470_locale_argument_substitute` (all four guards, legal controls through the same locale, and the
CHECKING-ON arm proving the condition is set and nothing is stored) and
`2002/pb470_locale_argument_substitute_2002`; every leg is read twice — a bracketed DISPLAY (§14.9.11.4 GR1)
beside a `FUNCTION LENGTH` read-out (§15.50.4 r3) — and nothing is MOVEd except the leg whose point is that nothing
arrived. Three `CobolLocaleTests` facts had asserted the old value and were corrected with it.
`AnnexA1RegisterDriftTests` caught a mis-targeted CONFORMANCE.md edit (the new `test-ref`s had landed on row
DOC-A.1-22, whose cell ends with the same golden name) before it could ship. `DOC-A.1-90` re-verdicted CONFORMS
with the GAP unchanged — the row was already closed, and what changed is its determination, its `code-location`
and its witnesses. `kb/Work/PB471` is explicitly NOT decided by this and the row now says so. No diagnostic code.

**PB193 correction — the §14.9.27.4 GR10 validated set varies by ORGANIZATION, and the uniform one rejected legal
source.** The fixed-file-attribute catalog landed with its Annex A.1 item 129 determination declared uniform across
every organization and storage medium, and with §9.1.7.2's record-sequential/line-sequential distinction folded
into §9.1.6's organization attribute. The wave-local gate measured what that costs: six conforming corpus programs
went red — `2002/io_status_04`, `2002/rw_suppress`, `2002/rw_suppress_bare`, `2002/rw_present_when`,
`2002/oo_object_report`, `2023/declined_rw_present_varying_control` — every one of them the
write-a-file-then-read-it-back idiom, of which Report Writer is only the commonest instance and one of the six is
not a Report Writer program at all; and `rw_suppress` degraded from a terminating program into an infinite READ
loop (OPEN '39' → READ '47' → neither AT END nor NOT AT END → `PERFORM UNTIL WS-EOF = 1` never ends). GR10's own
third sentence is the answer and the first landing did not use it: "The validation of fixed-file attributes may
vary depending on the organization or storage medium of the file." COBOL.NET now validates what the file's own
storage FIXES — the ORGANIZATION for every file (§9.1.6 names exactly three); additionally the record type, the
record sizes and the key descriptors for RELATIVE and INDEXED, whose store's structure IS those attributes; and
nothing further for SEQUENTIAL, because §9.1.7.2 puts a sequential file's record lengths in the data and in the
reading program and §9.1.13.2 items 3, 5 and 7 answer the disagreement with the successful '04', '06' and '09'.
`2002/io_status_04` exists to demonstrate item 3's '04', which a '39' at the OPEN makes unreachable. The uniform
set was also a second mechanism for the job `RecordLayoutNotice` already owned, which its own doc comment denied.
`FixedFileAttributes.MediumFixesRecordLayout` is the one place GR10's variation is exercised, with a drift test
requiring all three organizations to be deliberate arms; `Load` now reads an unrecognized `organization` as "not
recorded" (the sibling of the `record-type` arm, and the migration path for a `.cbattr` the first landing wrote);
the `_lineSequential ? "LINE-SEQUENTIAL" : "SEQUENTIAL"` fold is gone, because §9.1.7.2's distinction is §9.1.6's
separately listed RECORD DELIMITER and not a fourth organization — which is what broke the five Report Writer
programs independently of the record sizes. The 85 golden was re-derived onto a RELATIVE subject plus a SEQUENTIAL
one that shows the '04' the '39' was hiding, pinning BOTH directions, because a set that is too broad rejects legal
source exactly as surely as one that is too narrow reads rubbish; and one existing unit fact,
`AnAbsentOptionalFileCreatedByOpenExtend_RecordsItsAttributes_GR17`, was found red by the gate and re-probed on the
ORGANIZATION — its "contradicting" connector contradicted nothing a record-sequential subject validates, a miss
invisible to a read of the diff because the test is not in it. CONFORMANCE.md gains row DOC-A.1-129, FILES_DESIGN
D10 the per-organization determination, and kb/Work/PB193 the six broken programs and the infinite loop. Rows
`GR-14.9.27.4-10`, `DOC-A.1-129`, `GR-12.4.5.12.4-3` and `GR-12.4.5.6.4-3` → CONFORMS, then the first two
re-recorded with the corrected determination text. No diagnostic code — '39' is a runtime I-O status.
`kb/Work/PB329` Part 1 is recorded and the note stays open.

**The train.** Four clusters, four commits, one build and one gate, landed in the manifest's order — PB203
(`68496d5d`), PB201 (`5082cffc`), PB470 (`0e5dd8b8`), PB193 (`bf12e2e9`) — with the DEVLOG and plan §0 in a fifth. Every
implementer's content was taken as `git diff <base> <branch>` because each carried its work as WIP commits; a
filesystem walk against `git ls-tree -r <branch>` confirmed no cluster had an uncommitted deliverable. The only
merge conflict in the train was one block of `docs/CONFORMANCE.md`, where PB470 rewrote row `DOC-A.1-90` while
PB193 rewrote row `DOC-A.1-50`'s cross-reference and INSERTED a new row `DOC-A.1-129` between them; it was resolved
from evidence rather than by taking a side — HEAD carries no `DOC-A.1-129` at all, so PB193's is an insertion, its
`DOC-A.1-50` text says "by the row below", which requires `DOC-A.1-129` to follow it directly, and PB470's
`DOC-A.1-90` is the rewritten one (4638 characters against the base's 2660). `tests/version-matrix/
traceability-inventory.json` was excluded from every cluster patch and rebuilt by re-applying each cluster's
`record_verdicts.py` batch on the merged tree in manifest order, which is also what produced the train's authorita-
tive GAP: **3013 → 3005**, not the 3009 the train was dispatched expecting — PB201's four rows and PB193's four
rows are DISJOINT, so the two −4s add rather than coincide. No cluster was dropped. Every report's claims were
re-verified rather than inherited: three citations per cluster re-run through `cite.py --check` (12 of 12 OK), each
note's `inventory_rows` checked against the rows its batch actually carries, every new golden checked into its
manifest (the four negatives carrying a `*> reject-at:` first line), and the four assigned diagnostic ranges
checked for collision — none of COBOLNET1728–1735 is consumed anywhere in the tree, so next-free stays
COBOLNET1728. `work.py check` ✓ 532 items; `semgrep/verify.py` PASS and unchanged at every count
(bound-node 3, biginteger 36, decimal 2, raw-diagnostic-literal 439); both citation audits at 0 findings /
0 MISFILED. Union wave-local gate (28 `FullyQualifiedName~` terms: the four clusters' filters merged —
Redefines, Boolean, Corpus, Group, Initialize, Corresponding, Oo, National, Move, PositionCarrierOverloadDrift,
ArithmeticOperandClass, SpecTraceabilityInventory, Negative, Subscript, RefMod, Occurs, CorpusRunner, Locale,
Intrinsic, ExceptionCondition, AnnexA1Register, NistDifferentialTests, FileStatus, KeyedIo, SequentialIo,
SortMerge, FixedFileAttribute, DefectiveRowCoverage): citations 0 findings / 0 MISFILED, build 0 warnings 0 errors, **Conformance 2776/2776, Unit 5334/5336, Characterization 33/33** — the two unit reds are `ExternalCorpusPopulationDriftTests.TheCensus_NamesEveryPopulationAndItsProgramCount` and `…SweepExternalPopulation_EqualsTheDifferentialsCommittedCaseCount`, the known fresh-worktree shape (the GPL GnuCOBOL corpus is never committed), and nothing else failed. Population asserted rather than assumed: a targeted `DisplayName~` run over the train's own goldens plus `io_status_04` and the two `rw_suppress` programs returns 11 of 11 green, so the filter really did look at what changed. `CorpusRunnerTests` was carried in the union
deliberately, because PB193's first landing had broken six corpus programs and hung a seventh, and those six are
green on the merged tree.

