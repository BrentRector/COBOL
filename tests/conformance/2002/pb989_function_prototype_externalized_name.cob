      *> kb/Work PB989 - ISO 12.3.8.4 GR11 + 11.5.4 GR2: a function PROTOTYPE named by a REPOSITORY entry activates the
      *> function whose EXTERNALIZED name is the prototype's literal-1, and a function DEFINITION that merely shares the
      *> prototype's WORD (and follows the caller) is not that function.
      *>
      *> DERIVATION. PB989W is a function prototype AS "PB989X" (11.5.4 GR2: literal-1 "is the name of the function
      *> prototype that is externalized to the operating environment"). The REPOSITORY entry `FUNCTION PB989W` names it
      *> (12.3.8.3 SR10, first alternative: "the name of a function prototype specified in this compilation group").
      *> GR11 a) takes the details from a function DEFINITION "specified previously in the same compilation group" whose
      *> externalized name is the prototype's: PB989X (below the prototype, above the caller) is one, and PB989W (the
      *> definition that FOLLOWS the caller, externalized "PB989W") is not previous and is not "PB989X" either. So the
      *> call activates PB989X: 7 * 3 = 21. The old order-blind table keyed prototype and definition by WORD, replaced the
      *> prototype with the following definition PB989W, and activated PB989W (7 * 5 = 35).
      *> The second entry names the externalized name directly: `FUNCTION PB989V AS "PB989X"` is GR11 a) with literal-5
      *> "PB989X" (NOTE 2), the same function: 21.
      *> Expected: W=0021 then V=0021.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB989W AS "PB989X" IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION PB989W.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB989X.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           COMPUTE L-R = L-X * 3.
           GOBACK.
       END FUNCTION PB989X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB989M1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB989W
           FUNCTION PB989V AS "PB989X".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 7.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION PB989W(WS-N).
           DISPLAY "W=" WS-R.
           COMPUTE WS-R = FUNCTION PB989V(WS-N).
           DISPLAY "V=" WS-R.
           STOP RUN.
       END PROGRAM PB989M1.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB989W.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           COMPUTE L-R = L-X * 5.
           GOBACK.
       END FUNCTION PB989W.
