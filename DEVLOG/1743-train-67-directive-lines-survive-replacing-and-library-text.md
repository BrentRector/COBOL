## Entry 1743 — 2026-09-27 15:11 PDT — Train 67: directive lines survive REPLACING and library text keeps its COPY's format, one relation-comparability table, dynamic-table recreation and RESUME continuation, sequential size rules on the right axes

Four wave-67 clusters landed as one train: M (PB1352 + PB1067 + PB1543; PB1355 and PB1353 split to a finisher),
N (PB1468 + the relation half of PB1427, carrying wave-65 group A's PB1469 + PB1467), O (PB1267 + PB1269 + PB1410 +
PB1411, and PB1144 legs 1 and 3; leg 2 split) and P (PB1168 + PB1190 + PB1514 + PB1540 + PB1512). GAP 1035 → 998,
with 37 rows closed.

**M — the text-manipulation stage and library text.** Every note re-probed on the implementer's build. PB1352: a
compiler directive line inside matched text was rewritten by COPY REPLACING and REPLACE, though §7.3.4 1) says "A
compiler directive line is not affected by the replacing action of a COPY statement or a REPLACE statement" and
§7.2.3.4 9) c) 5. treats each one "as a single space" for matching. The ONE scanner now recognizes a whole
directive line as a `TextWordKind.DirectiveLine` element (through `CompilerDirectiveLine.TryParse`); matching steps
over it (`TextWord.IsSpaceForMatching`) and `ApplyReplacements` writes a directive line inside a matched span back on
its own line. Both arms share `ApplyReplacements`, so both are fixed. REPLACING operands now keep the elements scanned
in their statement (`CopyProcessor.Operand`): re-scanning an operand out of context made `==>>PAGE==` a directive
line. PB1067: library text was normalized by a private ≥50 %-sequence-number heuristic; it now goes through the same
§6.5 walker as the source, starting in the format in effect for its COPY statement (§7.3.24.3 3)), through a
per-file `ReferenceFormatMap`. A DETECTED-free main hands no format on (determination DOC-A.1-158). PB1543: text-words
split on the lexer's space set (`TextWordScanner.IsSeparatorSpace`), not Unicode White_Space (§8.3.5 1)). Did not
hold on today's tree: PB1353's missing-BY, unbalanced `==` and `=="A==B"==` legs were already fixed by w61a. Goldens
`2002/pb1352_directive_lines_not_replaced`, `2002/pb1067_library_text_source_format`,
`85/pb1543_nbsp_is_a_text_word_character`; 13 rows closed (GR-6.5-9 stays with PB1496).

**N — §8.8.4.2.1's closed comparison list, as ONE table.** `RelationComparability.Breach`, asked by
`StatementValidation.CheckGeneralRelationComparability` at the one relation checkpoint for every pair no other band
owns, with class taken from the Table-2 lattice's candidate set (empty set fails open). COBOLNET2532 refuses a
numeric operand opposite a character operand unless it is "an integer literal or an integer numeric data item of
usage display or national" (§8.8.4.2.5; --permissive warns; an INTEGER intrinsic is admitted as the Table-16 Integer
row). COBOLNET2533 refuses an index pair outside §8.8.4.2.13's three; an index-name against an alphanumeric operand
used to abort the run unit. The EVALUATE THRU `BoundRangeMembership` lowering now passes the same checkpoint, and a
materialized EVALUATE subject is screened as written (`ConditionBinder.RecordIntermediate`). PB1427 F4: NULL takes
the other side's class only opposite an object or pointer, so `IF X = NULL` is refused, and `IF B1 = SPACE` is
COBOLNET0844. The carried predecessor lands PB1469 (strongly-typed groups ordered element by element,
`StrongGroupComparison`, retiring the signed-leaf stage) and PB1467 (a variable-length group compared only with a
compatible group, COBOLNET2492, sharing `VariableLengthCompatibility.PairRefusal` with MOVE). An inherited
"§14.9.13.4 GR2 … as if the corresponding relation condition were written" quote that is in no clause was replaced
by §14.9.13.3 SR7 a) at five sites. `2002/pb495_group_usage_applies_to_each_leaf` wrote `IF A-INDEX = 3` (outside
row 3) and now writes `IF A-INDEX = T-X`, same output. Golden `85/pb1468_relation_defined_pairs` and negatives
`pb1468-relation-numeric-not-integer`, `pb1468-relation-index-pair` (plus the predecessor's two); 8 rows closed. PB1427's
NULL operand model stays open.

**O — dynamic-capacity tables.** PB1144: a variable-length group MOVE recreated the receiving table through the SET
primitive; `CobolDynTable.FromCurrentImage` is now §14.6.9.2's recreation — fresh space-filled elements up to FROM,
built whole and swapped in — and both implicit capacity changes raise EC-BOUND-OVERFLOW through one
`RaiseImplicitOverflow` (never the SET's EC-BOUND-SET). PB1269: RESUME AT NEXT STATEMENT from an EC-BOUND-OVERFLOW
declarative now continues the growth and the store (§8.5.1.9.6 1), "the operation shall be allowed to continue";
`NonfatalIfEnabled(…, nextStatementContinues)`, determination in CONFORMANCE A.4.4). PB1410: an allocation the host
cannot satisfy is the fatal EC-BOUND-TABLE-LIMIT with the table unchanged (§8.5.1.9.6 2)). PB1267: an INITIALIZED
table's created elements take the §8.5.1.9.5 INITIALIZE recipe through the new `SeedRecipe` axis. PB1411: every
group store receives through `PlaceRenderer.GroupTarget`, so a variable-length, bit or national group element grows
the table (the bit/national case was a CS0029 found in the sweep; `GroupStoreTargetDriftTests`). Four 2014 goldens,
negative `pb1267-occurs-dynamic-initialized-2002`; 6 rows closed. PB1144 leg 2 (element-wise MOVE of corresponding
elementary elements) is split to a finisher.

