      *> reject-at: 85
      *> kb/Work PB1132 - the edition floor of the positive golden
      *> 2002/w67q_pb1132_ls_activation_reseed: a LOCAL-STORAGE SECTION
      *> (§13.6, automatic data re-initialized on every activation,
      *> §8.6.4) holding a BASED entry (§13.18.5, whose implicit address
      *> §14.6.2.3.2 action 5 sets to null) is COBOL-2002 source; the 85
      *> compiler refuses both the section and the clause (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W67QN85S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WT PIC X(4) VALUE "WWWW".
       LOCAL-STORAGE SECTION.
       01 LB PIC X(4) BASED.
       PROCEDURE DIVISION.
           SET ADDRESS OF LB TO ADDRESS OF WT
           GOBACK.
       END PROGRAM W67QN85S.
