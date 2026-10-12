## Entry 1747 — 2026-09-28 01:48 PDT — Train 69: NULL is an identifier, the nearest declaring element and the class repository, library text located and REPLACE as a statement

Three wave-69 finisher clusters landed as one train, each carrying the wave-68 branch the train-68b lander had
dropped and that lander's review findings: Y (PB1427 + PB1193 + PB1171, carrying PB1035, and PB939), Z (PB1084 +
PB1100 + PB1381 + PB1047 + PB1243) and X (PB1355 + PB1357 + PB1358 + PB1356, and PB1353's first half). This is
the finisher wave the owner authorized at 00:05 PDT ("now and land the script fix now too. Hold all additional
work until my okay Monday morning"); nothing beyond it is dispatched. GAP 945 → 896, 49 rows closed.

**Y — the predefined NULL is an identifier.** PB1427: NULL was an arm of `figurativeConstant`, so every generic
figurative consumer ran it as LOW-VALUE — `STRING NULL …` stored a NUL, `INSPECT … FOR ALL NULL` tallied,
`FUNCTION LENGTH(NULL)` answered 1. ISO §8.4.3.1.2 Format 8 makes it an identifier (predefined-address;
§8.4.3.10.1, and the null object reference of §8.4.3.7.3 SR2), and §8.4.3.10.3 SR1 lists the only places it may be
written. The grammar now has one `predefinedNull` rule, the binder one node (`BoundPredefinedNull`), one producer
for the SR1 contexts (`ExpressionBinder.NullAdmittingOperand` — INITIALIZE and SET senders, prototype CALL,
function-prototype activation, method invocation, the pointer/object relation including EVALUATE) and one refusal
text (`PredefinedNullRule.Report`, COBOLNET2576) for every other slot. The finisher re-probed the train-68b
findings and found the decoders that sit outside the operand model: `01 K CONSTANT AS NULL.` crashed the binder
(a NullReferenceException in `DataBinder.Constants`, whose arm chain ended in `BOOLLIT()!`), `PIC X VALUE NULL`
stored LOW-VALUE (PB939, now landed), `PROGRAM-ID … AS NULL` was refused as "a boolean literal", and NULL = NULL
had its own wording — all are 2576 now, and every arm chain names each alternative and throws on an unknown one.
§13.16.3 SR10 (COBOLNET2168) keys on the VALUE clause being written, so `USAGE POINTER VALUE NULL` draws both
diagnostics. `STOP … STATUS NULL` was adjudicated to 2576 (SR1 decides whether NULL may stand there at all before
§14.9.42.3 SR2's category test), BY REFERENCE NULL is refused by §14.9.4.3 SR3 / §14.9.23.3 SR9 by name, and
`PredefinedNullContextDriftTests` pins one reporter and that every literal decoder reading the figurative arm also
decides `predefinedNull` (replayed on the pre-fix tree, both facts fail). PB1193: a contained WRITE / REWRITE of a
GLOBAL record under a container's non-GLOBAL FD is refused by §14.9.51.3 SR21 / §14.9.35.3 SR3 instead of the false
"not a logical record" reason. PB1171 (+PB1035): a report file is referenced only by USE, PERFORM WHEN, CLOSE and
OPEN OUTPUT/EXTEND (§13.4.5.3 SR9) — `ResolveFile` refuses every other statement (COBOLNET2577), SORT/MERGE
USING/GIVING resolve through it with their own rule, and record entries under a report FD are COBOLNET2578 (SR8);
EXCEPTION-FILE(report-file) stays refused (SR9 is the specific rule). Goldens `2002/pb1427_null_operand_model`,
`85/pb1171_report_file_legal_references`, `85/pb1193_global_record_under_local_fd` and eleven negatives. GAP
945 → 931.

**Z — the nearest declaring element, and a class's repository.** PB1047 + PB1243: a contained program skipped a
container's GLOBAL root whose spelling it already had, dropping the root's whole subtree, and merged the rest into
one flat tier. Every global root's subtree is now inherited at its depth (`DataBinder.InheritGlobalSubtree` —
condition-names, RENAMES and index-names too) and `SymbolTable.NearestDeclaring` applies §8.4.6.2.1 3) after
qualification; constants and index-names weigh the same tier. The finisher found the train-68b findings real:
a local `01 G` + `01 K REDEFINES G` re-spelled the container's `_redef_G` bridge (CS0102), fixed by reserving the
global root's C# STEM (`InheritedMemberNamesOf`), and its sibling — a shadowed GLOBAL BASED root re-spelling
`__addr_B` — by spelling `AddressCarrierName` from the root's C# name. `SymbolTable.TryResolveUnqualified` is now
the one unqualified lookup, so the FUNCTION-POINTER name and INITIALIZE's dynamic-table arm honour the nearest
tier; the INITIALIZE sibling was a wrong answer (it initialized the container's global table: `IN [QQQQ]` →
`IN [    ]`). PB1084: a self-naming FUNCTION specifier with AS was applied as a remap (COBOLNET1506 on a recursive
call); §12.3.8.3 SR11 / SR15 are now one `SpecifiesItself` rule. PB1100: a CLASS definition's REPOSITORY reaches
its methods (§12.3.4 GR1) — the group REPOSITORY tables are built once before the class bodies, which used to
bind against no table. PB1381: the activated element's half of an "enabled in both" gate folds at its procedure
division header (§7.3.25.4 GR6 / GR8, `TurnState.EnabledAtHeader`). Its golden was wrong, not the code: a USING
mismatch through a universal reference matches no method (§9.3.6 3) e)) and is EC-OO-METHOD after PB1500, so the
TAKE leg now crosses a RETURNING mismatch (bound by §9.3.6 7), violating §14.8.3.3 → §14.9.23.4 GR7 c)). Four new
goldens `w69z_pb1047_shadowed_*` (85, 2002, 2014, 2023) beside the wave-68 set. GAP 931 → 925.

