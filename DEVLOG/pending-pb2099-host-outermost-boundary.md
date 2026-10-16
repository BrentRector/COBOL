## PB2099: the .NET host surface lands (parts 1-7): the outermost activation boundary, R48's run-unit-ended result, the host type table, the door rules, the host entry, the published record struct with OCCURS DEPENDING, reference and variable-length members, the library output kind and the one host surface

Wave 1077 group E took kb/Work PB2099, slice 3 of DESIGN-external-repository: the typed host entry and the one host
surface. Two earlier waves had left it unstarted. The re-probe at a6744fa4d found none of the slice in the tree:
`ProgramTable.CallProgram` and `Cancel` took a non-null `callerPath`, `RunUnitEndedException`, `HostSurface`,
`HostLanding` and `HostEntryEmitter` did not exist, and `RoslynBackend` always emitted a console application. The
slice is five mechanisms. This branch lands the first one, the runtime boundary, and stops at that boundary for a
finisher (SPLIT).

**The mechanism.** A .NET host is an activator outside every program. `CallProgram(name, callerPath: null, ...)` now
means exactly that. A CALL written in a METHOD keeps `""`: it is contained in no program, but it runs inside the run
unit. The null arm, `ProgramTable.CallFromHost`, is the run unit's outermost boundary, the host's counterpart of
`RunMain`. The activation itself is still the one §14.9.4.4 GR3 boundary every COBOL CALL crosses (`Activate`). What
the arm adds is the ending. A STOP RUN performs "The operations described in 14.6.11, Normal run unit termination"
and then "Execution of the run unit terminates and control is transferred to the operating system" (§14.9.42.4 1) and
6)). A run-unit fatal terminates the run unit abnormally (§14.6.12). In both cases `RunUnit.EndForHost` performs the
§14.6.11 actions and replaces the host's ambient run unit with a new one, and the host receives
`RunUnitEndedException(ExitStatus, Abnormal)`, the one documented type owner decision R48 requires (DOC-A.1-167). A
`CobolCallException` still leaves as itself, which is item 2 of the design's exception contract. The arm recognizes
the ending with an exception filter and runs the epilogue after the try statement, never inside a catch block. A
catch block runs on top of the stack the condition was thrown from (kb/Work PB2659), and a host thread has only the
.NET stack margin.

**One list.** `ProgramTable.EndsRunUnitAbnormally` (`CobolFatalException`, which includes `CobolSizeError`, and
`CobolImplementorFatalException`) is now the one list of run-unit fatals. The COBOL main's boundary (`RunOnThisThread`)
and the host's boundary both read it. Before this, the main boundary spelled the list out as three catch clauses.
`RunUnit.Run` also ends a replacement run unit that its body's activations left behind.

**Witness.** `conformance-test:HostRunUnitEndingTests` is a real C# host process. It calls a counter program twice
(COUNT 1, COUNT 2), then a program that leaves a file open and runs STOP RUN WITH ERROR STATUS 7. The host gets
`ENDED STATUS=7 ABNORMAL=False`, and the file holds HELLO, so the implicit CLOSE ran. The counter then prints COUNT 1,
so the next activation ran in a new run unit. A NULL BASED reference gives `ENDED STATUS=1 ABNORMAL=True` with the
EC-DATA-PTR-NULL reason. The same sequence also runs inside `RunUnit.Run`. The host exits 0 with nothing on stderr.
With the arm disabled, the transcript stops after IN STOP SUB because `StopRun` escapes into the host. The three
existing test hosts (NonCobolActivatorReturn, NonCobolActivatorLinkage, DefaultLocaleDetermination) now pass the
host's `null`.

