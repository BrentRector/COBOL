      *>--- conformance-case
      *> id: pb531_picture_repetition_factor
      *> rules: [SR-13.18.40.3-4, SR-13.18.40.3-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB531 / PB532 - THE PICTURE CHARACTER-STRING'S OWN SHAPE, at the earliest edition COBOL.NET
      *> compiles.  Both rules are ALL FORMATS and read identically at 85, 2002, 2014 and 2023, so this is the
      *> single positive witness; the negatives below it pin each refusal.
      *>
      *> 13.18.40.3 SR6 - "An unsigned nonzero integer that is enclosed in parentheses indicates the number of
      *>   consecutive occurrences of the symbol that immediately precedes the left parenthesis. The integer may
      *>   be specified by a constant-name, in which case the length of the integer, not the length of the
      *>   constant-name, is counted toward the maximum number of characters in character-string-1."
      *> 13.18.40.3 SR4 - "The maximum number of characters allowed in character-string-1 is 63."
      *>
      *> SR6's second sentence is what fixes SR4's reading: the count is over character-string-1 AS WRITTEN,
      *> because the sentence distinguishes the length of the substituted INTEGER from the length of the
      *> constant-name.  The expanded reading would outlaw PIC X(30000), which is written in four characters.
      *>
      *> R1  X(20) - 20 consecutive 'X', so 13.18.40.4 GR4 (the size is "the number of symbols in
      *>     character-string-1 that represent ... character positions") gives 20.
      *> R2  9(5)V9(2) - five integer digit positions and two fractional; GR14 excludes 'V' from the size, so
      *>     the size is 7 and MOVE 12345.67 stores every digit.
      *> R3  the SR4 BOUNDARY: 63 written characters exactly (21 groups of 'XXX'), which is legal.
      *> R4  X(30000) - four written characters, 30000 character positions: the two rules in one item.
      *> R5  9(1) - the smallest factor SR6 admits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB531RF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1 PIC X(20).
       01 R2 PIC 9(5)V9(2).
       01 R3 PIC XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.
       01 R4 PIC X(30000).
       01 R5 PIC 9(1).
       PROCEDURE DIVISION.
           MOVE "ABC" TO R1.
           DISPLAY "R1=[" R1 "] SIZE=" FUNCTION LENGTH(R1).
           MOVE 12345.67 TO R2.
           DISPLAY "R2=" R2 " SIZE=" FUNCTION LENGTH(R2).
           DISPLAY "R3-SIZE=" FUNCTION LENGTH(R3).
           DISPLAY "R4-SIZE=" FUNCTION LENGTH(R4).
           MOVE 7 TO R5.
           DISPLAY "R5=" R5 " SIZE=" FUNCTION LENGTH(R5).
           STOP RUN.
