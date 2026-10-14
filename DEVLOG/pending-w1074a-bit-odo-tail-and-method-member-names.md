## PB3030's bit arm: one OCCURS DEPENDING tail counted in bits; PB2921 review round 2 (data named like a method)

Wave 1074 group A, finishing the w1071 B branch (merged at 1a473341f) and taking PB3030's second half.

**PB2921, review round 2.** R2-1: the per-method §8.3.2.2 census (round 2's fix) let an object data-name spelled
like a METHOD-ID through to the emitter, which gave the private data field and the method one C# member name
(CS0102 on legal source; probe `E:\COBOL-coord\scratch\w1074a\r2\p1.cob`, and `p5.cob` for the factory pair). The
object data and the instance methods share one emitted C# class, so `OoDriver.BindClassData` now reserves the methods'
C# names in the data binder's member scope before the data binds (`DataBinder.ReserveMethodMemberNames`) and the field
takes `PQ_2`. A derived class's data spelled like an inherited method (`p6.cob`) and a method's LOCAL-STORAGE item
spelled like a sibling method (`p8.cob`) were already right. Golden `2002/pb2921_data_named_like_method`. R2-2: the
census paragraph of `docs/COBOLNET_INTRINSICS_DESIGN.md` and two `DataBinder` comments still described one census per
binder and a method declared by the OO driver; both now name the per-method census and `OoBindMethodData`.

**PB3030.** Re-probed: `FUNCTION LENGTH` of a group holding a dynamic-capacity table of dynamic-length elements and an
OCCURS DEPENDING table of USAGE BIT items still warned COBOLNET1756 and aborted the run unit. `DataItem.
CurrentExtentComposes` refused the bit-bearing member; admitting it needed the variable-length group codec to count the
table in bits (§8.5.1.6.3 places each bit occurrence "at the next bit position in storage", so occurrences share
bytes). The fix is one tail: `OdoModel.FixedImageTail` states `OdoModel.GeometryOf` as the runtime `OdoTail`, which
gained a lead and a unit (`PositionsPerCharacter`, 8 for bits). `CutAt` takes the ceiling of the bits (§15.50.4 rule
9), `KeepUnused` is §13.18.38.4 GR8 a)'s receiving splice (a `CobolBits` window when the first unused occurrence starts
inside a byte), and `RecordUnit` / `RecordMaxUnits` state the table as the record's last component, one CHARACTER per
unit (new determination D-FRA (ix): a record of characters cannot say a bit count). The declared group's codec and a
CELL-BACKED group's window both read it. The cell arm was the sibling: it measured a bit table one character per
occurrence, and `DISPLAY` of an ADDRESS-OF-taken group of this shape threw ArgumentOutOfRange (`q8.cob`).

Golden `2014/pb3030_bit_odo_variable_group` covers LENGTH / BYTE-LENGTH at three counts, DISPLAY, a GR8 a) MOVE into a
declared and a cell-backed group, a nested CALL BY REFERENCE and a WRITE / READ round trip, each value derived by hand.
Unit tests `CobolVarGroupContiguousTests.BitOdoTail_*` cover the tail arithmetic. No edition-gated construct was added.
No inventory verdict changed (`closes_rows: []` with its reason). Found on the way, not fixed: a MOVE into a
CELL-BACKED group whose dynamic-capacity table has variable-length elements loses the table (capacity 0; `q9.cob`).
That is a lead in the report. PB3101 was not started.

