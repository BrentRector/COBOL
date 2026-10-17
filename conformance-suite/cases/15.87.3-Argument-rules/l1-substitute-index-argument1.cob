*>--- conformance-case
*> id: l1-substitute-index-argument1
*> rules: [AR-15.87.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.87.3 r1 — a class-INDEX DATA ITEM as argument-1. r1 admits "an identifier that references a
      *> data item or identifier that is class alphabetic, alphanumeric, or national, or an alphanumeric or
      *> national literal"; §8.5.2.1's Table 2 — "Class and category relationships for elementary data items" —
      *> carries "| Index | Index |" as its own row, so a USAGE INDEX item is class index and is refused.
      *>   python scripts/spec/cite.py --check 15.87.3 "Argument-1 shall be an identifier that references a data
      *>   item or identifier that is class alphabetic, alphanumeric, or national, or an alphanumeric or national
      *>   literal."  ->  OK  §15.87.3 1)  (Argument rules)
      *>   python scripts/spec/cite.py --check 8.5.2.1 "Each data item and each literal has a class and a
      *>   category."  ->  OK  §8.5.2.1  (General)
      *>
      *> THE THIRD ARM, and again a distinct code path: an index item is keyed by USAGE before the category
      *> table is consulted at all — the shape that once let FUNCTION INTEGER(index-item) silently compute the
      *> occurrence number (negative/intrinsic-index-argument, kb/Work R27). Written here so the SUBSTITUTE
      *> screen is witnessed on each of the three modelled exclusions rather than on whichever one was cheapest.
      *>
      *> ⛔ IT MUST BE THE INDEX **DATA ITEM**, NOT THE INDEX-**NAME** — and the first two drafts of this
      *> fixture got that wrong in two different ways. §13.18.38.3 SR1 a) forbids OCCURS on a level-01 entry,
      *> which the first draft violated; the second draft fixed that by subordinating the table but still passed
      *> the INDEXED BY name L1S-IX to SUBSTITUTE, and §13.18.38.3 SR7 (FORMATS 1, 2, AND 4) reads:
      *>       "7) Index-name-1 may be specified only in the following contexts:
      *>        — as a subscript;  — in the VARYING phrase of a PERFORM statement;
      *>        — in the VARYING phrase of a SEARCH statement;  — in the SET statement;
      *>        — as an operand in a relation condition."
      *> An intrinsic-function argument is none of those five, so an index-NAME there is illegal by SR7
      *> INDEPENDENTLY of §15.87.3 r1 — and COBOL.NET enforces SR7 (COBOLNET1637), so the rejection was not
      *> attributable to the rule under test. §15.87.3 r1 screens DATA ITEMS, and Table 2 is headed "Class and
      *> category relationships for elementary data items", so the rule under test cannot even reach an
      *> index-name. Passing a `USAGE INDEX` item instead is the shape r1 governs, and it draws COBOLNET1627.
      *>   python scripts/spec/cite.py --check 13.18.38.3 "Index-name-1 may be specified only in the following
      *>   contexts"  ->  OK  §13.18.38.3 7)  (Syntax rules)
      *>
      *> ⛔ THE TABLE STAYS SUBORDINATE TO L1S-G. §13.18.38.3 SR1 (ALL FORMATS): "The OCCURS clause shall not be
      *> specified in a data description entry that: a) Has a level-number of 01, 66, 77, or 88". COBOL.NET does
      *> not enforce SR1 a) today (no level-01 OCCURS screen exists anywhere under src/), so an illegal level-01
      *> table would reach the class screen and "pass" — resting on a second, unrecorded conformance gap and
      *> changing meaning the day that gap is closed.
      *>   python scripts/spec/cite.py --check 13.18.38.3 "The OCCURS clause shall not be specified in a data
      *>   description entry that"  ->  OK  §13.18.38.3 1)  (Syntax rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SUBNEGI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1S-G.
          05 L1S-T PIC X OCCURS 3 TIMES INDEXED BY L1S-IX.
       77 L1S-IDX USAGE INDEX.
       01 L1S-R PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           SET L1S-IX TO 2.
           SET L1S-IDX TO L1S-IX.
           MOVE FUNCTION SUBSTITUTE(L1S-IDX "A" "B") TO L1S-R.
           STOP RUN.