**Surface.** The four `CallProgram`/`Cancel` lines in `PublicAPI.Shipped.txt` change from `string!` to `string?`.
That is a nullable-annotation relaxation: the metadata signature and every existing caller stay the same, so
`RuntimeAbiPinDriftTests` is re-pinned without a major bump. `RunUnitEndedException` goes into Unshipped. The docs
changed in the same commit: DOC-A.1-167 names the type, and its "Diverges today" now covers only the ambient run
unit's files at process exit (PB1253). INTERPROGRAM gains the host-activator paragraph, and
DESIGN-external-repository §21 item 3 records what is in the tree and one question to settle before the emitter:
§4.3's `HostSurface.TypeOf` takes `DescriptionRecord`, a §6.1 type that slice 4 introduces.

**Not done (the finisher's):** `HostSurface`/`HostLanding`, the published `RecordStructEmitter` mode, `HostEntryEmitter`,
`OutputKind` library, the host harness, the §17.3 drift tests, and the retirement of the untyped host route.

**Self-review.** One list of run-unit fatals for both boundaries. No catch-block epilogue on the host's stack. The two
comments that claimed more than they proved were corrected. Semgrep verify passed with no rule count up.

**Gate.** `TestResults/build-local/20261014T192801Z-ea375c` (HEAD 2a098d875): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched
gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 5,539 · Unit 636 · Characterization 30 …
Conformance 6,126/11,665 · Unit 32,981/33,617 · Characterization 6/36 cases ran (skipped 0) in 1 of 2 leg(s)`.
`HostRunUnitEndingTests` passed in leg 1.

### Part 2: the host type of a description and the door rules (wave 1078 group C)

Wave 1078 group C cherry-picked part 1 onto main (c29af1704) and took the second mechanism: the one function that
names a description's .NET host type and the one runtime class that lands and decodes host values. The re-probe found
neither in the tree (`HostSurface`, `HostLanding` absent; part 1's boundary present and building 0/0).

**The design question, settled.** §4.3 named `HostSurface.TypeOf(DescriptionRecord)`, a §6.1 type slice 4 introduces.
§6.3 decodes a repository record into a codec-owned forest of bound `DataItem`s so derived members answer as on a
bound item, so the bound description is what both the emitter and the decoder hold. `TypeOf(DataItem)` is therefore
the final key, not an interim one; §4.3 and §21 item 3 say so.

**`HostSurface.TypeOf`** (`Binding/Model/HostSurface.cs`) returns a `HostType(Kind, CsType)`. "Decoding never narrows"
drove two corrections to the table. A capacity-truncated binary item with no scale is mapped by its container's exact
range: an unsigned 16-byte COMP-5 item holds [0, 2^128) and needs `UInt128`, which the table's "first of long,
Int128" lacked. A display item's P positions are counted once ("The symbol 'P' is not counted in the size of the
item", §13.18.40.4 14)); the first run of the witness caught `PIC 9(20)P(11)` mapped to `CobolDec` because
`UnscaledDigitBound` counts P positions as digits. One storage has no exact .NET type: an unsigned 16-byte container
with a scale reaches 2^128 units, past `CobolDec`'s `Int128` significand. §14.2.3 13) lets the implementor state the
restriction, and it is stated in the table: content of 2^127 units or more is refused by the decode, never narrowed.

**`HostLanding`** (runtime, `[EditorBrowsable(Never)]`, 15 Unshipped lines) is the one home of §4.3's door rules. A
number lands through the runtime's own COMPUTE store (`CobolNum.TryStore`/`TryStoreU`) under
`CobolRounding.Prohibited`, so "exact" is the capacity rule every COBOL receiver of the description obeys and an inexact
transfer is refused; a negative value into an unsigned item is refused because GR6's "absolute value of the sending
value is used" (§14.9.25.4 6)) drops the sign. A string lands through the MOVE's own alignment (`CobolString.Store`;
zero fill for boolean, §13.18.32.4 2)), refused when it would be truncated or when a boolean position would hold a
character other than 0 or 1 (§8.8.4.4.4 3) e)). `SharesStorage` is the alias rule (§14.2.3 8)), `RequireOccurrences`
the OCCURS array rule, `TryDecode` the validity rule (the class test NUMERIC, §8.8.4.4.4 3) n), as
`CobolNum.IsNumericImage` answers), keeping the first refusal for the entry to throw after every other write-back.

**Witnesses.** `HostLandingTests` (11 facts): the §17.3 loss fixture, with high order, low order, sign, P positions,
each binary capacity, string length and boolean characters refused and the exact boundary values accepted, and the
validity decode agreeing with `IsNumericImage` on zoned, packed and binary images. `HostSurfaceTests` pins every row of
the table over 31 bound LINKAGE descriptions. The expected values come from each storage's range, not from the code.

**Not done (the finisher's):** the published `RecordStructEmitter` mode, `HostEntryEmitter`, `OutputKind` library, the
host harness, the §17.3 drift tests over emitted code, and the retirement of the untyped host route.

**Self-review (part 2).** The decodes `HostLanding.Decimal` and `Integer` would have truncated a significand or wrapped
a product if a caller ever passed a value outside their row; both now throw `OverflowException` there, and the tests
pin it. Semgrep verify passed with no rule count up.

**Gate (part 2).** `TestResults/build-local/20261014T215550Z-036e32` (HEAD 6fad3f257): `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 5,500 · Unit 618 ·
Characterization 30 … Conformance 6,172/11,672 · Unit 33,030/33,648 · Characterization 6/36 cases ran (skipped 0) in 1
of 2 leg(s)`. The 11 `HostLandingTests`, `HostSurfaceTests` and `HostRunUnitEndingTests` passed in leg 1.

### Part 3: the host entry for elementary formals (wave 1079 group B)

Wave 1079 group B cherry-picked wave 1078 C's seven commits onto main (no conflicts) and re-gated them GREEN
(`20261015T002520Z-3592f5`). The re-probe at that head found `HostSurface` and `HostLanding` in the tree and no
`HostEntryEmitter`: a compiled module still offered a host only `ProgramRegistry.CallProgram`.

**The mechanism.** `HostEntryEmitter` writes one `public static class` per outermost unit into the module namespace,
named by `CsNames.Allocate` over a type scope seeded with every name the module already declares there. Its `Call` is
the .NET counterpart of a CALL statement and holds exactly §4.3's parts. `__CobolModule.EnsureRegistered()` comes first
(a definition's entry only). Each host value is then landed through `HostLanding` into a detached storage of the
formal's description, before anything is activated. One variable passed to two `ref` parameters of one description is
one storage (§14.2.3 8)), and of two descriptions an `ArgumentException`. One `CobolArg` per formal states what a CALL
site that checks EC-PROGRAM-ARG-MISMATCH states. The one `ProgramRegistry.CallProgram(name, null, …,
siteArgMismatchChecking: true)` follows, with a `finally` that decodes every BY REFERENCE variable, then the RETURNING
decode. `Cancel` is `ProgramRegistry.Cancel(name, null)`; a function has none. The detached storage of a fixed-point
formal is its record image under its `NumProfile`, so the write-back sees the content the activated program left (a
REDEFINES view can leave non-numeric content, which no native cell could hold) and decodes it only when the class test
NUMERIC is true of it (§8.8.4.4.4 3) n)). A float or index formal crosses in its native cell, a pointer or object
reference in its slot, and a character formal as its image. The length and profile each argument states are now one
rule with the CALL lane's: `CallEmitter.ArgumentLength`, `BoundaryLength` and `BoundaryProfile` gained item-level forms
that the place forms call. A unit with a GROUP formal or RETURNING item publishes no entry yet
(`HostEntryEmitter.ElementaryOnly`, which part 4 replaced): the published record struct was the next step.

**Witnesses.** `HostEntryTests` is the §17.1 host harness: a C# host compiled against two modules and run out of
process. Every expected line comes from the standard. 100.25 lands exactly (§14.2.3 9)), and 1.234 into two fraction
positions, 12345678 into seven integer positions and an 11-character name into X(10) are each refused with the
parameter named. The count of the activations that did run shows nothing was activated. A BY VALUE COMP-5 flag adds
into the amount. An omitted OPTIONAL formal's condition is true (§14.9.4.4 11)). WORKING-STORAGE keeps its count
across calls until `Cancel()` (§14.9.5). `Call(ref a, ref a, ref c)` shows `B=0005`, and one variable given to `PIC
9(4)` and `PIC 9(8)` formals is refused. A REDEFINES view's `ABCD` leaves the host's `long` at 7, writes the other
formal back and throws `FormatException` naming `LK_N`. 18- and 20-digit integers (`long`, `Int128`) round-trip, a
null data pointer reaches the program as NULL, and a function's entry returns its RETURNING item.
`HostEntryShapeDriftTests` reads the emitted C# of a fixture covering every elementary landing row and asserts
§17.3's "the host entry is a caller" (only §4.3's statements; one `CallProgram`; no catch, instance, `ICobolProgram`,
`ProgramTable` or EC-name test; `Cancel` one `ProgramRegistry.Cancel`) and "the door rules are one function" (each
landed storage is a `HostLanding` call, with no inline comparison). It also checks that the parameter types are
`HostSurface.TypeOf`'s rows. The 14 characterization snapshots gained their modules' entry classes. A compile sweep of
the 487 corpus programs that declare formals or a RETURNING item gave 121 entries with landings, and no failure came
from an entry. Its 32 failures were sources the sweep compiled without their manifest's options.

**Self-review (part 3).** Code review found that a .NET null passed for a data pointer would have reached the program
as a null reference. It now lands as `ManagedPointer.Null`, the value the predefined address NULL denotes, and the
harness pins it.
The duplication review found that the entry restated the CALL lane's argument-length rule; that rule is now one
function. Semgrep verify passed with no rule count up.

**Gate (part 3).** `TestResults/build-local/20261015T005018Z-521f40` (HEAD 0d945da2f): `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 5,456 · Unit 482 ·
Characterization 30 … Conformance 6,221/11,677 · Unit 33,183/33,665 · Characterization 6/36 cases ran (skipped 0) in 1
of 2 leg(s)`. `HostEntryTests` and `HostEntryShapeDriftTests` passed in leg 1.

### Part 4: the published record struct (wave 1080 group B)

Wave 1080 group B cherry-picked wave 1079 B's thirteen commits onto main (2b60d9fa9) with no conflicts and re-gated
them GREEN (`20261015T010318Z-bd98a1`). The re-probe: `HG1`, whose formal is a group with a FILLER, a COMP-3 member,
a nested group and a table, compiled to a module with no host entry at all, because `ElementaryOnly` refused the unit.

**The design correction.** DESIGN-external-repository §4.3 said the published record struct was a "published mode" of
`RecordStructEmitter`, sharing the internal record struct's member walk and `GroupImageCodec`. The probe showed that a
group FORMAL has no internal record struct: the program reads it through windows over its area (`_redef_LK_REC`). So
there was no member walk to share. What the published struct does share is the storage geometry and the door rules,
and §4.3 now says so.

**The fix.** `PublishedRecordEmitter` renders the record from the description. Each named, non-redefining subordinate
becomes a property of its `HostSurface.TypeOf` type. A subordinate group becomes a nested record struct, and an
OCCURS member becomes an array. Each member's place is `BitLayout.StartBitWithin`, the one §8.5.1.6.3 walk that the
binder's class offsets and the record-struct codec also use. A first version read `ClassBitOffset`, which exists only
on an area: a RETURNING group is a record struct, and its unit published no entry. The occurrence stride is
`BitLayout.StrideBits`. FILLER and a REDEFINES view's tail are not members. The struct keeps the image `FromImage`
read, so those positions round-trip, and a record the host builds holds spaces there.

Landing goes through `HostLanding` exactly as for an elementary formal: `Land`, `LandText`, `RequireOccurrences`,
and the new `RequireImage` for `FromImage`. A lossy, null or wrongly sized member is refused with the parameter and the
member named (`LK_SUB.LK_X`, `LK_TAB[1]`) before anything is activated. Decoding is `HostEntryEmitter.NumericDecode`,
now the one numeric decode for an elementary formal and a member alike. If any member fails the class test NUMERIC,
the record is not decoded and the host's variable is unchanged. A bit or national group crosses as the elementary item
it is treated as (§14.8.2.1's NOTE), which is how the CALL lane carries it. `CobolBits.WriteWindowInto` writes a bit
member in place with the same `Blit` law. `ElementaryOnly` is gone. `Published` admits a unit when
`PublishedRecordEmitter.Renders` accepts every group it has. The shapes still refused are those the CALL lane carries
as a cell area or a variable-length carrier: a pointer or object leaf, a dynamic-length item, a dynamic-capacity
table, and an OCCURS DEPENDING ON table. They are the next step.

**Witnesses.** `HostEntryTests.AHost_PassesGroupsAsPublishedRecords_…` is the host harness's group case. Every
expected line follows from §14.2.3 8) and 9) and the class test NUMERIC:
- A record lands, and the program's stores come back through it: `42 [BOB       ] -124.45 -7 [AB ] 0,10,0 Z0`.
- A RETURNING group returns as a record.
- A lossy COMP-3 member, a two-entry array for an OCCURS 3 member and a null text member are each refused before
  activation.
- FILLER content survives `FromImage` → `AsImage`.
- A REDEFINES view's `ABCD` in a numeric member leaves the host's record at `7 [AAA]` and throws `FormatException`
  naming `LK_G.LK_N`.
- National members and two USAGE BIT members that share a byte go in and come back.
- An OPTIONAL group is omitted, then passed.
- A national group (GROUP-USAGE NATIONAL) crosses as its national positions and comes back; a FLOAT-LONG member
  doubles, an INDEX member round-trips and a JUSTIFIED RIGHT member lands right-aligned.
- A function returns a group.

**Self-review (part 4).** The added witness found a defect: an INDEX member was decoded by the validity rule, whose
profile has no digits, so every record with an index member came back as `FormatException`. A class condition may not
be asked of an item of class index (§8.8.4.4.3 1)), so the member is now decoded as the record-struct codec decodes
it. `SortNumericKeyLaneTests` names `PublishedRecordEmitter` among the codecs allowed to decode an image, and the
call site says why.

`HostEntryShapeDriftTests` gained a group fixture. Every landing in a record's `__Put` is a `HostLanding` call, and
every decode in `__TryGet` is `HostLanding.TryDecode` or a nested record's. No record writes a comparison other than a
table's loop bound. The parameter and return types are the records. A compile sweep built all 335 corpus programs that
declare formals or a RETURNING item, with `--source-format auto` and their edition. 29 of them now publish records,
and none failed.

**Gate (part 4).** The first gate (`20261015T013921Z-c4ffb9`) was RED on `SortNumericKeyLaneTests`, which did not yet
name the published record among the image codecs; the self-review fix above followed. The re-gate,
`TestResults/build-local/20261015T014717Z-6f1cf9` (HEAD ef95b6cb8): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 5,549 · Unit 471 · Characterization 30 …
Conformance 6,129/11,678 · Unit 33,195/33,666 · Characterization 6/36 cases ran (skipped 0) in 1 of 2 leg(s)`. Both
`HostEntryTests` cases and `HostEntryShapeDriftTests` passed in leg 1. Semgrep verify passed with no rule count up.

### Part 5 (wave 1082 group B): OCCURS DEPENDING, reference members, the library, the retirement begun

Wave 1080 B's branch had not landed, so its sixteen commits were cherry-picked onto main. One conflict came up, in
`RuntimeAbiPinDriftTests`. Main had raised the runtime major to 4 with its own pins, so both hashes were re-pinned. The
runtime hash moved only because of the `callerPath` nullable relaxation. The boundary hash moved because the fixture
module now carries host-entry classes, which is a pure addition in the callee's own module. The earlier gates never ran
that pin: it sat in leg 2. A whole Unit and Characterization run on the merged tree passed before any new work.

**OCCURS DEPENDING ON.** `PublishedRecordEmitter.Renders` now admits an OCCURS DEPENDING ON table. Its storage is the
maximum allocation (§8.5.1.8), the image the CALL lane's text crossing already carries. Data-name-1 may sit inside the
record, where §13.18.38.3 20) places it before the table. The array then has as many entries as the DEPENDING ON value,
which §13.18.38.4 7) makes "the current number of occurrences". At landing, `HostLanding.LandedOccurrences` reads the
landed count from the image through the validity rule and the bounds, and `RequireOccurrences` holds the array to it.
The decode reads the count with `HostLanding.TryOccurrences` and decodes that many entries. A count the program left
outside the bounds leaves the host's variable unchanged, with a `FormatException`. A table counted outside the record
is its whole area. `new R()` gives the DEPENDING ON item the table's minimum and gives the array that many entries.

