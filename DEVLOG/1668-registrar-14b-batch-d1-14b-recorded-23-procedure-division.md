## Entry 1668 — 2026-09-24 04:24 PDT — Registrar 14b: batch d1-14b recorded (23 procedure-division subjects), 45 notes filed (PB1110–PB1154), GAP 1989 → 1955

**What.** Registrar 14b merged, clustered and recorded adjudication batch d1-14b — 23 subjects of the procedure
division: ACCEPT, alignment and transfer of data (§14.6.8), the common phrases (§14.7.5 SIZE ERROR, §14.7.6
CORRESPONDING, §14.7.7), elementary-item and parameter conformance (§14.8.2), exception conditions and their
handling (§14.6.13), execution (§14.6.3/§14.6.4/§14.6.11), FREE, GENERATE, INITIATE, INSPECT p1–p3, INVOKE p1–p3,
initial and last-used states (§14.6.2.3), MERGE p1–p2, MULTIPLY, dynamic-capacity tables (§14.6.9) and the
procedure division structure (§14.2). The adjudicators worked on the pinned tree `adj-d1` @68bbdb00f; trains
58/59's src diff since the pin was checked and fixes none of the findings. Landed on top of Registrar 14a
(rebased; the inventory was re-recorded with `record_verdicts` on 14a's tree, never merged as JSON hunks).

**Shape.** 219 records merged; GR-14.7.7-3 withdrawn to the owner (218 recorded) and 6 NEEDS-OWNER-DECISION rows
excluded. Histogram: CONFORMS 100 (34 closed the GAP, 66 test-needed — the golden lane's next input), DIVERGES 60,
PARTIAL 47, NOT-IMPLEMENTED 11. **GAP 1989 → 1955** (+34 closed of 4,348). Refuters overturned 39 of 135 checked
verdicts, every one toward the stricter reading or an emptied test-ref (four "CONFORMS (test-ref must be empty …)"
recorded as CONFORMS with no test-ref); none toward leniency. Several refutations measured wrong answers the
adjudicators' small probes missed: native MULTIPLY's unchecked Int128 scaled product (PB1143), the float `/` with no
zero-divisor test (PB1147), a PERFORM exit's GO TO read as fall-through (PB1148), OPTIONS INITIALIZE skipping numeric
items (PB1134), a national GROUP corrupted by INSPECT (PB1128).

**Shape violations fixed at the source.** Editions commentary ("2023 (… a COBOL-2002 introduction)") moved verbatim
into `notes`; parenthetical and signature suffixes stripped from `code-location`; the first gate run caught 24
symbols the resolver cannot find (`CobolReport.GenerateCore`, `FloatReceiver`, a bare `DOC-A.1-64`) — re-sited to the
member or the file, gate re-run green.

**Mechanisms — 40 notes from the findings, one owner note and four side-lead notes (PB1150, PB1152–PB1154); each a fill unit via `cluster:`.**
Wrong answers (MAJOR): PB1110 binary32 double rounding · PB1111 CORRESPONDING rule-6 uniqueness over the filtered set
· PB1114 nested BY CONTENT float formal reads 0 · PB1117 EC-DATA-INCOMPATIBLE unreachable (tolerant subscript decode;
BINARY/PACKED group leaves) · PB1119 >>PROPAGATE inert · PB1120 handled nonfatal still runs NOT INVALID KEY/NOT AT END
· PB1121 exception-object boundary (RAISING LAST class test; activator checking) · PB1122 declarative "normal
completion" ignores a fatal in scope · PB1123 operands item-identified at point of use (UNSTRING, INSPECT, INVOKE
RETURNING, ref-modified ODO) · PB1124 INSPECT LEADING anchor · PB1125 INSPECT grammar's `LA(2) != FOR` mis-parse ·
PB1126 EC-RANGE-INSPECT-SIZE never raised, figurative sized only statically · PB1128 INSPECT identifier-1 store/class
arms · PB1130 report nonfatal ECs and SUM size error · PB1131 CONTROL break by ordinal compare · PB1132 LOCAL-STORAGE
BASED address kept · PB1134 INITIALIZE fill skips numerics (overturns CONFORMANCE D23 and a pb152 pin — owner
visibility) · PB1135 INVOKE bare expression argument · PB1137 INVOKE argument screens (property passed BY REFERENCE)
· PB1140 SORT/MERGE short-record FILL · PB1141 MERGE USING close before the output procedure · PB1142 arithmetic
Format-1 inline-invocation receiver dropped / DIVIDE crash · PB1143 MULTIPLY Int128 wrap · PB1144 dynamic-table MOVE
· PB1145 PD-header screen written twice (Roslyn crash) · PB1147 float zero divide / PROHIBITED quotient EC · PB1148
PERFORM exit GO TO. Under-rejects and legal-source refusals (MINOR): PB1112 BY REFERENCE comparator · PB1113 BY CONTENT
admission lists · PB1115 function-prototype arm skips the comparator · PB1116 object-reference-subordinate groups ·
PB1118 DYNAMIC LENGTH agreement · PB1127 INSPECT operand screen (hex, ALL figuratives, constant-names) · PB1129 RD
CODE clause · PB1133 RECURSIVE container WS refused · PB1136 INVOKE receiver/selector · PB1138 METHOD external
describes · PB1139 MERGE/SORT SR1/SR7/SR11/L5 · PB1146 procedure-division Format 1/2. Doc gap: PB1149 (§14.2.3 GR13,
§14.9.23.4 GR2 b)). Side leads filed: PB1150 CODE-SET conversion deferred to CLOSE · PB1152 INSPECT BACKWARD matches
ending at the position · PB1153 DISPLAY of a report counter aborts · PB1154 five raw COBOLNET16xx codes.

