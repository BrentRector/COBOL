*>--- conformance-case
*> id: pb584-mixed-bit-group-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The NEGATIVE below the introducing edition of tests/conformance/2002/pb584_mixed_bit_group_image.
      *> The composition PB584 fixes is 8.5.1.6.3's ("an elementary bit data item immediately following an
      *> elementary bit data item or bit group item of the same level" shares a byte), and its antecedent is a
      *> data item of USAGE BIT - boolean data, which with the PICTURE symbol 1 is a COBOL-2002 introduction.
      *> Below 2002 the mixed group is refused at the DATA DIVISION entry, so no run can be composed at all and
      *> the alias has no bytes to read.
      *> Witness for kb/Work PB584.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB584NEG85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CTL.
           05  H1  PIC 1(4) USAGE BIT VALUE B"0100".
           05  H2  PIC 1(4) USAGE BIT VALUE B"0001".
           05  H3  PIC X(1) VALUE "B".
       01  CV  REDEFINES CTL PIC X(2).
       PROCEDURE DIVISION.
           DISPLAY "H=[" H1 "][" H2 "][" H3 "]"
           DISPLAY "CV2=[" CV (2:1) "]"
           STOP RUN.
