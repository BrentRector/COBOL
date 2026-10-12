## Entry 1939 — 2026-10-08 09:36 PDT — Train 1042: wave 1042's C, F, B, D, A — variable-length element tables, DPC function text and MinValue-safe division, GR9's MOVE leg, keyed files across run units, table initial state from occurrence runs (PB2496, PB2507, PB2640, PB2641, PB2587, PB2660, PB754, PB1722, PB1952 landed; PB2689–PB2693 filed)

**C — PB2496 (landed; PB2497 split).** A dynamic-capacity table whose elements are variable-length groups had no
carrier shape, so `MOVE G2 TO G3` aborted with a named NotImplemented. The re-probe also found a wrong answer on the
MOVE arm: compatible variable-length groups of DIFFERENT shapes were handed over ordinally (`x  a` where §14.6.9.2 and
§14.6.9.3 give `xa dd`). `CobolVarGroup` gains `Elements`: such a table is one nested component whose occurrences ride
as their own carriers, and `Reshape`, `Overlay` and `Compare` recurse per element (`IsNested` is the one test).
`CobolDynTable.Recreate` is the one §14.6.9.2 recreation; MOVE reshapes through `PlaceRenderer.VarGroupInShape` and the
relation walks both shapes. Goldens `2014/pb2496_vlg_dynamic_element_tables` and `…_call`, negative
`pb2496-dynamic-element-table-below-2014`. PB2497 (`WRITE REC`) still aborts and stays open with its design input. The
train gate found one red of C's own: GR-14.9.25.4-9 still named `PlaceRenderer.cs#VarGroupCarrier`, which C deleted; it
is re-sited to `VarGroupOperand`/`VarGroupInShape`. C's four leads are filed: **PB2689** (`GroupCompatibility.Walk`
compares the static `ElementBytes` before it recurses, so §8.5.1.12.3's dynamic-table length rule is never applied
inside an element pair: legal source refused with COBOLNET1931), **PB2690** (the CALL and INVOKE boundaries still pair
a fixed group by flat `CorrespondingSpans`, a named loud on legal source; and every `Compare`/`Reshape` allocates its
atom literals), **PB2691** (two USAGE BIT groups of different shapes still move and compare ordinally; unprobed).

**F — PB2507, PB2640, PB2641 (landed).** Under DECIMAL-POINT IS COMMA a numeric function's returned value printed
`.`. §12.3.7.4 GR14 a) makes the comma a numeric literal's decimal separator and the text is the literal form
(DOC-A.1-92 amended; GnuCOBOL 3.2 agrees). Four lanes now pass the unit's mode: the native and SDIDI calls, the
materialized temporary (`DataItem.FunctionValueSeparator`) and the compile-time fold. PB2640: NEAREST rounding doubled
the remainder, which wrapped negative on Int128 past a 2^126 divisor (9.025e37 ÷ 9.409e37 stored 0); `BumpsMagnitude`
compares |rem| with its complement and is the one rounding decision. Its sibling sweep found Int128.MinValue operands
dividing wrongly (MinValue ÷ −3 negative, ÷ −1 MinValue); they now divide on unsigned magnitudes (`LandQuotient`).
PB2641: −2^127 converted to float 0; the conversions test `CobolDec.UAbs`. Five goldens (one at 85, four at 2023).
The train gate's red of F's own was `AnnexA1RegisterDriftTests`: the amended row named the DOC-A.1-56 row key in prose;
it now says "A.1 item 56". F's leads, the remaining per-site Int128.Abs magnitudes, are appended to **PB2639**, which
owns that mechanism.

