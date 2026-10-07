      *> kb/Work PB1474 - the LEGAL side of ISO 8.4.2.3.3 SR8: "In the report section, neither a sum counter nor the LINE-COUNTER and PAGE-COUNTER
      *> identifiers may be used as a subscript."   cite.py: OK  8.4.2.3.3 8)   conformance:negative/pb1474-* are the refused side.
      *> A data item described outside the report section subscripts a table freely, and PAGE-COUNTER stays legal as a SOURCE operand of its own
      *> (8.4.3.15.3 SR1). Nothing here is newer than COBOL-85.
      *> DERIVATION. WS-K is 2, so TE(WS-K) is the second element of the table, 5. Column 1 prints it (SOURCE); columns 2-3 print the SUM counter
      *> of the same addend, 5 added once by the one GENERATE, as 05; column 4 prints PAGE-COUNTER, page 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1474P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1474P.TXT".
           SELECT CHK ASSIGN TO "PB1474P.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  WS-K PIC 9 VALUE 2.
       01  TB.
           05  TE1 PIC 9 VALUE 4.
           05  TE2 PIC 9 VALUE 5.
           05  TE3 PIC 9 VALUE 6.
       01  TBR REDEFINES TB.
           05  TE PIC 9 OCCURS 3.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 SOURCE TE(WS-K).
           03  COLUMN 2 PIC 99 SUM TE(WS-K).
           03  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
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
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
