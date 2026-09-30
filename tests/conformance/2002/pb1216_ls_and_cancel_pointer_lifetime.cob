      *> kb/Work PB1216 - a data pointer into storage whose life has
      *> ENDED is not a valid address: a LOCAL-STORAGE item after its
      *> activation returns, a WORKING-STORAGE item after CANCEL.
      *> §13.18.5.4 GR4: "If the subject of the entry is referenced while
      *>   its address is not NULL and not a valid address of storage,
      *>   the EC-BOUND-PTR exception condition is set to exist."
      *>   OK  §13.18.5.4 4)  (General rules)
      *> §8.6.5: "The association may cease to exist because the actual
      *>   data no longer exists, as specified 8.6.4"  OK  §8.6.5
      *> §8.6.4: LOCAL-STORAGE "persists while that instance of the
      *>   runtime element is in active state"; a static item persists
      *>   to "the execution of a CANCEL statement of a program that
      *>   directly or indirectly contains the items".  OK  §8.6.4
      *> §14.9.33.4 GR2: RESUME AT NEXT STATEMENT continues after the
      *>   interrupted statement (the MOVE B TO W4), so W4 keeps "....".
      *> DERIVATION. LS [....]: SUBLS returned ADDRESS OF its LOCAL-
      *>   STORAGE item; its activation ended, so MOVE B raises (HANDLED)
      *>   and W4 is unchanged. WS-LIVE [WWWW]: SUBWS is not cancelled,
      *>   its WORKING-STORAGE persists: no raise. WS-CANCELLED [....]:
      *>   after CANCEL the old storage is gone: HANDLED, unchanged.
      *>   OWN [OWNS]: a pointer to the caller's own WORKING-STORAGE is
      *>   valid throughout.
       >>TURN EC-BOUND-PTR CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1216A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 W4 PIC X(4) VALUE "....".
       01 OWN PIC X(4) VALUE "OWNS".
       01 OWNP USAGE POINTER.
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
           SET OWNP TO ADDRESS OF OWN
           CALL "PB1216LS" RETURNING P
           SET ADDRESS OF B TO P
           MOVE B TO W4
           DISPLAY "LS [" W4 "]"
           MOVE "...." TO W4
           CALL "PB1216WS" RETURNING P
           SET ADDRESS OF B TO P
           MOVE B TO W4
           DISPLAY "WS-LIVE [" W4 "]"
           CANCEL "PB1216WS"
           MOVE "...." TO W4
           MOVE B TO W4
           DISPLAY "WS-CANCELLED [" W4 "]"
           SET ADDRESS OF B TO OWNP
           MOVE B TO W4
           DISPLAY "OWN [" W4 "]"
           STOP RUN.
       END PROGRAM PB1216A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1216LS.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 L PIC X(4) VALUE "LLLL".
       LINKAGE SECTION.
       01 RP USAGE POINTER.
       PROCEDURE DIVISION RETURNING RP.
           SET RP TO ADDRESS OF L
           GOBACK.
       END PROGRAM PB1216LS.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1216WS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WV PIC X(4) VALUE "WWWW".
       LINKAGE SECTION.
       01 RP USAGE POINTER.
       PROCEDURE DIVISION RETURNING RP.
           SET RP TO ADDRESS OF WV
           GOBACK.
       END PROGRAM PB1216WS.