**P — sequential size rules keyed on the right axes.** PB1168: REWRITE's size rule read `IsVarying`; it now
dispatches on the organization — §14.9.35.4 GR16 on every record sequential file (record-name-1's byte size emitted
for a fixed file), GR17 on the untrimmed count for line sequential. PB1190: both WRITE arms share one length decision
(`WrittenLength` / `OutsideVaryingBounds`), so a print-control WRITE is held to RECORD VARYING bounds ('44', nothing
written). PB1540: the line sequential delimiter is the host newline, and a lone CR is data ('09'), per DOC-A.1-114.
PB1514: a fixed-length file's physical record is documented as the reading description's integer-1 bytes
(DOC-A.1-146 (a)). PB1512: a host failure on READ/START is '30'; an unsuccessful OPEN's handle release can no longer
throw; OPEN REVERSED counts inside the OPEN body. Goldens `85/pb1168_rs_rewrite_size_gr16`,
`2023/pb1168_ls_rewrite_gr17`, `85/pb1190_varying_advancing_bounds`, `2023/pb1540_ls_lone_cr_is_data`; re-derived
`2023/pb329_line_sequential_charset_2023` (it pinned the trimmed GR17 b) reading), `2002/io_status_04` and
`2023/pb964_ls_plain_write_after_after`; 10 rows closed (DOC-A.1-103 restated PARTIAL, residual on PB322). Worth knowing:
GR17 b) applied as written means a fixed line sequential record written with trailing spaces (GR21 trims them)
cannot be REWRITten by the same FD.

**The train.** Pipelined behind train 66: cut on origin/main, then — once train 66's CI run 36351316043 was green —
re-applied onto its head `2d95638ac`, because a `merge-tree` probe showed a source conflict in `PlaceRenderer.cs`
(train 66's direction-parameterized `BitsWrite` / `NatWrite` against O's `GroupTarget`; merged as both). Every
inventory hunk was discarded and each cluster's `record_verdicts` batch re-applied in order; a row-by-row comparison
against each implementer branch found 0 mismatches. Gate on the composition (build-local, filter
`~CobolNet.Tests.Conformance` = the whole assembly, Normal priority): Conformance `Passed! - Failed: 0, Passed: 9190` (the whole assembly), Unit `Passed: 29592`, Characterization `Passed: 33`, `=== WAVE-LOCAL GATE: GREEN ===`, citation / evidence / witness-loss audits green; legacy Integration `Passed: 503, Skipped: 1`. semgrep verify PASS, no count moved.
Review pass over the train diff: 0 confirmed correctness findings, no cluster dropped. Leads filed: PB1660 (PB1543's
sibling in the directive-operand stages), PB1661 (EVALUATE of an index data item read by two WHENs answers OTHER —
re-run on the train build), PB1662 (a folded intrinsic hides the operand from §8.8.4.2.13), PB1663 (INSPECT refuses
X"41" as numeric — re-run on the train build), PB1664 (adjudicate the print stream's CR LF). PB1317 was retired as a
duplicate of PB1269 after its own repro printed `CAP=[0000000005] E5=[042]` on the train build; PB1546, PB1233,
PB1191 and PB1640 were extended with the reports' re-measurements. A misstep: PB1663's first draft cited a clause
that does not exist (§8.3.3.2.3.2); `audit_code_citations` caught it and it now cites §8.3.3.2.1 / §8.3.3.2.2.

