      *>--- conformance-case
      *> id: pb1030_reference_answers
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1030 - the LEGAL neighbours of the reference shapes the resolver now refuses by
      *> name. ISO 8.4.2.3.2: a subscript is ALL, an arithmetic expression or an index-name
      *> optionally followed by + or - and an integer; the relative form E (I + 1) selects
      *> occurrence 3 when I is 2. ISO 8.4.3.3.3 SR4: a leftmost-position and a length are
      *> arithmetic expressions, so TB (I : 2) is positions 2-3 of TB. ISO 13.18.45.4 GR2: RENAMES
      *> THRU defines an alphanumeric group item over every elementary item from RA to RC, so RN is
      *> the character image AB, 123, C. A subscripted condition-name tests its conditional
      *> variable's occurrence (8.4.2.3 Format 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1030P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 E PIC X OCCURS 5.
       01 I PIC 9 VALUE 2.
       01 R.
          05 RA PIC X(2) VALUE "AB".
          05 RB PIC 9(3) VALUE 123.
          05 RC PIC X VALUE "C".
       66 RN RENAMES RA THRU RC.
       01 CT.
          05 CV PIC 9 OCCURS 3.
             88 C-THREE VALUE 3.
       PROCEDURE DIVISION.
           MOVE "ABCDE" TO TB.
           MOVE 1 TO CV (1).
           MOVE 3 TO CV (2).
           MOVE 5 TO CV (3).
           DISPLAY "E=" E (I + 1).
           DISPLAY "TB=" TB (I : 2).
           DISPLAY "RN=" RN.
           IF C-THREE (I)
               DISPLAY "C2=3"
           ELSE
               DISPLAY "C2=NOT3"
           END-IF.
           STOP RUN.
