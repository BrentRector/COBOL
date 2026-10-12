## Entry 1926 — 2026-10-07 19:23 PDT — Train 1036 (wave 1032: D, E, F): OPTIONS and CONFIGURATION SECTION order, one GLOBAL screen for OO definitions, inline invocation arguments and operator spacing

**Cluster D — PB1508 (landed).** ISO §5.2.1 requires a general format's elements "in the sequence given in the general
format, unless otherwise specified by the rules of that format". The OPTIONS paragraph (§11.9.2), the CONFIGURATION SECTION
(§12.3.2) and the identification division's `[ options-paragraph ]` (§11.2.1) parsed order-free and repeatable with no screen,
so reordered or repeated clauses compiled clean and the first (or last) one won; `OPTIONS. .` (the independent `[.]`
bracket) was refused, and `INITIALIZE WORKING-STORAGE WORKING-STORAGE` folded into one target by a bitwise OR. The re-probe
reproduced every arm, and found two siblings the note did not name: two OPTIONS paragraphs (the second was never read) and the
CONFIGURATION SECTION's paragraph order. The note's citation of §5.3.2 did not hold; the order rule is §5.2.1. Every
`ClauseCardinalities` row now states an ORDER decision, `Sequence` (null where a syntax rule frees the order, and the row
cites it), and `ClosedFormatPass.ScreenClauseRepetition` reports the new COBOLNET2987 at an element written after one the
figure prints later and COBOLNET2423 at a repeat. `OptionsBinder.SectionsOf` reads the INITIALIZE choice indicators through
`ChoiceIndicators.AtMostOnce` (COBOLNET2104). `ClauseCardinalityDriftTests` requires each Sequence to rank exactly the list's
once-only alternatives. Goldens `2002/pb1508_options_period_bracket` and `2023/pb1508_options_printed_sequence`, and six
negatives. Three corpus programs (2014/pb1133, 2023/pb728, negative/pb1075) and `ConditionNameSubjectClassDriftTests` wrote
SPECIAL-NAMES before OBJECT-COMPUTER and were corrected. The last of these was the implementer's gate-red fix (7015ec12c),
never re-gated before the STOP; this train's gate was its first. Rows: FMT-11.9.2 DIVERGES → CONFORMS, FMT-11.9.10.2
PARTIAL → CONFORMS, FMT-12.3.2, FMT-11.2.1 and SR-11.9.3-1 restated. Lead filed: PB2532 (the COBOL-85 comment-entry
paragraphs are not screened for repetition or order at `--std 85`).

**Cluster E — PB1045 (landed).** §13.18.27.3 SR4 ("The GLOBAL clause shall not be specified in a factory definition, an
instance definition, or a method definition") had two private reporters, each with a hole: the class arm asked the bound
files, reports and level-1 roots, and the method arm asked data description entries. The re-probe found the note's SR1 arm
(a 77 or 05 GLOBAL) already refused by PB518, and the defect wider than filed: a GLOBAL constant compiled clean in every OO
definition (object, factory, method, interface prototype), as did a TYPEDEF GLOBAL in object or factory storage, and the
1520s that did fire carried no position. Both arms are deleted. `OoDefinitionRules.Screen` walks each factory, instance and
method data division over `GlobalClauseSites` (SR1 a), b), d), e)) and reports COBOLNET1520, now a catalog descriptor, at
the clause. A drift test holds the site list equal to the parser rules whose ATN matches GLOBAL. Dead
`DataBinder.OoGateUnsupportedShapes` removed (semgrep raw-diagnostic-code-literal 273 → 271). Goldens
`2002/pb1045_global_constant_beside_class`, negative `pb1045-global-constant-in-object`, two OoSpineTests facts. Row
SR-13.18.27.3-4 DIVERGES → CONFORMS. Lead filed: PB2533 (a reserved-word clause in a closed entry format, such as `SD SF
GLOBAL.`, draws a generic COBOL0307 instead of a named refusal).

**Cluster F — PB1430 (landed).** §8.4.3.4.2 brackets the inline method invocation's whole parenthesised group but encloses the
argument forms in a brace repeated inside the one pair, and §5.2.6.3 requires one brace alternative, so `O :: "M" ( )` is
no printed spelling; it ran. The grammar now requires `argumentList` inside the optional parentheses, and
`CobolErrorStrategy` names the refused shape COBOLNET2994. The sibling rule, §8.7.4's separator spaces round `::` and §8.7.3's
round `&`, was documented leniency (`SPACE&SPACE` and `O:: "M"` compiled); it is now decided in `SeparatorRule.Violations` as
COBOLNET2993, and when a literal touches the operator the operator owns the boundary, so there is one report, never a 2633
beside it. One note claim did not hold on today's tree: `MethodOmittedArgumentTests` no longer pins a spaceless `O::"CHK"`
(PB1394 respaced it). Golden `2002/pb1430_operator_separator_spaces` and three negatives. Rows FMT-8.4.3.4.2
DIVERGES → CONFORMS and SR-8.4.3.1.3-5 PARTIAL → CONFORMS. No new defect (§8.7.1 arithmetic-operator spacing is PB2017,
whose fix site is now beside the new arm).

**The train.** Three clusters, below the 4-6 band, because they were the only finished unlanded branches; all three
implementers had split on a STOP before the Linux gate. The three append-only manifest conflicts were resolved as whole
elements and `docs/DIAGNOSTICS.md` took both sides (2987, 2993, 2994 in code order; the catalogue drift test agrees). The
inventory hunks were discarded and the three batches re-applied in order on the merged tree, one per cluster commit. Codes
2987 (D's range 2987-2989) and 2993, 2994 (F's range 2993-2995) do not collide; 2988, 2989, 2990-2992 and 2995 are returned.
Whole-population gate (lander mode, run 20261008T020330Z-8085d6): GREEN, Conformance 10,962/10,962, Unit 32,594/32,594,
Characterization 35/35. Linux gate GREEN (hooks, unit, characterization, conformance, guard). Arch oracle DIFFERENT, 15 of
7,476 cases: 14 ADDED (this train's new goldens and negatives) and one DIAGNOSTICS change,
`negative/pb1075-character-classification-for-repeated`, whose COBOLNET2104 moves from line 9 to line 7 at every edition
because cluster D moved its OBJECT-COMPUTER paragraph ahead of SPECIAL-NAMES (message unchanged). No emitted C# changed. The
baseline is re-recorded as 9879d9e40069. Semgrep 3/46/2/273 before, 3/46/2/271 after. Audits (code and doc citations,
evidence supersession, witness loss, drift rules, work.py) green. Review of the train diff: 0 findings, 0 fixed, 0 clusters
dropped. Inventory GAP 152 → 147.

