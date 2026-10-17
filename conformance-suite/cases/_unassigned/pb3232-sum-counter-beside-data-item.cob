*>--- conformance-case
*> id: pb3232-sum-counter-beside-data-item
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3232 - ISO 13.18.54.4 GR5: "If a data-name immediately follows the level number in the entry
      *> containing the SUM clause, the data-name is the name of the sum counter, not the name of the associated
      *> printable item, if any."  TOT-N below therefore names the sum counter of the CFG entry AND the data item
      *> under WS-G: two data-names of one spelling.  8.4.2.2.3 SR1: "For each non unique user-defined name that is
      *> explicitly referenced, uniqueness shall be established through a sequence of qualifiers that precludes any
      *> ambiguity of reference."  The unqualified `MOVE TOT-N TO WS-SEEN` is such a reference and no qualifier
      *> separates the two (TOT-N OF WS-G reaches the data item only, TOT-N OF R-1 the counter only), so it is
      *> refused - COBOLNET2145, as the same reference to two WORKING-STORAGE items of one spelling is refused
      *> (COBOLNET1639).  It used to bind the counter silently, because the sum counter was asked BEFORE the
      *> ordinary lookup and the data item was consulted only when the written qualifiers reached no counter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3232N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb3232n1.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  WS-G.
           02  TOT-N PIC 99 VALUE 7.
       01  WS-A PIC 99 VALUE 11.
       01  WS-SEEN PIC 99 VALUE 0.
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
           MOVE TOT-N TO WS-SEEN.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
