      *> kb/Work PB1288 + PB1224 - the LEGAL side of ISO 13.15.3 SR5, SR9 and SR12 and of 13.15.4 GR2's BLANK WHEN ZERO and
      *> JUSTIFIED subject rules; conformance:negative/pb1288-* and pb1224-* are the refused side.
      *> SR5: "The TYPE clause may be specified only in a level 1 entry and shall be specified in every level 1 entry."
      *>   cite.py: OK  13.15.3 5)  (Syntax rules)
      *> SR9: "Every elementary entry with a COLUMN clause but no LINE clause shall be subordinate to an entry with a LINE clause."
      *>   cite.py: OK  13.15.3 9)  (Syntax rules)
      *> SR12: "A PICTURE clause shall be specified in every elementary entry that has a SOURCE or SUM clause."
      *>   cite.py: OK  13.15.3 12)  (Syntax rules)
      *> 13.18.8.3 SR1: BLANK WHEN ZERO "may be specified only for an elementary item described by its picture character-string as
      *>   category numeric-edited or as numeric without the picture symbol 'S'."   cite.py: OK  13.18.8.3 1)  (Syntax rules)
      *> 13.18.32.3 SR3: JUSTIFIED "may be specified only for a data item whose category is alphabetic, alphanumeric, boolean, or
      *>   national."   cite.py: OK  13.18.32.3 3)  (Syntax rules)
      *> Nothing here is newer than COBOL-85: the report writer's entry rules are the same at every edition.
      *> DERIVATION. One detail group D1 of two lines; the report is paged (PAGE LIMIT 20), so the first body group's first line
      *> is the FIRST DETAIL integer, 1 (13.18.35.4 GR5 b 3; the 13.18.39.4 GR3 defaults make it HEADING = 1).
      *>   Line 1 (03 LINE PLUS 1): the COLUMN entries are subordinate to it. COLUMN 1 prints WA = "A". UNP is an elementary
      *>   SOURCE entry with a PICTURE and no COLUMN: SR12 is satisfied and 13.18.53.4 GR3 makes it print nothing. COLUMN 3 is
      *>   PIC 99 BLANK WHEN ZERO over WZ = 0: two spaces. COLUMN 6 is PIC X(4) JUSTIFIED RIGHT over WB = "ab": "  ab" in columns
      *>   6-9. The line is "A", six spaces (columns 2-7), then "ab".
      *>   Line 2 (03 LINE PLUS 1 COLUMN 2 ...): LINE and COLUMN in ONE entry satisfy SR9 by the entry's own LINE clause: " L".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1288P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1288P.TXT".
           SELECT CHK ASSIGN TO "PB1288P.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC XX VALUE "ab".
       01  WN PIC 9 VALUE 7.
       01  WZ PIC 99 VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES.
       01  D1 TYPE DE.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X SOURCE WA.
               05  UNP PIC 9 SOURCE WN.
               05  COLUMN 3 PIC 99 BLANK WHEN ZERO SOURCE WZ.
               05  COLUMN 6 PIC X(4) JUSTIFIED RIGHT SOURCE WB.
           03  LINE PLUS 1 COLUMN 2 PIC X VALUE "L".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
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
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:10) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