**The branch review, answered (wave 1074 A2 finisher, review R1-1).** The bit tail above measured a table from the member
that holds it, and a member that CONTINUES a same-level §8.5.1.6.3 run has no characters of its own: `05 B PIC 1 USAGE
BIT. 05 TB PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON N.` directly in a variable-length group packed B and TB into the
leader's field at the maximum, while the codec cut TB's EMPTY field to the count. Re-probed: `DISPLAY G` threw
ArgumentOutOfRange, at OCCURS 1 TO 8 it showed a NUL character LENGTH did not count, a cell-backed group of the shape
showed the same extra character, and a FILE record of it threw at WRITE. Fix: `GroupImageCodec.VarParts` classifies a
run's leader field by the member the run holds, and `OdoModel.FixedImageTail` counts the run's earlier bits as lead
(`BitLayout.BitsBeforeInRun`, read off the one run law `RunsOf`), so the declared codec, the record layout and the cell
window measure the table from the run's first bit. Golden `2014/pb3030_bit_odo_run_continuation` covers the three arms,
a nine-bit leader, a GROUP-USAGE BIT holder continuing a run, a GR8 a) MOVE and a CALL at the maximum, each value derived
by hand; it fails on the reviewed head. The PB3030 golden's comment cited GR8 b) for a sending group whose data-name-1
lies outside it, which GR8 a) governs; corrected. Gate run `20261014T064005Z-0faf96`: `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN`, both PB3030 goldens Passed in leg 1; semgrep verify PASS.

Finisher (landing attempt 20261014T065404523254Z, `ENQUEUE: GATE RED`): the landing's gate ran
`SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree` in leg 1 and it was red. Review round 2's
per-method census deleted `OoDriver.DeclareMethodWords`, but inventory row SR-12.3.8.3-12 still named it as a
code-location; the branch's own gate had left that case in leg 2 (its `not-run-Unit.txt` lists it), so nothing local saw
it (kb/Work PB3131). The row is re-sited by a `record_verdicts.py` batch to `DataBinder.Oo.cs#OoBindMethodData`, which
now declares the METHOD-ID (or the property-name it names) through `DeclareUserWord` inside the method's own census,
and retires the deleted member with its reason; a sweep of every inventory code-location against the tree finds no
other dangling member, and the branch deleted no other method declaration.
Finisher gate run `20261014T072258Z-315405`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`,
`EveryCodeLocation_ResolvesInTheTree` Passed in leg 1 (the previous red, tier 0a).

Finisher (landing attempt 20261014T075420363360Z, `ENQUEUE: GATE RED` after main was merged in at a0d94a0f2): main's
PB3132 landing (PR #47) had added `negative/pb3132-method-id-spells-its-class-id-and-invokes-it`, a METHOD-ID spelled
like its CLASS-ID in a method whose body also writes `INVOKE PB3132X "NEW"`, and retired PB3132 because the shared
class census refused the METHOD-ID. Review round 2's per-method census (rightly: a method is a source element of its own,
§10.5) made the METHOD-ID legal, and with it the INVOKE: the class-name a method references is declared by its CLASS, so
no declaration of the method's announced it, while §8.3.2.2 says "used". Both intents now hold: `OoNameResolution.Lookup`,
the one class-name and interface-name resolver, records every name it resolves in the referring METHOD's census
(`OoClassTable.RecordReferenceUse`; `UserWordCensus.Record` takes a declaration or a use; `UserWordCensus.ConflictMessage`
is the one COBOLNET2692 wording), so the second of the declaration and the reference is refused, and a method that
never references the class keeps its class-named METHOD-ID (`2002/pb2921_class_named_method` unchanged). New negative
`pb3132-method-paragraph-spells-a-class-it-references` pins the reference-first path (a `USAGE OBJECT REFERENCE` item,
then a same-spelled paragraph) beside a sibling method that declares the word as a data-name and stays legal. A
drift test keeps the two census recorders the only ones. PB3132 reopened and landed with this branch; the nested-program
residue its note names (an inherited configuration name used beside a local declaration of another type) is unchanged.
Finisher gate run `20261014T083522Z-e09c86`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`; both
PB3132 negatives and `TheCensus_IsRecordedOnlyByTheDeclarationAndReferenceFunnels` Passed in leg 1; semgrep verify PASS.

