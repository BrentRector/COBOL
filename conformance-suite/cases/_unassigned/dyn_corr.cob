*>--- conformance-case
*> id: dyn_corr
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> OCCURS DYNAMIC review #2 regression (ISO 14.7.6 rule 4): MOVE CORRESPONDING must EXCLUDE a Format-4 DYNAMIC
      *> table member (an OCCURS item) rather than emit member access on the CobolDynTable<T> field (uncompilable C#).
      *> Only the ordinary scalar pair (SCAL) corresponds; the dynamic ELEM pair is skipped.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-CORR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 ELEM OCCURS DYNAMIC FROM 1.
             10 A PIC X(3).
          05 SCAL PIC X(5) VALUE "HELLO".
       01 DST.
          05 ELEM OCCURS DYNAMIC FROM 1.
             10 A PIC X(3).
          05 SCAL PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE CORRESPONDING SRC TO DST.
           DISPLAY "SCAL=[" SCAL OF DST "]".
           STOP RUN.
