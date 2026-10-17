*>--- conformance-case
*> id: typedef_weak_group
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> TYPEDEF weak GROUP TYPE (data-model D17; ISO 13.18.58 / 13.18.57, COBOL-2002). Two items reference the same
      *> group type declaration FEATURE; each gets an INDEPENDENT clone of the subtree (its own KIND/CNT storage,
      *> distinct emitted record-struct type + numeric profile). Subordinate names qualify (KIND OF F1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TYPEDEF-WEAK-GROUP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FEATURE TYPEDEF.
          05 KIND PIC X(4).
          05 CNT  PIC 9(3).
       01 F1 TYPE FEATURE.
       01 F2 TYPE FEATURE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AAAA" TO KIND OF F1.
           MOVE 11 TO CNT OF F1.
           MOVE "BBBB" TO KIND OF F2.
           MOVE 22 TO CNT OF F2.
           DISPLAY "F1=[" KIND OF F1 "][" CNT OF F1 "]".
           DISPLAY "F2=[" KIND OF F2 "][" CNT OF F2 "]".
           STOP RUN.
