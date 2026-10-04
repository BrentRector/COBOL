      *> kb/Work PB1216 - pointer lifetime for RECURSIVE (static WS, per-
      *> activation LS) and INITIAL programs. Rules: §13.18.5.4 4),
      *> §8.6.5, §8.6.4 as in pb1216_ls_and_cancel_pointer_lifetime;
      *> §8.6.4 also: "An initial item persists while the program is in
      *> active state".  OK  §8.6.4
      *> DERIVATION. RWS-LIVE [WWWW]: a RECURSIVE program's WORKING-
      *>   STORAGE is one static copy, alive until CANCEL. RWS-CANCELLED:
      *>   HANDLED, [....] (storage ended at CANCEL). RWS-AGAIN [WWWW]: a
      *>   pointer taken after the re-activation names the new life.
      *>   INIT-RETURNED: an INITIAL program's item ended at its return:
      *>   HANDLED, [....]. REC-LS-RETURNED: each RECURSIVE activation's
      *>   LOCAL-STORAGE ends at ITS OWN return: the inner activation's
      *>   pointer is dead in the outer (INNER-LS-DEAD [....]), the
      *>   outer's own stays live (OUTER-LS-LIVE [RLRL]), and the pointer
      *>   the outer returned is dead in the main program.
       >>TURN EC-BOUND-PTR CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1216B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 W4 PIC X(4) VALUE "....".
       01 DEP2 PIC 9 VALUE 2.
       01 B PIC X(4) BASED.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BP SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-PTR.
       D-BP-P.
           DISPLAY "HANDLED " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           CALL "PB1216RW" RETURNING P
           SET ADDRESS OF B TO P
           MOVE "...." TO W4
           MOVE B TO W4
           DISPLAY "RWS-LIVE [" W4 "]"
           CANCEL "PB1216RW"
           MOVE "...." TO W4
           MOVE B TO W4
           DISPLAY "RWS-CANCELLED [" W4 "]"
           CALL "PB1216RW" RETURNING P
           SET ADDRESS OF B TO P
           MOVE "...." TO W4
           MOVE B TO W4
           DISPLAY "RWS-AGAIN [" W4 "]"
           CALL "PB1216IN" RETURNING P
           SET ADDRESS OF B TO P
           MOVE "...." TO W4
           MOVE B TO W4
           DISPLAY "INIT-RETURNED [" W4 "]"
           CALL "PB1216RL" USING BY CONTENT DEP2 RETURNING P
           SET ADDRESS OF B TO P
           MOVE "...." TO W4
           MOVE B TO W4
           DISPLAY "REC-LS-RETURNED [" W4 "]"
           STOP RUN.
       END PROGRAM PB1216B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1216RW RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WV PIC X(4) VALUE "WWWW".
       LINKAGE SECTION.
       01 RP USAGE POINTER.
       PROCEDURE DIVISION RETURNING RP.
           SET RP TO ADDRESS OF WV
           GOBACK.
       END PROGRAM PB1216RW.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1216IN INITIAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IV PIC X(4) VALUE "IIII".
       LINKAGE SECTION.
       01 RP USAGE POINTER.
       PROCEDURE DIVISION RETURNING RP.
           SET RP TO ADDRESS OF IV
           GOBACK.
       END PROGRAM PB1216IN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1216RL RECURSIVE.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LV PIC X(4) VALUE "RLRL".
       01 IP USAGE POINTER.
       01 IB PIC X(4) BASED.
       01 W PIC X(4) VALUE "....".
       01 DEP1 PIC 9 VALUE 1.
       01 OUTER-P USAGE POINTER.
       LINKAGE SECTION.
       01 DEPTH PIC 9.
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING DEPTH RETURNING RP.
       DECLARATIVES.
       D-BP SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-PTR.
       D-BP-P.
           DISPLAY "HANDLED " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           SET IP TO ADDRESS OF LV
           IF DEPTH > 1
               CALL "PB1216RL" USING BY CONTENT DEP1 RETURNING OUTER-P
               SET ADDRESS OF IB TO OUTER-P
               MOVE "...." TO W
               MOVE IB TO W
               DISPLAY "INNER-LS-DEAD [" W "]"
               SET ADDRESS OF IB TO IP
               MOVE "...." TO W
               MOVE IB TO W
               DISPLAY "OUTER-LS-LIVE [" W "]"
           END-IF
           SET RP TO IP
           GOBACK.
       END PROGRAM PB1216RL.
