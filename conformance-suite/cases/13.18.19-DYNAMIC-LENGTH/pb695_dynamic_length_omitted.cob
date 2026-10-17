*>--- conformance-case
*> id: pb695_dynamic_length_omitted
*> rules: [FMT-13.18.19.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> !! THE DYNAMIC LENGTH CLAUSE'S OPTIONAL WORDS, OMITTED (kb/Work PB695 family 2).
      *> ISO 13.18.19.2, read off the printed page (PDF p427 / folio 397). The format is
      *>     DYNAMIC LENGTH [ dynamic-length-structure-name-1 ] [ LIMIT IS integer-1 ]
      *> and the underlines fall on DYNAMIC and LIMIT ALONE. LENGTH and IS are printed plain, so
      *> 8.3.2.4.3 makes `DYNAMIC LIMIT 30` the same clause as `DYNAMIC LENGTH LIMIT IS 30`; COBOL.NET
      *> required the LENGTH and answered COBOL0001. DYNAMIC stays the required anchor and heads no other
      *> data-description clause, so the relaxation adds no ambiguity, and the COBOL-2014 introduction
      *> gate keys on the CLAUSE context (VersionConformancePass.VisitDynamicLengthClause), not on the
      *> word - `--std 2002` still answers COBOLNET0900.
      *> 13.18.19.4 GR1 sets the minimum length to zero, so D-BARE is length 0 before the MOVE; GR2 makes
      *> integer-1 the maximum, and 3 <= 30 so the MOVE is exact. FUNCTION LENGTH of a dynamic-length item
      *> is its CURRENT length (15.50), hence LEN0=0 then LEN1=3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695DYNL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D-BARE          PIC X DYNAMIC.
       01  D-LIMIT         PIC X DYNAMIC LIMIT 30.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "LEN0=" FUNCTION LENGTH (D-BARE)
           MOVE "ABC" TO D-BARE
           DISPLAY "LEN1=" FUNCTION LENGTH (D-BARE)
           DISPLAY "VAL1=" D-BARE
           MOVE "WXYZ" TO D-LIMIT
           DISPLAY "LEN2=" FUNCTION LENGTH (D-LIMIT)
           DISPLAY "VAL2=" D-LIMIT
           DISPLAY "DONE"
           STOP RUN.
