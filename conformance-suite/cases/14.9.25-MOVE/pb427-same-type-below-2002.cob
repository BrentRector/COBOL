*>--- conformance-case
*> id: pb427-same-type-below-2002
*> rules: [SR-14.9.25.3-2]
*> source-format: free
*> portable: unverified
*>---
*> The edition floor under pb427_same_type_by_position: 8.5.3.1's same-type rule has nothing to speak
*> about below COBOL-2002, because the TYPEDEF clause that declares a type and the TYPE clause that
*> references one are both 2002 introductions (ISO 1989:2023 13.18.58 / 13.18.57; Annex F.2).  The
*> minimal shape of the positive -- one STRONG type declaration and two items described with it -- is
*> therefore COBOLNET0900 at 85 and conforming from 2002 up.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB427BELOW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEST-T IS TYPEDEF STRONG.
          05 OUTERG.
             10 INNERG.
                15 LEAFX PIC X(4).
       01 N1 TYPE NEST-T.
       01 N2 TYPE NEST-T.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "WXYZ" TO LEAFX OF N1
           MOVE OUTERG OF N1 TO INNERG OF N2
           DISPLAY "A=" LEAFX OF N2
           STOP RUN.
