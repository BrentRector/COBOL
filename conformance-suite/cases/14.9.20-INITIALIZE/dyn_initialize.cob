*>--- conformance-case
*> id: dyn_initialize
*> rules: [GR-14.9.20.4-10]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> INITIALIZE of a whole OCCURS DYNAMIC table (increment 4, data-model D9; ISO 14.9.20 GR10). Every occurrence
      *> up to the current capacity is initialized by the INITIALIZE statement's own stores -- the CATEGORY DEFAULTS
      *> (SPACES for alphanumeric, ZEROES for numeric), NOT the OCCURS grow-seed / VALUE clause -- and the current
      *> capacity is left unchanged. FROM 2: both occurrences, whose fields carry VALUE "XYZ"/55, become [   ]/00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-INITIALIZE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TABLE.
          05 WS-ROW OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2.
             10 WS-NAME PIC X(3) VALUE "XYZ".
             10 WS-QTY  PIC 9(2) VALUE 55.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AAA" TO WS-NAME OF WS-ROW (1).
           MOVE 11 TO WS-QTY OF WS-ROW (1).
           MOVE "BBB" TO WS-NAME OF WS-ROW (2).
           MOVE 22 TO WS-QTY OF WS-ROW (2).
           DISPLAY "B1=[" WS-NAME OF WS-ROW (1) "][" WS-QTY OF WS-ROW (1) "]".
           INITIALIZE WS-ROW.
           DISPLAY "A1=[" WS-NAME OF WS-ROW (1) "][" WS-QTY OF WS-ROW (1) "]".
           DISPLAY "A2=[" WS-NAME OF WS-ROW (2) "][" WS-QTY OF WS-ROW (2) "]".
           STOP RUN.
