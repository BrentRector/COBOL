      *>--- conformance-case
      *> id: l1c10_function_id_prototype_as_and_is
      *> rules: [FMT-11.5.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.5.2 Format 2 — prototype WITH AS literal-1, IS optional
      *>
      *> Row pinned: FMT-11.5.2 (the Format 2 arms no other golden
      *> covers: "[ AS literal-1 ]" present, and "IS" omitted).
      *>  Format 2 (prototype): "FUNCTION-ID. function-prototype-
      *>    name-1 [ AS literal-1 ] IS PROTOTYPE."  AS and PROTOTYPE
      *>    are underlined; IS is not, so by §5.2.3 it is an optional
      *>    word ("shown in uppercase and not underlined in general
      *>    formats") and may be omitted.
      *>  Format 1 (definition): "FUNCTION-ID. user-function-name-1
      *>    [ AS literal-1 ] ." (with AS, as the definition of L1C10SQ).
      *>  §11.5.4 GR2: literal-1 "is the name of the function
      *>    prototype that is externalized to the operating
      *>    environment". §12.3.8.4 GR11 b): a REPOSITORY function
      *>    specifier whose externalized name (its AS literal-5)
      *>    matches a prototype definition's takes the details from
      *>    that prototype; the function activated is the one with
      *>    the same externalized name - here the definition
      *>    FUNCTION-ID. L1C10SQ AS "L1C10SQX" that FOLLOWS the
      *>    caller. L1C10CU uses the prototype spelling without IS
      *>    and without AS (externalized name = the name itself).
      *> cite.py --check:
      *>  OK  §11.5.2   (General format)  Format 2 (prototype)
      *>  OK  §11.5.2   (General format)  Format 1 (definition)
      *>  OK  §5.2.3   (Optional words)
      *>  OK  §11.5.4 2)  (General rules)
      *>  OK  §12.3.8.4 11) b)  (General rules)
      *> Derivation: both prototypes are legal Format 2 source, so
      *> the group compiles; each reference activates its definition:
      *>  SQ=000049   L1C10SQ(7) = 7 * 7
      *>  CU=000343   L1C10CU(7) = 7 * 7 * 7
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1C10SQ AS "L1C10SQX" IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION L1C10SQ.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1C10CU PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION L1C10CU.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C10F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION L1C10SQ AS "L1C10SQX"
           FUNCTION L1C10CU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 0007.
       01 WS-R PIC 9(6).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE WS-R = FUNCTION L1C10SQ(WS-N).
           DISPLAY "SQ=" WS-R.
           COMPUTE WS-R = FUNCTION L1C10CU(WS-N).
           DISPLAY "CU=" WS-R.
           STOP RUN.
       END PROGRAM L1C10F.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1C10SQ AS "L1C10SQX".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       SQ-P.
           COMPUTE L-R = L-X * L-X.
           GOBACK.
       END FUNCTION L1C10SQ.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1C10CU.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       CU-P.
           COMPUTE L-R = L-X * L-X * L-X.
           GOBACK.
       END FUNCTION L1C10CU.
