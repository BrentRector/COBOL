      *>--- conformance-case
      *> id: l1_read_relative_key_moved
      *> rules: [GR-12.4.5.13.4-2, GR-14.9.30.4-25]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR25 — "For a relative file, if the RELATIVE KEY
      *> clause is specified for file-name-1, the execution of a READ
      *> statement moves the relative record number of the record made
      *> available to the relative key data item according to the rules
      *> for the MOVE statement."
      *> The key item is clobbered with 9999 before EVERY read, so the
      *> value printed after each one can only have come from the store
      *> GR25 mandates; K1/K2/K3 must be 0001/0002/0003.
      *> K4 is the at end read.  GR25's subject is "the record MADE
      *> AVAILABLE" and GR24 makes an at end execution unsuccessful, so
      *> no record is made available and no store is owed: the key item
      *> must still read 9999.  An implementation that stored the file
      *> position indicator unconditionally would print 0003 there.
      *> GR25 is a FORMAT 1 rule (it sits under the Format-1 heading
      *> with GR19-GR27); the Format-2 random read makes the key item
      *> the SOURCE instead (GR29), so every read below is sequential.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RD25A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1rd25a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-K
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       01 WS-K PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE 1 TO WS-K.
           MOVE "R001" TO F-REC.
           WRITE F-REC.
           MOVE 2 TO WS-K.
           MOVE "R002" TO F-REC.
           WRITE F-REC.
           MOVE 3 TO WS-K.
           MOVE "R003" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           OPEN INPUT F.
           MOVE 9999 TO WS-K.
           READ F NEXT AT END DISPLAY "K1-ATEND" END-READ.
           DISPLAY "K1=" F-ST " " WS-K " " F-REC.
           MOVE 9999 TO WS-K.
           READ F NEXT AT END DISPLAY "K2-ATEND" END-READ.
           DISPLAY "K2=" F-ST " " WS-K " " F-REC.
           MOVE 9999 TO WS-K.
           READ F NEXT AT END DISPLAY "K3-ATEND" END-READ.
           DISPLAY "K3=" F-ST " " WS-K " " F-REC.
      *> The at end read makes no record available: no store is owed.
           MOVE 9999 TO WS-K.
           READ F NEXT AT END DISPLAY "K4-ATEND=" F-ST END-READ.
           DISPLAY "K4=" F-ST " " WS-K.
           CLOSE F.
           STOP RUN.
