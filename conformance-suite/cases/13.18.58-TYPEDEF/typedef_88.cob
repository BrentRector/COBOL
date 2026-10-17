*>--- conformance-case
*> id: typedef_88
*> rules: [GR-13.18.58.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> level-88 condition-names inside a TYPEDEF (data-model D17 inc 3; ISO 13.18.58.4 GR1 - the 88s are PART of
      *> the type). Each TYPE reference clones its OWN copy of the condition-names, testing / SETting its own storage;
      *> the two records' 88s are independent, and the template's names are not globally referenceable (GR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TYPEDEF-88.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 STATE-T TYPEDEF.
          05 ST-CODE PIC X.
             88 OPEN-ST   VALUE "O".
             88 CLOSED-ST VALUE "C".
       01 DOOR TYPE STATE-T.
       01 GATE TYPE STATE-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET OPEN-ST OF DOOR TO TRUE.
           MOVE "C" TO ST-CODE OF GATE.
           IF OPEN-ST OF DOOR
               DISPLAY "DOOR OPEN"
           END-IF.
           IF CLOSED-ST OF GATE
               DISPLAY "GATE CLOSED"
           END-IF.
           IF CLOSED-ST OF DOOR
               DISPLAY "DOOR CLOSED"
           ELSE
               DISPLAY "DOOR NOT CLOSED"
           END-IF.
           STOP RUN.
