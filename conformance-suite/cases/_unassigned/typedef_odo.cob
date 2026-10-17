*>--- conformance-case
*> id: typedef_odo
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> OCCURS DEPENDING ON inside a TYPEDEF (data-model D17; review DEVLOG 664 fix #4). Each TYPE reference's internal
      *> DEPENDING binds to the CLONE's OWN counter (13.18.57.4 GR1 + 13.18.38 SR20), not a globally-first same-named
      *> item - so two records of the same type have INDEPENDENT logical lengths. Before the fix, T2's ELEM bound to
      *> T1's CNT, giving T2 a wrong 5-char image.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TYPEDEF-ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL-T TYPEDEF.
          05 CNT  PIC 9.
          05 ELEM PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       01 T1 TYPE TBL-T.
       01 T2 TYPE TBL-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 3 TO CNT OF T1.
           MOVE "A" TO ELEM OF T1 (1).
           MOVE "B" TO ELEM OF T1 (2).
           MOVE "C" TO ELEM OF T1 (3).
           MOVE 1 TO CNT OF T2.
           MOVE "Z" TO ELEM OF T2 (1).
           DISPLAY "T1=<" T1 ">".
           DISPLAY "T2=<" T2 ">".
           STOP RUN.
