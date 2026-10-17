      *>--- conformance-case
      *> id: pb416_initialize_operand_screens_85
      *> rules: [SR-14.9.20.3-1, SR-14.9.20.3-4, SR-14.9.20.3-5, SR-14.9.20.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB416 — the POSITIVE side of the four §14.9.20.3 screens INITIALIZE never asked, at COBOL-85,
      *> where every one of the four rules already applies (INITIALIZE, its REPLACING phrase and the five classic
      *> category names are all COBOL-85; the negatives are tests/conformance/negative/pb416-*).
      *>
      *> A screen is only as good as what it still ADMITS, so this file is the over-rejection witness for all
      *> four: SR1 (identifier-1's class), SR4 (the implied MOVE's validity), SR5 (no RENAMES on identifier-1)
      *> and SR7 (identifier-1 is the receiving operand, and therefore subject to the receiving-operand
      *> prohibitions). The negative witnesses are five files under tests/conformance/negative/.
      *>
      *> EXPECTED, DERIVED FROM THE STANDARD BEFORE THE RUN:
      *>   T1  INITIALIZE G REPLACING <five '85 categories>. §14.9.20.3 SR4's second paragraph admits every one
      *>       of these pairs, because §14.9.25.3 Table 16 marks each "Yes":
      *>         ALPHABETIC          BY "Q"    — Alphanumeric row x Alphabetic column      -> AB = "Q  "
      *>         ALPHANUMERIC        BY "pqr"  — Alphanumeric row x Alphanumeric column    -> AN = "pqr"
      *>         ALPHANUMERIC-EDITED BY "mn"   — Alphanumeric row x Alphanumeric column    -> AE = "mn   "
      *>                                         (PIC XXBXX: data positions 1,2,4,5 take "mn" then space fill,
      *>                                          the B inserts a space at position 3 — §13.18.40.5 GR3)
      *>         NUMERIC             BY 42     — Numeric/Integer row x Numeric column      -> NU = 042
      *>         NUMERIC-EDITED      BY 9      — Numeric/Integer row x Numeric column      -> NE = "   9"
      *>       IX is USAGE INDEX and is SUBORDINATE to identifier-1, so §14.9.20.4 GR5a1 excludes it from the
      *>       receiver set SILENTLY — that exclusion is NOT SR1 and must keep its silence; SR1 is a rule about
      *>       identifier-1 itself.
      *>   T2  the bare COBOL-85 form (GR5c4 + GR6c's fill table): SPACES for the three character categories
      *>       (alphabetic, alphanumeric, alphanumeric-edited) and ZEROES for numeric; the numeric-edited
      *>       receiver takes the figurative ZERO through the ordinary MOVE editing path, PIC ZZZ9 suppressing
      *>       the leading zeros -> "   0".
      *>   T3  identifier-1 of CLASS NUMERIC — one of the eight classes SR1 admits (§8.5.2.1 Table 2). -> 0000
      *>   T4  identifier-1 of CLASS ALPHABETIC — likewise admitted; GR6c fills it with SPACES. -> "    "
      *>   T5  identifier-1 that is a level-01 group with a level-66 RENAMES entry BESIDE it. SR5 forbids a
      *>       RENAMES clause in the data description entry of identifier-1, and REC's entry has none — the
      *>       66 entry is a separate entry, not storage subordinate to REC (§13.18.45), so REC is initialized
      *>       whole and the rule does not reach it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB416OS85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 AB PIC A(3) VALUE "abc".
          05 AN PIC X(3) VALUE "xyz".
          05 AE PIC XXBXX VALUE "ab cd".
          05 IX USAGE INDEX.
          05 NU PIC 9(3) VALUE 123.
          05 NE PIC ZZZ9 VALUE "  42".
       01 SOLO-NUM PIC 9(4) VALUE 77.
       01 SOLO-ALPHA PIC A(4) VALUE "wxyz".
       01 REC.
          05 R1 PIC 9(3) VALUE 555.
          05 R2 PIC X(3) VALUE "def".
       66 R-BOTH RENAMES R1 THRU R2.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING ALPHABETIC DATA BY "Q"
                                  ALPHANUMERIC DATA BY "pqr"
                                  ALPHANUMERIC-EDITED DATA BY "mn"
                                  NUMERIC DATA BY 42
                                  NUMERIC-EDITED DATA BY 9.
           DISPLAY "T1=[" AB "][" AN "][" AE "][" NU "][" NE "]".
           INITIALIZE G.
           DISPLAY "T2=[" AB "][" AN "][" AE "][" NU "][" NE "]".
           INITIALIZE SOLO-NUM.
           DISPLAY "T3=[" SOLO-NUM "]".
           INITIALIZE SOLO-ALPHA.
           DISPLAY "T4=[" SOLO-ALPHA "]".
           INITIALIZE REC.
           DISPLAY "T5=[" R1 "][" R2 "]".
           STOP RUN.
