*>--- conformance-case
*> id: pb3166-redefines-bit-item-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The NEGATIVE below the introducing edition of
      *> tests/conformance/2002/pb3166_redefines_bit_item_in_shared_byte.
      *> The storage association PB3166 fixes is 13.18.44.4 GR1's (the redefining entry starts at the first
      *> bit of data-name-2), and its antecedent is a data item of USAGE BIT - boolean data, which with the
      *> PICTURE symbol 1 is a COBOL-2002 introduction.  Below 2002 the bit item is refused at the DATA
      *> DIVISION entry, so there is no bit position for the redefinition to start at.
      *> Witness for kb/Work PB3166.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3166NEG85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GB.
           05  RBA PIC 1(2) USAGE BIT.
           05  RB  PIC 1(6) USAGE BIT.
           05  RB2 REDEFINES RB PIC 1(2) USAGE BIT.
       PROCEDURE DIVISION.
           MOVE B"101010" TO RB
           DISPLAY RB2
           STOP RUN.
