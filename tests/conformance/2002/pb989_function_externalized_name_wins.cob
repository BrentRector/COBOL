      *> kb/Work PB989 - ISO 12.3.8.4 GR11 a), keyed by EXTERNALIZED name, with determination D-R3
      *> (docs/rearchitecture/DESIGN-external-repository.md 8.3): `FUNCTION PB989F` activates the function externalized
      *> "PB989F" even though a DIFFERENT function carries the user-function-name PB989F.
      *>
      *> DERIVATION. Two function definitions precede the caller: PB989F AS "PB989G" (user-function-name PB989F,
      *> externalized "PB989G") and PB989FX AS "PB989F" (user-function-name PB989FX, externalized "PB989F").
      *> GR11 NOTE 2: a specifier with no AS phrase has the function-prototype-name itself as its externalized name, so
      *> `FUNCTION PB989F` asks GR11 a) for a previous definition externalized "PB989F": PB989FX is it - PB989F + 200 = 201.
      *> 8.4.6.7 independently lets the word PB989F name PB989F AS "PB989G"; the standard does not rank the two clauses,
      *> and the externalized name wins (GnuCOBOL 3.2.0 resolves by externalized name only), with warning COBOLNET2969.
      *> `FUNCTION PB989H AS "PB989G"` is GR11 a) with literal-5 "PB989G": the definition PB989F AS "PB989G" - 1 + 100 = 101.
      *> Expected: F=0201 then H=0101.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB989F AS "PB989G".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           COMPUTE L-R = L-X + 100.
           GOBACK.
       END FUNCTION PB989F.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB989FX AS "PB989F".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           COMPUTE L-R = L-X + 200.
           GOBACK.
       END FUNCTION PB989FX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB989M2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB989F
           FUNCTION PB989H AS "PB989G".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 1.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION PB989F(WS-N).
           DISPLAY "F=" WS-R.
           COMPUTE WS-R = FUNCTION PB989H(WS-N).
           DISPLAY "H=" WS-R.
           STOP RUN.
       END PROGRAM PB989M2.