**B — PB2587 (landed; PB2097 not started).** §14.2.3 GR9's second regime fills a non-numeric formal's record
"otherwise, a MOVE statement", but every CALL lane adopted the argument's image: `ABCD` into `PIC X(6) JUSTIFIED RIGHT`
was `ABCD  ` and −12 into `PIC X(4)` was `01K `. A program defined after its caller is a GR10 c) run-time-located
CALL, so the run-time lane is the common case. One receiving half (`MoveEmitter.CharacterStore`) serves the MOVE
statement and both lanes: the activating element performs the MOVE when it knows the formal, and otherwise states
`CobolArg.Sending` for the registered `MoveLanding` to apply at the boundary (GR9's first regime untouched). The
PB1113 and PB1946 special arms are deleted. Golden `2002/pb2587_content_move_leg`; GR-14.2.3-9 closed. The lander
checked one interaction with F: `CobolArgAdapt.WithValueSending` renders a fraction with `.`, but Table 16 forbids a
noninteger numeric MOVE into an alphanumeric receiver and an edited receiver takes the value, not the characters, so no
legal program reaches it. PB2097 has now been attempted five times; its note carries the design pointer.

**D — PB2660, PB754 (landed).** Two run units under SHARING WITH ALL OTHER lost each other's RELATIVE/INDEXED updates
and ignored each other's record locks. Store format 3 carries a generation; a connector that may meet another run
unit's writer runs each record statement under a byte-range store mutex, reloads when the generation moved and
persists what it changed. Record locks publish as host byte-range locks (`HostRegionLocks`: LockFileEx, Linux OFD), and
a RETRY waits outside the mutex. PB754: `KeyedStoreTable.AttachCreated` is the only emptying. **The train gate's six
Conformance reds were D's** (`pb322_clauseless_reader_sees_record_lock`, `l1_read_self_lock_ignored`,
`l1_read_lock_mode_governs`, `l1_lock_mode_omitted_sets_no_lock`, `gn1_lock_mode_omitted_implementor_default`,
`pb683_open_sharing_read_only`: 51 expected, 00 printed), and the lander's review had already found the mechanism. On
Windows, `PhysicalFileTable.Publish` held an OPEN INPUT handle's record byte shared and then tested it with an exclusive
`LockFileEx` through the same handle. Windows locks are per handle, so the test met its own hold, `LockRecord` answered
51 (which the READ discards) and the lock was never set. Implementer D's leg 1 never ran those six cases, and its
Linux-shaped reasoning (OFD tests ignore the asker's own hold) was correct only on Linux. A Windows handle now holds
exclusively (`HostRegionLocks.ExclusiveNeedsWritableHandle`); the regression is
`CrossRunUnitKeyedStoreDriftTests.AnInputConnectorsReadWithLock_Succeeds_AndIsSeenByAnotherRunUnit`, and
DESIGN-runtime-library §2.2 says so. DOC-A.1-107's `RecordFraming.cs#WriteStore` was re-sited too. D's leads are filed:
**PB2692** (sequential record locks are not published across run units) and **PB2693** (an indexed duplicate-key walk
renumbered by another run unit's reload; RESERVE inert for keyed files, DOC-A.1-164). GR-12.4.5.9.4-2 is released, not
closed (owner signature needed; macOS is PB2484).

**A — PB1722, PB1952 (landed; PB1941, PB1965, PB2051 not started).** A Format 2 table VALUE was a per-element map, and
the record-struct lane emitted a per-occurrence array literal: the filed 1000×1000 table took 42.8 s, 2.5 GB and 27 MB
of C#. The plan is now its phrases (`LiteralAt`: GR15 last-first, GR13 modulo; `TableValueRuns.Of`; `RankForm`), and
one emitter (`OccurrenceRunEmit`) serves every lane: `CobolTable.Fill`, image repeats, switches and INITIALIZE's rank
ranges. After: 2.9 s, 224 MB, 3.6 KB; the 10000×10000 table in 1.7 s. No element-count ceiling: ISO is silent and a
ceiling would reject legal source. PB1952: FLAG-02 d) and e) now resolve a method body's operands. Golden
`2002/pb1722_table_value_runs`, negative `pb1722-table-value-overwritten-literal-class`. The train gate's reds of A's
own: a two-line section banner that `VacuousTestDriftTests` read as an empty section, and code locations recorded as
`TableValuePlan.LiteralAt` and `TableValueRuns.Of`, spellings that occur nowhere in the file (re-sited). PB1941 (a)
still gives COBOLNET0899; PB2051 still compiles; PB1965 needs an owner layering decision.

**The train.** Five clusters dispatched, five landed, none ejected or dropped. Four clusters' reds were fixed in the
train: D's Windows reader lock (six Conformance cases), stale inventory sites in A, C and D, A's banner and F's doc
token. Gating was batched: every implementer gated leg 1 only. All five branches applied cleanly on `7fb5cab75`, which stayed
origin/main under the landing lease (taken at once), so no rebase was needed. Whole-population run 1 (run
20261008T155543Z-3f7735) was RED: Conformance 6 and Unit 3 failures. Attribution re-ran only the failing cases on each
cluster's branch head alone (7 minutes, no interaction): D 6 Conformance + one inventory site, A two, C one, F one, B
none. Run 2 (run 20261008T161520Z-b26f2c) was GREEN: Conformance 11,248/11,248 · Unit 32,776/32,776 ·
Characterization 36/36. The Linux gate was GREEN on `8d3bf88e2` (unit 32,807, characterization 36, conformance
11,248, guard: NIST 362 MATCH, 0 regressions). The lander's review of the train diff found one correctness defect, D's
Windows reader lock, fixed in D's commit. The checkpoint history was rebuilt as one commit per cluster, and its tree is byte-identical
to the gated one. The oracle differed in 642 of 7625 cases against 39a6ad782bfe: 631 emitted-C# changes and 11 added
goldens, no diagnostic changes. Per-cluster captures attribute every case: A 503 (table initial state as
`CobolTable.Fill` over occurrence runs, switch arms, rank-range INITIALIZE), B 59 (`formals:` MoveLanding
registrations, `with { Sending }` arguments, moved records), C 44 (`SpaceFillElements` takes the element image,
`Reshape`/`Compare` over atoms replacing `To/FromFixedImage`), 31 cases changed by two or three of A, B and C, F and D
none. Re-recorded as 8d3bf88e2926. Semgrep unchanged (biginteger 46, decimal 2, rendered-text 3, raw-code 268). CI
audits all green; work register 2236 items well-formed. Inventory GAP 136 → 136; no diagnostic codes claimed
(COBOLNET3194–3205 and 3209–3211 returned).

