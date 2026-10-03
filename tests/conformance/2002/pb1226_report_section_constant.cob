      *> kb/Work PB1226 - ISO 13.8.2: the REPORT SECTION is
      *>   REPORT SECTION. [ report-description-entry
      *>     { constant-entry | report-group-description-entry } ... ] ...
      *> so a constant entry (13.10, COBOL-2002) may stand between the report
      *> groups of an RD.
      *>   cite.py: OK  13.8.2  (General format)
      *> 13.10.4 GR1: "If literal-1 ... is specified, the effect of specifying
      *> constant-name-1 in other than this entry is as if literal-1 ... were
      *> written where constant-name-1 is written."
      *>   cite.py: OK  13.10.4 1)  (General rules)
      *> 13.10.3 SR2: "If constant-name-1 is an integer, it may also be used to
      *> specify repetition in a picture character-string"
      *>   cite.py: OK  13.10.3 2)  (Syntax rules)
      *> 13.10.4 GR4: an arithmetic-expression constant is an integer.
      *> The report arm of the grammar had no constant-entry alternative, so
      *> every entry below was a reserved-word error on CONSTANT.
      *> DERIVATION: KC = 7; K2 = KC + 1 = 8 (a later constant reads an earlier
      *>   one). The procedure division displays both.
      *>   PAGE LIMIT 10, no HEADING/FIRST DETAIL (defaults), relative LINEs:
      *>   DL line 1: "7" at column 1 (VALUE KC) and PIC X(KC) = X(7) at
      *>   column 3 holding "ABCDEFG", so the line reads "7 ABCDEFG";
      *>   DL2 line 2: "8" at column 1 (VALUE K2).
      *> The read-back numbers each physical line and prints its first 12 bytes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1226CON.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1226CON.TXT".
           SELECT CHK ASSIGN TO "PB1226CON.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R PAGE LIMIT IS 10 LINES.
       01  KC CONSTANT AS 7.
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 VALUE KC.
           03  COLUMN 3 PIC X(KC) VALUE "ABCDEFG".
       01  K2 CONSTANT AS KC + 1.
       01  DL2 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 VALUE K2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "KC=" KC " K2=" K2.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE DL.
           GENERATE DL2.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:12) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
