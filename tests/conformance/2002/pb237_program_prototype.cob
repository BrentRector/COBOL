      *> ISO/IEC 1989:2023 §12.3.8.2 program-specifier + §14.9.4 CALL Format 2 + §14.9.5 CANCEL
      *> ------------------------------------------------------------------------------------
      *> §12.3.8.2 declares a program-prototype-name with `PROGRAM program-prototype-name-1
      *> [AS literal-3]` (PDF page 334: PROGRAM and AS underlined, the AS phrase bracketed).
      *> §12.3.8.4 GR10 NOTE 1: "Literal-3, if specified, is the externalized name of the
      *> program prototype; otherwise, the externalized name is program-prototype-name-1."
      *> Both spellings of §14.9.4.2 Format 2 are exercised: the rendered general format is
      *>     CALL [ { identifier-1 | literal-1 } AS ] { NESTED | program-prototype-name-1 }
      *> so the target-plus-AS bracket may be omitted whole (GR3 b), third bullet).
      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237ADD IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(4).
       PROCEDURE DIVISION USING LK-N.
       END PROGRAM PB237ADD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237MUL IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE LK-V RETURNING LK-R.
       END PROGRAM PB237MUL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237PT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB237ADD.
           PROGRAM SCALE-IT AS "PB237MUL".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9(4) VALUE 0.
       01 WS-OUT PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
      *> §14.9.4.4 GR3 b) third bullet - no identifier-1 and no literal-1, so the prototype
      *> determines the externalized program-name. §14.6.2.3.2: first activation, initial state.
           CALL PB237ADD USING WS-N
           DISPLAY "CALL-1 =" WS-N
      *> literal-1 + AS phrase: GR3 b) FIRST bullet names the program, GR7 makes the prototype
      *> determine its characteristics. §14.6.2: a non-INITIAL program keeps its last-used state.
           CALL "PB237ADD" AS PB237ADD USING WS-N
           DISPLAY "CALL-2 =" WS-N
      *> literal-3 is the externalized name, so SCALE-IT activates PB237MUL. Its formal is
      *> BY VALUE, so GR9 b) passes the keyword-less argument BY VALUE and §14.2.3 GR10 leaves
      *> WS-N untouched; §14.9.4.3 SR25 -> §14.8.3 checks the RETURNING pair at bind.
           CALL SCALE-IT USING WS-N RETURNING WS-OUT
           DISPLAY "SCALED =" WS-OUT
           DISPLAY "UNCHG  =" WS-N
      *> §14.9.5.2's operand list with TWO targets, the second reaching a program through its
      *> prototype (GR1 c)). §14.9.5.4 GR3: a subsequently called canceled program is in its
      *> initial state.
           CANCEL PB237ADD SCALE-IT
           CALL PB237ADD USING WS-N
           DISPLAY "CALL-3 =" WS-N
           CALL "PB237IN" AS NESTED
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237IN.
       PROCEDURE DIVISION.
       CMAIN.
      *> §8.4.6.8: "Program-prototype-names referenced within a source element shall be
      *> either the program-name of a containing program definition or a program-prototype-name
      *> declared in the REPOSITORY paragraph" - PB237PT needs no specifier here at all.
      *> §14.9.4.4 GR3 f): the containing program is in the ACTIVE state and does not have the
      *> recursive attribute, so EC-PROGRAM-RECURSIVE-CALL is set to exist, the call is not
      *> successful, and GR3 h)1 transfers control to imperative-statement-1.
           CALL PB237PT
               ON EXCEPTION DISPLAY "CONTAINER-BLOCKED"
           END-CALL
           GOBACK.
       END PROGRAM PB237IN.
       END PROGRAM PB237PT.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237ADD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-CALLS PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 LK-N PIC 9(4).
       PROCEDURE DIVISION USING LK-N.
       MAIN.
           ADD 1 TO WS-CALLS
           MOVE WS-CALLS TO LK-N
           GOBACK.
       END PROGRAM PB237ADD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237MUL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE LK-V RETURNING LK-R.
       MAIN.
           COMPUTE LK-R = LK-V * 2
           GOBACK.
       END PROGRAM PB237MUL.
