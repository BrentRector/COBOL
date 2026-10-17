*>--- conformance-case
*> id: pb234_recursive_address_of_static
*> rules: [GR-14.9.5.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB234 - ADDRESS OF a static WORKING-STORAGE item of a
      *> RECURSIVE program. Before PB234 the program was refused outright
      *> (COBOLNET0899); nothing in 8.4.3.11 or 14.9.5.4 forbids it.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE RULES:
      *>  - 13.5.4 GR1: the working-storage of a program without the initial
      *>    attribute is STATIC data - one copy shared by every activation.
      *>    The first CALL writes "WXYZ" through a BASED view of ADDRESS OF W
      *>    (8.4.3.11.4 GR1 / 14.9.39.4 GR13), so the direct DISPLAY sees
      *>    W=WXYZ, and the nested (recursive) activation's own ADDRESS OF W
      *>    names the SAME copy: INNER=WXYZ, and the two data-pointers are
      *>    equal (8.8.4.2): SAME.
      *>  - The second CALL without CANCEL finds the last-used state
      *>    (14.6.2.3.3): ENTRY=WXYZ D=2.
      *>  - 14.9.5.4 GR3: after CANCEL the next CALL finds the INITIAL state -
      *>    14.6.2.3.2 action 2 re-seeds W from its VALUE: ENTRY=ABCD D=1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB234MN.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB234RC"
           CALL "PB234RC"
           CANCEL "PB234RC"
           CALL "PB234RC"
           STOP RUN.
       END PROGRAM PB234MN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB234RC RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABCD".
       01 D PIC 9 VALUE 0.
       01 R PIC 9 VALUE 0.
       01 P USAGE POINTER.
       01 B PIC X(4) BASED.
       LOCAL-STORAGE SECTION.
       01 Q USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           IF R = 1
               MOVE 0 TO R
               SET Q TO ADDRESS OF W
               SET ADDRESS OF B TO Q
               DISPLAY "INNER=" B
               IF Q = P
                   DISPLAY "SAME"
               ELSE
                   DISPLAY "DIFFERENT"
               END-IF
               GOBACK
           END-IF
           ADD 1 TO D
           DISPLAY "ENTRY=" W " D=" D
           SET P TO ADDRESS OF W
           SET ADDRESS OF B TO P
           MOVE "WXYZ" TO B
           DISPLAY "W=" W
           MOVE 1 TO R
           CALL "PB234RC"
           GOBACK.
       END PROGRAM PB234RC.
