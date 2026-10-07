      *> ISO §12.3.8.3 SR15 — a program-specifier naming its own or a
      *> containing program definition is ignored
      *> RULE §12.3.8.3 SR15: "If the specified program-prototype-name-1
      *>   is the name of the program definition in which this
      *>   REPOSITORY paragraph is specified or the name of a containing
      *>   program definition, references to program-prototype-name-1
      *>   are to the named program definition and this
      *>   program-specifier is ignored."
      *>   cite.py --check 12.3.8.3 "If the specified program-prototype-
      *>   name-1 is the name of the program definition in which this
      *>   REPOSITORY paragraph is specified or the name of a containing
      *>   program definition" -> OK §12.3.8.3 15)
      *>   cite.py --check 12.3.8.4 "Program-prototype-name-1 is the
      *>   name of a program prototype that may be used throughout the
      *>   scope of the containing environment division"
      *>     -> OK §12.3.8.4 10)
      *> SET-UP: L1C24R (RECURSIVE, outermost) writes PROGRAM L1C24R AS
      *>   "L1C24T" and, as a control, PROGRAM L1C24U AS "L1C24T".
      *>   L1C24T is a real program printing OTHER-T. L1C24S is directly
      *>   contained in L1C24R and inherits its REPOSITORY (GR10 scope).
      *>   L1C24V counts activations of L1C24R in its WORKING-STORAGE
      *>   and returns the count into L1C24R's LOCAL-STORAGE MY-DEPTH.
      *>   (L1C24R itself declares no WORKING-STORAGE: a RECURSIVE
      *>   program with contained programs AND working-storage is a
      *>   documented recognized-not-implemented shape, COBOLNET0899
      *>   recursive-contained-working-storage, unrelated to SR15.)
      *> EXPECTED OUTPUT, DERIVED:
      *>   SELF-R DEPTH=1  the run unit starts in L1C24R.
      *>   OTHER-T         control: L1C24U is not self-named, so CALL
      *>                   L1C24U reaches the program known as L1C24T.
      *>   SELF-R DEPTH=2  CALL L1C24R inside L1C24R: the self-named
      *>                   specifier is ignored (SR15 own-definition
      *>                   arm);
      *>                   L1C24R recurses (RECURSIVE) - not OTHER-T.
      *>   SELF-R DEPTH=3  CALL "L1C24S" enters the contained program,
      *>                   whose CALL L1C24R names a CONTAINING program
      *>                   definition (SR15 containing arm): L1C24R
      *>                   again.
      *>   END-R           after the calls return to depth 1.
      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24T IS PROTOTYPE.
       PROCEDURE DIVISION.
       END PROGRAM L1C24T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24R RECURSIVE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM L1C24R AS "L1C24T"
           PROGRAM L1C24U AS "L1C24T".
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 MY-DEPTH PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "L1C24V" USING MY-DEPTH.
           DISPLAY "SELF-R DEPTH=" MY-DEPTH.
           IF MY-DEPTH = 1
               CALL L1C24U
               CALL L1C24R
               CALL "L1C24S"
               DISPLAY "END-R"
           END-IF.
           GOBACK.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24S.
       PROCEDURE DIVISION.
       S-P.
           CALL L1C24R.
           GOBACK.
       END PROGRAM L1C24S.
       END PROGRAM L1C24R.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24T.
       PROCEDURE DIVISION.
       T-P.
           DISPLAY "OTHER-T".
           GOBACK.
       END PROGRAM L1C24T.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ACTIVATIONS PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 LK-DEPTH PIC 9.
       PROCEDURE DIVISION USING LK-DEPTH.
       V-P.
           ADD 1 TO ACTIVATIONS.
           MOVE ACTIVATIONS TO LK-DEPTH.
           GOBACK.
       END PROGRAM L1C24V.
