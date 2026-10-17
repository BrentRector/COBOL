*>--- conformance-case
*> id: ca36_search_range_dispatch
*> rules: [GR-14.9.37.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      >>TURN EC-RANGE CHECKING ON
      *> CA36 (CONFORMANCE-FIX-QUEUE): with EC-RANGE checking ON, a SEARCH that ends unsuccessfully and has NO AT END
      *> phrase must transfer to an applicable exception-processing statement (ISO 14.9.37.4 GR1b2) — here the USE
      *> AFTER EXCEPTION CONDITION EC-RANGE-SEARCH-NO-MATCH declarative. No element equals 9, so the scan advances
      *> past occurrence 3 (GR6) and EC-RANGE-SEARCH-NO-MATCH is set; AT END is absent -> the declarative runs
      *> ("DECL-RAN"); it is Nonfatal (Table 13) and returns normally, so control transfers to the end of the SEARCH
      *> and execution continues ("AFTER-SEARCH"). Pre-fix the SEARCH set the EC status but never DISPATCHED it, so
      *> the declarative was skipped and only "AFTER-SEARCH" printed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA36.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC 9 OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-SEARCH-NO-MATCH.
       H-PARA.
           DISPLAY "DECL-RAN".
       END DECLARATIVES.
       M-SEC SECTION.
       M-PARA.
           MOVE 1 TO E(1)
           MOVE 2 TO E(2)
           MOVE 3 TO E(3)
           SET IX TO 1
           SEARCH E WHEN E(IX) = 9 CONTINUE END-SEARCH
           DISPLAY "AFTER-SEARCH"
           STOP RUN.
