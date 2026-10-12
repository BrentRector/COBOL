## Entry 1474 — 2026-09-05 01:26 PDT — Landing train 3: five clusters in one landing — PB231's byte-window carriage gate, PB485's level-number domain, PB230's EC-DATA-INCOMPATIBLE wiring, PB236's split of BoundUnsupported and PB235's Table 14 (GAP 2925 -> 2906)

**PB231 — the byte-window carriage gate collapses to ONE predicate, and a USAGE BIT leaf now rides the shared
cell.** `01 R BASED. 05 B PIC 1(8) USAGE BIT.` drew a bind-time COBOLNET1695 while its byte-identical REDEFINES
twin compiled and ran. §13.18.5.3 SR1/SR2 restrict a BASED subject to "shall not be of class object" and "shall
not be a dynamic-length elementary item or a variable-length group", §14.9.3.3 SR1 asks only for the BASED
clause, and §13.18.22 conditions EXTERNAL on nothing subordinate — so that was rejects-legal-source, the same
argument that retired `pb151-based-comp-leaf` at the PB164 landing. The cause was two spellings of one law:
`ComputeTier` (REDEFINES) was a deny-list and `ForceStringCanonical`'s `CellCapable` (EXTERNAL / ADDRESS-OF /
BASED cells) an allow-list, and PB203 opened only the first — the NINTH instance of this repo's two-arm dispatch
shape, on the same pair whose eighth instance the Step D wave already fixed once. The machinery was never
missing: `AssignClassOffsets`' bit-laid arm already walks the §8.5.1.6.3 cursor and `CobolBits.ReadWindow`/
`WriteWindow` already window a bit member over a class backing, both shared by the cell path; only the gate was
closed, which is why the fix needed no emitter change. The fix is the collapse, not the widening (rule 5):
`DataBinder.ByteWindowResidueOf` is now THE ONE byte-window carriage gate — an allow-list returning the residue
clause naming why a leaf is refused — and both callers route through it, each wrapping the clause in its own
surface phrasing. Two smaller divergences closed with it (a Pic-less leaf and a NATIONAL-usage numeric were
denied by the cell and admitted by REDEFINES), and a third arm surfaced: `PtrBinder.PtrBindAddressOf`'s
COBOLNET0869 printed a hand-written LIST of the reasons a record might not be cell-backed instead of the class's
own `RejectReason`, which its two siblings had always interpolated — so the one surface that could not say what
was wrong was the one the user reads. `ByteWindowResidueDriftTests` (18 cases) pins all four surfaces (REDEFINES
- EXTERNAL — BASED+ALLOCATE — ADDRESS OF) to ONE verdict per leaf, with the agreement assertion stated without
hard-coding the verdict so a future residue landing needs no edit; its failure branch was fired before being
trusted, and it caught the ADDRESS-OF arm on its first run. `conformance:2002/pb231_based_bit_leaf` is the
positive witness, every value derived rather than observed: two same-level 4-bit runs SHARE byte 0
(§8.5.1.6.3 rule 1), so §14.9.3.4 GR3's byte quantity is FOUR where a character sum says five, and GR7's
`WITH FILLER ALL TO VALUE THEN TO DEFAULT` is observed over an unnamed `1(4) BIT` FILLER run whose byte reads
ORD 1 (§14.9.20.4 GR6c's Boolean default). `GR-14.9.3.4-3/-4/-7` re-verdicted with the bit-arm evidence and
stay PARTIAL — GAP unchanged, which is the honest number: the NATIONAL third (RESIDUE-11) and the pointer-class
third (GR9's null-seeding needs an object slot beside the byte image) are each a distinct storage-model
mechanism, and `kb/Work/PB231.md` stays open carrying exactly what each needs. Design D-BW1 records the gate;
DESIGN-data-model §2.3 Step D item 4 gains the as-built note that its own "widen it in the same wave"
instruction was right and still not enough. One citation failed `cite.py --check` during derivation and was
corrected before it reached code (§13.18.29.**1** carries "treated as an elementary item of usage bit"; the
layout rule is §13.18.29.**4** GR1c).

**PB485 — the level-number had no domain, on either of its two axes.** A level-number was screened NOWHERE in the
typed-native front end. `DataBinder` read it with a bare `int.TryParse` and no test of any kind, so `78 K VALUE
5.` — a MicroFocus/GnuCOBOL extension ISO does not define — bound as a memberless GROUP nested under whatever
entry preceded it, compiled clean in the strict `--std 2023` lane, and threw `NotImplementedCobolFeatureException`
at RUN time on any numeric use. `50 BAD PIC X(3).` and a twelve-digit level ran clean too; §4.2.2 requires the
compile-time indication and a run-time abort is not one. `LevelNumberPass` is now the one place a level-number is
judged, running FIRST in `BinderDriver.Bind` because the rules are pure syntax and a bad level makes the storage
tree it heads meaningless. It carries TWO INDEPENDENT AXES: the SECTION (§13.18.33.3 SR2/SR4/SR5/SR6 — four
*different* sets; 77 is legal in working-storage and illegal in a record area, which the legacy `CBL0815`
message's flat "1-49, 66, 77, 88" gets wrong) as COBOLNET1746, and the entry FORMAT (§13.18.33.4 GR2a/b/c,
§13.16.2's format shapes, §13.16.3 SR1/SR2) as COBOLNET1747. Two codes, not one, because the user action
differs. The second axis was hiding the worse bug: `05 R RENAMES A THRU B.` has a perfectly in-range level,
reached the EMITTER, and produced uncompilable C#. The note said two parser arms reach `levelNumber`; there are
FOUR, reaching three binders plus, for `linkageProcedureParameter`, none at all — so the arm list is DERIVED.
`LevelNumberArmDriftTests` reads the `.g4` files and fails until `Classify` names every rule that spells a
level-number and every section that hosts a data description entry; its first form scanned the whole pass file
and was a FALSE GREEN (with the screen arm deleted from `Classify`, the same context type still stood in the
neighbouring `EntryName` switch and the test passed), so it was break-injected, observed, region-scoped,
re-broken and observed RED. Thirteen negative goldens (51 edition-cells) and three positives. Inventory:
SR-13.18.33.3-1..5, SR-13.16.3-1/-2 and GR-13.18.33.4-2 CONFORMS (the two §13.16.3 rows re-adjudicated from
NOT-IMPLEMENTED), GAP -8. SR-13.18.33.3-6 was deliberately left alone — `DOCUMENTED-NON-SUPPORT` is an owner
adjudication about Annex A.4.2, not about the level-number set. `CBL0815` was deliberately not reused: it is
live in the legacy oracle and its flat set is wrong. Registered PENDING and NOT fixed: §13.16.3 SR9's implied
PICTURE (`01 W VALUE "AB".`) emits uncompilable C# on legal source.

**PB230 — §14.6.13.2's non-float rules had no wiring, and the exemption table is now a structure.** `>>TURN
EC-DATA-INCOMPATIBLE CHECKING ON` was inert for every arithmetic statement, every comparison, DISPLAY, STRING, a
SORT key compare — and for a plain numeric `MOVE`. The queue note blamed `EcBinder.EcWrap`'s `node is BoundMove`
gate; the re-probe showed that was only half, because **no fixed-point raise site existed at all**, so even the
statement kind the gate did arm continued in silence. §14.6.13.2 states five sibling rules over one subject
and rule 3 (EC-DATA-NOT-FINITE) was already wired end to end, so rules 1 and 2 got the identical shape at the
same chokepoints. The structural fix is `SendingRef`: rule 2 exempts TWO contexts (class condition, VALIDATE)
where rule 3 exempts FOUR, and the retired `floatCheck` boolean could carry exactly one list — which is *why*
rule 2 was unimplementable without drift. `CobolNum.IsNumericImage` is now the one §8.8.4.4.4 GR3 n)1
predicate and the class condition calls it over the RAW window, correcting an answer that had been reading a
reformatted DISPLAY image and so reporting every packed/binary window NUMERIC. Rule 1 was a silent WRONG ANSWER,
not just a missing condition: a `Q` planted through a REDEFINES window propagated into a boolean RESULT.
§14.7.6's last paragraph — the condition set "after all of the implied statements are completed" — is a
runtime latch region around the CORRESPONDING pairs, the same shape that clause's SIZE ERROR paragraph already
had, and `CobolTable.Sorted` unwraps the framework sort's `InvalidOperationException` so a key-compare raise
still reaches the statement guard. Two sweeps found two more arms after the first fix looked complete (the
boolean string channel; three verbatim-read returns each spelling their own `PlaceRenderer.Read`), and
`RuntimeApi.NumParseImage`'s new REQUIRED `sending` argument makes the next windowed decode state its answer
rather than inherit one — which it did, at this landing: main had grown a THIRD call site after the cluster's
base (`GroupImageCodec.TableElementFromImage`) and the train's first build failed on it, exactly as the
no-default design intends; it takes `sending: false` as the receiving side of a group decode, with the reason
written at the call site. A self-review citation sweep corrected 25 `§8.8.4.4 GR<n>` references to
`§8.8.4.4.4` (four predating this change) plus two wrong rule ordinals. Four goldens across three editions;
GR-14.9.2.4-6, GR-14.9.12.4-3 and GR-14.9.8.4-1 PARTIAL -> CONFORMS (GAP -3). The residue `GR-14.9.8.4-3` -
`ConditionBinder.RefModLen` over-stating a run-time-length reference-modification operand's boolean width — is a
ref-mod defect and not this mechanism, so the row moved to `kb/Work/PB589` and PB230 flipped to `landed`.

**PB236 — `BoundUnsupported` was answering three questions; now it answers one, and the other two answer at
compile time.** It carried a genuine feature DEFERRAL, an ill-formed OPERAND and an illegal PLACEMENT, and
`StatementEmitter` rendered all three as the same `NotImplemented.Run(...)`. Two costs, both re-measured on this
tree before anything changed: a violated syntax rule reached the programmer as a claim that THE COMPILER was
incomplete — at RUN time, with the sec/SR already sitting in the message the binder had built — and on a path the
flow skipped it reached them not at all (`RELEASE <FD record>` and `RETURN <FD file>` behind a `GO TO` compiled
AND ran to normal completion printing nothing, and so did `UNLOCK`, `CLOSE`, `READ`, `DELETE` and `START` of a
file declared nowhere). §4.2.2 ¶2 makes the compile-time mechanism mandatory for exactly this. The
separation is structural, and the part that matters most is the part nobody has to remember: every statement the
grammar accepts funnels through `StatementBinder.BindStatement`, which already positions the diagnostic cursor
(PB82), so a `BoundUnsupported` arriving there is announced as COBOLNET1756 — a WARNING, because a gap in
COBOL.NET must not reject a program the standard gives a meaning to. That is the answer to the asymmetry PB236
measured (two of START's five refusals carried a diagnostic and three did not): a per-site habit produced it, so
the fix is one funnel and not a list, and a `BoundUnsupported` written in any future verb binder inherits the
report by construction. The two user-error jobs then left the carrier: file-name resolution became ONE step
(`StatementValidation.ResolveFile`, replacing nine private copies of `FilesByName.TryGetValue`) reporting the
EXISTING COBOLNET1639, and the operand syntax rules moved into `StatementValidation` under COBOLNET1757 — the
CORRESPONDING group rule in its three spellings, RELEASE §14.9.32.3 SR1, RETURN §14.9.34.3 SR1, and
SORT/MERGE's general-format and key rules — with START's missing-RELATIVE-KEY case (§14.9.41.4 GR8, whose
substitution has no operand) rejecting in the COBOLNET0862 channel that method already used. Adding the announce
immediately exposed a second population of the same defect: FOURTEEN sites reported their diagnostic and still
returned the deferral carrier, so they emitted an error and a contradicting "not implemented" warning together;
each now binds to `BoundNop`, and the sweep deliberately spared `Edition.Removed`/`Warning`/`Declined` sites
where dropping the node under `--permissive` would turn a tolerated construct into a silent no-op. One thing the
note said did not hold: its entire `SR-14.9.2.3-4` section no longer reproduces — PB128's `ScreenResultant`
(`6f85040d`) closed it, and closed the ref-mod arm for the right reason, by screening the **Place** rather than
the decorated item; that row had been carrying a fixed defect and is now CONFORMS with the two witnesses it never
had. Nine negative goldens, a four-fact drift test whose announce arms were falsified once by disabling the
announce, nine rows re-verdicted, GAP -4. The two residue rows are a different mechanism and moved out at the
landing — `SR-14.9.2.3-6` (SR6's un-modelled group sub-kinds) to `kb/Work/PB601` and `SR-14.9.41.3-5`
(`PlaceDecorator.Item => Inner.Item` makes SR5 ask about storage where the rule asks about the data item) to
`kb/Work/PB602` — so PB236 flipped to `landed`.

**PB235 — CLOSE's post-close state model is Table 14 itself.** `CLOSE` changed only a status because Table 14 had
been hand-copied into one method per written form, and the §14.9.6.4 GR2 category every cell is indexed by
existed only as a sentence in a doc comment — one that `docs/CONFORMANCE.md` §2 contradicted, while Annex A.1
item 24 (GR3 symbol c, "closing operations specified by the implementor") required it to be documented and
`grep '14.9.6' docs/CONFORMANCE.md` returned nothing. `PhysicalFileCategory` now carries GR2's four categories,
`FileConnector.Category` is abstract so the partition is total by construction, `Table14.Cell` returns the printed
symbol set for all sixteen cells, and `FileRegistry.CloseByFormat` is the one dispatch that executes them -
refusing an `N/A` cell or a cell needing a reel/unit-structured medium with an invariant that names what a new
medium owes. `REEL/UNIT FOR REMOVAL` became its own Table 14 row end to end, which finally gave `opt.REMOVAL()` a
consumer after two waves of riding `REEL/UNIT`'s, and §14.9.6.4 GR1 is hoisted to the top of the dispatch so an
unsuccessful CLOSE performs none of the closing actions (`SharedClose` used to release GR9's locks ahead of the
connector's own `Close()` on every path). The placement is not a preference: §9.1.13.2 item 6 defines the `'07'`
this compiler already reports for the NO REWIND, REEL/UNIT and FOR REMOVAL phrases as the status of a CLOSE that
"references a physical file on a non-reel/unit medium", and Table 14 prints symbol g only in the Non-unit column.
Two of the note's four charges did not survive re-probing, and that is the more interesting half. `CLOSE F UNIT`
already retained the file lock and the record locks at HEAD, and that is CORRECT: Table 14 gives `CLOSE UNIT`
symbol e alone, whose non-unit branch is "the file remains in the open mode ... and no other action takes place",
and GR9's release rides symbol c "Close file" — releasing an open connector's own lock would leave it arbitrating
against nothing. And GR7's "no availability bit" is a determination, not a gap: the standard defines nothing about
referencing an unavailable record area (Annex A.2 item 5 makes the unsuccessful case outright undefined), while
the half a program CAN observe — the area surviving a member's CLOSE while a SAME RECORD AREA sibling is open -
holds structurally and is now pinned. Both harm flags cleared. `docs/CONFORMANCE.md` §7 gains the A.1 item 24
determination (42 -> 43 required items discharged), §8 gains `DRV-GR-14.9.6.4-L2.1` with the §1.1
hand-listed population guard edited explicitly and the argument in a comment, §2 rows 28-30 / 33-34 stop
contradicting it, and `COBOLNET_FILES_DESIGN.md` gains **D11**. Goldens `85/pb235_same_record_area_close` and
`2002/pb235_close_unit_locks`; drift test `CloseTable14Tests`, falsified once by corrupting a cell (three reds,
including the dispatch's own loud arm). Four rows PARTIAL/NOT-IMPLEMENTED -> CONFORMS, GAP -4. One inherited
citation caught on the way in: the `-9` row's prose attributed `'51'` to §14.9.30.4 GR10a, which `--check`
rejects — it is §9.1.13.8 item 1.

**The train.** Five clusters, one build and one gate, on top of a main that had moved past every base (bases
`6e7caa91`..`08314683`; landed on `408e360f`). Conflicts were all structural rather than semantic and every one
resolved as a union: five golden manifests, `DiagnosticCatalog.cs` (PB485's two descriptors placed BEFORE PB205's
comment so the comment stayed attached to its own descriptor), and `kb/Work/PB485.md` taken from the branch
because the implementer had already rebased it on main's registrar-extended version. The traceability inventory
was never merged textually: every cluster's `record_verdicts.py` batch was re-applied in manifest order on the
merged tree and `build_inventory.py` run once, **GAP 2925 -> 2906** (PB231 +-0, PB485 -8, PB230 -3, PB236 -4,
PB235 -4). `docs/DIAGNOSTICS.md` was regenerated once from the merged catalogue. Union filter over 40
`FullyQualifiedName~` terms (ByteWindowResidue, Redefines, Pointer, NationalBoolean, Corpus, External, Bit,
CorpusRunner, LevelNumber, DiagnosticRegistry, SpecTraceabilityInventory, BindPipeline, Exception,
ClassCondition, Corresponding, Arithmetic, Move, Inspect, Sort, Boolean, DefectiveRowCoverage, Ec, Table,
RuntimeApiGuard, KeyedIo, SequentialIo, StringUnstring, Accept, Diagnostic, Traceability, DefectiveRow,
StatementDeferral, CorpusManifest, FileIo, SequentialFileIoSpec, InterProgramFile, RunUnitTermination,
CloseTable14, AnnexA1Register, Derivation): Conformance `Failed: 0, Passed: 3512`, Unit `Failed: 2, Passed:
4075` — the two being the known fresh-worktree `ExternalCorpusPopulationDriftTests` reds (no GPL corpus in this
worktree), attributed by name — and Characterization `Failed: 0, Passed: 33`. `work.py check` 660 items well
formed; `audit_annex_a1.py --check` and `audit_derivations.py --check` both clean (9 rows closed on a
derivation); `fleet_active_build.py --self-test` PASS. **No cluster was dropped.**

Two things the train had to adjudicate that no single cluster owned. **The GnuCOBOL differential moved three
cases, not one.** PB485's five rewritten rows held and battery #44's two flips (`syn_move:453`, `syn_misc:5093`)
were back on their committed rows, as its report predicted; the expected residual flip `syn_misc:388` (owed to
PB598) is still there. But PB236's new bind-time rules moved two more, both verified against the corpus source
and the spec before being rebaselined: `syn_move:374` ("CORRESPONDING — Operands must be groups")
WE_ACCEPT_THEY_REJECT -> AGREE_REJECT, an over-acceptance closed by COBOLNET1757 on a case GnuCOBOL rejects too;
and `run_misc:3544` ("SORT: EBCDIC table") AGREE_ACCEPT -> WE_REJECT_THEY_ACCEPT, where the program writes
`SORT TBL DESCENDING KEY X SEQUENCE EBCDIC` with no `ALPHABET EBCDIC IS ...` declaration — the corpus's own
comment calls it "code-name as collation". `cite.py --find EBCDIC` returns **no clause at all**: EBCDIC is not in
ISO/IEC 1989:2023, and §14.9.40.3 SR1 requires alphabet-name-1 to "reference an alphabet that defines an
alphanumeric collating sequence". Rejecting it is enforcing the standard against a vendor extension, which is
latitude we never follow, so the row was rewritten rather than the check softened. Differential after: **1
per-case flip**, the PB598 one. **And semgrep went UP by two.** `cobolnet-raw-diagnostic-code-literal` 439 -> 441,
diffed per FILE against a semgrep run over `git archive 408e360f` rather than assumed from the total: both are
PB236 call sites (`KeyedIoBinder.cs` 13 -> 14, `SortBinder.cs` 5 -> 6) of two codes that ALREADY have no
catalogue descriptor — `COBOLNET0862`, the START statement rule band, and `COBOLNET0898`, the SORT/MERGE and
USAGE-family band. Reusing them was the right call under one_rule_one_place, and no new code was allocated, so
none of the four holes the rule guards (no descriptor, no `DIAGNOSTICS.md` row, no drift test, invisible to
next-free and to the suppress-key machinery) is deepened. Recorded as a half-1 disposition in `kb/Work/PB175`
with the two bands sized for the conversion that is still owed (39 sites in 5 files, uniformly `.Error`), and
re-baselined to 441 so an `UP` again means a real increase.

**One pre-existing defect repaired at the landing.** `audit_code_citations.py` was at ONE finding on main and 
none of the five clusters had touched it: a PHANTOM §8.8.4.1.2 at `kb/Work/PB594.md:87`, landed by golden-lane
round 3 (`03b59a1d`). There is no §8.8.4.1.2 — §8.8.4.1 is "General" and has no sub-clauses at all; simple
relation conditions are §8.8.4.2. The rule PB594 means is **§8.8.4.2.7**, "Comparison is made with respect to
the collating sequence of characters specified for the current alphanumeric program collating sequence", with
the national twin at **§8.8.4.2.9**; both `--check`ed, both written into the note's prose and its `spec_refs`,
and the retired clause named in place so the correction cannot be re-inherited. Audit back to 0. This is
CLAUDE.md rule 1's named failure mode — not an invented citation but an INHERITED one, propagating out of a
note no `--check` had ever been run against — so it was repaired on discovery rather than filed. PB598's
worktree carries the same repair for train 4; the merge is trivial.