**References.** A strongly-typed group holding pointer or object-reference members (§13.18.60.3 14)) crosses as the
CALL lane's cell area, the same way a COBOL caller's cell-backed group does (`CallEmitter.AreaOf`). The record's
`__Area` is a `StorageCell` holding the image, with each reference in its managed slot (`CobolPtr.SlotWrite`). The
argument states `Area: new CellPointer(cell, 0)`, so the formal occupies that storage (§14.2.3 8)). `__TryGet` reads each
slot back. The witness passes a data pointer the program set back into it, and the program reads its storage through
that pointer.

**Library.** `ProgramEmitter.MainUnit` is the one answer to whether a group has a main program, and `EmitMain` and the
backend both read it. A group without one has no `Main` and is compiled as `OutputKind.DynamicallyLinkedLibrary`, with
no `runtimeconfig.json`. `cobol --run` refuses a library. The dead null arm of `RunModule` went with the old library
`Main`. `CompiledProgramCache` entries now carry `Library` (format 3).

**Retirement, begun.** `ProgramRegistry` is now `[EditorBrowsable(Never)]`, and its remarks call it the generated
code's ABI. DOC-A.1-141 is now design §9.4's determination, with `UInt128` added to the integer row and
`HostEntryTests` as its witness. DOC-A.1-65 and -167 name the host entry as the route. INTERPROGRAM's host bullet names
the entry. The rest is the next implementer's: DOC-A.1-116 with its witness, converting the host tests to the entries,
§17.3's host-route drift test, and the variable-length shapes. The manual's host chapter has no home yet, because
`docs/manual/` does not exist (kb/Work PB1610).

