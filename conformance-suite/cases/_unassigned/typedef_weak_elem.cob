*>--- conformance-case
*> id: typedef_weak_elem
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> TYPEDEF weak elementary TYPE (data-model D17; ISO 13.18.58 / 13.18.57, COBOL-2002). A named type declaration
      *> (DOLLARS) allocates NO storage; a TYPE reference clones its PICTURE into the referencing item, which then
      *> behaves exactly as a hand-written PIC 9(5)V99 item. The subject's own VALUE overrides (13.18.57.4 GR3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TYPEDEF-WEAK-ELEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DOLLARS TYPEDEF PIC 9(5)V99.
       77 AMOUNT TYPE DOLLARS VALUE 12.34.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "AMT=" AMOUNT.
           ADD 1 TO AMOUNT.
           DISPLAY "AMT2=" AMOUNT.
           STOP RUN.
