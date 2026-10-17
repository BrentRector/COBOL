*>--- conformance-case
*> id: dyn_search
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> SEARCH over an OCCURS DYNAMIC table (increment 4, data-model D9; ISO 14.9.37 / 8.5.1.9.1). The serial scan
      *> bounds over the table's CURRENT capacity (a run-time value) -- NOT a compile-time maximum (a dynamic table
      *> has none): a WHEN match within capacity is found; a value beyond the populated occurrences reaches AT END
      *> exactly at Capacity (the scan never runs past the current capacity). The scan is bracketed by
      *> EnterSearch/ExitSearch so a SET Format 14 on the same table during the search would raise EC-FLOW-SEARCH
      *> (GR31); here the search completes normally and ExitSearch runs via the finally.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-SEARCH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-POS PIC 9(2).
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 5
             INDEXED BY IDX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 10 TO WS-E (1).
           MOVE 20 TO WS-E (2).
           MOVE 30 TO WS-E (3).
           MOVE 40 TO WS-E (4).
           MOVE 50 TO WS-E (5).
           SET IDX TO 1.
           SEARCH WS-E
               AT END DISPLAY "NF-30"
               WHEN WS-E (IDX) = 30
                   SET WS-POS TO IDX
                   DISPLAY "FOUND VALUE=" WS-E (IDX) " POS=" WS-POS.
           SET IDX TO 1.
           SEARCH WS-E
               AT END DISPLAY "NF-99"
               WHEN WS-E (IDX) = 99
                   DISPLAY "FOUND 99".
           STOP RUN.