### Part 6 (wave 1085 group A): the one host surface, one renderer, the published names

Wave 1085 group A cherry-picked wave 1082 B's 22 commits onto main f5fec711b with no conflict. The build was 0/0, and the
ABI pins and the host-entry shape test passed on the merged tree before any new work.

**The untyped host route is retired.** The four tests that hosted COBOL through `ProgramRegistry.CallProgram`,
`ProgramRegistry.RunMain` and a reflective `EnsureRegistered` now compile their hosts against the COBOL modules and call
the generated entries: `NonCobolActivatorReturnTests`, `NonCobolActivatorLinkageTests`, `HostRunUnitEndingTests` and
`DefaultLocaleDeterminationTests` (whose run units are now `RunUnit.Run` bodies). The linkage witness lost its
main-entry arm, because no host route reaches `ICobolProgram.Activate` any more. The operating system's activation,
which §13.7.4 3) leaves undefined, stays pinned by the golden `pb2671_omitted_formal_category_default`. The re-probe
also corrected the witness itself: my first rewrite asserted that the RETURNING item starts as spaces, and the program
showed RRRR, the value its instance kept from the last call. §14.2.3 7) says "The initial value of the returning item,
data-name-2, is undefined", so the test no longer asserts it. DOC-A.1-116 now states the entry's determination: access
is guaranteed, each formal starts as the host's landed value, a `null` OPTIONAL parameter is OMITTED, and the RETURNING
item is the return value. The same sweep found five more user-facing rows that described the old route or the old
registrar: §3's return-to-a-non-COBOL-activator determination, DOC-A.1-64 (which also gave `ICobolProgram.Call` a wrong
signature), DOC-A.1-13 (`__CobolModule.Register`), and DOC-A.1-134 and -136 (a host's `RunMain`). All five now name the
entry. `ProgramTable.RunModule`'s remark no longer tells a host to enter through `RunMain`. The renamed witness was
re-sited on GR-13.7.4-3, GR-13.7.4-5 and DOC-A.1-116 with a retirement record (batch
`w1085a-PB2099-host-entry-witnesses`); GAP stays at 101.

**`OneHostSurfaceDriftTests`** is §17.3's one-host-surface scan. It reads every test file's code as Roslyn tokens, so
comments are skipped and the C# hosts a test compiles are scanned. It also reads the user documents (`README.md`,
`docs/CONFORMANCE.md`, `docs/manual/`, `samples/`). Either kind fails when it names an untyped route. The ABI's own
tests are listed with their reasons, and a listed test that names no route is red as a stale exemption. A probe copy
of the old `NonCobolActivatorReturnTests` made it fail on `ProgramRegistry.CallProgram`.

