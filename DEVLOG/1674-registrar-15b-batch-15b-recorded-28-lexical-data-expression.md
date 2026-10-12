## Entry 1674 — 2026-09-24 10:28 PDT — Registrar 15b: batch 15b recorded (28 lexical / data / expression / identifier subjects), 41 notes filed (PB1390–PB1432), GAP 1812 → 1762

**What.** Registrar 15b registered lane-3 adjudication batch 15b: 28 subjects from clause 8 (abbreviated combined
relation conditions through LINAGE-COUNTER: literals, character sets and the COBOL repertoire, COBOL words, class and
category, conditional / boolean / concatenation expressions, the identifier formats, data-, function- and
program-address identifiers, dynamic-capacity tables, figurative constants, levels, lexical elements), adjudicated
on the pinned tree `adj-d2` @071ff04ea. Nothing that landed on main since the pin touches `src/` (registrar landings
only), so no finding had already been fixed. **GAP 1812 → 1762** (+50 closed of 4,348).

**Verdicts.** 190 records: CONFORMS 74 (50 closed their row; 24 are CONFORMS-but-untested and go to the golden lane's
next input set), PARTIAL 58, DIVERGES 57, NOT-IMPLEMENTED 1. No row was verdicted NEEDS-OWNER-DECISION and no owner
verdict was touched. The refuters re-examined 115 rows and overturned 41 — 34 to PARTIAL, 7 to DIVERGES — every
overturn toward MORE defect. Two subjects (complex-combined-conditions, function-address-identifier) had no refuter
checkpoint; neither carried a CONFORMS record, so nothing was excluded. The adjudicators' free-text shapes were
normalized at source before recording (156 `editions` fields written as prose, 26 code-locations naming a symbol the
file does not spell), with the original text kept in `notes`.

**Notes: 41 filed (PB1390–PB1394, PB1397–PB1432), 22 extended, four drafts folded** — PB1396 (the REDEFINES
byte codec dropping the high byte of an alphanumeric code unit above U+00FF) went into **PB547**, which the
adjudicator itself named as the root. Clustered by code site; the ones that change what a user's program does:

- **Wrong answers / crashes (MAJOR):** PB1394 (§8.3.5 separator context is checked nowhere: X"GG" / B"012" / BX"G1"
  re-lex as an identifier plus a literal and run as two operands); PB1401 (class conditions on LINE-/PAGE-/LINAGE-COUNTER and numeric functions crash);
  PB1405 (a universal INVOKE dispatches by the DECLARED method-name, not the externalized one); PB1406 (ConcatFolder:
  word operands refused, first-operand class, national HIGH-VALUE folded as U+00FF); PB1407 (ADDRESS OF has no
  §8.4.3.11.3 screen and refuses a legal ref-mod); PB1409 (READ INTO a zero-length Format 3 record); PB1410 (dynamic
  table growth dies with OutOfMemoryException instead of EC-BOUND-TABLE-LIMIT); PB1411 (a variable-length-group
  element past capacity is written to the scratch slot); PB1414 (INSPECT ignores the PCS for LOW-VALUE); PB1418 (the
  user-function argument binder re-implements CALL's and aliases a CONSTANT RECORD BY REFERENCE); PB1419 (whole
  RETURNING categories refused); PB1420 (a nested integer function in CONCAT aborts at run time); PB1423 (function
  arguments are not evaluated left to right); PB1425 (identifier Formats 4–7 missing from object-reference positions);
  PB1429 (chained inline invocation on a universal temp → internal error); PB1432 (`goto __xfer` inside a lambda,
  CS0159, under EC-ALL); PB1390 (abbreviated relations after OR-then-AND or after XOR do not parse); PB1402 (no
  extended letter lexes in any word, although CONFORMANCE.md claims A.4.6).
- **Under-rejects / wrong diagnostics (MINOR):** PB1391, PB1392, PB1393, PB1397 (six undocumented A.1
  determinations), PB1398, PB1399, PB1400, PB1403, PB1404, PB1408, PB1412, PB1413, PB1415, PB1416, PB1417, PB1421,
  PB1422, PB1424, PB1426, PB1427, PB1428, PB1430, PB1431.
- **Extended instead of duplicated:** PB1162 (the edition-blind subscript trigger also breaks '(' after XOR and after
  every boolean operator), PB547, PB1093, PB1144, PB1199, PB1086, PB656, PB579, PB1127, PB1078, PB1062, PB1042,
  PB1167, PB1142, PB1137, PB1049, PB578, PB1246, PB1228, PB1269, PB1353, PB1383; PB372 carries the batch's 101 dossier gaps.

**Folds at landing.** Registrars 14d and 14e landed while this batch waited its turn, so two more drafts were folded
into their notes before filing: PB1433 (the level-nesting walk roots an entry that empties the stack, pushes a 77 as
level 77, lets a 66 own a 77) → **PB1246**, and PB1434 (CONSTANT FROM compilation-variable-name staged loud, which
makes §8.3.2.2 1)'s sharing permission unverifiable) → **PB1228**. 14e's **PB1269** (EC-BOUND-OVERFLOW raised before
the implicit growth) took the §8.5.1.9.6 1) finding instead of a new note. Registrar 15a (landed just ahead) filed
**PB1353** on the same `ReadReplaceOperand` scan, so PB1395 (an unterminated `==` swallows the rest of the source as
the REPLACE operand; no §8.3.5 SR6 separator context) was folded there, and the §8.3.2.2 2) b) row went to 15a's
**PB1383** (`>>CALL-CONVENTION` accepts any word) rather than to PB661. PB1395, PB1396 and PB1433–PB1439 are unused.

**Gate.** `dotnet build CobolSharp.sln -c Debug` clean; the WHOLE Unit assembly on the rebased tree (GnuCOBOL corpus fetched, so the two ExternalCorpusPopulationDriftTests ran for real): `Passed!  - Failed: 0, Passed: 29190, Skipped: 0, Total: 29190`; `work.py check` all well-formed.

