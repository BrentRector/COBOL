      *> kb/Work PB1226/PB1287 - ISO 13.15.2 / 13.18.20: the entry-name clause of a
      *> report group description entry may be written in its FILLER format.
      *> 13.18.20.3 SR3: "If the entry-name clause is specified in a data
      *> description entry or a report group description entry, either
      *> data-name-1 or FILLER shall be specified."
      *>   cite.py: OK  13.18.20.3 3)  (Syntax rules)
      *> 13.18.20.4 GR1: "The word FILLER may be used to name a data, report, or
      *> screen item."
      *>   cite.py: OK  13.18.20.4 1)  (General rule)
      *> 13.18.20.3 SR2: an omitted entry-name clause is FILLER, so a FILLER-named
      *> entry is an unnamed report item exactly like an entry with no name.
      *> The grammar's report arm listed only a cobolWord for the name, so every
      *> entry below was a parse error at every edition.
      *> DERIVATION: PAGE LIMIT 10, HEADING 1, FIRST DETAIL 3.
      *>   PAGE HEADING (an 01 FILLER group): line 1 holds "HDR" at column 1.
      *>   DE D1: line 3 holds the SOURCE item "A" at column 1 and the VALUE
      *>   "XYZ" at column 3, so the line reads "A XYZ".
      *>   Line 2 is unoccupied and prints blank.
      *> The read-back numbers each physical line and prints its first 8 bytes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1226FIL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1226FIL.TXT".
           SELECT CHK ASSIGN TO "PB1226FIL.TXT".
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
       01  WA      PIC X     VALUE "A".
       REPORT SECTION.
       RD  R PAGE LIMIT 10 LINES HEADING 1 FIRST DETAIL 3.
       01  FILLER TYPE PAGE HEADING.
           03  LINE 1.
               05  FILLER COLUMN 1 PIC X(3) VALUE "HDR".
       01  D1 TYPE DE.
           03  LINE 3.
               05  FILLER COLUMN 1 PIC X SOURCE WA.
               05  FILLER COLUMN 3 PIC X(3) VALUE "XYZ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE D1.
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
               WHEN WS-BYTE = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:8) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
