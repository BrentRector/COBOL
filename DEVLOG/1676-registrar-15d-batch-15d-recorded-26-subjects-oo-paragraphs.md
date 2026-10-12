## Entry 1676 — 2026-09-24 11:06 PDT — Registrar 15d: batch 15d recorded (26 subjects: OO paragraphs, conformance, reference format, I-O status, OPTIONS clauses), 27 notes filed (PB1491–PB1520, 4 folded into Registrar 15a/15c's), 23 extended, GAP 1723 → 1664

**What.** Lane-3 adjudication batch 15d — 26 subjects adjudicated on the pin `adj-d2` @`071ff04ea`: the OO identification
paragraphs (CLASS-ID, FACTORY, OBJECT, METHOD-ID, INTERFACE-ID, FUNCTION-ID, PROGRAM-ID), §9.3 objects/polymorphism/
conformance, §6 reference format (fixed form, floating indicators, logical conversion), §9.1.13 I-O status (p1/p2), §9.1
files, end markers, the compilation group, and the OPTIONS clauses (ARITHMETIC, DEFAULT ROUNDED, ENTRY-CONVENTION,
FLOAT-DECIMAL, INITIALIZE, INTERMEDIATE ROUNDING, the paragraph itself). Every subject had its refuter checkpoint on disk,
so no CONFORMS row was excluded as unrefuted. Nothing that landed after the pin (Registrars 14a–15c, all register-only) touches
a finding's code.

**Shape.** 196 records → 185 recorded + 11 held for the owner. Histogram: CONFORMS 88 (59 close their row, 29 test-needed
for the golden lane), PARTIAL 49, DIVERGES 42, NOT-IMPLEMENTED 6. Refuters overturned **34 CONFORMS**, all DOWNWARD (30 →
PARTIAL, 4 → DIVERGES); the dominant overturn shapes were a second half the adjudicator never probed (BY VALUE halves of
§9.3.6 behind PB1051; interface-prototype arms of METHOD-ID; library-text arms of §6.5) and a probe written lexically
where the rule is dynamic (§9.1.12 active PERFORM). record_verdicts on the merged tree: rows changed 185, GAP
1723 → 1664 (+59).

**Mechanisms (one note per root cause; each note is a fill unit through `cluster:`).**
Reference format — the floating literal continuation indicator is implemented nowhere (three subjects found it;
drafted as PB1490, FOLDED into Registrar 15a's PB1359); PB1491 fixed-form continuation joins onto the last OUTPUT line (blank line CRASHES the compiler; short record loses
trailing spaces); PB1492 continuation lines unvalidated + opening quote not remembered; PB1493 comment recognition not one
stage (no separator check, no 85 gate, CopyProcessor and PICMODE blind to comments); PB1494 CCVS column-7 letters and
the comment-entry heuristic unscoped (a PROCEDURE DIVISION paragraph named REMARKS is silently dropped); the mode chosen
by heuristic and the free-form directive cut at column 72 (drafted as PB1495, FOLDED into 15a's PB1362 and PB1361); PB1496 255-position limit + A.1 156/157/117 undocumented.
OO — PB1497 ACTIVE-CLASS refused in an interface prototype (COBOLNET1924); PB1498 §9.3.8.2.3 anti-circularity absent;
PB1499 interface-typed return through a universal prototype dies in Roslyn; PB1500 universal INVOKE no-match never sets
EC-OO-METHOD; the invocation calling convention never compared (drafted as PB1501, FOLDED into 15a's PB1383,
>>CALL-CONVENTION accepts any name); PB1502 interface INHERITS SR5 / SR4 b) / SR6;
PB1503 PROPERTY accessor shape (SR5–SR7); the GET/SET PROPERTY prototype refused 0899 (drafted as PB1504, FOLDED into 15c's PB1449); PB1505 parameterized class
skips §11.3.3 checks + 0820 citation; **PB1506 the standard class BASE (§16.1) is not provided** (a refuter's unfiled lead,
re-run here: `INHERITS FROM BASE` → COBOLNET0821); PB1507 source-unit shape and PROGRAM-ID attributes screened only for
prototypes; PB1520 DOC-A.1-121 missing. OPTIONS/arithmetic — PB1508 OPTIONS grammar order/repeat/period, FLOAT-DECIMAL
and INITIALIZE choice groups; PB1509 report SUM accumulated through `(long)` (wraps at 20 digits); PB1510
CobolDec.AddSigned unsigned sticky (wrong SDIDI under INTERMEDIATE ROUNDING); PB1511 DOC-A.1-123 incomplete.
I-O — PB1512 host IOException on sequential READ/WRITE kills the run unit ('30' owed); PB1513 ODO READ '34' has no
producer; PB1514 fixed-length record-sequential READ fragments records (no '04'); PB1515 pre-2023 '07' unverified;
PB1516 no-PICTURE RELATIVE KEY disables '14'.
Extended (rows + evidence): PB1051 (every BY VALUE half of §9.3.6, §11.8.3 SR2, §9.3.8.2.3 rule 1's comparator),
PB1166 (the one DescriptionMismatch comparator on the override/IMPLEMENTS/INVOKE lanes), PB1112, PB480, PB1165, PB1113,
PB1086 (three more external-repository consumers), PB989, PB988, PB1133, PB442, PB1120, PB1168, PB1192, PB298, PB1067,
PB372 (dossier gaps: all 26 subjects, the citation-keyed shape again), PB579.

**Owner questions (rows excluded, never hand-verdicted).** PB1517 — GR-11.9.5.2-2 STANDARD-BINARY (A.3 item 2 decline):
DOCUMENTED-NON-SUPPORT? PB1518 — GR-9.1.13.7-5 '45', reachable only through declined modules (PB1255's shape).
PB1519 — GR-9.3.6-L5.1/L5.2/L5.3, the exact-match half of §9.3.6 (parametric polymorphism, A.4.10 item 3, not claimed).
PB579 extended — SR-11.9.9.3-1…-6, the FLOAT-DECIMAL clause (A.3 item 13): the same answer as PB579's four usage rules.

**Mechanics.** The merge carried free-text `editions` and `code-location` fields the schema refuses (208 shape
violations on the first dry-run): normalized to the edition vocabulary and to `path#Symbol`, the adjudicator's verbatim
text kept in the row notes. The first whole-Unit gate then caught what the shape check cannot — 27 qualified fragments
(`OoClassTable.Build`) the resolver's word search does not find and two test-refs naming the wrong directory / a legacy
test — fixed at the batch and re-recorded from origin's inventory, never by editing the JSON. Rebased onto Registrars
14d–15c with the inventory taken from origin/main and the batch re-recorded; kb conflicts resolved as the union of
`inventory_rows` with every registrar's extension section kept. Registrars 15a and 15c landed four of this batch's mechanisms
first, so those drafts were FOLDED (rows + body moved, every reference in kb/Work and the batch notes rewritten):
PB1490→PB1359, PB1495→PB1362 (GR-6.1-1/-2) + PB1361 (GR-6.5-1), PB1501→PB1383, PB1504→PB1449. Net 27 notes filed,
23 extended (PB1359, PB1361, PB1362, PB1383, PB1449 by fold). Ids PB1490, PB1495, PB1501, PB1504 and PB1521–PB1529 unused.

**Gate.** Build CobolSharp.sln; WHOLE Unit assembly: 29190 passed, 0 failed (the GnuCOBOL corpus fetched into the worktree, so ExternalCorpusPopulationDriftTests ran green); `work.py check`: 1457 well-formed.

