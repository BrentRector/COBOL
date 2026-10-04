      *> kb/Work PB1937 - a VARIABLE-LENGTH GROUP crossing a Format-2 CALL boundary into a
      *> formal whose DYNAMIC LENGTH LIMIT is SMALLER than the argument's content.
      *>
      *> 14.2.3 GR8: "the activated runtime element operates as if the formal parameter occupies
      *> the same storage area as the argument", so the formal's dynamic-length member holds the
      *> argument's WHOLE content - it must not be shortened to the formal's LIMIT on the way in,
      *> because the copy-back on return would then write the shortened value over the caller's
      *> (silent data loss). 14.6.13.2 rule 5 is what the smaller LIMIT means for the callee: "When
      *> the internal format of a dynamic-length elementary item is not correctly formed or does not
      *> agree with the corresponding DYNAMIC LENGTH clause an EC-DATA-INCOMPATIBLE exception
      *> condition is set to exist" - raised by a REFERENCE to the member (kb/Work PB1118), never
      *> by the crossing itself.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   B1 checked callee, D holds 8 characters, LIMIT 3: MOVE D OF LG TO W3 sets the fatal
      *>      condition, the declarative runs and RESUME AT NEXT STATEMENT leaves W3 [...].
      *>   A  the caller's D still holds all 8 characters afterwards: [HabcdefghF] 8.
      *>   C  an UNCHECKED callee (14.6.13.1.1: the condition does not exist) reads the group whole,
      *>      [HabcdefghF], and the caller's D is again intact.
      *>   B2 D now holds 2 characters, which agree with LIMIT 3: no condition, W3 [ab ].
      *>   E  the caller's group is [HabF] with D of length 2.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1937A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 H PIC X VALUE "H".
          05 D PIC X DYNAMIC LENGTH LIMIT 10.
          05 F PIC X VALUE "F".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abcdefgh" TO D OF G
           CALL "PB1937B" AS NESTED USING G
           DISPLAY "A [" G "] " FUNCTION LENGTH(D OF G)
           CALL "PB1937C" AS NESTED USING G
           DISPLAY "C [" G "] " FUNCTION LENGTH(D OF G)
           MOVE "ab" TO D OF G
           CALL "PB1937B" AS NESTED USING G
           DISPLAY "E [" G "] " FUNCTION LENGTH(D OF G)
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1937B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W3 PIC X(3).
       LINKAGE SECTION.
       01 LG.
          05 H PIC X.
          05 D PIC X DYNAMIC LENGTH LIMIT 3.
          05 F PIC X.
       PROCEDURE DIVISION USING LG.
       DECLARATIVES.
       DI SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       DI-1.
           DISPLAY "HANDLED " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-B.
           MOVE "..." TO W3
           MOVE D OF LG TO W3
           DISPLAY "W3 [" W3 "]"
           GOBACK.
       END PROGRAM PB1937B.

       >>TURN EC-DATA-INCOMPATIBLE CHECKING OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1937C.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 H PIC X.
          05 D PIC X DYNAMIC LENGTH LIMIT 3.
          05 F PIC X.
       PROCEDURE DIVISION USING LG.
       MAIN-C.
           DISPLAY "LG [" LG "]"
           GOBACK.
       END PROGRAM PB1937C.
       END PROGRAM PB1937A.
