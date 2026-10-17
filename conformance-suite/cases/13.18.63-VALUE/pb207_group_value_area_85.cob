      *>--- conformance-case
      *> id: pb207_group_value_area_85
      *> rules: [GR-13.18.63.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB207 - the COBOL-85 EDITION LEG of the 13.18.63.4 GR5 group-VALUE area rule.
      *> GR5 - "If a VALUE clause is specified in a data description entry of a group item, the group area is
      *> initialized without consideration for the individual elementary or group items contained within this
      *> group" - is edition-invariant, but its two 2002+ pins cannot witness it at 85: pb184_group_value_area
      *> writes TYPEDEF / TYPE and pb207_bit_group_value writes GROUP-USAGE, both COBOL-2002 introductions.
      *> This file is the 85 witness, in constructs COBOL-85 already had.
      *> EXPECTED VALUES, COMPUTED FROM THE SPEC BEFORE THE CONFIRMING RUN.  GA is an alphanumeric group item
      *> (13.18.29.4 GR3), so 13.18.63.4 GR7 sends the literal through 14.6.8.5 - "aligned at the leftmost character
      *> position in the data item with space fill or truncation to the right":
      *>   GA  area "ABCD" (exactly 4)     -> P1=AB  P2=CD
      *>   GB  figurative ZEROS            -> 8.3.3.6.4 GR2 repeats the one character to the area width and
      *>                                      GR4 gives it as the character '0' -> "0000", Q1=00 Q2=00
      *>   GC  area "AB" into 4 positions  -> "AB  " (14.6.8.5 SPACE fill), R1=AB R2=two spaces
      *> Every subordinate is usage DISPLAY, which is what 13.18.63.3 SR14 requires of an alphanumeric group
      *> item carrying a VALUE; the complement is pinned by negative/pb184-group-value-non-display.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB207G85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GA VALUE "ABCD".
          05 P1 PIC X(2).
          05 P2 PIC X(2).
       01 GB VALUE ZEROS.
          05 Q1 PIC X(2).
          05 Q2 PIC X(2).
       01 GC VALUE "AB".
          05 R1 PIC X(2).
          05 R2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A1=[" GA "] [" P1 "][" P2 "]".
           DISPLAY "A2=[" GB "] [" Q1 "][" Q2 "]".
           DISPLAY "A3=[" GC "] [" R1 "][" R2 "]".
           STOP RUN.
