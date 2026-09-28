      *> kb/Work PB1132 - a program that is neither INITIAL nor RECURSIVE
      *> keeps ONE instance across CALLs, but its LOCAL-STORAGE is
      *> automatic data: §8.6.4 "Their storage is allocated and set to
      *> initial state each time the runtime element containing them is
      *> activated", and §14.6.2.3.2 action 5 "The address of each based
      *> item is set to null". So every CALL of W67QLSS sees LB NULL, and
      *> LA (whose ADDRESS OF is taken) back at its VALUE "INIT" - never
      *> the previous activation's address or "MUT!". The linkage-section
      *> based entry KB is not a formal: §8.6.5 ends its association "at
      *> the end of the execution of the runtime element", so each CALL
      *> starts with no association (NULL). The WORKING-STORAGE based entry
      *> WB is STATIC data (§8.6.4) and keeps its last-used address: NULL
      *> on the first CALL, held on the second.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W67QLSM.
       PROCEDURE DIVISION.
           CALL "W67QLSS"
           CALL "W67QLSS"
           STOP RUN.
       END PROGRAM W67QLSM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. W67QLSS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WB PIC X(4) BASED.
       01 WT PIC X(4) VALUE "WWWW".
       LOCAL-STORAGE SECTION.
       01 LB PIC X(4) BASED.
       01 LA PIC X(4) VALUE "INIT".
       01 LP USAGE POINTER.
       LINKAGE SECTION.
       01 KB PIC X(4) BASED.
       PROCEDURE DIVISION.
           IF ADDRESS OF LB = NULL
               DISPLAY "LB NULL"
           ELSE
               DISPLAY "LB HELD " LB
           END-IF
           IF ADDRESS OF KB = NULL
               DISPLAY "KB NULL"
           ELSE
               DISPLAY "KB HELD"
           END-IF
           IF ADDRESS OF WB = NULL
               DISPLAY "WB NULL"
           ELSE
               DISPLAY "WB HELD " WB
           END-IF
           DISPLAY "LA=" LA
           SET LP TO ADDRESS OF LA
           MOVE "MUT!" TO LA
           SET ADDRESS OF LB TO ADDRESS OF WT
           SET ADDRESS OF WB TO ADDRESS OF WT
           ALLOCATE KB
           GOBACK.
       END PROGRAM W67QLSS.
