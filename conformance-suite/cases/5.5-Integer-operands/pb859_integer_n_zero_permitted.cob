      *>--- conformance-case
      *> id: pb859_integer_n_zero_permitted
      *> rules: [SR-5.5-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 §5.5 1) (kb/Work PB859): "When the term 'integer-n' (n = 1, 2, ...) is used in a
      *> general format and associated rules, it refers to a fixed-point integer literal that shall be unsigned
      *> and nonzero unless otherwise specified in the associated rules." This is the POSITIVE control: every
      *> position below is one whose associated rule DOES otherwise-specify, so each zero is legal and the new
      *> NONZERO screen (COBOLNET2386) must not touch it:
      *>   LINAGE ... LINES AT TOP 0 / AT BOTTOM 0 - §13.18.34.3 SR4 "Integer-3, integer-4 may be zero."
      *>   RECORD CONTAINS 0 TO 80              - §13.18.43.3 SR8 "Integer-4 shall be greater than or equal to zero."
      *>   RECORD IS VARYING IN SIZE FROM 0     - §13.18.43.3 SR7 "Integer-2 shall be greater than or equal to zero."
      *>   OCCURS 0 TO 3 TIMES DEPENDING ON     - §13.18.38.3 SR16 "Integer-1 shall be greater than or equal to zero"
      *>   WRITE ... AFTER ADVANCING 0 LINES    - §14.9.51.3 SR15 "Integer-1 shall be positive or zero."
      *> Expected values, from the general rules: OPEN OUTPUT sets LINAGE-COUNTER to one and a WRITE with an
      *> ADVANCING phrase increments it by the integer (§13.18.34.4 GR7), so ADVANCING 0 leaves it at 1 and a
      *> following ADVANCING 2 makes it 3. The ODO table holds N = 0 occurrences, then 2 after MOVE 2 TO N.
      *> Fails if: any of these zeros draws COBOLNET2386 (an exception row missing from the one table in
      *> Validation/IntegerOperandPass.cs), or ADVANCING 0 moves the counter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W56IZPOS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "w56izpos-p.tmp"
               ORGANIZATION IS SEQUENTIAL.
           SELECT VARF ASSIGN TO "w56izpos-v.tmp"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF
           RECORD CONTAINS 0 TO 80 CHARACTERS
           LINAGE IS 10 LINES WITH FOOTING AT 8
               LINES AT TOP 0 LINES AT BOTTOM 0.
       01  PR                 PIC X(80).
       FD  VARF
           RECORD IS VARYING IN SIZE FROM 0 TO 20 CHARACTERS.
       01  VR                 PIC X(20).
       WORKING-STORAGE SECTION.
       01  N                  PIC 9 VALUE 0.
       01  LC                 PIC 99.
       01  T.
           05 E               PIC X OCCURS 0 TO 3 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY "N=" N
           MOVE 2 TO N
           MOVE "A" TO E (1)
           MOVE "B" TO E (2)
           DISPLAY "T=" T
           OPEN OUTPUT PRTF
           MOVE LINAGE-COUNTER TO LC
           DISPLAY "LC-OPEN=" LC
           MOVE "LINE" TO PR
           WRITE PR AFTER ADVANCING 0 LINES
           MOVE LINAGE-COUNTER TO LC
           DISPLAY "LC-ADV0=" LC
           WRITE PR AFTER ADVANCING 2 LINES
           MOVE LINAGE-COUNTER TO LC
           DISPLAY "LC-ADV2=" LC
           CLOSE PRTF
           OPEN OUTPUT VARF
           MOVE "V" TO VR
           WRITE VR
           CLOSE VARF
           DISPLAY "DONE"
           STOP RUN.
