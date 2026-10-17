      *>--- conformance-case
      *> id: pb370_start_forward_operators
      *> rules: [GR-14.9.41.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.41.4 GR9 a) (RELATIVE FILES): "If the relational
      *> operator is EQUAL, GREATER, NOT LESS, or GREATER OR EQUAL, the
      *> file position indicator is set to the relative record number of
      *> the first logical record in the file whose key satisfies the
      *> comparison searching the file sequentially."
      *> The COMPLEMENT of start-reverse-operators-2002 (kb/Work PB370):
      *> that gate folds the written operator with the binder's own
      *> relational fold and refuses only the REVERSE results (< and <=)
      *> below 2002. NOT LESS and NOT < fold to >=, a FORWARD search,
      *> and are COBOL-85 (the NIST CCVS-85 programs write them), so at
      *> --std 85 they must still compile and search forward. A gate
      *> keyed on the word LESS, or on the presence of NOT, would refuse
      *> this program.
      *> FR holds relative record numbers 2, 4 and 6; READ NEXT after
      *> each START prints the selected record and its number in RK.
      *>   NL3  KEY NOT LESS 3: forward, the first number >= 3 is 4.
      *>   NS5  KEY NOT < 5: forward, 6.
      *>   GT2  KEY GREATER 2: forward, 4.
      *>   EQ6  KEY EQUAL 6: 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370SF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "pb370sf.dat"
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
       MAIN.
           OPEN OUTPUT FR
           MOVE 2 TO RK MOVE "REC-2" TO R-REC WRITE R-REC
           MOVE 4 TO RK MOVE "REC-4" TO R-REC WRITE R-REC
           MOVE 6 TO RK MOVE "REC-6" TO R-REC WRITE R-REC
           CLOSE FR
           OPEN INPUT FR
           MOVE 3 TO RK
           START FR KEY IS NOT LESS THAN RK
               INVALID KEY DISPLAY "NL3 INVALID"
           END-START
           READ FR NEXT AT END DISPLAY "NL3 AT END" END-READ
           DISPLAY "NL3 " ST " " RK " " R-REC
           MOVE 5 TO RK
           START FR KEY IS NOT < RK
               INVALID KEY DISPLAY "NS5 INVALID"
           END-START
           READ FR NEXT AT END DISPLAY "NS5 AT END" END-READ
           DISPLAY "NS5 " ST " " RK " " R-REC
           MOVE 2 TO RK
           START FR KEY GREATER RK
               INVALID KEY DISPLAY "GT2 INVALID"
           END-START
           READ FR NEXT AT END DISPLAY "GT2 AT END" END-READ
           DISPLAY "GT2 " ST " " RK " " R-REC
           MOVE 6 TO RK
           START FR KEY EQUAL RK
               INVALID KEY DISPLAY "EQ6 INVALID"
           END-START
           READ FR NEXT AT END DISPLAY "EQ6 AT END" END-READ
           DISPLAY "EQ6 " ST " " RK " " R-REC
           CLOSE FR
           STOP RUN.