**One renderer.** The host entry built its argument's `Length:` and `Description:` itself. Now
`CallEmitter.Described(built, length, description)` renders what an argument states for §14.9.4.4 3) d), for a CALL
statement and a host entry alike. The entry's arguments now take the with-expression form, so the ABI fixture's
boundary hash is re-pinned. The `CobolArg` fields are unchanged, so `CallAbi` stays 1.
`HostEntryShapeDriftTests.AHostEntryArgument_IsStatedByTheCallLanesRenderer` pins the shared renderer.

**Names.** A collision fixture (AsImage, FromImage, Equals, ToString and GetHashCode as data-names, a word twice in one
record, a member spelling its record's type, and a program whose host class spells a formal's record type) compiles
with every name allocated (`AsImage_2`, `S1_Record_2`, `L_G_Record_2`), and two compiles agree
(`ThePublishedNames_AreAllocated_…`).

**Found on the way.** `ProgramEmitter`'s `EmitMain` summary had been left above `MainUnit` as an orphan; it is back on
`EmitMain`. `ProgramRegistry.RunMain` has no caller now, but removing a public member is a runtime-major bump, which is
a tree-wide change, so it is a lead rather than part of this slice.

**Still open.** The variable-length shapes `PublishedRecordEmitter.Renders` refuses (a dynamic-length item, a
dynamic-capacity table: the §8.5.1.12 carrier), and the manual's host chapter, which has no home until kb/Work PB1610
creates `docs/manual/`.

