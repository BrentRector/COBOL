*>--- conformance-case
*> id: pb589_call_content_bool_width
*> rules: [GR-14.9.8.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB589 - the CALL arm of the one boolean width. A BY
      *> CONTENT boolean-expression-1 (14.9.4.2 Format 2) crosses as a
      *> value whose length is the largest boolean ITEM referenced (8.8.2
      *> rule 10, the width 14.9.8.4 GR3 states for COMPUTE). The ANY
      *> LENGTH formal takes the argument's length (13.18.2.4 GR1b), so
      *> it SHOWS the width. B8(1:N) with N = 3 is a 3-position item
      *> (8.4.3.3.4 GR5); B"1111111" is a literal and adds none:
      *> C1 = [111] LEN 3 (before the fix: B8's full 8 -> 11111110).
      *> C2 - the literal-length control, B8(1:3): the same [111] LEN 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB589CCW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8 PIC 1(8) USAGE BIT VALUE B"00000000".
       01 N  PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB589CS" AS NESTED
                USING BY CONTENT B8(1:N) B-OR B"1111111"
           CALL "PB589CS" AS NESTED
                USING BY CONTENT B8(1:3) B-OR B"1111111"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB589CS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 AL PIC 1 ANY LENGTH.
       PROCEDURE DIVISION USING BY REFERENCE AL.
       S1.
           DISPLAY "C=[" AL "] LEN=" FUNCTION LENGTH(AL)
           GOBACK.
       END PROGRAM PB589CS.
       END PROGRAM PB589CCW.
