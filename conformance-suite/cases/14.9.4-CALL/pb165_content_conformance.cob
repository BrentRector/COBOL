*>--- conformance-case
*> id: pb165_content_conformance
*> rules: [SR-14.9.4.3-25, GR-14.9.4.4-3, GR-14.9.4.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB165 - ISO 14.8.2.3.3, ELEMENTARY ITEMS PASSED BY CONTENT OR BY VALUE, reached from a
      *> Format-2 CALL through 14.9.4.3 SR25 ("the rules for conformance specified in 14.8.2, Parameters and
      *> 14.8.3, Returning items"). Rule 2 is the regime a NESTED call runs in, and it selects the rule by the
      *> FORMAL's shape: 2a COMPUTE for a numeric formal, 2b SET for an index item, 2c ANY LENGTH, 2d MOVE
      *> otherwise. Before PB165 the CALL lane had NO by-content screen at all and CobolArgAdapt's converting
      *> views silently adapted whatever arrived - so this golden pins the CONFORMING crossings, i.e. that the
      *> new screen admits every pairing the rule admits (its refusals are pb165-content-nonconforming).
      *>   N1  rule 2a - numeric argument into a numeric formal: COMPUTE rules, so a 4-digit COMP argument
      *>       reaches a 6-digit DISPLAY formal as 000042 (14.2.3 GR9's second regime allocates the record
      *>       with the FORMAL's description and COMPUTEs the argument into it).
      *>   A1  rule 2d - alphanumeric into alphanumeric: MOVE rules, left justified, space padded to 6.
      *>   I1  rule 2d - an unsigned INTEGER numeric argument into an alphanumeric formal is a conforming MOVE
      *>       (14.9.25.3 Table 16), delivered as its digit characters.
      *>   L1  rule 2d over a LITERAL argument, and V1 rule 2a over a BY VALUE numeric literal.
      *>   Y1  rule 2c - "If the formal parameter is described with the ANY LENGTH clause, its length is
      *>       considered to match the length of the corresponding argument", which is also the FIRST arm of
      *>       14.2.3 GR9's second regime: "a data item of the same category, usage, and length as the
      *>       argument, if the formal parameter is described with the ANY LENGTH clause". With N1/S above
      *>       (the "otherwise" arm - the formal's own description) and pb165_dynamic_length_boundary (the
      *>       DYNAMIC LENGTH arm), all THREE arms of that regime are pinned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) COMP VALUE 42.
       01 A PIC X(3) VALUE "AB".
       01 I PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB165AN" AS NESTED USING BY CONTENT N
           CALL "PB165AA" AS NESTED USING BY CONTENT A
           CALL "PB165AA" AS NESTED USING BY CONTENT I
           CALL "PB165AA" AS NESTED USING BY CONTENT "ZQ"
           CALL "PB165AV" AS NESTED USING BY VALUE 55
           CALL "PB165AY" AS NESTED USING BY CONTENT A
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165AN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9(6).
       PROCEDURE DIVISION USING LN.
       M1.
           DISPLAY "N1=" LN
           GOBACK.
       END PROGRAM PB165AN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165AA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X(6).
       PROCEDURE DIVISION USING LA.
       M2.
           DISPLAY "S=[" LA "]"
           GOBACK.
       END PROGRAM PB165AA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165AV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE LV.
       M3.
           DISPLAY "V1=" LV
           GOBACK.
       END PROGRAM PB165AV.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165AY.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LY PIC X ANY LENGTH.
       PROCEDURE DIVISION USING LY.
       M4.
           DISPLAY "Y1=[" LY "] LEN=" FUNCTION LENGTH(LY)
           GOBACK.
       END PROGRAM PB165AY.
       END PROGRAM PB165A.
