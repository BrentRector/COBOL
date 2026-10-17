      *>--- conformance-case
      *> id: pb370_start_relative_reverse
      *> rules: [GR-14.9.41.4-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.41.4 GR9 (RELATIVE FILES) - the START comparison is
      *> NUMERIC ("Numeric comparison rules apply") and its DIRECTION
      *> follows the relational operator:
      *>   a) "If the relational operator is EQUAL, GREATER, NOT LESS,
      *>      or GREATER OR EQUAL, the file position indicator is set
      *>      to the relative record number of the first logical record
      *>      in the file whose key satisfies the comparison searching
      *>      the file sequentially."
      *>   b) "If the relational operator is LESS, NOT GREATER, or LESS
      *>      OR EQUAL, the file position indicator is set to the
      *>      relative record number of the first logical record in the
      *>      file whose key satisfies the comparison searching the file
      *>      in reverse order."
      *>   c) "If the comparison is not satisfied by any record in the
      *>      file, the invalid key condition exists and the execution
      *>      of the START statement is unsuccessful."
      *> The reverse-search operators are a COBOL-2002 introduction
      *> (start-reverse-operators-2002; §A.3 item 39 binds them to READ
      *> PREVIOUS), so this golden runs at 2002; the negative
      *> pb370-start-reverse-operator-85 pins the gate below it.
      *> FR holds records at relative record numbers 2, 4 and 6 only.
      *> Each START is followed by READ NEXT, which (§14.9.30.4 GR21 b)
      *> and GR25) makes the selected record available and moves its
      *> number into RK, so the file position indicator prints outright.
      *>   LT5  KEY < 5: searching in REVERSE, the first record whose
      *>        number is < 5 is 4 - NOT 2, the lowest such number,
      *>        which is what a forward search would find. This is the
      *>        leg that tells b) from a).
      *>   NG5  KEY NOT GREATER 5: the same reverse search, 4.
      *>   LE4  KEY LESS OR EQUAL 4: reverse, and 4 itself satisfies <=.
      *>   GT4  KEY > 4: forward (a), the first number above 4 is 6.
      *>   NL5  KEY NOT LESS 5: forward, 6.
      *>   EQ3  KEY = 3: no record 3, so c): INVALID KEY and '23' (the
      *>        one §9.1.13.5 invalid-key value a START can produce).
      *>   LT2  KEY < 2: no record numbered below 2 - c) again.
      *>   NUM  RK is COMP: KEY > RK with RK = 4 compares the NUMBER 4,
      *>        never a character image - 6 again.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370SR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "pb370sr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST.
           SELECT FB ASSIGN TO "pb370sb.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS BK
               FILE STATUS IS SB.
       DATA DIVISION.
       FILE SECTION.
       FD FR.
       01 R-REC PIC X(5).
       FD FB.
       01 B-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 BK PIC 9(4) COMP.
       01 ST PIC XX.
       01 SB PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FR
           MOVE 2 TO RK MOVE "REC-2" TO R-REC WRITE R-REC
           MOVE 4 TO RK MOVE "REC-4" TO R-REC WRITE R-REC
           MOVE 6 TO RK MOVE "REC-6" TO R-REC WRITE R-REC
           CLOSE FR
           OPEN OUTPUT FB
           MOVE 2 TO BK MOVE "BIN-2" TO B-REC WRITE B-REC
           MOVE 4 TO BK MOVE "BIN-4" TO B-REC WRITE B-REC
           MOVE 6 TO BK MOVE "BIN-6" TO B-REC WRITE B-REC
           CLOSE FB
           OPEN INPUT FR
           MOVE 5 TO RK
           START FR KEY < RK
               INVALID KEY DISPLAY "LT5 INVALID"
           END-START
           READ FR NEXT AT END DISPLAY "LT5 AT END" END-READ
           DISPLAY "LT5 " ST " " RK " " R-REC
           MOVE 5 TO RK
           START FR KEY IS NOT GREATER THAN RK
               INVALID KEY DISPLAY "NG5 INVALID"
           END-START
           READ FR NEXT AT END DISPLAY "NG5 AT END" END-READ
           DISPLAY "NG5 " ST " " RK " " R-REC
           MOVE 4 TO RK
           START FR KEY IS LESS THAN OR EQUAL TO RK
               INVALID KEY DISPLAY "LE4 INVALID"
           END-START
           READ FR NEXT AT END DISPLAY "LE4 AT END" END-READ
           DISPLAY "LE4 " ST " " RK " " R-REC
           MOVE 4 TO RK
           START FR KEY > RK
               INVALID KEY DISPLAY "GT4 INVALID"
           END-START
           READ FR NEXT AT END DISPLAY "GT4 AT END" END-READ
           DISPLAY "GT4 " ST " " RK " " R-REC
           MOVE 5 TO RK
           START FR KEY IS NOT LESS THAN RK
               INVALID KEY DISPLAY "NL5 INVALID"
           END-START
           READ FR NEXT AT END DISPLAY "NL5 AT END" END-READ
           DISPLAY "NL5 " ST " " RK " " R-REC
           MOVE 3 TO RK
           START FR KEY = RK
               INVALID KEY DISPLAY "EQ3 INVALID"
               NOT INVALID KEY DISPLAY "EQ3 FOUND"
           END-START
           DISPLAY "EQ3 " ST
           MOVE 2 TO RK
           START FR KEY < RK
               INVALID KEY DISPLAY "LT2 INVALID"
               NOT INVALID KEY DISPLAY "LT2 FOUND"
           END-START
           DISPLAY "LT2 " ST
           CLOSE FR
           OPEN INPUT FB
           MOVE 4 TO BK
           START FB KEY > BK
               INVALID KEY DISPLAY "NUM INVALID"
           END-START
           READ FB NEXT AT END DISPLAY "NUM AT END" END-READ
           DISPLAY "NUM " SB " " BK " " B-REC
           CLOSE FB
           STOP RUN.