**Gate.** `TestResults/build-local/20261015T210923Z-d4c692` at fe2ec5ef1: `LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 6,103/11,713, Unit 33,229/33,688, Characterization 6/36 in leg 1). Leg 1 did not hold the changed `NonCobolActivatorReturnTests`, `DefaultLocaleDeterminationTests` or the re-pinned `RuntimeAbiPinDriftTests.TheCallAbi_IsPinnedBesideTheBoundaryLayout`, because the gate puts only added tests in tier 0a; each passed in a direct run on the same build, and the whole Characterization assembly (36) passed. Semgrep verify: PASS, no count moved.

### Part 7 (wave 1093 group B): variable-length groups, and the slice lands

**Cherry-pick.** Wave 1085 A's 28 commits went onto main bfb2eb47e. Main had landed kb/Work PB3163 (a run unit under a
host no longer writes the process exit code) in the meantime, so `ProgramTable` conflicted three times. The merged
`AbnormalTermination` writes the run unit's `ExitStatus` (PB3163) through the predecessor's `AbnormalTerminationStatus`
and report, and `RunModule` keeps PB3163's one exit-code write with the predecessor's non-null `mainPath`. PB3163's new
witness, `RunUnitHostExitCodeTests`, hosted the run units through `ProgramRegistry.RunMain`, which
`OneHostSurfaceDriftTests` refused. It now calls the generated entries. Its fatal arm is a NULL BASED reference, because
an absent program's CALL is a `CobolCallException` the host receives and ends no run unit; that CALL is its own arm.
CONFORMANCE.md's exit-code paragraph, DOC-A.1-136, COBOLNET_DESIGN and the conditions design no longer name `RunMain` as a
route. The boundary hash is re-pinned alone (F1BA4F9B825BEC61): each side had re-pinned alone, and the merge combines
them.

**Re-probe.** `PublishedRecordEmitter.Renders` refused every group with a dynamic-length item or a dynamic-capacity
table, so a unit with such a formal published no `Call`.

**Fix.** A variable-length group (ISO §8.5.1.12) publishes its record. A dynamic-length member is `string`. A
dynamic-capacity table is an array whose length is the current capacity; `HostLanding.RequireCapacity` refuses an array
below the minimum capacity (§13.18.38.4 16): "Integer-4 is the minimum capacity of the table"). The record crosses as
the CALL lane's carrier of its own description (`PublishedRecordEmitter.EmitVar`), so the activated program's formal
adopts it as a COBOL argument of the same description. `__Var` builds the fixed run from the landed image with each
table's reservation cut out. The components follow `CellComponents` order: each dynamic-length member's content goes
through `HostLanding.LandVariable` (refused past its LIMIT). Each table's occurrence images are concatenated, or, for a
table of variable-length elements, each occurrence's own carrier is kept. `__TryGet(CobolVarGroup …)` and `__TryGetDyn`
decode the carrier back by the same validity rule. The fixed-run rule is now ONE function, `CobolVarGroup.RunOf` and its
inverse `WindowOf`, and `StorageCell.RunOf`/`StoreRun` call it. The first run returned `RET` as `"  R"`: a RETURNING
item has no storage class, so `CellComponents.Of` placed every component at offset 0 (probe
`scratch/w1093b/pret/H5.g.cs`). `CellComponents.InLayout` runs the same walk with positions from `BitLayout`. A variable-length
record has no `AsImage`/`FromImage`, because its storage has no single image (§8.5.1.10.3, §8.5.1.9.1). Design §4.3
states one GR13 restriction: a group holding both a variable-length component and a pointer or object reference
publishes no entry.

**Witnesses.** `HostEntryTests.AHost_PassesVariableLengthGroups_AsTheCallLanesVariableLengthCarrier` uses a real C#
host, out of process. It covers LIMIT truncation (§8.5.1.10.4), implicit table growth (§8.5.1.9.3), a table of
variable-length elements, a fixed table of them, an OCCURS DEPENDING ON table after a dynamic-length item, a RETURNING
item, and four refusals before activation. `HostEntryShapeDriftTests` adds an H2VAR arm.

**Rows.** Batch `w1093b-PB2099-host-surface-gr13`: GR-14.2.3-13 and DOC-A.1-141 PARTIAL → CONFORMS (PB1149's GR13
half), DOC-A.1-167 DIVERGES → CONFORMS (R48), GAP 98 → 95. PB2099 is landed and closes those rows. PB1149 keeps its
INVOKE half. The five R3 notes that waited on PB2099 now wait on PB2100.

**Still open, not this slice's.** The manual's host chapter belongs to kb/Work PB1610, which creates `docs/manual/`; its
text is design §9.4's. `ProgramRegistry.RunMain` has no caller, and removing it is a runtime-major bump.

**Gate (part 7).** `TestResults/build-local/20261016T140250Z-5116d8` at 8c067d41d: `LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 6,015/11,776, Unit 33,206/33,714, Characterization 6/36 in leg 1; every test this part added or changed ran in leg 1 and passed). The first gate (`20261016T135015Z-7dcf04`) was RED on `RuntimeApiGuardTests` only: three bare `CobolVarGroup.` accesses in `PublishedRecordEmitter`, now routed through `RuntimeApi` (8c067d41d). Semgrep verify: PASS. LINUX GATE: the lander's (batched gating).
