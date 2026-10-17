      *> kb/Work PB3232 - A SUM COUNTER AND AN ORDINARY DATA ITEM MAY SHARE A SPELLING; QUALIFICATION TELLS THEM APART.
      *> ISO 13.18.54.4 GR5: "If a data-name immediately follows the level number in the entry containing the SUM
      *> clause, the data-name is the name of the sum counter, not the name of the associated printable item, if any."
      *> So TOT-N below is the name of a sum counter AND (declaration 02 under WS-G) the name of a data item.  That
      *> is legal source: 8.4.2.2.1 / 8.4.2.2.3 SR1 ask for uniqueness only of a REFERENCE - "For each non unique
      *> user-defined name that is explicitly referenced, uniqueness shall be established through a sequence of
      *> qualifiers that precludes any ambiguity of reference" - and 8.4.2.2.3 SR2 lets a name be qualified at will.
      *> The counter's qualifiers are the report group entries above it and, last, its report-name (8.4.2.2.3 SR4,
      *> 8.4.2.2.2 Format 1's file-report-qualifier), so:
      *>   TOT-N OF WS-G   - the qualifier reaches only the data item (WS-G is no ancestor of the counter): 07
      *>   TOT-N OF R-1    - the qualifier reaches only the counter (R-1 is no ancestor of the data item): the counter
      *> The UNQUALIFIED `TOT-N` reaches both and is refused - tests/conformance/negative/pb3232-sum-counter-beside-
      *> data-item.cob.  It used to be silently bound to the counter, so `MOVE 3 TO TOT-N` altered the report total.
      *>
      *> DERIVATION.  One GENERATE after the INITIATE: 13.18.54.4 GR2 zeroes the counter at INITIATE and GR7 c) 1)
      *> adds each addend once per GENERATE, so the counter holds WS-A = 11 between the GENERATE and the TERMINATE.
      *> WS-G's TOT-N keeps its VALUE 07 until the MOVE 5, which alters only the data item: afterwards the item
      *> reads 05 and the counter still reads 11.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3232Q.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb3232q.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  WS-G.
           02  TOT-N PIC 99 VALUE 7.
       01  WS-A     PIC 99 VALUE 11.
       01  WS-ONE   PIC 99 VALUE 0.
       01  WS-TWO   PIC 99 VALUE 0.
       01  WS-THREE PIC 99 VALUE 0.
       01  WS-FOUR  PIC 99 VALUE 0.
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  TOT-N COLUMN 1 PIC 99 SUM WS-A.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DET-1.
           MOVE TOT-N OF WS-G TO WS-ONE.
           MOVE TOT-N OF R-1 TO WS-TWO.
           MOVE 5 TO TOT-N OF WS-G.
           MOVE TOT-N OF WS-G TO WS-THREE.
           MOVE TOT-N OF R-1 TO WS-FOUR.
           TERMINATE R-1.
           CLOSE PRT.
           DISPLAY "DATA-ITEM=" WS-ONE.
           DISPLAY "COUNTER=" WS-TWO.
           DISPLAY "DATA-ITEM-AFTER-MOVE=" WS-THREE.
           DISPLAY "COUNTER-AGAIN=" WS-FOUR.
           STOP RUN.
