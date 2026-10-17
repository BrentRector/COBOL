      *>--- conformance-case
      *> id: pb155_composite_edges
      *> rules: [SR-14.9.2.3-1, SR-14.9.12.3-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB155CE.
      *> kb/Work PB155 - the composite of operands on true 14.7.7 r2
      *> premises. (1) A floating-point literal is EXCLUDED from the
      *> composite (r2a's list): 1.5E+3 beside 9(28)V9 leaves the
      *> composite at 29 <= 31 - counting the E-form's characters
      *> rejected this legal ADD with a spurious COBOLNET0805.
      *> (2) ADD's Format-2 composite excludes the data items after
      *> GIVING (14.9.2.3 SR1b): EW's 2-integer + 29-fraction digit
      *> positions do not count, so A(18 int) + B(4 int) compose at 18
      *> - counting EW would superimpose 18 + 29 = 47 > 31. (The DIVIDE
      *> twin, whose composite DOES count its GIVING receiver, is the
      *> negative pb155-divide-composite-edited.)
      *> (3) DIVIDE's composite excludes ONLY the item after REMAINDER
      *> (14.9.12.3 SR4): the composite is maxInt + maxFrac ACROSS the
      *> superimposed operands, so with Q's 1 fraction digit, counting
      *> R's 31 integer digits would span 31 + 1 = 32 > 31 - the
      *> statement is legal only because identifier-4 is excluded.
      *> 7 / 2 -> Q=3.5 exact; the subsidiary quotient (14.9.12.4 GR6c)
      *> carries Q's digits and decimal-point location, so R =
      *> 7 - 3.5*2 = 0 (GR7).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F PIC 9(28)V9 VALUE 0.
       01 A PIC 9(18) VALUE 12.
       01 B PIC 9(4) VALUE 7.
       01 EW PIC Z9.9(29).
       01 Q PIC 9V9.
       01 R PIC 9(31) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1.5E+3 TO F
           DISPLAY "F=" F
           ADD A TO B GIVING EW
           DISPLAY "E=" EW
           DIVIDE 7 BY 2 GIVING Q REMAINDER R
           DISPLAY "Q=" Q " R=" R
           STOP RUN.
