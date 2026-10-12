## Entry 1696 — 2026-09-24 23:19 PDT — Register: fourteen notes from golden lane #2 and wave 59; SORT SR6 transcription repaired; ledger v72

Register-only checkpoint between landings (no compiler change). Every lead below came from a golden-lane-2 refuter or a
wave-59 implementer report, with its repro and code site recorded in the note.

- **Defects (wrong answer / crash / rejects legal source):** PB1556 (an elementary numeric record crashes READ/RETURN
  codegen, CS0029 — reproduced by a refuter), PB1557 (a 65,536-character ALPHABET wraps a `ushort` ordinal counter:
  CLASS/SYMBOLIC … IN reject legal source and ORD/CHAR answer wrong — reproduced), PB1558 (`ALPHABET … IS LOCALE` is
  rejected when the LOCALE clause follows it, the order §12.3.7.2 prints — reproduced on the rendered page; the golden
  `pb64t1_locale_declare` uses the non-conforming order), PB1560 (report SUM/PAGE-COUNTER carried in a `long`),
  PB1562 (variable-length group record under a fixed RECORD CONTAINS mis-splits), PB1565 (VARIANCE / STANDARD-DEVIATION
  cancel after narrowing), PB1566 (EXP(1000) prints Infinity), PB1567 (STRING without DELIMITED accepted at 85 —
  likely under-reject).
- **Diagnostic / tooling:** PB1559 (a report-writer rule violation filed under the 0899 not-implemented band), PB1564
  (the grammar-diagram drift test shares a machine-wide folder — the false red three implementers hit).
- **Analyses:** PB1555 (§14.9.30.4 GR21 e)1. vs e)3. disagree in the PRINTED standard, page 728 — not a transcription
  error), PB1561 (ODO object past its maximum now saturates — verify EC-BOUND-ODO), PB1563 (LINE SEQUENTIAL records
  with several dynamic-length members cannot carry the PB1053 extent table — derive what READ must do).
- **Folded / widened:** PB1550 and PB1552 RETIRED as duplicates of PB996 (inherited clause numbers in messages), which
  gained the SORT/MERGE, COBOLNET0805 and COBOLNET0844 cases; PB1551, PB1553, PB1554, PB1086, PB1425, PB1548, PB1556,
  PB1557 widened with new repros. PB1178's owner question was answered by the standing owner-decision protocol: the
  GnuCOBOL-surveyed A.1 item 192 determination (a STATUS value wins over ERROR/NORMAL) stands.
- **Transcription repaired on discovery:** `specs/ISO_COBOL.md` §14.9.40.3 SR6 items e)–g) had lost their list
  indentation, so `cite.py` labelled g) as `6) d)`; re-indented, `cite.py --check` now answers `6) g)`.
- **Ledger** artifact v72 published (GAP 1458); trend point appended.

