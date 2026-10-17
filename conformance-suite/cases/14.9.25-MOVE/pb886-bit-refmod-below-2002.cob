*>--- conformance-case
*> id: pb886-bit-refmod-below-2002
*> rules: [GR-14.9.25.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> The NEGATIVE below the introducing edition of tests/conformance/2002/pb886_refmod_intermediate_positions.
      *> ISO 8.4.3.3.4 GR5 a)'s BIT-position arm ("If the usage of identifier-1 is bit, positions used in
      *> evaluation are bit positions") has an antecedent only a boolean data item can satisfy, and boolean data
      *> - the PICTURE symbol 1 and USAGE BIT (ISO 8.5.2 category boolean / 13.18.60) - is a COBOL-2002
      *> introduction. Below 2002 the whole shape is refused at the DATA DIVISION entry, so no multi-receiver
      *> MOVE of a boolean slice can reach the intermediate at all.
      *> Witness for kb/Work PB886.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB886NEG85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BE             PIC 1(8) USAGE BIT VALUE B"10110011".
       01 B1             PIC 1(4) USAGE BIT.
       01 B2             PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION.
           MOVE BE(1:4) TO B1 B2
           DISPLAY "B1=" B1 " B2=" B2
           STOP RUN.
