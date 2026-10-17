      *>--- conformance-case
      *> id: pb370_read_relative_invalid_key
      *> rules: [GR-14.9.30.4-29]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR29: "For a relative file, execution of a READ
      *> statement sets the file position indicator to the value
      *> contained in the data item referenced by the RELATIVE KEY
      *> clause for the file, and the record whose relative record
      *> number equals the file position indicator is made available in
      *> the record area associated with file-name-1. If the physical
      *> file does not contain such a record, the invalid key condition
      *> exists and execution of the READ statement is unsuccessful.
      *> (See 9.1.14, Invalid key condition.)"
      *> §9.1.14 2): with INVALID KEY written, control goes to its
      *> imperative statement and no exception procedure runs; 3): with
      *> no INVALID KEY phrase, "any applicable exception processing
      *> statements are executed" - here the file's USE procedure.
      *> '23' is the §9.1.13.5 3) a) value: "an attempt is made to
      *> randomly access a record that does not exist in the physical
      *> file". FR holds relative record numbers 2, 4 and 5.
      *>   R4   random READ of RRN 4: found, NOT INVALID KEY, REC-4.
      *>   N4   READ NEXT: the indicator was SET FROM RK (4), so the
      *>        next record is 5.
      *>   R2   random READ of RRN 2, then N2 READ NEXT -> 4. N4 had
      *>        left the file at its last record, so only an indicator
      *>        re-set from RK (2) by the random READ can deliver 4
      *>        here.
      *>   R3   RRN 3 was never written: INVALID KEY, '23'.
      *>   R9   RRN 9 is past the last record: INVALID KEY, '23'.
      *>   R0   RRN 0 numbers no record (relative records number from
      *>        1): INVALID KEY, '23'.
      *>   R7   RRN 7, no INVALID KEY phrase: 9.1.14 3) runs the USE
      *>        procedure for FR (USE-FR 23), then the next statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370RI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "pb370ri.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD FR.
       01 R-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 ST PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-FR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FR.
       D-FR-P.
           DISPLAY "USE-FR " ST.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT FR
           MOVE 2 TO RK MOVE "REC-2" TO R-REC WRITE R-REC
           MOVE 4 TO RK MOVE "REC-4" TO R-REC WRITE R-REC
           MOVE 5 TO RK MOVE "REC-5" TO R-REC WRITE R-REC
           CLOSE FR
           OPEN INPUT FR
           MOVE 4 TO RK
           READ FR
               INVALID KEY DISPLAY "R4 INVALID"
               NOT INVALID KEY DISPLAY "R4 FOUND"
           END-READ
           DISPLAY "R4 " ST " " R-REC
           READ FR NEXT AT END DISPLAY "N4 AT END" END-READ
           DISPLAY "N4 " ST " " RK " " R-REC
           MOVE 2 TO RK
           READ FR
               INVALID KEY DISPLAY "R2 INVALID"
           END-READ
           DISPLAY "R2 " ST " " R-REC
           READ FR NEXT AT END DISPLAY "N2 AT END" END-READ
           DISPLAY "N2 " ST " " RK " " R-REC
           MOVE 3 TO RK
           READ FR
               INVALID KEY DISPLAY "R3 INVALID"
               NOT INVALID KEY DISPLAY "R3 FOUND"
           END-READ
           DISPLAY "R3 " ST
           MOVE 9 TO RK
           READ FR
               INVALID KEY DISPLAY "R9 INVALID"
               NOT INVALID KEY DISPLAY "R9 FOUND"
           END-READ
           DISPLAY "R9 " ST
           MOVE 0 TO RK
           READ FR
               INVALID KEY DISPLAY "R0 INVALID"
               NOT INVALID KEY DISPLAY "R0 FOUND"
           END-READ
           DISPLAY "R0 " ST
           MOVE 7 TO RK
           READ FR
           DISPLAY "R7 " ST
           CLOSE FR
           STOP RUN.
