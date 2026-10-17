      *>--- conformance-case
      *> id: pb181_call_tier_b_window
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB181P.
      *> kb/Work PB181 (MEASURED, not deduced) - THE CALL BOUNDARY OVER
      *> A TIER-B BYTE-FORM WINDOW. THE ELEMENTARY BOUNDARY CONVENTION
      *> CARRIES THE DISPLAY IMAGE (14.2.3 GR8 - the storage-area
      *> aliasing realized through the image round trip), AND THE
      *> WRITE-BACK INTO A WINDOWED MEMBER MUST DECODE AND RE-ENCODE
      *> THROUGH THE ONE BYTE-FORM RECIPE: 1234 CROSSES, THE CALLEE
      *> ADDS 1, AND 1235 COMES HOME AS ITS TWO RADIX-2 BYTES 04 D3
      *> (ORD 5/212 ON THE REDEFINED X VIEW, 15.70) WITH THE SIBLING
      *> INTACT. THE OLD RAW SPLICE PUT THE RETURNED CHARACTERS INTO
      *> THE 2-BYTE WINDOW - 1234 CAME HOME AS 2594 (ORD 50/51).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G-N PIC 9(4) COMP VALUE 1234.
          05 G-A PIC X(3) VALUE "abc".
       01 R REDEFINES G PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB181S" USING G-N
           DISPLAY G-N
           DISPLAY FUNCTION ORD(R(1:1)) " " FUNCTION ORD(R(2:1))
           DISPLAY G-A
           STOP RUN.
       END PROGRAM PB181P.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB181S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4) COMP.
       PROCEDURE DIVISION USING L-N.
       MAIN.
           ADD 1 TO L-N
           GOBACK.
       END PROGRAM PB181S.
