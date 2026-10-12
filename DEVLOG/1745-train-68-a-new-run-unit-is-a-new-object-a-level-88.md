## Entry 1745 — 2026-09-27 21:59 PDT — Train 68: a new run unit is a new object, a level-88 figurative sized as read and one RESUME landing, the whole §12.4.5.3 GR1 identity and a method's external check, one abbreviated tail at every tier, one report presence snapshot and per-level RESET

Five wave-68 clusters landed as one train, three of them carrying a predecessor branch: W (PB1069 partial +
PB1532 + PB1178; PB1123 split to a finisher), T (PB1477 + PB1432 + PB1465, carrying wave-65 group E's PB1401 and
PB1466's BINARY/PACKED arm; PB1466's national arm stays open), R (PB1079 + PB1138, carrying wave-67 group Q's
PB1405 + PB1500 + PB1064 + PB1132), S (PB1390 + PB1371 + PB1392, carrying wave-66 group H's PB1370 + PB1413) and V2
(PB1272 + PB1296 + PB1297 + PB1188, carrying its same-file predecessor V's PB1130 + PB1131 + PB1186 + PB1187 +
PB1247). GAP 998 → 945, 53 rows closed.

**W — the run unit's lifetime.** PB1069: a host that ran two run units in one process saw the first one's switch,
locale, report-flow and factory state in the second (`RunUnitScopeHostTests`: `RUN 2 / SW1 ON / COUNT 2` before,
`SW1 OFF / COUNT 1` both times after), because run-unit begin was a hand-written list of member resets that had
forgotten several. The emitted `ProgramRegistry.Reset()` is now `RunUnit.Begin`, and `RunUnit.Run` shares its
`StartAfter`: every run unit is a NEW `RunUnit` object, and only the `HostConfiguration` members (Clock, DebugMode)
carry over. `ResetCurrent`, `ProgramTable.Reset` and four store `Reset`s are deleted; `RunUnitStateDriftTests`
reflects over every member so a new one is fresh by construction. Factory objects are the run unit's (§9.3.14.2
"A factory object is created before it is first referenced by a run unit"): the emitted `__Instance` is a property
over `RunUnit.FactoryObject<F>()`, no longer a process-lifetime static. `CobolTable.Scratch<T>` became
`[ThreadStatic]`. PB1532: an unreachable object's finalizer resolved `RunUnit.Current` on the GC thread, reached an
empty orphan run unit, and its file stayed open (status 61 at the next OPEN OUTPUT); the object now captures its
creating run unit's `FileRegistry` and enqueues there (`InstanceFileFinalizerTests`, 61 → 00). PB1178's
ERROR/NORMAL rows are re-verdicted on DOC-A.1-192's determination that a STATUS value wins (`STOP RUN WITH ERROR
STATUS 0` exits 0; owner visibility). PB1069 stays open on static method WORKING-STORAGE (a pre-2023 method's WS is a
plain `private static` with no reset; §14.6.2.3.2 1)). PB1123 was re-probed (all four arms still reproduce) and
split to a finisher. GAP 998 → 993.

**T — the condition renderer's seams.** PB1477: a level-88 figurative VALUE on an ODO group was folded to the
declared width, so `88 ALL "X"` answered false where `IF GV = ALL "X"` answered true; the figurative is now a seed
sized at run time against the variable as read (§8.8.4.5.3 2), §13.18.38.4 8) a)), and `RenderStringRange` is the one
range rendering for 88 THRU and EVALUATE WHEN. PB1432: a goto out of a lambda-wrapped operand step was CS0159 (not
UDF-only — a §15.4 subscript temporary's store did it too), and a hoisted store's RESUME NEXT fell back into its IF;
every drained operand step is now `BoundStatement.OperandEvaluation`, `StatementEmitter` opens the operand scope,
and the ONE landing `ResumeTransfer` unwinds from inside it (§8.8.4.13 1), §14.9.33.4 2) a) 1.). CallEmitter's
`Resume(inExpression, …)` and `EmitSelection`'s landing parameter are deleted. PB1465: `IF BZ B-OR (BW)` was
COBOL0001 because the lexer opened SUBSCRIPT mode after any word; `TokenRetypes.PrimeLexer` narrows that trigger per
compile to words the compile admits as user words (Table 4, §8.8.2 6)). From wave-65 E: PB1401's computed-operand
class conditions and PB1466's BINARY/PACKED image promotion (a promoted formal's window is its storage width,
`CallEmitter.ElementaryFormalWindow`). Goldens `85/pb1477_cond88_odo_figurative`,
`2002/pb1432_operand_eval_resume`, `2002/pb1465_boolean_paren_after_operator`. GAP 993 → 986.

**R — external file identity and the method activation boundary.** PB1079: `OoEmitter.SelectFingerprint` is now
the ONE §12.4.5.3 GR1 identity and covers c) RECORD DELIMITER, d) RESERVE, g) COLLATING SEQUENCE (key-level clauses
by key position), j)/k) key description and relative location, and k) SUPPRESS WHEN; before, a mismatch in any of
them ran the activated program with no EC-EXTERNAL-FILE-MISMATCH (`FileModel.ReserveAreas` and `RecordDelimiter`
are new carriers; c) was a sibling the note missed). PB1138: a METHOD activation never checked its factory's or
object's external items (§14.9.23.4 7) d) "External items are checked"); each class half now emits
`__DescribeExternals` and every method prologue calls `ExternalStore.DescribeAtMethodActivation` before the
transfer. The activating half of §14.8.4.1 is one mechanism for CALL and INVOKE: the statement guard's EC-EXTERNAL
checking flags, read by the boundary; the pending `ExternalCheckMask` register and its three hand-zeroing sites are
deleted. From wave-67 Q: universal dispatch keyed by the method's externalized name, with a non-matching method
handing the search to its base and EC-OO-METHOD at the root (PB1405, PB1500, §9.3.6); INVOKE character channels
(PB1064); cached-singleton activation re-seeding LOCAL-STORAGE/LINKAGE cell roots through the one
`ActivationPointerSeeds` (PB1132). Goldens `2023/w68r_pb1079_external_entry_identity`,
`2023/w68r_pb1138_method_external_check` and two below-2023 negatives. GAP 986 → 978.

**S — one abbreviated tail at every tier.** PB1390: the §8.8.4.12 abbreviated-combined-relation tail was spelled
three ways across the OR/XOR/AND tiers, so `A = B OR < C AND D` and `A = B XOR < C` were COBOL0001. The grammar now
has leading tiers and succeeding twins; every connective takes a succeeding operand that may lead with an
abbreviated relation, the EVALUATE partial spine shares the tails, and `abbreviatedAndChain` / `BindXorSequence` are
deleted. GR1's termination case (an abbreviated relation with no subject to carry) is COBOLNET2552 instead of the
internal-error net. PB1371/PB1392: XOR/EXCLUSIVE-OR is one `xorOperator` rule gated by one `LogicalOperatorGate`, so
the `>>IF` and partial-expression XOR are gated below 2023, and the constant-conditional tiers gained XOR and a
non-recursive NOT (Table 5). From wave-66 H: the boolean shift-count screen (PB1370, PB1413, COBOLNET2513). Goldens
`85/pb1390_abbreviated_tail_and_or` (seven shapes over 81 tuples) and `2023/pb1390_abbreviated_tail_xor`, four
negatives. GAP 978 → 967.

**V2 — the report engine.** PB1272: PRESENT WHEN conditions were evaluated at use; one generated presence probe per
group now snapshots every conditioned line, item and SUM entry once per presentation, before the page fit
(§13.18.41.4 2)). `CobolReport.BeginGroup` is the one prologue of all five presentations and `PresentLines` the one
line loop (was four). PB1297: RESET ON resets per control LEVEL in the one `ProcessControlFootings` sequence, and a
detail repeated in UPON adds once per appearance (§13.18.54.4 7) c) 2.). PB1296: the counter's GR1 digit count is
per PICTURE category (`DataBinder.SumCounterDigits`) — V's size-error landing had made `ZZ9` a one-digit counter.
PB1188: EC-REPORT-PAGE-LIMIT, -COLUMN-OVERLAP and -PAGE-WIDTH are raised with §14.9.46.4 5) resumption. From V: the
sum-counter size error and EC-REPORT-SUM-SIZE (PB1130), the collated control break (PB1131), SUPPRESS per named group
(PB1186), TERMINATE's restore last (PB1187), unpaged first-line placement (PB1247), and the held line owned by the
connector. Nine goldens across V and V2. GAP 967 → 945.

**The train.** Gate: the WHOLE Conformance assembly (`-Filter "~CobolNet.Tests"`, 9,232 cases) plus the unfiltered
Unit (29,653) and Characterization (33) assemblies, Normal priority. It was RED on one case,
`MethodOmittedArgumentTests.Universal_OmittedIntoNonOptionalFormal_StopsAtRuntime`, which no implementer filter
selected: it expected EC-OO-UNIVERSAL ("does not conform") for an OMITTED argument into a non-OPTIONAL formal through
a universal receiver. Attributed to R by its predecessor PB1500, whose report states the new rule — §9.3.6 match
rule 3 b) makes that method a non-match, and §9.3.6 6) sets EC-OO-METHOD. The expectation was stale, not the
compiler; the lander updated the assertion inside R's commit. Three conflicts resolved from both sides: the INVOKE
literal window (T's `ElementaryFormalWindow` with R's comment), `ExternalStore` (R's `DescribeAtMethodActivation`
kept, the `Reset` W deleted stays deleted) and one DATA_MODEL paragraph (T's PB1466 sentence plus R's INVOKE
channels). Semgrep counts unchanged. New leads filed: PB1665 (group MOVE with a national leaf truncates), PB1666 (SUM
counter carrier is a long), PB1667 (a zero advance on an open print line welds), PB1668 (`IF TRUE` reaches the
internal-error net), PB1669 (`--permissive` still rejects `B-OR (`); PB781 and PB1123 got update paragraphs. No
cluster dropped.

