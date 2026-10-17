*>--- conformance-case
*> id: pb99-floating-literal-range
*> rules: [SR-8.3.3.3.3-3, DOC-A.1-82]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 8.3.3.3.3 r3: "The maximum permitted value and minimum permitted value of the exponent is
      *> implementor-defined".  TWO different rules live here and they answer to two different carriers - the split
      *> is owner decision D-B (kb/Work PB156 + PB195, 2026-08-30):
      *>
      *>   (a) A VALUE clause on a FLOATING-POINT item - 13.18.63.3 SR2 wants "permissible values within the
      *>       range indicated by the PICTURE clause or the USAGE clause", and a FLOAT-* subject has no
      *>       PICTURE, so the range is that ITEM's carrier: binary32 for FLOAT-SHORT / FLOAT-BINARY-32,
      *>       binary64 for FLOAT-LONG / FLOAT-BINARY-64.  F1-F4 below.
      *>
      *>   (b) A literal in a STATEMENT - the decimal128 range, about 1E-6176 to 9.99E+6144.  That lane has its
      *>       OWN fixture, pb99-floating-literal-range-statement.cob, and this one has no PROCEDURE-DIVISION
      *>       literal at all (kb/Work PB276).  ⛔ THE HARNESS ASSERTS ONLY THAT SOME DIAGNOSTIC CONTAINS
      *>       COBOLNET1661: while both lanes shared a file, F1-F4 alone satisfied that assertion and the
      *>       statement screen was never witnessed - deleting it outright would have left this negative green.
      *>
      *> Every entry here is COBOLNET1661 (kb/Work PB99 - before it Roslyn reported CS0594 on the generated C#
      *> double literal, no COBOL diagnostic).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB99NR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 USAGE FLOAT-LONG VALUE 1.0E+400.
       01 F2 USAGE FLOAT-SHORT VALUE 1.0E+39.
       01 F3 USAGE FLOAT-LONG VALUE 1.0E-400.
       01 F4 USAGE FLOAT-LONG.
          88 F4-HUGE VALUE 1.0E+400.
       PROCEDURE DIVISION.
           STOP RUN.