**X — library text located, REPLACE as a statement.** PB1355: library text is located by the DOC-A.1-40
determination (§7.2.3.4 GR1–GR3, GnuCOBOL's order) — the working directory, then each `--copy`; the name as
spelled, then `.CPY .CBL .COB .cpy .cbl .cob`; OF/IN is the first place holding that subdirectory, with no
fallback — and text that cannot be located is CBL3620 on every dialect (the lenient `*> not found` comment is
gone). The source file's directory is not a library place: GnuCOBOL 3.2 XFAILs its own "COPY: relative copybooks"
group, so the two DiagnosticPositionTests reds were the tests leaning on that search and now pass `--copy`. A
period only in a directory part (`../lib/BOOK`) no longer blocks the suffixes. PB1357 + PB1358: REPLACE is found
wherever it stands as a text-word (§7.2.4.3 SR1), not only at a line start, and runs the §7.2.4.4 GR4–GR7 state
machine (ALSO pushes and merges, LAST OFF pops), gated at 2002. PB1356: §7.2.3.4 GR13 / §7.2.4.4 GR9's "shall not
contain a comment" was dead — a text-word never starts with `*>` — and is now asked of the produced text
(`TextWordScanner.CommentStartsAt` / `HoldsComment`), so `REPLACE LEADING ==Q== BY ==*==` over `Q>1` is
COBOLNET2574 instead of silently commenting out the rest of the line. PB1353's operand content screens
(COBOLNET2572) and delimiter placement (COBOLNET2573) land; its SR10 consumed-directive half stays open. The
GnuCOBOL differential now writes each AT_DATA member at its relative path, runs in the case directory and maps
`-I` to `--copy`; its full run showed syn_copy:286 fixed and syn_copy:125 flipped to WE_REJECT — an agreement
with what cobc actually does, mis-scored because the harness ignores AT_XFAIL (PB1681). GAP 925 → 896.

**The train.** Brought in from each branch as a base..branch diff with the inventory excluded; the manifests and
`docs/DIAGNOSTICS.md` conflicted only on whole elements (kept both, element sets checked against the union of the
branches' additions: 85 306, 2002 551, negative 1731), and every inventory change was re-applied from the
implementers' eleven `record_verdicts` batches in order — the merged inventory matches the three branches row
for row (55 rows, 0 mismatches). Gate at Normal priority, `build-local.ps1 -Filter "~."` (the whole Conformance
assembly): Conformance 9274/9274, Unit 29693/29693, Characterization 33/33, `=== WAVE-LOCAL GATE: GREEN ===`;
legacy integration 503 + 1 skipped. semgrep unchanged (3 / 46 / 2 / 301). Review (step 5b, full-code pass over the
train diff): no correctness finding in any cluster, no cluster dropped; EVALUATE/NULL and relation shapes probed on
the built compiler (legal `EVALUATE NULL WHEN P` runs EQ). One pre-existing wrong-diagnostic lead filed (PB1682).
Leads filed from the reports: PB1673 (NULL at `--std 85` draws 2576), PB1674 (a GLOBAL record's CAPACITY IN
register is invisible in a contained program — legal source rejected), PB1680 (library-text diagnostics print the
suffix spelling tried), PB1681 (the differential ignores AT_XFAIL), PB1682 (EVALUATE's Table 15 classifier calls
NULL a literal); PB791 gained the CURRENCY SIGN NULL sibling and PB1672 the §14.9.23.4 GR7 c) reading that the
disabled-checking fatal stop may be nonconforming. No diagnostic codes were claimed from this wave's ranges
(COBOLNET2575, 2579–2583, 2596–2601 return unused).