**Extended, not duplicated.** PB165 (Format-1 length rule rows), PB1036 (sort-merge EC family: GR-14.6.13.1.3-2,
GR-14.9.24.4-8), PB1051 (the whole BY VALUE half of INVOKE and the method header, plus the PROGRAM float BY VALUE
sibling), PB1052 (key-class screen), PB995 (USING half and MERGE twins), PB994 and PB1035 (MERGE twins), PB546
(two widths reach §14.8.2.2 SR1), PB603 (CloseAll has no per-file guard), PB1069 (factory singletons outlive the run
unit; A.1-168), PB284 (five ACCEPT band rows, fused §14.7.5 rows), PB372 (the batch's dossier gaps), PB1121/PB1126/
PB1135/PB1145 (stale design text, a green test pinning non-spec behaviour, inherited comment citations).

**Owner question.** PB1151 (decision, status owner): record DOCUMENTED-NON-SUPPORT for the five ACCEPT Format-3-band
rows and §14.6.11 rule 7's MCS implicit SEND, and say whether GR-14.7.7-3 (STANDARD-DECIMAL half verified,
STANDARD-BINARY half declined by PB198) closes CONFORMS or DNS.

**Dossier gaps.** 97 reported; the pattern (code that implements a rule without citing it, code that fails to
implement one, notes that own a mechanism under a different clause, a truncated source list, one stale golden path)
is appended to PB372. Unfiled leads, not confirmed: EXCEPTION-STATEMENT answers spaces inside an EC-DATA-INCOMPATIBLE
declarative (EcFunctions §15.32.3 r1 returns spaces when no location information is saved — possibly conforming);
COBOLNET1505 refusing a FUNCTION reference from a class unit (the message is UdfBinder's no-prototype path).

**Gate.** `dotnet build CobolSharp.sln -c Debug` 0 errors; Unit filter SpecTraceabilityInventory | DefectiveRowCoverage
| DerivedVerdict: `Passed! - Failed: 0, Passed: 47`. `work.py check`: all well-formed. Every citation in the new notes
went through `cite.py --check` (scripted; a FAIL aborts the writer).

