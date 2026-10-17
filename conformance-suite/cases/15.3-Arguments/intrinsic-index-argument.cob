*>--- conformance-case
*> id: intrinsic-index-argument
*> rules: [AR-15.3-5, AR-15.44.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R27 (ledger F82) - 8.5.2.1 Table 2 makes class INDEX distinct from class numeric,
      *> and 15.44.3 r1 requires class numeric - but an index DATA item's PicInfo carries category
      *> NUMERIC for storage, so it passed every class-numeric screen, and an index-NAME fell into
      *> the computed-operand-is-numeric arm: FUNCTION INTEGER(IX) computed the occurrence number
      *> silently. The usage-keyed CobolClass.Index arm now rejects both shapes.
      *>
      *> ⛔ THE TABLE IS SUBORDINATE TO T-G AND MUST STAY THAT WAY. §13.18.38.3 SR1 (ALL FORMATS): "The OCCURS
      *> clause shall not be specified in a data description entry that: a) Has a level-number of 01, 66, 77, or
      *> 88". This fixture previously wrote `01 T PIC 9 OCCURS 3 TIMES INDEXED BY IX.`, which is ILLEGAL COBOL —
      *> a negative fixture must be legal in every respect EXCEPT the rule under test, or its rejection is not
      *> attributable to §15.44.3 r1. COBOL.NET does not enforce SR1a today (no level-01 OCCURS screen exists
      *> under src/), so the illegal form reached the class screen and passed for the wrong reason; it would
      *> change meaning the day that screen lands.
      *>   python scripts/spec/cite.py --check 13.18.38.3 "The OCCURS clause shall not be specified in a data
      *>   description entry that"  ->  OK  §13.18.38.3 1)  (Syntax rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R27NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-G.
          05 T PIC 9 OCCURS 3 TIMES INDEXED BY IX.
       77 IX-ITEM USAGE INDEX.
       01 R PIC 9(9).
       PROCEDURE DIVISION.
           SET IX TO 2.
           SET IX-ITEM TO IX.
           COMPUTE R = FUNCTION INTEGER(IX-ITEM).
           COMPUTE R = FUNCTION INTEGER(IX).
           DISPLAY R.
           STOP RUN.
