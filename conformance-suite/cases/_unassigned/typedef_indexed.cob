*>--- conformance-case
*> id: typedef_indexed
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> A TYPEDEF whose OCCURS carries an INDEXED BY phrase, referenced ONCE (data-model D17). The clone drives the
      *> table through the (single) index-name; SET / subscripted store+read all work. Two references are legal too:
      *> each clone declares its own IX, referenced as IX OF A / IX OF B (kb/Work PB919; TypedefResidueTests).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TYPEDEF-INDEXED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL-T TYPEDEF.
          05 ROW OCCURS 3 INDEXED BY IX PIC X.
       01 A TYPE TBL-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "P" TO ROW OF A (1).
           SET IX TO 2.
           MOVE "Q" TO ROW OF A (IX).
           MOVE "R" TO ROW OF A (3).
           DISPLAY "R1=" ROW OF A (1) " R2=" ROW OF A (2) " R3=" ROW OF A (3).
           STOP RUN.
