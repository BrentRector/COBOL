## Entry 1675 — 2026-09-24 10:49 PDT — Registrar 15c: batch 15c recorded (27 clause-8 subjects), 29 notes filed (PB1441–PB1477, 9 folded into 15b's notes), 18 extended, GAP 1762 → 1723

**What.** Registrar 15c registered lane-3 adjudication batch 15c: 27 subjects from Clause 8 — names and scope
(local and global names, scope of names, scope and life cycle of data, qualification), literals (national,
numeric), the identifier formats (NULL, object property, object view, program-address-identifier, reference
modification, report counters, SELF/SUPER, subscripts), reserved words, types, variable-length items, the relational
operators and the seven simple-condition forms — adjudicated on the pinned tree `adj-d2` @071ff04ea. Everything that
landed since the pin is registrar-only, so no finding had been fixed in the meantime. The registrar prepared while
14d, 14e, 14f, 15a and 15b landed, rebased onto each, and after every rebase took origin/main's traceability
inventory and RE-RECORDED the batch with `record_verdicts` (never merged as JSON hunks). **GAP 1762 → 1723** (+39
closed of 4,348).

**Verdicts (168 records).** CONFORMS 78 (39 closed their row, 39 are CONFORMS-but-untested and go to the golden
lane's next input set), DIVERGES 39, PARTIAL 38, NOT-IMPLEMENTED 13. The refuters overturned 19 adjudicator
CONFORMS: 17 to PARTIAL, 1 to DIVERGES, 1 restated CONFORMS with an empty test-ref. Every overturn went toward MORE
defect. Two NEEDS-OWNER-DECISION rows were excluded: GR-8.4.3.10.4-4 (the null message-tag content) and
GR-8.8.4.2.1-9 (comparing two message-tag operands). They are the same question as the open owner note **PB1198**
(the `mcs-only` derived-verdicts selector) and were appended to it. PB1198 now also claims SR-8.8.4.2.3-5, which is
PARTIAL only because of its message-tag third class.

**Folds (one note per mechanism, CLAUDE.md rule 8).** Registrar 15b adjudicated neighbouring clause-8 subjects on the
same pin and filed four of the same mechanisms first. Nine 15c drafts were folded into those notes (rows, body and
every reference rewritten, including the batch's notes field before the final re-record), and each target's harm flags
and severity were raised to the union: PB1440 + PB1442 → **PB1393** (literal screens in one funnel: the NATIONAL
length cap in a VALUE clause, and the keyword-omitted intrinsic arm that skips NX/X digit grouping, printing
`LENGTH(NX"041")` = 0000), PB1443 + PB1444 → **PB1427** (NULL carried as a figurative constant: legal NULL slots
refused and `BY CONTENT NULL` aborting, illegal contexts run as LOW-VALUE), PB1447 + PB1451 + PB1461 + PB1462 →
**PB1425** (the identifier's objectReference tier: property receivers, object-view, `x OF SUPER`, SELF/SUPER as
identifiers), PB1463 → **PB1412** (HasBoolOp omits B-SHIFT). PB1440, PB1442–PB1444, PB1447, PB1451, PB1461–PB1463 and
PB1478–PB1489 are unused.

**Notes: 29 filed, one per mechanism, clustered by code site** (a note's `cluster:` names its fill unit). The ones
that change what a user's program does:

- **Wrong answers / crashes (MAJOR):**
  - PB1441: `NX"00G1"` re-lexes as the identifier NX plus a string. With an item named NX in scope, the program
    silently computes with that item.
  - PB1445: `VALUE --5` crashes the C# backend (CS1059). `VALUE +-5` and `MOVE - 5` are silently accepted.
  - PB1446: under DECIMAL-POINT IS COMMA, `1 ,5` is fused into the literal 1,5 (1.5 instead of 0.5).
  - PB1453: EC-PROGRAM-NOT-FOUND from a program-address-identifier is never set in a relation, and is nonfatal on
    SET.
  - PB1466: IS NUMERIC folds to `true` for packed, binary and national items, so validating a packed field after a
    READ answers yes on garbage. It answers FALSE for `FUNCTION NUMVAL("1.5")`.
  - PB1467: a relation on a variable-length group aborts the run unit, whether the pair is compatible or not.
  - PB1468: there is no class-pair comparability screen. An index-name compared with an alphanumeric item crashes.
  - PB1469: strong-group ordering compares byte images, so a FLOAT leaf orders wrongly. A program-pointer leaf
    escapes the SR4 ordering ban and aborts.
  - PB1471: `IF FL IS POSITIVE` on a FLOAT-LONG zero is true, because the sign-condition partition uses IsFloat, not
    IsStandardFloat. The golden ca8_float_sign_format2 pins the wrong answer.
  - PB1475: type equivalence ignores DECIMAL-POINT IS COMMA, so a CALL turns 1.50 into 150.00. It also compares
    locale NAMES rather than the external identification.
  - PB1477: a level-88 on an ODO group sizes a figurative to the maximum extent and disagrees with the relation.
- **Rejects legal source:** PB1448 and PB1449 (group-valued and interface properties), PB1452 (program-address
  identifier-1 category + the function-identifier arm; it also asks for the stale PB610 to be discharged), PB1454
  (the sum-counter data-qualifier), PB1456 (the report-section counter of another report, with PB1049), PB1464
  (parenthesized bare operands: `IF (BW)`, `IF (X1)`, `IF (S1-OFF)`), PB1465 (parenthesized boolean
  sub-expressions after a boolean operator).
- **Under-rejects:** PB1450 (GET/SET property descriptions), PB1455 (qualifier/subscript order), PB1457 (SEARCH ALL
  key ref-mod), PB1459 (the `*>` / `<>` edition gate), PB1460 (§8.4.6.3 rule 2 at bind), PB1470 (both-literal
  relation), PB1472–PB1474 (the subscript SR4 / SR6 / SR8 arms), PB1476 (the TYPE-in-TYPEDEF exemption). PB1458 is
  process-only: an internal-error message where §8.4.3.3.2 should be named.

**Extended rather than duplicated (18):** the four fold targets above; PB1047 (GLOBAL names: the condition-name arm and
the subordinate-of-a-shadowed-root arm of the same seam); PB1034 (the relational-operator superset, now seen from
§8.7.5.1, with the >>IF duplicate); PB1136 (the NX method-name decode and the NULL receiver); PB1137 (NULL as an INVOKE
argument); PB715; PB1133; PB160 (A.1 items 97/122); PB546 (boolean-national ref-mod width); PB1198; and five notes
14d/14e filed that reached main during the wait — PB1153 (report counters in string contexts: MOVE, STRING, IS NUMERIC,
PERFORM TIMES), PB1275 (the property polarity taxonomy, plus STRING INTO over-applying SR3), PB1306 (VARYING counters
referenced by subordinates), PB1280 (REDEFINES must resolve to the original entry), PB1247 (LINE PLUS 0 overprint).

**Dossier gaps (91 reported).** The dominant class is again the PB372 shape: the implementing site cites no clause
(BinderDriver's GLOBAL loop, the lexer's literal bodies, CollatingModel, BoundStores, the SEARCH ALL rules,
SortBindTable, PictureAnalyzer), so a citation-keyed dossier cannot surface it. Also reported: kb/Work PB610 is stale
(the program-address grammar landed with PB549); CobolLexer.g4's NATLIT/BOOLLIT comments still call NX/BX "deferred";
SymbolTable's doc comments cite §8.4.6.2.1 3) a) for a §11.7.4 question; and A.1 item 97's parenthetical still says
"General rule 4" (the rule is GR5 — a spec renumbering residue).

**Gate.** Build CobolSharp.sln; the WHOLE Unit assembly — 29,190 passed, 0 failed (the registrar fetched the GnuCOBOL
testsuite into its worktree first, so both ExternalCorpusPopulationDriftTests ran against a real population);
`work.py check` — 1,430 well-formed. The first gate run caught 39 adjudicator code-locations written as
`Class.Member` fragments, which the drift gate cannot resolve. They were narrowed to the member name (or the file) in
the batch, and the batch was re-recorded on a clean inventory.

