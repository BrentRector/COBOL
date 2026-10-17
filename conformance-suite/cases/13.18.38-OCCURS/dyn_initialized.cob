*>--- conformance-case
*> id: dyn_initialized
*> rules: [GR-13.18.38.4-18, GR-14.6.2.3.2-L2.6]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> OCCURS DYNAMIC INITIALIZED with a GROUP element (increment 3, data-model D9; ISO 8.5.1.9.5). Every occurrence
      *> -- those opened at FROM and any grown later -- is seeded with the one-occurrence element image (the group's
      *> VALUE clauses). A subscripted element read/write crosses the RefSending/RefReceiving accessor then the group
      *> field tail. FROM 3: occ 1 and 3 keep their seeded VALUEs; occ 2 is overwritten.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-INITIALIZED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TABLE.
          05 WS-ROW OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 3 INITIALIZED.
             10 WS-NAME PIC X(4) VALUE "----".
             10 WS-QTY  PIC 9(2) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCD" TO WS-NAME OF WS-ROW (2).
           MOVE 99 TO WS-QTY OF WS-ROW (2).
           DISPLAY "R1=[" WS-NAME OF WS-ROW (1) "][" WS-QTY OF WS-ROW (1) "]".
           DISPLAY "R2=[" WS-NAME OF WS-ROW (2) "][" WS-QTY OF WS-ROW (2) "]".
           DISPLAY "R3=[" WS-NAME OF WS-ROW (3) "][" WS-QTY OF WS-ROW (3) "]".
           STOP RUN.
