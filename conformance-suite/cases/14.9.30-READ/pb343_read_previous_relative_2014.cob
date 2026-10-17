*>--- conformance-case
*> id: pb343_read_previous_relative_2014
*> rules: [GR-14.9.30.4-21]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB343 — READ ... PREVIOUS on a file with RELATIVE organization, the 2014 leg.
      *> Until PB343 the connector applied the INDEXED block's after-OPEN carve-out here: a READ
      *> PREVIOUS immediately after OPEN INPUT returned the at end condition where the relative
      *> block's own rule b) owes the first existing record.
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.27.4 GR14 - "When the organization of the file referenced by file-name-1 is
      *>    sequential or relative and the INPUT or I-O phrase is specified in the OPEN statement,
      *>    the file position indicator for that file connector is set to 1."
      *>  14.9.30.4 GR21 "When the file is a relative file":
      *>    b) indicator established by a prior successful OPEN or START -> "the first existing
      *>       record that is selected is made available, REGARDLESS of whether NEXT or PREVIOUS is
      *>       specified"                                          (R1, S1/S2, T1, T2)
      *>    c) established by a prior successful READ -> the first existing record whose relative
      *>       key number is "greater than the file position indicator if NEXT ... or is less than
      *>       the file position indicator if PREVIOUS"             (R2, R4, R5, R6)
      *>    e) no record found -> the at end condition               (R2, E1)
      *>    f) the indicator becomes the RRN of the record made available (R6 proves it: the
      *>       forward read after two backward ones resumes from the REPOSITIONED indicator)
      *>  14.9.30.4 GR24 a)/c) - the at end condition sets '10' and transfers control to the AT END
      *>    imperative                                              (R2, E1)
      *>  14.9.30.4 GR25 - a successful sequential READ MOVEs the RRN of the record made available
      *>    into the RELATIVE KEY item, which is what the middle field of each line shows
      *>  14.9.41.4 GR9 a)/b) - START sets the indicator to "the relative record number of the first
      *>    logical record in the file whose key satisfies the comparison", searching forward for
      *>    EQUAL/GREATER/NOT LESS/GREATER OR EQUAL and IN REVERSE ORDER for LESS/NOT GREATER/LESS
      *>    OR EQUAL; c) makes an unsatisfied comparison the invalid key condition.  So a relative
      *>    file's indicator, when a START established it, always names an EXISTING record  (T1, T2)
      *>
      *> WHY THE SPARSE FILE IS HERE.  On a DENSE file rule b) cannot be told apart from a backward
      *> walk that merely includes the indicator: both answer RRN 1.  The sparse file has its lowest
      *> record at RRN 5, so OPEN's indicator of 1 names an EMPTY slot and only a direction-BLIND
      *> rule b) can make S1 (PREVIOUS) and S2 (NEXT) name the same record.
      *>
      *> EDITIONS: identical at 2002, 2014 and 2023 -- which is what the three copies of this program
      *> assert.  Annex E is INFORMATIVE (its heading is "(informative)") and its only READ-
      *> positioning change, E.2 item 22, amends the INDEXED sub-rule d.3 -- "If no such record is
      *> found or PREVIOUS is specified and the previous operation on the file was an OPEN statement,
      *> the at end condition exists" -- which is written in the indexed block alone.  The relative
      *> block prints rule b) unamended, so this organization's selection takes no edition parameter.
      *> At --std 85 the whole program is rejected: negative/pb343-read-previous-relative-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB343R4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DEN ASSIGN TO "pb343d4.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS D-K
               FILE STATUS IS D-ST.
           SELECT SPA ASSIGN TO "pb343s4.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS S-K
               FILE STATUS IS S-ST.
           SELECT EMP ASSIGN TO "pb343e4.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS E-K
               FILE STATUS IS E-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  DEN.
       01  D-REC         PIC X(4).
       FD  SPA.
       01  S-REC         PIC X(4).
       FD  EMP.
       01  E-REC         PIC X(4).
       WORKING-STORAGE SECTION.
       01  D-ST          PIC XX.
       01  S-ST          PIC XX.
       01  E-ST          PIC XX.
       01  D-K           PIC 9(4).
       01  S-K           PIC 9(4).
       01  E-K           PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT DEN
           MOVE 1 TO D-K
           MOVE "AAAA" TO D-REC
           WRITE D-REC
           MOVE 2 TO D-K
           MOVE "BBBB" TO D-REC
           WRITE D-REC
           MOVE 3 TO D-K
           MOVE "CCCC" TO D-REC
           WRITE D-REC
           CLOSE DEN
      *> Phase 1 - rule b) on its own: PREVIOUS immediately after OPEN INPUT makes RRN 1
      *> available (indicator 1 per 14.9.27.4 GR14), and the SECOND backward read is then
      *> governed by rule c) - no RRN less than 1 exists, so rule e)'s at end condition.
           OPEN INPUT DEN
           READ DEN PREVIOUS RECORD
               AT END DISPLAY "R1=ATEND|" D-ST
               NOT AT END DISPLAY "R1=" D-REC "|" D-K "|" D-ST
           END-READ
           READ DEN PREVIOUS RECORD
               AT END DISPLAY "R2=ATEND|" D-ST
               NOT AT END DISPLAY "R2=" D-REC "|" D-K "|" D-ST
           END-READ
           CLOSE DEN
      *> Phase 2 - rule c) in both directions, and rule f).
           OPEN INPUT DEN
           READ DEN NEXT RECORD
           READ DEN NEXT RECORD
           READ DEN NEXT RECORD
           DISPLAY "R3=" D-REC "|" D-K "|" D-ST
           READ DEN PREVIOUS RECORD
           DISPLAY "R4=" D-REC "|" D-K "|" D-ST
           READ DEN PREVIOUS RECORD
           DISPLAY "R5=" D-REC "|" D-K "|" D-ST
           READ DEN NEXT RECORD
           DISPLAY "R6=" D-REC "|" D-K "|" D-ST
           CLOSE DEN
      *> Phase 3 - the SPARSE file: OPEN's indicator of 1 names an EMPTY slot, so the two
      *> directions can only agree if rule b) ignores the direction, which is what
      *> "regardless of whether NEXT or PREVIOUS is specified" says.
           OPEN OUTPUT SPA
           MOVE 5 TO S-K
           MOVE "EEEE" TO S-REC
           WRITE S-REC
           MOVE 6 TO S-K
           MOVE "FFFF" TO S-REC
           WRITE S-REC
           CLOSE SPA
           OPEN INPUT SPA
           READ SPA PREVIOUS RECORD
               AT END DISPLAY "S1=ATEND|" S-ST
               NOT AT END DISPLAY "S1=" S-REC "|" S-K "|" S-ST
           END-READ
           CLOSE SPA
           OPEN INPUT SPA
           READ SPA NEXT RECORD
               AT END DISPLAY "S2=ATEND|" S-ST
               NOT AT END DISPLAY "S2=" S-REC "|" S-K "|" S-ST
           END-READ
           CLOSE SPA
      *> Phase 4 - rule b)'s START leg, forward and REVERSE START.  T2's START searches in
      *> reverse (14.9.41.4 GR9 b) and lands on RRN 5; rule b) then makes RRN 5 itself
      *> available to a FORWARD read, which is the same "regardless" from the other side.
           OPEN INPUT SPA
           MOVE 6 TO S-K
           START SPA KEY IS EQUAL TO S-K
               INVALID KEY DISPLAY "T1=BADSTART"
           END-START
           READ SPA PREVIOUS RECORD
               AT END DISPLAY "T1=ATEND|" S-ST
               NOT AT END DISPLAY "T1=" S-REC "|" S-K "|" S-ST
           END-READ
           CLOSE SPA
           OPEN INPUT SPA
           MOVE 6 TO S-K
           START SPA KEY IS LESS THAN S-K
               INVALID KEY DISPLAY "T2=BADSTART"
           END-START
           READ SPA NEXT RECORD
               AT END DISPLAY "T2=ATEND|" S-ST
               NOT AT END DISPLAY "T2=" S-REC "|" S-K "|" S-ST
           END-READ
           CLOSE SPA
      *> Phase 5 - rule e) on an EMPTY file, backward: rule b) selects the first existing
      *> record and there is none, so this is the at end condition, NOT a positioning failure.
           OPEN OUTPUT EMP
           CLOSE EMP
           OPEN INPUT EMP
           READ EMP PREVIOUS RECORD
               AT END DISPLAY "E1=ATEND|" E-ST
               NOT AT END DISPLAY "E1=" E-REC "|" E-K "|" E-ST
           END-READ
           CLOSE EMP
           STOP RUN.
