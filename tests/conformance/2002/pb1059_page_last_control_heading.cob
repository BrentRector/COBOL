       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1059L.
      *> kb/Work PB1059 - ISO 13.18.39.2: the PAGE clause's fifth phrase
      *>   LAST {CONTROL HEADING | CH} IS integer-5
      *> (with FIRST DE / LAST DE as the 13.18.39.3 SR1 synonyms).
      *>   cite.py: OK  13.18.39.2  (General format)
      *> 13.18.39.4 GR2 e): "Integer-5 is the LAST CONTROL HEADING integer. It
      *> defines the last line position on which any line of a control heading
      *> may be printed."
      *>   cite.py: OK  13.18.39.4 2) e)  (General rules)
      *> 13.18.57.4 GR8 d): "The lower limit for a control heading is the line
      *> given by the LAST CONTROL HEADING integer."
      *>   cite.py: OK  13.18.57.4 8) d)  (General rules)
      *> 13.18.35.4 GR4 c): a body group whose first LINE clause is relative
      *> fits the page iff its trial sum does not exceed the group's lower
      *> limit; otherwise a page advance takes place before it is printed.
      *>   cite.py: OK  13.18.35.4 4)  (General rules)
      *> 13.18.39.4 GR3 c): if LAST CONTROL HEADING is omitted integer-5 is the
      *> LAST DETAIL integer, FOOTING, or the page limit - so WITHOUT the
      *> phrase the second control heading below would fit on page 1.
      *> DERIVATION: PAGE LIMIT IS 20 LINES, LAST CH IS 3 (HEADING and FIRST
      *> DETAIL default to 1, GR3 a)/b)). CONTROL IS CTL.
      *>   GENERATE with CTL "A": the first body group since INITIATE needs no
      *>   fit test; the control heading CH-A prints at lines 1 and 2 ("H=A",
      *>   "--") and the detail at line 3; LINE-COUNTER is 3.
      *>   GENERATE with CTL "B": control break. The control heading's lines
      *>   are both relative, trial sum >= 3 + 1 = 4 > the lower limit 3
      *>   (GR8 d)), so a page advance takes place and the heading prints at
      *>   lines 1 and 2 of page 2, the detail at line 3.
      *> The read-back numbers each physical line of a page; a form feed prints
      *> "(page)" and restarts the numbering.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1059L.TXT".
           SELECT CHK ASSIGN TO "PB1059L.TXT".
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
       01  CTL     PIC X     VALUE "A".
       REPORT SECTION.
       RD  R CONTROL IS CTL PAGE LIMIT IS 20 LINES
              LAST CH IS 3.
       01  CH-1 TYPE CONTROL HEADING CTL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC XX VALUE "H=".
               03  COLUMN 3 PIC X SOURCE CTL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC XX VALUE "--".
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "D".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE D1.
           MOVE "B" TO CTL.
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
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   DISPLAY "(page)"
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