Review round 4 (R4-1, answered by wave 1076 A): the reference record reached only a METHOD census, and its comment
justified that with "a reference outside every method is its own element's declaration already". That is false for a
contained program, whose container's REPOSITORY applies to it (§12.3.4 GR1) without being its declaration (§10.5): the
reviewer's `E:\tmp\r4probe\n1.cob` (a contained program with `USAGE OBJECT REFERENCE RQX` and a paragraph `RQX.`, the
class declared by the container) compiled clean while its flat twin was refused. Re-probed on the merged tree: compiled
clean. The fix makes the census one object per source element for the whole compilation: `OoClassTable.CensusOf(site)`
(a method's is its symbol's; every other element's is kept by parse node), the binders take theirs from it
(`DataBinder.OwnWords`, passed by `BinderDriver.BindUnitData` and `OoDriver`), and `RecordReferenceUse` records into it
whatever element refers, so a reference and a declaration meet whichever runs first. The first build of that shape
refused three parameterized goldens (`pb2051_parameterized_definition_binds_itself` and both `pb2937` skeleton goldens):
the skeleton's binder declares a formal's REPOSITORY specifier as an object-class-name or interface-name, while the
reference recorded it as a parameter-name. The reference now records the type the formal's own specifier declares
(§11.3.3 SR8 / §11.6.3 SR4). A compile sweep of the 633 corpus programs carrying CLASS-ID, INTERFACE-ID or REPOSITORY
draws no COBOLNET2692 the corpus does not pin. After the fix the reviewer's probe is refused at the paragraph, like its flat
twin. New negative `pb3132-contained-program-paragraph-spells-a-class-it-references` (with a sibling contained program
that declares the paragraph and never references the class) and positive `2002/pb3132_contained_program_word_it_never_references`.
A factory or instance definition shows no change: its binder already declares the class's REPOSITORY entries in its own
census (a pre-existing over-refusal, reported as a lead). Also still uncovered and reported as a lead: a contained program
USING a word a containing program's SPECIAL-NAMES passes down (a class condition's class-name) beside a same-spelled
paragraph (`E:\COBOL-coord\scratch\w1076a\s1.cob` compiles clean). The merge with main re-pinned the runtime surface hash
(800009C74EC953FB): both sides had raised the major from 2 to 3 for their own incompatible changes, and no 3.x has shipped,
so one major covers both.
R4-1 gate run `20261014T100113Z-d14226`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`; the new
negative and positive, both earlier PB3132 negatives and the census and runtime-pin drift tests Passed in leg 1; semgrep
verify PASS.

**Finisher: the landing's Linux gate (two reds, both this branch's).** `enqueue.py land` stopped on 739b86210 with the
Linux unit and conformance legs red. (1) `NulByteDriftTests.NoTrackedTextFileContainsANulByte`: the golden
`2014/pb546_odo_national_tail.out` pinned the raw UTF-16BE bytes of `N"X"` and `N"Y"` (00 58, 00 59), two NULs, so every
grep over the corpus read it as binary. The program now stores `NX"5831"` and `NX"5932"`, characters whose two bytes are
both printable, so DISPLAY, the GR9 group MOVE and the record round trip still pin every byte of the image, as text. The
drift test's hint `grep -c -P` counted 0 on a file holding a NUL (grep treats it as binary); it now says `grep -c -a -P`.
(2) `IntrinsicFunctionDifferentialTests.StagedLoudResidue_FailsLoud_NeverWrong` pinned as a loud stage the very shape
PB3030 closed (a runtime-length table beside a bit OCCURS DEPENDING table, now 0003 as the golden
`2014/pb3030_bit_odo_variable_group` pins): a green test holding a closed gap open. It now proves the doctrine on the
residue that is left, the ALIGNED OCCURS DEPENDING table of a GROUP-USAGE BIT group under LENGTH (§15.50.4 r1 counts
boolean positions; PB2498). Neither red was in the Windows gate's leg 1: the NUL sweep is a Sweep class too dear for tier
0c, and with no impact map the residue test was a leg-2 case; the landing's Linux whole-population legs caught both.
