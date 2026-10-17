      *>--- conformance-case
      *> id: pb155-divide-composite-edited
      *> rules: [SR-14.9.12.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.12.3 SR4: the DIVIDE composite of operands is ALL
      *> operands excluding only the REMAINDER item - the numeric-edited
      *> GIVING quotient (SR2-admitted) IS a composite member. E is a
      *> LEGAL 31-digit-position picture (2 integer + 29 fraction);
      *> superimposed with A's 18 integer digits the composite spans
      *> 18 + 29 = 47 > 31 (14.7.7 r2a). The old Category==Numeric
      *> filter dropped E and this compiled clean (kb/Work PB155).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB155N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(18) VALUE 100.
       01 B PIC 9(4) VALUE 7.
       01 E PIC Z9.9(29).
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE A BY B GIVING E
           STOP RUN.
