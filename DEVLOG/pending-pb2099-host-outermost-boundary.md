## PB2099 parts 1-4: the host's outermost activation boundary, R48's run-unit-ended result, the host type table, the door rules, the host entry and the published record struct

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
