      *>--- conformance-case
      *> id: l1c03_report_column_synonyms_printable_size
      *> rules: [SR-13.18.14.3-1, GR-13.18.14.4-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.14.3 SR1 and §13.18.14.4 GR3 - COLUMN / COL /
      *> COLUMNS / COLS are one clause; printable-size comes from the
      *> PICTURE, or with no PICTURE from the VALUE literal
      *>
      *> "1. COLUMN, COL, COLUMNS, and COLS are synonyms."
      *>   cite.py: OK  §13.18.14.3   (Syntax rules)   (rule 1.)
      *> "3) The printable-size of a printable item is the number of
      *> columns required for printing the characters described by the
      *> item's PICTURE clause or, in the absence of a PICTURE clause,
      *> the literal specified in the VALUE clause. There is a
      *> one-to-one correspondence between a column and a character in
      *> an alphanumeric character set."
      *>   cite.py: OK  §13.18.14.4 3)  (General rules)
      *> Relative placement: "The position of the item's leftmost
      *> character is obtained by adding integer-2 to the current
      *> line's horizontal counter."
      *>   cite.py: OK  §13.18.14.4 8)  (General rules)
      *> "The rightmost column position of each printable item becomes
      *> the new value of the horizontal counter."
      *>   cite.py: OK  §13.18.14.4 9)  (General rules)
      *> Absolute with no LEFT/CENTER/RIGHT: "LEFT is assumed."
      *>   cite.py: OK  §13.18.14.3 9)  (Syntax rules)
      *> "Any unoccupied columns in each print line are filled with
      *> space characters."  cite.py: OK  §13.18.14.4 10)
      *> Lines: unpaged report, relative LINE clauses, first group at
      *> LINE-COUNTER 0 + 1 (cite.py: OK §13.18.35.4 5) 5. c)), each
      *> next group at LINE-COUNTER + 1.
      *> Edition: the COL / COLS / COLUMNS spellings and COLUMN PLUS
      *> are 2002 introductions (COBOL-85 has only COLUMN NUMBER
      *> IS integer-1), so this golden runs at 2002.
      *>
      *> DERIVATION of each expected report line.
      *>   L1 (SR1): COLUMN 1 "A", COL 3 "B", COLS 5 "C", COLUMNS 7 "D",
      *>       COL NUMBER IS 9 "E", COLUMN NUMBERS 11 "F" - every
      *>       spelling places its item at its own integer-1 (LEFT):
      *>       "A B C D E F".
      *>   L2 (GR3 via the horizontal counter, each item followed by
      *>       COLUMN PLUS 1 "|", so each "|" lands one column after the
      *>       previous item's rightmost column):
      *>       PIC 9(3)V99 = 12.34 -> 5 columns "01234" (V prints none)
      *>       PIC S999 SIGN LEADING SEPARATE = -12 -> 4 columns "-012"
      *>       PIC -ZZ9.99 = -12.34 -> 7 columns "- 12.34"
      *>       VALUE "ABC", no PICTURE -> 3 columns "ABC"
      *>       -> "01234|-012|- 12.34|ABC|".
      *>   L3 (GR3, PICTURE wins over the VALUE literal): COLUMN 1
      *>       PIC X(4) VALUE "PQ" -> printable-size 4 from the PICTURE
      *>       (not 2 from the literal), so the relative "|" lands at
      *>       column 5: "PQ  |".
      *> (A COLUMN RIGHT / CENTER leg is deliberately absent: the
      *> alignment phrase has no grammar surface yet - open defect
      *> kb/Work PB1220, a different rule.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C03J.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "l1c03j.txt".
           SELECT CHK ASSIGN TO "l1c03j.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-J.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC 9(3)V99 VALUE 12.34.
       01  WS-S    PIC S999    VALUE -12.
       01  WS-E    PIC S99V99  VALUE -12.34.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-J.
       01  DET1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "A".
           03  COL 3 PIC X VALUE "B".
           03  COLS 5 PIC X VALUE "C".
           03  COLUMNS 7 PIC X VALUE "D".
           03  COL NUMBER IS 9 PIC X VALUE "E".
           03  COLUMN NUMBERS 11 PIC X VALUE "F".
       01  DET2 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9(3)V99 SOURCE WS-N.
           03  COLUMN PLUS 1 PIC X VALUE "|".
           03  COLUMN PLUS 1 PIC S999 SIGN LEADING SEPARATE
                   SOURCE WS-S.
           03  COLUMN PLUS 1 PIC X VALUE "|".
           03  COLUMN PLUS 1 PIC -ZZ9.99 SOURCE WS-E.
           03  COLUMN PLUS 1 PIC X VALUE "|".
           03  COLUMN PLUS 1 VALUE "ABC".
           03  COLUMN PLUS 1 PIC X VALUE "|".
       01  DET3 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(4) VALUE "PQ".
           03  COLUMN PLUS 1 PIC X VALUE "|".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-J.
           GENERATE DET1.
           GENERATE DET2.
           GENERATE DET3.
           TERMINATE R-J.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:24) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
